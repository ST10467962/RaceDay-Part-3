# RaceDay (PROG6212 Part 3)

Student: Ntsakiso Chobede | Module: PROG6212 | Year: 2026

RaceDay is a race-event platform. **Organisers** create events, set up categories, confirm enrolments and publish results. **Participants** browse upcoming events, enrol, follow their enrolment status and see their race history.

| Folder | What it is |
| --- | --- |
| `RaceDayAPI/RaceDayAPI` | The Part 2 RESTful API (ASP.NET Core, EF Core, SQL Server) with Azure Blob Storage added in Part 3. |
| `RaceDayAPI/RaceDayAPI.Tests` | xUnit tests from Part 2. |
| `RaceDayMVC/RaceDayMVC` | **Part 3.** ASP.NET Core MVC front end. No database access and no business rules; everything goes through the API. |

```
Browser -> RaceDayMVC (session, Razor views) -> RaceDayAPI (rules, EF Core) -> SQL Server
                                                      |
                                                      +-> Azure Blob Storage (banners, profile pictures)
```

The MVC app never talks to Azure. Images are uploaded to the API, stored in a private blob container, and read back through `GET /api/Files/{blobName}`.

## Run it locally

**You need:** .NET 8 SDK, SQL Server LocalDB or Express, and Azurite (local Azure Storage emulator) or an Azure Storage account.

### 1. Start Azure Blob Storage

```bash
npx azurite --silent --location ./azurite-data
```

The default `AzureBlobStorage:ConnectionString` (`UseDevelopmentStorage=true`) points at Azurite. To use a real storage account, keep the connection string out of Git:

```bash
cd RaceDayAPI/RaceDayAPI
dotnet user-secrets init
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>"
```

### 2. Start the API

The database is migrated automatically on start-up. In Development the API also creates demo data (organisers, participants, events, enrolments and results). Every demo account uses the password `password321`, for example `thabo.mokoena@raceday.co.za` (organiser) and `sipho.ndlovu@example.com` (participant).

```bash
cd RaceDayAPI/RaceDayAPI
dotnet run --launch-profile docker-host
```

This listens on `http://0.0.0.0:5068`, which the Docker container needs. Without Docker you can use `--launch-profile http` instead. Swagger is at `http://localhost:5068/swagger`.

**Registering a new organiser** needs the access code set in `RaceDayAPI/RaceDayAPI/appsettings.json` under `OrganiserRegistration:AccessCode`. It is empty by default, which closes organiser registration. Set a code (or use the demo organiser accounts) before registering one.

### 3. Start the MVC app

```bash
cd RaceDayMVC/RaceDayMVC
dotnet run
```

Open `http://localhost:5100`. The API address is `ApiSettings:BaseUrl` in `RaceDayMVC/RaceDayMVC/appsettings.json`.

## Run it in Docker

Docker Desktop must be running, and the API and Azurite must be running as above. From the repository root, one command builds and starts the MVC container:

```bash
docker build -t raceday-mvc -f RaceDayMVC/RaceDayMVC/Dockerfile RaceDayMVC && docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway -e ApiSettings__BaseUrl=http://host.docker.internal:5068 raceday-mvc
```

Open `http://localhost:8080`. It behaves the same as the local run because it reads the same API. If the API is somewhere else, change the address after `ApiSettings__BaseUrl=`.

## Using the app

**Organiser** (Dashboard, Events, Categories, Enrolments, Results)
1. Create an event with name, description, date, location, distance, type and an optional banner.
2. Add age or distance categories.
3. Confirm or cancel enrolments.
4. After the event date, enter finish times. Positions are worked out from the times and published straight away.

**Participant** (Events, My enrolments, My results, Profile)
1. Browse and filter upcoming events.
2. Open an event, pick a category and enrol. The enrolment is recorded as Pending (amber) until confirmed (green).
3. View enrolments and results (for example "47th out of 312 finishers").
4. Update personal details and upload a profile picture.

## How the Part 3 requirements are met

| Requirement | Where |
| --- | --- |
| MVC consuming the API, no DB access | `RaceDayMVC/RaceDayMVC/Infrastructure/ApiClient.cs` is the only data source. |
| Register and log in through the API | `AccountController` calls `api/Auth/register` and `api/Auth/login`. |
| Session-based state and role | `Program.cs` (`AddSession`), `Infrastructure/SessionKeys.cs`. |
| Distinct interface per role | Role-based navigation in `Views/Shared/_Layout.cshtml`; separate organiser and participant controllers. |
| Protected pages | `[SessionAuthorize]` in `Infrastructure/SessionAuthorizeAttribute.cs`; the API enforces roles too. |
| Log out and redirect | `Account/Logout` clears the session and returns to the welcome page. |
| Azure Blob Storage via the API | `RaceDayAPI/RaceDayAPI/Services/BlobStorageService.cs`, plus the banner, profile picture and `FilesController` endpoints. |
| Docker | `RaceDayMVC/RaceDayMVC/Dockerfile` and the run command above. |
| UI design | Bootstrap 5 and `wwwroot/css/site.css`; green Confirmed, amber Pending. |
| Invalid input handled | Form validation, `ApiExceptionFilter` and the `/Home/Error` page. |
| CI builds the MVC project | `.github/workflows/ci.yml` |

## Git

Use a clearly named branch such as `part-3-mvc`, commit in small steps (at least 20 meaningful commits), and push. CI runs on `main`, `master` and `part-3*` branches.

## Demo video

YouTube (unlisted):https://youtu.be/Rq1XW2bNeDw?si=JNtOEkZ0tBwpkFnO

The video should show the full app running, explain the MVC structure (controllers, views, `ApiClient`, session filter), show images stored through Blob Storage, and show the Docker build and run.

---

The Part 2 API documentation is in `RaceDayAPI/RaceDayAPI/README.md`.
