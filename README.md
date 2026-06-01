# LoafNCatting Mobile Backend

ASP.NET Core API for the LoafNCatting mobile app.

## Local Setup

1. Run the database script from the `Database_Mobile` repo.
2. Copy `LoafNCatting.Api/appsettings.Development.example.json` to `LoafNCatting.Api/appsettings.Development.json`.
3. Update the SQL Server password in `appsettings.Development.json`.
4. Run the API:

```powershell
dotnet run --project LoafNCatting.Api/LoafNCatting.Api.csproj
```

The API runs on `http://localhost:5117` by default.

Static images are served from `LoafNCatting.Api/wwwroot/Images`, matching database paths such as `/Images/Beverages/cafeda.jpg`.
