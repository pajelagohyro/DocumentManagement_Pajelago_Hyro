# Document Management REST API

A basic ASP.NET Core REST API for uploading, listing, retrieving, downloading, and deleting documents. Document metadata is stored in SQL Server, while file contents are stored on the local filesystem.

## Prerequisites

- .NET 8 SDK (developed with SDK 8.0.423)
- A running SQL Server instance and permission to create or update the target database

## Configure the database

From `src\DocumentManagement.Api`, copy the example configuration to create your local settings file:

```powershell
Copy-Item appsettings.example.json appsettings.json
```

Set `ConnectionStrings:DocumentManagement` in `appsettings.json` to a connection string for your SQL Server database. This local file is ignored by Git; do not commit real passwords or other credentials.

Open a terminal in `src\DocumentManagement.Api` and apply the existing EF Core migrations:

```powershell
dotnet ef database update
```

This applies the migrations in `DocumentManagement.Infrastructure\Data\Migrations` to the configured database. The application does not apply migrations automatically when it starts.

## Configure file storage

By default, files are stored in:

```text
src\DocumentManagement.Api\App_Data\documents
```

The path is configured by `DocumentStorage:RootPath` in your local `appsettings.json`. A relative path is resolved from the API content root. Change it to another local directory if needed, and ensure the API process has read/write permission there. Uploaded files are ignored by Git.

## Run the application

Open a terminal in `src\DocumentManagement.Api` and run:

```powershell
dotnet run
```

The included HTTP launch profile uses `http://localhost:5064` and sets the environment to Development. Open Swagger UI at [http://localhost:5064/swagger](http://localhost:5064/swagger) to explore and try the endpoints, including file uploads.

## API endpoints

| Method | Route | Description | Success response |
|---|---|---|---|
| `POST` | `/api/documents` | Upload one `multipart/form-data` file using the `file` field | `201 Created` with document metadata |
| `GET` | `/api/documents` | List document metadata; file contents are not included | `200 OK` |
| `GET` | `/api/documents/{id}` | Retrieve metadata for a document | `200 OK` |
| `GET` | `/api/documents/{id}/download` | Download the document file | `200 OK` with file content |
| `DELETE` | `/api/documents/{id}` | Delete document metadata and its stored file | `204 No Content` |

Unknown document IDs return `404`. Invalid or empty uploads return `400`; files larger than 10 MB return `413`. Errors are returned using `ProblemDetails`.

## Architecture and implementation

The solution follows a simple Clean Architecture dependency direction:

```text
Api → Application → Domain
Api → Infrastructure
Infrastructure → Application + Domain
Domain → no project dependencies
```

- **Api** handles HTTP requests, responses, and dependency registration.
- **Application** coordinates document operations, validates upload data with .NET DataAnnotations, and defines repository/storage interfaces.
- **Domain/Models** contains the `Document` entity representing persisted document metadata.
- **Infrastructure** implements metadata persistence with EF Core and SQL Server, and file storage with the local filesystem.
- **tests** contains automated application-level tests using in-memory fakes.

EF Core migrations define the SQL Server schema. The `Documents` table stores the ID, original filename, storage identifier, content type, file size, and upload time. File bytes are stored separately on disk. The API returns the document ID and client-facing metadata; the storage identifier remains internal.

## Run automated tests

From the repository root:

```powershell
dotnet test DocumentManagement.sln
```

The tests cover upload, metadata retrieval, missing documents, oversized and invalid uploads, deletion, listing, successful download, and a missing stored file. They use in-memory fakes, so a SQL Server instance is not required to run the test suite.

## Assumptions and limitations

- Any file type is accepted. The supplied content type is stored as metadata; the API does not inspect file bytes to verify the format or scan for malware.
- Files are limited to 10 MB each, and empty files are rejected. The HTTP request limit allows a small additional amount for multipart framing; that overhead allowance is an implementation setting, not a standard multipart value.
- Storage names are generated from document IDs. Client-provided filenames are sanitized for metadata/download use and are not used as filesystem paths.
- Files use local filesystem storage; the application does not provide shared or cloud storage.
- If file storage succeeds but saving metadata fails, an unreferenced file may remain. Deletion removes the database record before deleting the file; if file removal fails, a file may remain without a metadata record.
- Listing is unpaginated. Authentication and authorization are not implemented.
- Tests cover application behavior with fakes, not HTTP end-to-end behavior or a live SQL Server instance.

## AI Tools Used

I used GitHub Copilot (interactive mode in VS Code) to speed up development and handle boilerplate.

- **Structure first:** I set up the initial project structure myself, following Clean Architecture, to give the project its shape.
- **Dependency injection:** I defined the interfaces and wired everything together in `Program.cs` using a dependency injection pattern.
- **AI-assisted implementation:** I then used Copilot to fill in parts of the code, such as model setup, DbContext setup, repository implementations, and other boilerplate.
- **Scoped to requirements:** I trimmed down over-engineered suggestions, such as elaborate validation, and kept the implementation focused on the stated requirements.