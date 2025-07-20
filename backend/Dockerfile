# Use the official .NET 8.0 SDK image for building and running (development approach)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Set the working directory
WORKDIR /app

# Copy solution and project files
COPY AdhdProductivitySystem.sln ./
COPY src/AdhdProductivitySystem.Api/AdhdProductivitySystem.Api.csproj ./src/AdhdProductivitySystem.Api/
COPY src/AdhdProductivitySystem.Application/AdhdProductivitySystem.Application.csproj ./src/AdhdProductivitySystem.Application/
COPY src/AdhdProductivitySystem.Domain/AdhdProductivitySystem.Domain.csproj ./src/AdhdProductivitySystem.Domain/
COPY src/AdhdProductivitySystem.Infrastructure/AdhdProductivitySystem.Infrastructure.csproj ./src/AdhdProductivitySystem.Infrastructure/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY src/ ./src/

# Create logs directory
RUN mkdir -p logs

# Expose the port the app runs on
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:5000

# Add a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser -m appuser
RUN chown -R appuser:appuser /app
RUN chown -R appuser:appuser /home/appuser
USER appuser

# Set the entry point to run in development mode
ENTRYPOINT ["dotnet", "run", "--project", "src/AdhdProductivitySystem.Api/AdhdProductivitySystem.Api.csproj"]