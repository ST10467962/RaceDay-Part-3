# RaceDay API - Part 2 (PROG6212)

Student: Ntsakiso Chobede  
Module: Programming 2B (PROG6212)  
Year: 2026

---

## Project Description

This is the RESTful API that powers the RaceDay system.  
It handles all data operations for race events, categories, participant enrolments, results, and user management.

The API is built using ASP.NET Core Web API with Entity Framework Core (Code-First) and implements server-side session authentication with role-based access control for Organiser and Participant roles.

---

## What is an API?

An API (Application Programming Interface) is a set of rules and tools that allows different software applications to communicate with each other.  

In simple terms, an API acts like a messenger. It takes a request from one system (for example a website or mobile app), sends it to another system (the server), and then brings the response back.

### How APIs Work

1. A client (browser, mobile app, or another program) sends a request to the API.
2. The API receives the request and processes it.
3. The API talks to the database or other services if needed.
4. The API sends a response back to the client (usually in JSON format).

### What is a RESTful API?

REST stands for Representational State Transfer.  
A RESTful API is an API that follows REST principles. It uses standard HTTP methods to perform operations on resources:

- GET     - Retrieve data (read)
- POST    - Create new data
- PUT     - Update existing data
- DELETE  - Remove data

Each resource (for example Events, Categories, or Users) has its own URL (endpoint).  
This project is a RESTful API because it uses these standard methods and returns data in JSON format.

### Example of how this API works

- A user sends a POST request to /api/Auth/login with email and password.
- The API checks the credentials in the database.
- If correct, the API creates a server-side session and returns a success message.
- The user can then call other endpoints such as /api/Events to view or manage race events.

---

## Features Implemented

- User Registration and Login (Organiser and Participant)
- Server-side Session Authentication (stores UserId and Role)
- Role-Based Access Control
- Full CRUD for:
  - Events
  - Categories
  - Entries (Event Enrolments)
  - Results
  - Participants
  - Race Routes
  - Weather
- Password hashing
- Swagger UI for testing all endpoints
- Unit Tests (xUnit) including Authentication tests
- Continuous Integration with GitHub Actions

---

## Technologies Used

- ASP.NET Core 8 Web API
- Entity Framework Core 8 (Code-First)
- SQL Server
- Cookie and Session Authentication
- Swashbuckle (Swagger)
- xUnit and FluentAssertions (Unit Testing)
- GitHub Actions (CI/CD)

---

## Getting Started

### Prerequisites

- Visual Studio 2022 (or later)
- .NET 8 SDK
- SQL Server (LocalDB or SQL Server Express)

### 1. Clone the Repository

```bash
git clone <your-github-repo-url>
cd RaceDayAPI
```

### 2. Open the Solution

Open RaceDayAPI.sln in Visual Studio.

### 3. Database Connection

The connection string is located in appsettings.json:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(local)\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Update the connection string if necessary to match your SQL Server instance.

### 4. Apply Migrations (if needed)

Open Package Manager Console and run:

```powershell
Update-Database
```

### 5. Run the API

- Press F5 or click the green Start button.
- The API will start and Swagger UI will open automatically at:
  https://localhost:xxxx/swagger

---

## Docker Setup

This project includes a Dockerfile for containerization. Follow these steps to build and run the API in Docker.

### Prerequisites

- Docker Desktop installed and running

### Build the Docker Image

Run the following command from the workspace root directory:

```powershell
docker build -t raceday-mvc:latest -f RaceDayMVC/Dockerfile .
```

**Command explanation:**
- `-t raceday-mvc:latest` – Tags the image with a name and version
- `-f RaceDayMVC/Dockerfile` – Specifies the path to the Dockerfile
- `.` – Uses the current directory as the build context

### Run the Docker Container

```powershell
docker run -p 8080:8080 --name raceday-mvc raceday-mvc:latest
```

**Command explanation:**
- `-p 8080:8080` – Maps port 8080 from container to your host machine
- `--name raceday-mvc` – Names the running container
- `raceday-mvc:latest` – Specifies the image to run

The API will be accessible at:
- `http://localhost:8080` (HTTP)

### Run in Detached Mode (Background)

To run the container in the background:

```powershell
docker run -d -p 8080:8080 --name raceday-mvc raceday-mvc:latest
```

### View Container Logs

To check logs from a running container:

```powershell
docker logs raceday-mvc
```

Or follow logs in real-time:

```powershell
docker logs -f raceday-mvc
```

### Stop and Remove the Container

To stop the container:

```powershell
docker stop raceday-mvc
```

To remove the container:

```powershell
docker rm raceday-mvc
```

```powershell
docker rm raceday-api
```

---

## Authentication and Roles

### Roles

| Role         | Permissions                                      |
|--------------|--------------------------------------------------|
| Organiser    | Can create, update and delete Events, Categories, Results, Race Routes, Weather and Participants |
| Participant  | Can enrol in events (create Entries) and view data |

### How Authentication Works

- Users register or log in via /api/Auth/register and /api/Auth/login
- A server-side session is created that stores:
  - UserId
  - Role
  - Email
- Protected endpoints require a valid session
- Role-based authorization is enforced using [Authorize(Roles = "Organiser")] or [Authorize(Roles = "Participant")]

### Testing Authentication in Swagger

1. Call POST /api/Auth/register or POST /api/Auth/login
2. The session cookie is automatically stored by the browser
3. You can now call protected endpoints

---

## Main API Endpoints

| Resource       | Endpoint                  | Access                  |
|----------------|---------------------------|-------------------------|
| Auth           | /api/Auth/register        | Public                  |
| Auth           | /api/Auth/login           | Public                  |
| Auth           | /api/Auth/logout          | Authenticated           |
| Events         | /api/Events               | View: Both / Write: Organiser |
| Categories     | /api/Categories           | View: Both / Write: Organiser |
| Entries        | /api/Entries              | Create: Participant / Manage: Organiser |
| Results        | /api/Results              | View: Both / Write: Organiser |
| Participants   | /api/Participants         | View: Both / Write: Organiser |
| Race Routes    | /api/RaceRoutes           | View: Both / Write: Organiser |
| Weather        | /api/Weathers             | View: Both / Write: Organiser |

---

## Running Unit Tests

1. Open Test Explorer in Visual Studio (Test -> Test Explorer)
2. Click Run All

Or from the terminal:

```bash
dotnet test
```

### Tests Included

- CategoriesController comprehensive CRUD tests
- AuthController tests (Registration + Login + validation scenarios)

---

## Continuous Integration (CI/CD)

This project uses GitHub Actions.

- Workflow file: .github/workflows/ci.yml
- On every push to main/master the following runs:
  - Restore
  - Build
  - Unit Tests

CI Build Status:  


---

## Video Presentation

A full demonstration video of the API has been recorded and is available here:

Video Link: [Insert your Google Drive / YouTube / OneDrive link here]

The video covers:
- Project structure
- Authentication (Register and Login)
- Role-based access demonstration
- Swagger testing of endpoints
- Unit tests execution

---

## Project Structure

```
RaceDayAPI/
├── Controllers/          # API Controllers
├── Data/                 # DbContext
├── Models/               # Entity Models + DTOs
├── Services/             # Authentication Service
├── Migrations/           # EF Core Migrations
├── .github/workflows/    # CI Pipeline
└── RaceDayAPI.Tests/     

---

## Author

Ntsakiso Chobede  
Student Number: [ST10467962]  
Module: PROG6212 - Programming 2B 

