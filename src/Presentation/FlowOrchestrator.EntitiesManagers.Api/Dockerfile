# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Presentation/FlowOrchestrator.EntitiesManagers.Api/FlowOrchestrator.EntitiesManagers.Api.csproj", "Presentation/FlowOrchestrator.EntitiesManagers.Api/"]
COPY ["src/Core/FlowOrchestrator.EntitiesManagers.Core/FlowOrchestrator.EntitiesManagers.Core.csproj", "Core/FlowOrchestrator.EntitiesManagers.Core/"]
COPY ["src/Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure.csproj", "Infrastructure/FlowOrchestrator.EntitiesManagers.Infrastructure/"]
COPY ["src/Application/FlowOrchestrator.EntitiesManagers.Application/FlowOrchestrator.EntitiesManagers.Application.csproj", "Application/FlowOrchestrator.EntitiesManagers.Application/"]

RUN dotnet restore "Presentation/FlowOrchestrator.EntitiesManagers.Api/FlowOrchestrator.EntitiesManagers.Api.csproj"

# Copy source code
COPY src/ .

# Build the application
RUN dotnet build "Presentation/FlowOrchestrator.EntitiesManagers.Api/FlowOrchestrator.EntitiesManagers.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Presentation/FlowOrchestrator.EntitiesManagers.Api/FlowOrchestrator.EntitiesManagers.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose ports
EXPOSE 5000 5001

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001

ENTRYPOINT ["dotnet", "EntitiesManager.Api.dll"]
