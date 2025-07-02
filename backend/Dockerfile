# Use the official .NET 8.0 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

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

# Build the application
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish src/AdhdProductivitySystem.Api/AdhdProductivitySystem.Api.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 8.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p logs

# Expose the port the app runs on
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Add a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Set the entry point
ENTRYPOINT ["dotnet", "AdhdProductivitySystem.Api.dll"]