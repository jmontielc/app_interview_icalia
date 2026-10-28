# Credit and Employment Verification

Written for: engineers running the app locally for the first time, and reviewers checking the security design.

A lender-facing app that checks credit reports and employment status through Equifax and TransUnion. It stores PDFs in S3 and processes them with a Lambda. The API is ASP.NET Core and the frontend is Vue.

- Architecture, security checkpoints, and deploy flow: [docs/architecture.md](docs/architecture.md)
- Whiteboard summary: [docs/whiteboard.md](docs/whiteboard.md)
- Requirements: [requirement.md](requirement.md)

## Layout

| Path | What it is |
|---|---|
| `src/Api` | ASP.NET Core Web API: auth, RBAC, SSN handling, bureau clients, documents |
| `src/Shared/AppInterview.Pdf` | PDF content rules shared by the API and the Lambda |
| `src/Lambda/PdfProcessor` | S3-triggered Lambda that re-validates uploads and tags the result |
| `tests/Api.Tests` | xUnit tests: unit, RBAC and endpoint integration tests |
| `web` | Vue 3 + TypeScript frontend |
| `infra` | Local emulator config: WireMock bureau stubs, LocalStack init and deploy scripts |
| `docker-compose.yml` | SQL Server, LocalStack and the bureau mock |
| `.github/workflows` | CI (build, test, CodeQL, dependency review, secret scan) and deploy (Dev → QA → Prod) |

## Prerequisites

- .NET SDK 10
- Node.js 24 LTS
- Docker Desktop (for SQL Server, LocalStack and the bureau mock)
- Optional: `awslocal` (`pip install awscli-local`) for the LocalStack scripts

## Run locally

```bash
# 1. Start the emulators: SQL Server, LocalStack (S3, Secrets Manager, Lambda, KMS) and WireMock bureau stubs
docker compose up -d

# 2. (Optional) Deploy the PdfProcessor Lambda and wire the S3 notification
bash infra/localstack/deploy-pdf-processor.sh

# 3. Start the API on port 5000 (the Vite proxy expects this port)
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project src/Api

# 4. In another terminal, start the web app on http://localhost:5173
cd web
npm install
npm run dev
```

Sign in at http://localhost:5173 with one of the development accounts:

| Username | Role | Can do |
|---|---|---|
| `loanofficer` | LoanOfficer | Create applicants, run checks, upload PDFs, view everything else |
| `admin` | Admin | Same as LoanOfficer, plus audit log |
| `auditor` | Auditor | View applicants, reports and documents, download links, audit log. Cannot run checks or upload. |

The password is `Local-Dev-Password-1!`, set in `src/Api/appsettings.Development.json`. The development sign-in endpoint returns 404 outside Development.

## Tests

```bash
dotnet test AppInterviewIcalia.slnx      # API unit, RBAC and integration tests (SQLite and in-memory S3 stand-in)
cd web && npm test && npm run build     # frontend unit tests and production build
```

Integration tests need no Docker. They run the real pipeline on SQLite with stub bureau clients and in-memory S3.

## Security design (summary)

| Concern | How it is handled |
|---|---|
| Bureau credentials | Fetched at runtime from Secrets Manager through `ISecretProvider`. Never in code or images. Cached for 5 minutes. |
| SSN validation | Structural SSA rules in `SsnValidator`. The client mirrors them for feedback, and the server is authoritative. |
| SSN masking | Responses and logs show `***-**-1234`. The JSON converter always writes the masked form. |
| SSN at rest | Encrypted with ASP.NET Data Protection. Only the last four digits are stored in clear. |
| SSN in transit | Sent only in the request body over TLS. Never in URLs or logs. |
| Authorization | JWT bearer tokens. Named policies (`CanRunCreditCheck`, `CanViewReports`, `CanViewAudit`, `CanManageDocuments`) map to roles. Every endpoint requires sign-in unless marked anonymous. |
| Documents | Server-generated S3 keys. Content checked by magic bytes, not the file name. SSE-KMS. Download links last 5 minutes and are audited. |
| Errors | ProblemDetails with no stack traces. Bureau responses are never echoed. |
| Browser | CORS limited to the configured origin. CSP meta tag. The access token is held in memory only. |

## Known gaps

These are listed on purpose so reviewers know where the work stops:

- **Migrations run only in development.** The development startup applies the EF migrations in `src/Api/Data/Migrations` to SQL Server. Production must run `dotnet ef database update` (or an equivalent migration job) as a deploy step before the new API version starts.
- **Development secrets are in config.** The signing key, dev password and bureau tokens are placeholders in `appsettings.Development.json`. Production must supply them from environment variables or a secret store.
- **Local encryption keys.** Data Protection uses the local key ring. Production needs a persistent key store backed by KMS.
- **No rate limiting or extra security headers yet.**
- **No applicant or document list endpoints.** The UI opens records by ID.
- **Bureau integration is against stubs.** The Equifax and TransUnion request and response shapes come from the mock, not from the vendors' real API specs.
- **Deploy workflow is partly placeholder.** The `deploy.yml` steps for ECR, ECS, S3 and CloudFront are marked TODO. The CI checks are real.
- **Not yet verified against Docker.** The SQL Server path and the LocalStack and WireMock flows have not been run.
