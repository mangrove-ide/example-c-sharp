# Cost demo — CRM (C#)

A basic CRM for adding and removing customers (first name, last name, email). It's the C# version of
`example-typescript`: same REST API, same UI, same SQLite schema.

- `src/Crm.Api/` — ASP.NET Core minimal API storing data in a local SQLite file (Microsoft.Data.Sqlite). It also serves the frontend.
- `src/Crm.Web/` — Blazor WebAssembly frontend (C#) that calls the API at `/api`.
- `src/Crm.Shared/` — the `Customer` model shared by both.
- `tests/` — xUnit API tests (`Crm.Api.Tests`) and bUnit UI tests (`Crm.Web.Tests`).

Requires the .NET 10 SDK. CI (`.github/workflows/ci.yml`) builds and tests every push and pull request.

## Setup

```sh
dotnet build
dotnet run --project src/Crm.Api -- seed   # optional demo customers
```

## Run

```sh
PORT=4000 dotnet run --project src/Crm.Api
```

Then open http://localhost:4000. Unlike the TypeScript version, the frontend and the API run in one process on one port.

| Variable        | Default                        | Purpose                      |
| --------------- | ------------------------------ | ---------------------------- |
| `PORT`          | `4000`                         | Port the app listens on      |
| `DATABASE_PATH` | `src/Crm.Api/data/crm.sqlite`  | SQLite file (ignored by git) |

## Test

```sh
dotnet test
```

## API

| Method   | Path                  | Body                               | Responses                     |
| -------- | --------------------- | ---------------------------------- | ----------------------------- |
| `GET`    | `/api/customers`      | —                                  | `200` list of customers       |
| `POST`   | `/api/customers`      | `{firstName, lastName, email}`     | `201`, `400` invalid, `409` duplicate email |
| `DELETE` | `/api/customers/:id`  | —                                  | `204`, `404`                  |
