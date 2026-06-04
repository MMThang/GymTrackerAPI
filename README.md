# Gym Tracker API

Backend API for Gym Tracker, a fitness tracking application that allows users to create, manage, and track workout sessions over time.

Built with ASP.NET Core Web API, PostgreSQL, Entity Framework Core, JWT Authentication, and Refresh Token Rotation.

---

## Features

### Authentication & Security

* User Registration
* User Login
* JWT Access Token Authentication
* Refresh Token Rotation
* Refresh Token Reuse Detection
* Cookie-based Authentication
* Protected API Endpoints

### Workout Management

* Create Workout Sessions
* Update Existing Workout Sessions
* Retrieve Workout Session History
* Calendar-based Workout Tracking

### Data Management

* PostgreSQL Database
* Entity Framework Core
* Dapper
* Database Migrations

---

## Technology Stack

### Backend

* C#
* ASP.NET Core Web API
* Entity Framework Core
* Dapper

### Database

* PostgreSQL

### Authentication

* JWT Authentication
* Refresh Token Rotation
* Refresh Token Reuse Detection

### DevOps

* Docker
* GitHub Actions

### Documentation

* Swagger / OpenAPI

---

## Architecture

```text
Controllers
    │
    ▼
Interfaces
    │
    ▼
Repositories
    │
    ▼
Entity Framework Core / Dapper
    │
    ▼
PostgreSQL
```

### Main Project Structure

```text
GymTracker.API/
├── Controllers/
├── Data
├── DTOs/
├── Entities/
├── Interfaces/
├── Repositories/
├── Migrations/
├── Program.cs
└── appsettings.json
```

---

## Database Schema

### User

Stores user account information.

### WorkoutSession

Stores workout sessions created by users.

### Exercise

Stores exercises associated with workout sessions.

### Set

Stores set information such as repetitions and weight.

### RefreshToken

Stores refresh tokens used for authentication and token rotation.

---

## Authentication Flow

1. User logs in with credentials.
2. API generates an Access Token and Refresh Token.
3. Tokens are stored in cookies.
4. Access Token is used to authorize API requests.
5. When the Access Token expires, the client calls the Refresh Token endpoint.
6. API validates the Refresh Token.
7. Previous Refresh Token is revoked.
8. A new Access Token and Refresh Token are generated.
9. Refresh Token reuse detection is performed to identify potentially compromised tokens.

---

## API Endpoints

### Authentication

```http
POST /User/register
POST /User/login
POST /Token/refresh-token
```

### Workout Sessions

```http
POST /WorkoutSession/workout-session

PUT /WorkoutSession/workout-session

GET /WorkoutSession/workout-sessions/{userId}

GET /WorkoutSession/workout-session/{workoutSessionId}

GET /WorkoutSession/workout-calendar/{userId}/{month}/{year}
```

---

## API Documentation

Swagger is available during development.

```text
/swagger
```

Use Swagger UI to explore and test available endpoints.

---

## Database Migrations

Create a migration:

```bash
dotnet ef migrations add <MigrationName>
```

Apply migrations:

```bash
dotnet ef database update
```

---

## Local Development

### Prerequisite

* .NET SDK 8
* PostgreSQL
* Docker (optional)

### Clone Repository

```bash
git clone <repository-url>
cd GymTrackerAPI
```

### Configure Environment Variables

Update configuration values in:

```text
appsettings.Development.json
```

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "Jwt": {
    "Secret": "..."
  }
}
```

### Run Application

```bash
dotnet restore

dotnet build

dotnet run
```

---

## Docker

### Development

```bash
docker build -f Dockerfile.dev -t gym-tracker-api-dev .
```

### Production

```bash
docker build -t gym-tracker-api .
```

---

## CI/CD Pipeline

GitHub Actions automatically:

1. Builds the application
2. Executes automated tests
3. Builds Docker images
4. Pushes Docker images to Docker Hub
5. Pushes Docker images to GitHub Container Registry (GHCR)

---

## Future Improvements

* SMS workout sharing
* Additional authentication monitoring
* More comprehensive automated testing

---

## Learning Objectives

This project was built to deepen knowledge in:

* ASP.NET Core Web API Development
* Entity Framework Core
* PostgreSQL Database Design
* JWT Authentication
* Refresh Token Rotation
* Secure API Development
* Docker-based Development
* CI/CD Pipelines with GitHub Actions
* Backend Application Architecture

