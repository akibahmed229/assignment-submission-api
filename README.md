# Assignment & Submission Management System — Backend

A role-based (Admin / Teacher / Student) REST API for a school/college
Assignment & Submission Management System, built for the OnnoRokom Projukti
Assistant Software Engineer assessment.

## Overview

Admins manage users, classes, subjects, and who teaches/attends what.
Teachers create assignments for the classes/subjects they're assigned to,
publish them, and grade student submissions. Students see published
assignments for their enrolled classes, submit answers, and view their
grades and feedback.

## Live Demo

- **Frontend Application (Live):** [https://assignment-submission-frontend-six.vercel.app](https://assignment-submission-frontend-six.vercel.app)
- **API Swagger Documentation (Live):** [https://assignment-submission-api-bd69.onrender.com/swagger](https://assignment-submission-api-bd69.onrender.com/swagger)
- **For User Credentials:** [Demo Credentials](#-demo-credentials)

## Tech Stack

| Layer            | Technology                                                     |
| ---------------- | -------------------------------------------------------------- |
| Framework        | ASP.NET Core Web API (.NET 10)                                 |
| Language         | C#                                                             |
| Database         | PostgreSQL                                                     |
| ORM              | Entity Framework Core (Npgsql provider)                        |
| Auth             | JWT bearer tokens, role-based authorization                    |
| Password hashing | BCrypt                                                         |
| Validation       | DataAnnotations + `[ApiController]` automatic model validation |
| Logging          | Serilog (console + rolling daily file)                         |
| API docs         | Swagger / OpenAPI (Swashbuckle)                                |
| Testing          | xUnit, Moq, FluentAssertions (or `Assert`), EF Core InMemory   |

## Architecture

Classic **Controller → Service → DbContext** (MVC-style), all in one API
project — no separate Domain/Application/Infrastructure split, no MediatR.
Chosen deliberately over a heavier Clean Architecture layout: same
functional requirements, far less ceremony for a project this size.

```
Controllers/   Thin HTTP layer. Parses requests, calls a service, returns
               the result. Contains [Authorize(Roles = "...")] attributes
               for role gating.

Services/      All business logic. Each service takes a dependency on
               AppDbContext (or ICurrentUserService, IPasswordHasher, etc.)
               and enforces rules the [Authorize] attribute alone can't
               express — e.g. "is THIS teacher assigned to teach THIS
               class+subject", "is THIS student enrolled in the class this
               assignment belongs to".

Data/          AppDbContext (EF Core) and DbSeeder (creates demo accounts
               and applies migrations on startup).

Models/
  Entities/    Database tables.
  Dtos/        Request/response shapes. Entities are never returned
               directly from a controller.
  Enums/       Role, AssignmentStatus, SubmissionStatus.

Exceptions/    NotFoundException, ForbiddenAccessException — thrown from
               services, mapped to HTTP status codes by...

Middleware/    ExceptionHandlingMiddleware — single place that turns
               thrown exceptions into consistent JSON error responses,
               so controllers/services never need try/catch for this.
```

### Two layers of access control

1. **`[Authorize(Roles = "...")]`** on each controller action — checks
   _what kind_ of user is calling (any Teacher, any Student, etc.).
2. **Service-level checks** — checks whether _this specific_ user is
   allowed to touch _this specific_ row (e.g. only the teacher who created
   an assignment can publish/delete it; only a student enrolled in a
   class can submit to its assignments). The attribute alone cannot
   express this; it lives in the service methods (`AssignmentService`,
   `SubmissionService`).

Both layers matter — removing either one leaves a real gap. `[Authorize]`
without service checks would let any Teacher grade any other teacher's
assignments; service checks without `[Authorize]` would let an
unauthenticated request reach business logic at all.

## Database Schema

See [`DATABASE_SCHEMA.md`](./doc/DATABASE_SCHEMA.md) and
[`schema-erd.mermaid`](./doc/schema-erd.mermaid) for the full entity
relationship diagram and table-by-table explanation. Summary:

- **`User`** — one table for all three roles, distinguished by `Role`.
- **`SchoolClass`**, **`Subject`** — independent lookup tables.
- **`TeacherAssignment`** — join table: which teacher teaches which
  subject for which class. Unique per `(TeacherId, SchoolClassId, SubjectId)`.
- **`StudentEnrollment`** — join table: which student belongs to which
  class. Unique per `(StudentId, SchoolClassId)`.
- **`Assignment`** — created by a teacher for a specific class+subject;
  starts as `Draft`, becomes `Published` when ready for students to see.
- **`Submission`** — a student's answer to an assignment, plus grading
  state (`Marks`, `Feedback`, `GradedAt`). Unique per
  `(AssignmentId, StudentId)` — one submission per student per assignment.

## Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL), or a local PostgreSQL instance
- `dotnet-ef` CLI tool (`dotnet tool install --global dotnet-ef`)

## Setup

### 1. Environment variables

Copy the example file and fill in real values:

```bash
cp .env.example .env
```

Generate a real JWT secret (32+ random characters) for `Jwt__Secret`.
`.env` uses double-underscore (`__`) as the config-section separator —
`Jwt__Secret` maps to `Configuration["Jwt:Secret"]`, `Seed__AdminPassword`
maps to `Configuration["Seed:AdminPassword"]`, and so on.

### 2. Start PostgreSQL

```bash
docker compose up -d postgres
```

### 3. Apply migrations

```bash
cd src/AssignmentSubmissionSystem.Api
dotnet ef database update
```

(Migrations are already checked into the repo under `Migrations/` — no
need to generate new ones for a fresh clone.)

### 4. Run the API

```bash
dotnet run
```

On startup the app automatically applies any pending migrations and
seeds one demo account per role (only if the `Users` table is empty — safe
to re-run without duplicating data). Swagger UI opens at `/swagger`.

## Demo Credentials

Seeded automatically on first run. Passwords come from `.env`
(`Seed__AdminPassword`, etc.) — the values below are the defaults if you
haven't overridden them.

| Role    | Email                            | Password     |
| ------- | -------------------------------- | ------------ |
| Admin   | `admin@assignmentsystem.local`   | `Admin123`   |
| Teacher | `teacher@assignmentsystem.local` | `Teacher123` |
| Student | `student@assignmentsystem.local` | `Student123` |

## API Overview

Full request/response schemas are in Swagger (`/swagger`) — this is a
quick reference of what exists and who can call it.

| Endpoint                                        | Method(s)                 | Who                                         |
| ----------------------------------------------- | ------------------------- | ------------------------------------------- |
| `/api/auth/register`, `/api/auth/login`         | POST                      | Anyone                                      |
| `/api/users?role={Role}`                        | GET                       | Admin                                       |
| `/api/schoolclass`                              | GET, POST, DELETE `/{id}` | GET: any authenticated user · write: Admin  |
| `/api/subjects`                                 | GET, POST, DELETE `/{id}` | GET: any authenticated user · write: Admin  |
| `/api/teacherassignments`                       | GET, POST, DELETE `/{id}` | Admin                                       |
| `/api/teacherassignments/mine`                  | GET                       | Teacher                                     |
| `/api/studentenrollments`                       | POST, DELETE `/{id}`      | Admin                                       |
| `/api/studentenrollments/class/{schoolClassId}` | GET                       | Admin, Teacher                              |
| `/api/assignments`                              | POST                      | Teacher                                     |
| `/api/assignments/mine`                         | GET                       | Any (scope depends on role — see below)     |
| `/api/assignments/{id}`                         | GET, DELETE               | Ownership/enrollment-checked in the service |
| `/api/assignments/{id}/publish`                 | PATCH                     | Teacher (must own the assignment)           |
| `/api/assignments/{assignmentId}/submissions`   | POST, GET                 | POST: Student · GET: Admin, Teacher         |
| `/api/submissions/{id}`                         | PUT                       | Student (own submission only)               |
| `/api/submissions/{id}/grade`                   | POST                      | Admin, Teacher (must own the assignment)    |
| `/api/submissions/mine`                         | GET                       | Student                                     |

**`GET /api/assignments/mine` is role-scoped, not a fixed list:**
Admin sees every assignment; a Teacher sees only what they created; a
Student sees only `Published` assignments for classes they're enrolled in.

## Running Tests

```bash
cd tests/AssignmentSubmissionSystem.UnitTests
dotnet test
```

Tests cover the business rules and authorization logic in
`AssignmentService` and `SubmissionService` (e.g. a teacher can't create
an assignment for a class they're not assigned to; a student can't submit
to a class they're not enrolled in; a graded submission can't be edited;
marks can't exceed an assignment's max) plus the auth flow in
`AuthService` (duplicate email rejection, wrong-password/inactive-account
login rejection). Uses EF Core's InMemory provider — no real database
needed to run the suite.

## Frontend

A companion Next.js/TypeScript frontend consumes this API — see its own
README for setup. It expects `NEXT_PUBLIC_API_URL` to point at this API's
base URL (e.g. `http://localhost:5201/api`).

The companion Next.js frontend is deployed live on Vercel at [https://assignment-submission-frontend-six.vercel.app](https://assignment-submission-frontend-six.vercel.app).
The github repo for companion fronted at [https://github.com/akibahmed229/assignment-submission-frontend](https://github.com/akibahmed229/assignment-submission-frontend).

## Assumptions

- **PostgreSQL over MongoDB** — the brief allowed either; Postgres fits
  the relational shape of Users → Classes → Assignments → Submissions.
- **Custom JWT + BCrypt instead of ASP.NET Core Identity** — Identity
  brings cookie auth, external logins, and email confirmation machinery
  this project doesn't need. A plain `User` entity + hand-issued JWTs is
  simpler to reason about at this scope.
- **One `User` table for all three roles**, distinguished by a `Role`
  column, rather than separate tables per role — they share every field
  except `Role`; splitting them would add real EF mapping complexity for
  no benefit here.
- **`Register` is open (`[AllowAnonymous]`)** rather than
  Admin-only — convenient for local testing and Swagger. In a production
  deployment this would be locked to `[Authorize(Roles = "Admin")]` so
  only Admins create Teacher/Student accounts, per the brief's stated
  role responsibilities.
- **Late submissions are accepted, not blocked** — a submission made
  after the deadline is still saved, just flagged `Late` instead of
  `Submitted`, so a teacher can decide case-by-case whether to grade it.
  Hard-blocking late submissions instead is a one-line change in
  `SubmissionService.SubmitAsync` if the evaluator's expectation differs.

## Known Limitations

- No refresh-token flow — JWTs expire after `Jwt__ExpiryMinutes` (default
  60 minutes) and the client must log in again. No revocation table, so a
  token can't be invalidated before it naturally expires.
- No rate limiting on `/api/auth/login`.
- No pagination on list endpoints (`/api/assignments/mine`,
  `/api/teacherassignments`, etc.) — acceptable at the assessment's data
  scale, would need addressing for a larger real-world dataset.
- No endpoint to list _all_ student enrollments across every class at
  once — `StudentEnrollment` is only queryable scoped to one class at a
  time (`GET /api/studentenrollments/class/{id}`).
- File-upload submissions aren't supported — `Submission.AnswerText` is
  plain text only. Extending to file uploads would mean adding a storage
  provider (local disk or cloud) and a `FileUrl` field.
