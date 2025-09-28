FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /App

COPY . ./
RUN dotnet restore && dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "Proxy.dll"]