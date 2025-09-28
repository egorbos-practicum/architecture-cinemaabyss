using System.Net;
using System.Net.Http.Headers;
using Proxy.Abstractions;
using Proxy.Enums;
using Proxy.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var portEnv = Environment.GetEnvironmentVariable("PORT");
var port = string.IsNullOrEmpty(portEnv) ? 5000 : int.Parse(portEnv);
builder.WebHost.UseUrls($"http://*:{port}");

var monolithUrl = builder.Configuration.GetValue<string>("MONOLITH_URL")
    ?? throw new Exception("MONOLITH_URL is missing");
var moviesServiceUrl = builder.Configuration.GetValue<string>("MOVIES_SERVICE_URL")
                       ?? throw new Exception("MOVIES_SERVICE_URL is missing");
var eventsServiceUrl = builder.Configuration.GetValue<string>("EVENTS_SERVICE_URL")
                       ?? throw new Exception("EVENTS_SERVICE_URL is missing");

builder.Services.AddHttpClient("Monolith", client =>
{
    client.BaseAddress = new Uri(monolithUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("Movies", client =>
{
    client.BaseAddress = new Uri(moviesServiceUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("Events", client =>
{
    client.BaseAddress = new Uri(eventsServiceUrl);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddSingleton<IRequestDistributor, CounterBasedRequestDistributor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var gradualMigration = bool.Parse(app.Configuration["GRADUAL_MIGRATION"] ?? "true");
var migrationPercent = int.Parse(app.Configuration["MOVIES_MIGRATION_PERCENT"] ?? "50");

// Healthcheck
app.MapGet("/health", (IRequestDistributor distributor) => Results.Ok(new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Configuration = new {
        GradualMigration = gradualMigration,
        MigrationPercent = migrationPercent,
        MonolithUrl = monolithUrl,
        MoviesServiceUrl = moviesServiceUrl,
        EventsServiceUrl = eventsServiceUrl
    },
    DistributionStats = distributor.GetDistributionStats()
}))
    .WithName("Healthcheck")
    .WithOpenApi();

// Обработчик для маршрутов /api/events/**
app.Map("/api/events/{**catchall}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    await ProxyRequest(context, httpClientFactory, "Events", eventsServiceUrl);
});

// Обработчик для маршрутов /api/users/**
app.Map("/api/users/{**catchall}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    await ProxyRequest(context, httpClientFactory, "Monolith", monolithUrl);
});

// Обработчик для маршрутов /api/payments/**
app.Map("/api/payments/{**catchall}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    await ProxyRequest(context, httpClientFactory, "Monolith", monolithUrl);
});

// Обработчик для маршрутов /api/subscriptions/**
app.Map("/api/subscriptions/{**catchall}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
{
    await ProxyRequest(context, httpClientFactory, "Monolith", monolithUrl);
});

// Обработчик для маршрутов /api/movies/**
app.Map("/api/movies/{**catchall}", async (HttpContext context, IHttpClientFactory httpClientFactory, IRequestDistributor distributor) =>
{
    try
    {
        // Определяем целевой сервис
        var targetService = distributor.DetermineTargetService(gradualMigration, migrationPercent);

        // Создаем HTTP запрос к целевому сервису
        var targetUrl = targetService switch
        {
            TargetService.Monolith => monolithUrl,
            TargetService.MoviesService => moviesServiceUrl
        };
        var clientName = targetService switch
        {
            TargetService.Monolith => "Monolith",
            TargetService.MoviesService => "Movies"
        };
        await ProxyRequest(context, httpClientFactory, clientName, targetUrl);
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка обработки запроса");
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

app.Run();

async Task ProxyRequest(HttpContext context, IHttpClientFactory httpClientFactory, string clientName, string baseUrl)
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var client = httpClientFactory.CreateClient(clientName);
        
        // Создаем новый запрос на основе входящего
        var request = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = new Uri($"{baseUrl}{context.Request.Path}{context.Request.QueryString}")
        };
        
        // Копируем заголовки
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;
                
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
        
        // Копируем тело запроса если есть
        if (context.Request.ContentLength > 0 || 
            (context.Request.ContentLength == null && 
             (context.Request.Method == "POST" || context.Request.Method == "PUT" || context.Request.Method == "PATCH")))
        {
            var contentStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(contentStream);
            contentStream.Position = 0;
            request.Content = new StreamContent(contentStream);
            
            if (context.Request.Headers.ContainsKey("Content-Type"))
            {
                request.Content.Headers.ContentType = 
                    MediaTypeHeaderValue.Parse(context.Request.Headers["Content-Type"]);
            }
        }

        logger.LogInformation("Перенаправляем {RequestMethod} {RequestPath} к сервису {ClientName}", context.Request.Method, context.Request.Path, clientName);
        
        var response = await client.SendAsync(request, context.RequestAborted);
        
        context.Response.StatusCode = (int)response.StatusCode;
        
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");
        await response.Content.CopyToAsync(context.Response.Body);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Ошибка перенаправления запроса к сервису {ClientName}", clientName);
        context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
        await context.Response.WriteAsync($"Bad Gateway: Unable to connect to {clientName} service");
    }
    catch (TaskCanceledException ex)
    {
        logger.LogWarning(ex, "Запрос к сервису {ClientName} отменен", clientName);
        context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
        await context.Response.WriteAsync("Gateway Timeout");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Непредвиденная ошибка при перенапралении запроса к сервису {ClientName}", clientName);
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsync("Internal Server Error");
    }
}



