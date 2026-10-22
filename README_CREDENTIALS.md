# Local development configuration

Keep connection strings, JWT signing keys, email credentials, and administrator
passwords in environment variables or .NET user-secrets. Never commit real values
to `appsettings*.json`, `.env` files, source control, or screenshots.

## Configure the API

From `backend/HaladeHighSchool.Api`, set local values outside the repository:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<PostgreSQL connection string>"
dotnet user-secrets set "Jwt:Key" "<random signing key of at least 32 characters>"
$env:SuperAdminSettings__Email = "superAdmin01@gmail.com"
dotnet user-secrets set "SuperAdminSettings:Password" "<unique SuperAdmin password>"
$env:AdminSettings__Email = "admin@example.edu"
$env:AdminSettings__Password = "<unique administrator password>"
```

The SuperAdmin email defaults to `superAdmin01@gmail.com` and can be overridden with
`SuperAdminSettings__Email`. Startup creates that account if it does not exist and
ensures it has the `SuperAdmin` role. Existing account passwords are not reset.
Production startup fails if `SuperAdminSettings__Password` is missing.

The administrator seeder reads `AdminSettings__Email` and
`AdminSettings__Password` directly from the process environment. Production startup
fails if either variable is missing. The administrator's full name is optional and
can be supplied as `AdminSettings__FullName`.

## Render environment

Configure these variables in the Render service environment; do not commit their
values:

- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string in Npgsql
  format (`Host=...;Port=...;Database=...;Username=...;Password=...;SSL Mode=Require`).
- `Jwt__Key`: strong signing key of at least 32 characters.
- `SuperAdminSettings__Password`: unique SuperAdmin password.
- `AdminSettings__Email` and `AdminSettings__Password`: credentials required by the
  current production Admin seeder.

ASP.NET Core maps double underscores to configuration section separators, so
`ConnectionStrings__DefaultConnection` is read by
`GetConnectionString("DefaultConnection")`. The application does not automatically
convert Render's `DATABASE_URL`; set the named connection-string variable above.

The API uses a 15-minute access token and rotates refresh tokens. The `Jwt` section
also requires `Issuer`, `Audience`, and `Key`; provide `Jwt__Key` through a secret
provider outside Development. User-secrets are loaded only in the Development
environment.

## Optional development accounts

Demo teacher/student seeding is disabled by default. To enable it locally, configure
unique passwords first:

```powershell
dotnet user-secrets set "SeedDemoAccounts:Teacher:Password" "<unique teacher password>"
dotnet user-secrets set "SeedDemoAccounts:Student:Password" "<unique student password>"
dotnet user-secrets set "SeedDemoAccounts:Enabled" "true"
```

Demo accounts are only seeded in Development. Existing accounts are not reset by the
seeder.

## Database schema

Apply the EF Core migrations from the API project with:

```powershell
dotnet ef database update
```

Schema changes are maintained in `backend/HaladeHighSchool.Api/Migrations`; the
legacy SQL scripts have been removed.
