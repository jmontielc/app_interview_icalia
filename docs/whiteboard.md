# Whiteboard (simplified)

Written for: an interview whiteboard session. Shows the main elements at a high level.

```mermaid
flowchart TB
    USERS["👥 Users<br/>LoanOfficer · Admin · Auditor"]

    subgraph FE["Frontend — Vue 3 SPA (S3 + CloudFront)"]
        FEX["Login · Applicants · Credit checks · Documents"]
    end

    subgraph BE["Backend — ASP.NET Core Web API (ECS Fargate)"]
        M1["Auth"]
        M2["Applicants"]
        M3["Credit (Equifax / TransUnion)"]
        M4["Documents"]
        M5["Audit"]
    end

    DB[("Database<br/>SQL Server (RDS, encrypted)")]
    AUTH["🔐 Authentication<br/>Cognito (OIDC, JWT)"]
    SEC["🔒 Secrets<br/>AWS Secrets Manager<br/>(bureau tokens, DB creds)"]
    S3[("S3 — PDFs / objects<br/>SSE-KMS")]
    LMB["Lambda — PdfProcessor"]
    BUR["Bureaus<br/>Equifax · TransUnion"]

    USERS --> FE
    FE -->|"JWT"| AUTH
    FE -->|"HTTPS /api"| BE
    BE --> DB
    BE -->|"validate token"| AUTH
    BE -->|"read at runtime"| SEC
    BE -->|"HTTPS + token"| BUR
    BE --> S3
    S3 --> LMB

    subgraph DEPLOY["Deploy flow"]
        D1["PR + review 🔒"] --> D2["CI: tests · SAST · scans 🔒"] --> D3["Dev"] --> D4["QA 🔒 DAST"] --> D5["Prod 🔒 approval"]
    end
```

🔒 = security check. See [architecture.md](architecture.md) for the full numbered list.
