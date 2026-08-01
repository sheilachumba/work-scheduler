# Support Worker Availability & Placement Portal

ASP.NET Core web application for CCWA support workers to submit availability, university, and placement information through a **dynamic form** that admins can manage without code changes.

## Tech stack

- ASP.NET Core 10 (Razor Pages + Identity)
- Entity Framework Core (code-first migrations)
- SQL Server (LocalDB for local development)
- Bootstrap 5

> **Note:** This machine uses .NET 10 SDK. The project targets `net10.0`. For ASP.NET Core 8 specifically, install the .NET 8 SDK and change `<TargetFramework>net8.0</TargetFramework>` in `SupportWorkerPortal.csproj`.

## Prerequisites

1. [.NET SDK](https://dotnet.microsoft.com/download) (10.x or 8.x depending on target framework)
2. [SQL Server LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio) or SQL Server Express
3. EF Core CLI tools:

```bash
dotnet tool install --global dotnet-ef
```

## Open in Visual Studio

1. Open **`SupportWorkerPortal.sln`** in Visual Studio 2022 (17.10+ recommended for .NET 10).
2. Set **SupportWorkerPortal** as the startup project (it should be selected by default).
3. Press **F5** to run (HTTPS) or **Ctrl+F5** to run without debugging.

Visual Studio will apply EF Core migrations and seed data automatically on first run.

> A newer **`SupportWorkerPortal.slnx`** file also exists (.NET 10 format). Use the classic `.sln` if your Visual Studio version does not recognize `.slnx`.

## Getting started

### 1. Configure connection string

Default LocalDB connection in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SupportWorkerPortal;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

Update this if using a different SQL Server instance.

### 2. Configure admin user

Admin credentials are seeded from configuration (not hardcoded):

```json
"AdminUser": {
  "Email": "admin@ccwa.local",
  "Password": "Admin123!"
}
```

Override in `appsettings.Development.json` or user secrets for production.

### 3. Run migrations

Migrations run automatically on startup. To apply manually:

```bash
cd "C:\Users\Admin\Work Scheduler"
dotnet ef database update
```

To create a new migration after model changes:

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### 4. Run locally

```bash
dotnet run
```

Open:

- **Public form:** https://localhost:5001/Form
- **Admin login:** https://localhost:5001/Identity/Account/Login
- **Question builder:** `/Admin/Questions` (Admin role required)
- **Submissions:** `/Admin/Submissions` (Admin role required)

### Default admin login

| Field | Value |
|-------|-------|
| Email | `admin@ccwa.local` |
| Password | `Admin123!` |

Change these in configuration before deploying to production.

## Application structure

### Public form (`/Form`)

- Loads **active** `FormQuestion` records ordered by `DisplayOrder`
- Renders the correct input control per `FieldType`
- Validates on client and server using each question's `IsRequired` and `FieldType`
- Creates a `FormSubmission` plus one `FormAnswer` per question on submit

### Admin — Question builder (`/Admin/Questions`)

- List, add, edit, reorder (drag-and-drop + up/down), and soft-disable questions
- Live preview panel shows the current active form
- Options for Dropdown / Radio / Multi-select are stored as JSON

### Admin — Submissions (`/Admin/Submissions`)

- Filter by status, date range, and search across answer text
- Summary cards: Total, Pending, Approved
- Detail view with status workflow and admin notes
- CSV export handles submissions with different question sets over time

## Dynamic form / EAV data model

Instead of hard-coded form fields, questions are stored as rows in `FormQuestions`:

| Column | Purpose |
|--------|---------|
| `Label` | Question text shown to users |
| `FieldType` | Input type (Text, Email, RadioButtons, etc.) |
| `Options` | JSON array for choice-based fields |
| `IsRequired` | Server-side validation flag |
| `DisplayOrder` | Render order |
| `IsActive` | Soft-disable without breaking historical data |

When a user submits:

1. A `FormSubmission` row is created (status defaults to `Pending`)
2. One `FormAnswer` row is created per active question
3. `AnswerValue` stores plain text or a JSON array (multi-select)

This **Entity-Attribute-Value (EAV)** pattern means:

- Admins can add/edit/reorder questions without redeploying code
- Old submissions remain linked to the `FormQuestion` they answered via FK
- Deactivating a question (`IsActive = false`) hides it from new submissions but preserves past answers
- CSV export builds columns from the union of questions present across exported submissions

## Field types

`Text`, `Email`, `Phone`, `Number`, `TextArea`, `Dropdown`, `MultiSelectCheckbox`, `DatePicker`, `RadioButtons`

## Seeded default questions

On first run, the app seeds questions based on the CCWA Microsoft Form (name, contact details, study/placement info, availability shifts, etc.) so the form is usable immediately.

## Security notes

- Admin pages require the **Admin** Identity role
- Do not commit production passwords — use user secrets or environment variables
- Review Identity password and lockout settings before production deployment

## Useful commands

```bash
# Build
dotnet build

# Run with hot reload
dotnet watch run

# Reset database (development only)
dotnet ef database drop
dotnet ef database update
```
