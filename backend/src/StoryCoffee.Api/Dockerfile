FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/src/StoryCoffee.Domain/StoryCoffee.Domain.csproj backend/src/StoryCoffee.Domain/
COPY backend/src/StoryCoffee.Contracts/StoryCoffee.Contracts.csproj backend/src/StoryCoffee.Contracts/
COPY backend/src/StoryCoffee.Application/StoryCoffee.Application.csproj backend/src/StoryCoffee.Application/
COPY backend/src/StoryCoffee.Infrastructure/StoryCoffee.Infrastructure.csproj backend/src/StoryCoffee.Infrastructure/
COPY backend/src/StoryCoffee.Api/StoryCoffee.Api.csproj backend/src/StoryCoffee.Api/
RUN dotnet restore backend/src/StoryCoffee.Api/StoryCoffee.Api.csproj

COPY backend backend
RUN dotnet publish backend/src/StoryCoffee.Api/StoryCoffee.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "StoryCoffee.Api.dll"]
