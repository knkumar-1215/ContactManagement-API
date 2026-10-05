# Contact Management API

A production-grade REST API built with 
.NET 8 demonstrating enterprise patterns.

## Features
-  JWT Authentication + Role-based Authorization
-  API Versioning (Asp.Versioning 8.x)
-  Global Exception Handling Middleware
-  Structured Logging (ILogger)
-  Input Validation (Data Annotations)
-  Response Caching (IMemoryCache)
-  Rate Limiting (3 policies)
-  Health Checks + HealthChecksUI
-  Swagger with Bearer Auth + XML Docs
-  Consistent ApiResponse<T> envelope
-  Pagination on list endpoints
-  10 Unit Tests + 8 Integration Tests

## Architecture
Solution: ContactManagmentAPIApp
├── ContactManagmentAPI        (Web API)
├── ContactManager.DataAccess  (Class Library - Dapper)
└── ContactManagmentAPITests   (xUnit Tests)

## Tech Stack
- .NET 8
- ASP.NET Core Web API
- Dapper + SQL Server
- xUnit + Moq
- Swashbuckle 6.6.2
- Asp.Versioning 8.1.1

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server
- Visual Studio 2022

### Setup
1. Clone the repository
2. Create the Contacts table:
   See /sql/create-contacts-table.sql
3. Add User Secrets:
   dotnet user-secrets set 
     "ConnectionStrings:Default" "your-connection-string"
   dotnet user-secrets set 
     "Authentication:SecretKey" "your-256-bit-secret"
   dotnet user-secrets set 
     "Authentication:Issuer" "https://localhost:7xxx"
   dotnet user-secrets set 
     "Authentication:Audience" "https://localhost:7xxx"
4. Run: dotnet run

## API Endpoints

### Authentication
POST /api/v1/Authentication/registration
POST /api/v1/Authentication/token

### Contacts (requires JWT)
GET    /api/v1/Contacts
GET    /api/v1/Contacts/{id}
GET    /api/v1/Contacts/search?lastName={lastName}
POST   /api/v1/Contacts          (Admin only)
PUT    /api/v1/Contacts/{id}     (Admin only)
DELETE /api/v1/Contacts/{id}     (Admin only)

### Health
GET /health/live
GET /health/ready
GET /health

## License
MIT
