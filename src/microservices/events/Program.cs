using System.Text.Json;
using System.Text.Json.Serialization;
using EventsService.Infrastructure.Abstractions;
using EventsService.Infrastructure.Kafka.Configuration;
using EventsService.Infrastructure.Kafka.Consumers.MovieEvents;
using EventsService.Infrastructure.Kafka.Consumers.PaymentEvents;
using EventsService.Infrastructure.Kafka.Consumers.UserEvents;
using EventsService.Infrastructure.Kafka.Factory;
using EventsService.Infrastructure.Kafka.Producers.MovieEvents;
using EventsService.Infrastructure.Kafka.Producers.PaymentEvents;
using EventsService.Infrastructure.Kafka.Producers.UserEvents;
using EventsService.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Json
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// Kafka
builder.Services.Configure<KafkaConfiguration>(config =>
{
    config.Servers = builder.Configuration
        .GetValue<string>("KAFKA_BROKERS") ?? throw new Exception("KAFKA_BROKERS is missing");
});

builder.Services.Configure<TopicConfiguration>(config =>
{
    config.MovieEventsTopicName = builder.Configuration
        .GetValue<string>("MOVIE_EVENTS_TOPIC") ?? throw new Exception("MOVIE_EVENTS_TOPIC is missing");
    config.PaymentEventsTopicName = builder.Configuration
        .GetValue<string>("PAYMENT_EVENTS_TOPIC") ?? throw new Exception("PAYMENT_EVENTS_TOPIC is missing");
    config.UserEventsTopicName = builder.Configuration
        .GetValue<string>("USER_EVENTS_TOPIC") ?? throw new Exception("USER_EVENTS_TOPIC is missing");
});

builder.Services.AddSingleton<KafkaFactory>();
builder.Services.AddSingleton<IKafkaConsumerFactory>(sp => sp.GetRequiredService<KafkaFactory>());
builder.Services.AddSingleton<IKafkaProducerFactory>(sp => sp.GetRequiredService<KafkaFactory>());

builder.Services.AddHostedService<MovieEventsConsumer>();
builder.Services.AddHostedService<PaymentEventsConsumer>();
builder.Services.AddHostedService<UserEventsConsumer>();

builder.Services.AddSingleton<IMovieEventsPublisher, MovieEventMessageProducer>();
builder.Services.AddSingleton<IPaymentEventsPublisher, PaymentEventMessageProducer>();
builder.Services.AddSingleton<IUserEventsPublisher, UserEventMessageProducer>();

// Host
var portEnv = Environment.GetEnvironmentVariable("PORT");
var port = string.IsNullOrEmpty(portEnv) ? 5000 : int.Parse(portEnv);
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/events/movie", async (MovieEvent movieEvent,
        [FromServices] IMovieEventsPublisher publisher) =>
    {
        await publisher.PublishAsync(movieEvent, CancellationToken.None);
        return Results.Created("/api/events/movie", new { Status = "success" });
    })
    .WithName("HandleMovieEvent");

app.MapPost("/api/events/payment", async (PaymentEvent paymentEvent,
        [FromServices] IPaymentEventsPublisher publisher) =>
    {
        await publisher.PublishAsync(paymentEvent, CancellationToken.None);
        return Results.Created("/api/events/payment", new { Status = "success" });
    })
    .WithName("HandlePaymentEvent");

app.MapPost("/api/events/user", async (UserEvent userEvent,
        [FromServices] IUserEventsPublisher publisher) =>
    {
        await publisher.PublishAsync(userEvent, CancellationToken.None);
        return Results.Created("/api/events/user", new { Status = "success" });
    })
    .WithName("HandleUserEvent");

app.MapGet("/api/events/health", () => Results.Ok(new
    {
        Status = true
    }))
    .WithName("Healthcheck")
    .WithOpenApi();

app.Run();