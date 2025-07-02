# ADHD Productivity System - Backend API

A comprehensive productivity system backend designed specifically for individuals with ADHD, built with ASP.NET Core 8.0 following Clean Architecture principles.

## Features

### Core Functionality
- **Task Management**: Create, update, and track tasks with ADHD-friendly features
- **Brain Dump System**: Quick capture of thoughts and ideas
- **Timer Sessions**: Pomodoro and custom timer support with real-time updates
- **Time Blocking**: Schedule and manage time blocks for better focus
- **Progress Tracking**: Daily progress monitoring and analytics
- **User Management**: Authentication and personalized settings

### Technical Features
- **Clean Architecture**: Separation of concerns with Domain, Application, Infrastructure, and API layers
- **CQRS with MediatR**: Command Query Responsibility Segregation for scalable operations
- **JWT Authentication**: Secure token-based authentication
- **SignalR Hubs**: Real-time notifications and timer updates
- **Entity Framework Core**: Database operations with SQL Server
- **AutoMapper**: Object-to-object mapping
- **FluentValidation**: Request validation
- **Swagger/OpenAPI**: API documentation
- **Docker Support**: Containerized deployment

## Architecture

```
src/
├── AdhdProductivitySystem.Domain/          # Core business entities and enums
├── AdhdProductivitySystem.Application/     # Business logic and DTOs
├── AdhdProductivitySystem.Infrastructure/  # Data access and external services
└── AdhdProductivitySystem.Api/            # Web API controllers and configuration
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB for development)
- Docker (optional, for containerized deployment)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd backend
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update database connection string**
   Edit `src/AdhdProductivitySystem.Api/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AdhdProductivitySystemDb_Dev;Trusted_Connection=true;"
     }
   }
   ```

4. **Run the application**
   ```bash
   cd src/AdhdProductivitySystem.Api
   dotnet run
   ```

5. **Access the API**
   - API: https://localhost:7000 or http://localhost:5000
   - Swagger UI: https://localhost:7000 (development only)

### Docker Development

1. **Start services with Docker Compose**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

2. **Access the API**
   - API: http://localhost:5001
   - Database: localhost:1434

### Production Deployment

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up -d
   ```

2. **Access the application**
   - API: http://localhost:5000
   - Database: localhost:1433

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh token
- `GET /api/auth/me` - Get current user info
- `POST /api/auth/logout` - Logout user

### Tasks
- `GET /api/tasks` - Get user tasks
- `GET /api/tasks/{id}` - Get specific task
- `POST /api/tasks` - Create new task
- `PUT /api/tasks/{id}` - Update task
- `DELETE /api/tasks/{id}` - Delete task
- `PATCH /api/tasks/{id}/status` - Update task status

### Capture (Brain Dump)
- `GET /api/capture` - Get capture items
- `GET /api/capture/{id}` - Get specific capture item
- `POST /api/capture` - Create new capture item
- `PUT /api/capture/{id}` - Update capture item
- `DELETE /api/capture/{id}` - Delete capture item
- `POST /api/capture/{id}/process` - Process capture item
- `POST /api/capture/{id}/convert-to-task` - Convert to task

### Timer
- `GET /api/timer/sessions` - Get timer sessions
- `GET /api/timer/sessions/{id}` - Get specific session
- `POST /api/timer/sessions/start` - Start timer session
- `POST /api/timer/sessions/{id}/pause` - Pause session
- `POST /api/timer/sessions/{id}/resume` - Resume session
- `POST /api/timer/sessions/{id}/stop` - Stop session
- `POST /api/timer/sessions/{id}/complete` - Complete session
- `GET /api/timer/statistics` - Get timer statistics

### SignalR Hubs
- `/hubs/timer` - Real-time timer updates
- `/hubs/notification` - Real-time notifications

## Configuration

### Environment Variables

#### Required
- `ConnectionStrings__DefaultConnection` - Database connection string
- `JWT__SecretKey` - JWT signing key (minimum 32 characters)
- `JWT__Issuer` - JWT issuer
- `JWT__Audience` - JWT audience

#### Optional
- `JWT__TokenExpirationMinutes` - Token expiration (default: 60)
- `AllowedOrigins` - CORS allowed origins

### Database

The application uses Entity Framework Core with SQL Server. The database is automatically created on first run in development mode.

For production, ensure the database connection string is properly configured and the database server is accessible.

### Logging

The application uses Serilog for structured logging:
- Console output for development
- File logging to `logs/` directory
- Configurable log levels in `appsettings.json`

## Development Guidelines

### Adding New Features

1. **Domain Layer**: Add entities and enums in `Domain/`
2. **Application Layer**: 
   - Create DTOs in `Application/Common/DTOs/`
   - Add commands/queries in `Application/Features/`
   - Create handlers implementing business logic
3. **Infrastructure Layer**: Add data access or external service implementations
4. **API Layer**: Create controllers exposing the endpoints

### Testing

The application includes comprehensive error handling and validation:
- FluentValidation for request validation
- Global exception handling
- Structured logging for debugging
- Health checks for monitoring

### Security

- JWT authentication with configurable expiration
- CORS configuration for cross-origin requests
- Security headers in production
- Input validation and sanitization
- Rate limiting (configured in nginx)

## Monitoring and Health Checks

- Health check endpoint: `/health`
- Structured logging with Serilog
- Request/response logging
- Error tracking and monitoring

## Contributing

1. Follow Clean Architecture principles
2. Implement proper error handling
3. Add appropriate logging
4. Include XML documentation for public APIs
5. Test thoroughly before submitting

## License

This project is licensed under the MIT License - see the LICENSE file for details.