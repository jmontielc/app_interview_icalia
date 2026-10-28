# Architecture

Written for: engineers and reviewers evaluating the security design and deploy flow.

Security checkpoints are marked with 🔒 and numbered. The same numbers appear in the diagrams and the table at the end.

## 1. Production architecture (AWS)

```mermaid
flowchart LR
    U["👤 Users<br/>LoanOfficer / Admin / Auditor"]

    subgraph EDGE["Edge"]
        CF["CloudFront + WAF 🔒1"]
    end

    subgraph WEB["Frontend hosting"]
        S3W["S3 (static Vue SPA)<br/>private, OAC 🔒2"]
    end

    subgraph VPC["VPC (private subnets)"]
        ALB["ALB (TLS 1.2+) 🔒3"]
        subgraph API["Backend: ASP.NET Core API (ECS Fargate)"]
            AUTH["Auth module<br/>JWT validation 🔒4"]
            APP["Applicants module<br/>SSN value object 🔒5"]
            CREDIT["Credit module<br/>bureau clients"]
            EMP["Employment module"]
            DOC["Documents module<br/>presigned URLs 🔒6"]
            AUD["Audit module 🔒7"]
            RBAC["RBAC policies 🔒8"]
        end
        RDS[("RDS SQL Server<br/>encrypted, KMS 🔒9")]
        SM["Secrets Manager<br/>bureau tokens, DB creds 🔒10"]
        VPCE["VPC endpoints<br/>(S3, Secrets Manager) 🔒11"]
    end

    COG["Cognito (OIDC IdP)<br/>MFA, JWT issuer 🔒4"]
    EQ["Equifax API<br/>HTTPS, bearer token"]
    TU["TransUnion API<br/>HTTPS, bearer token"]
    S3D[("S3 bucket: documents<br/>SSE-KMS, block public access 🔒12")]
    LMB["Lambda: PdfProcessor<br/>validate + scan 🔒13"]
    LOG["CloudWatch / CloudTrail<br/>audit logs, no PII 🔒7"]

    U -->|HTTPS| CF
    CF --> S3W
    U -->|login| COG
    S3W -->|SPA calls with JWT| CF
    CF -->|/api/*| ALB
    ALB --> API
    API -->|validate tokens| COG
    API --> RDS
    API -->|fetch secret via IAM role 🔒10| SM
    API -->|via VPC endpoint| VPCE
    VPCE --> S3D
    VPCE --> SM
    API -->|HTTPS + token from SM| EQ
    API -->|HTTPS + token from SM| TU
    API -->|PutObject / presigned GET| S3D
    S3D -->|ObjectCreated event| LMB
    LMB -->|status update| API
    API --> LOG
    LMB --> LOG
```

### Notes
- Bureau calls leave the VPC through a NAT gateway with an egress allowlist 🔒14.
- The API runs under an IAM task role. No access keys are stored in code, config, or images 🔒10.
- The SSN is sent to bureaus only in the request body over TLS. It never appears in URLs, logs, or the browser 🔒5.

## 2. Localhost architecture

The same flow runs on a developer machine. External services are replaced with local emulators.

```mermaid
flowchart LR
    U["👤 Browser<br/>http://localhost:5173"]
    VITE["Vue (Vite dev server) :5173"]
    API["ASP.NET Core API :5000<br/>JWT (dev issuer) 🔒4 · RBAC 🔒8"]
    SQL[("SQL Server container :1433<br/>SSN encrypted 🔒5")]
    LS["LocalStack :4566<br/>S3 · Secrets Manager · Lambda · KMS"]
    BM["WireMock bureau-mock :8080<br/>Equifax + TransUnion stubs"]

    U --> VITE
    VITE -->|/api proxy| API
    API --> SQL
    API -->|secrets via AWS SDK endpoint override 🔒10| LS
    API -->|S3 PutObject / presigned GET| LS
    LS -->|ObjectCreated| LMB["Lambda PdfProcessor (LocalStack) 🔒13"]
    API -->|HTTP stub, bearer token| BM
```

## 3. Deploy flow

```mermaid
flowchart LR
    DEV["Developer<br/>feature branch"] --> PR["Pull request"]
    PR --> REV{"Code review 🔒15<br/>CODEOWNERS, 2 approvals"}
    REV --> CI

    subgraph CI["CI pipeline (GitHub Actions)"]
        B["Build + unit tests"]
        SAST["SAST (CodeQL) 🔒16"]
        DEP["Dependency + secret scan 🔒17"]
        IMG["Container + IaC scan 🔒18"]
        B --> SAST --> DEP --> IMG
    end

    CI --> MERGE["Merge to main"]
    MERGE --> DEVENV["Dev environment<br/>auto-deploy (OIDC role) 🔒19"]
    DEVENV --> E2E["Integration + E2E tests"]
    E2E --> QA["QA environment<br/>DAST + UAT 🔒20"]
    QA --> APPROVE{"Manual approval 🔒21"}
    APPROVE --> PROD["Prod environment<br/>rolling / blue-green, smoke tests, rollback"]
```

Each environment has its own AWS account, its own Secrets Manager entries, and its own KMS key. GitHub Actions assumes an OIDC role per environment, so no long-lived AWS keys are stored in GitHub 🔒19.

## 4. Security checkpoints

| # | Control | Where it is enforced |
|---|---|---|
| 1 | WAF rules, TLS at the edge | CloudFront + WAF |
| 2 | Private S3 bucket, origin access control | S3 static hosting |
| 3 | TLS 1.2+, HTTPS redirect, HSTS | ALB + API middleware |
| 4 | JWT validation (issuer, audience, expiry), MFA at IdP | API auth module, Cognito |
| 5 | SSN validation, masking, encryption at rest | `Ssn` value object, `SsnMasker`, data layer |
| 6 | Short-lived presigned URLs (≤5 min) | Documents module |
| 7 | Audit log, PII-free logging | Audit module, Serilog masking |
| 8 | Role-based authorization policies | Controllers and policies |
| 9 | Encrypted database, KMS key | RDS |
| 10 | Secrets from Secrets Manager via IAM role, no secrets in code | `ISecretProvider` |
| 11 | Private networking, VPC endpoints | VPC |
| 12 | Block public access, SSE-KMS | S3 documents bucket |
| 13 | File type and size validation, PDF scan | Lambda PdfProcessor |
| 14 | Egress allowlist to bureau hosts | NAT / security groups |
| 15 | Required PR review, CODEOWNERS | GitHub branch protection |
| 16 | SAST (CodeQL) | CI |
| 17 | Dependency vulnerability and secret scanning | CI |
| 18 | Container and IaC scanning | CI |
| 19 | OIDC federation, least-privilege deploy role | Deploy pipeline |
| 20 | DAST and security sign-off | QA environment |
| 21 | Manual approval gate | Prod deploy |

## 5. Component diagram

Components as implemented in `web/`, `src/Api`, `src/Lambda/PdfProcessor`, and `src/Shared/AppInterview.Pdf`. Arrows point from the caller to the component it uses. The dashed box is planned, not built.

```mermaid
flowchart LR
    subgraph FE["Frontend: web/ (Vue 3)"]
        APP["App.vue<br/>navigation, sign out"]
        ROUTER["router/index.ts<br/>route guards by role"]
        AUTHSTORE["stores/auth.ts<br/>in-memory session"]
        APICLIENT["api/client.ts<br/>bearer token, ApiError"]
        VLOGIN["LoginView"]
        VNEW["NewApplicantView"]
        VDETAIL["ApplicantDetailView"]
        VAUDIT["AuditView"]
        SSNUI["utils/ssn.ts<br/>client-side SSN rules"]
    end

    subgraph API["Backend: src/Api (ASP.NET Core)"]
        JWT["AddJwtAuthentication<br/>JWT bearer validation"]
        POL["Policies and Roles<br/>AddAppAuthorization"]
        subgraph CTRL["Controllers"]
            CAUTH["AuthController<br/>dev sign-in"]
            CAPP["ApplicantsController"]
            CCR["CreditReportsController"]
            CDOC["DocumentsController"]
            CAUD["AuditController"]
        end
        subgraph SEC["Security"]
            SSNVO["Ssn value object<br/>SsnValidator, SsnMasker"]
            SSNJSON["SsnJsonConverter<br/>writes masked only"]
            SSNPROT["SsnProtector<br/>Data Protection"]
        end
        TOK["TokenService<br/>issues HS256 JWT"]
        subgraph SVC["Services"]
            APPSVC["ApplicantService"]
            BCS["BureauCheckService"]
            DOCSVC["DocumentService"]
            AUDLOG["AuditLogger"]
        end
        subgraph INT["Integrations"]
            ICB["ICreditBureauClient"]
            CBB["CreditBureauClientBase<br/>token, HTTP, error mapping"]
            EQ["EquifaxClient"]
            TU["TransUnionClient"]
            ISP["ISecretProvider"]
            CACHE["CachedSecretProvider<br/>5-minute TTL"]
            CFG["ConfigSecretProvider<br/>development"]
            ASM["AwsSecretsManagerProvider"]
        end
        subgraph STO["Storage"]
            IOS["IObjectStorage"]
            S3S["S3ObjectStorage<br/>SSE-KMS, presigned links"]
        end
        subgraph DATA["Data"]
            DB["AppDbContext<br/>EF Core"]
            SEED["DevDataSeeder<br/>migrations, dev users"]
        end
        ERR["BureauExceptionHandler<br/>502 ProblemDetails"]
        PDFR["AppInterview.Pdf<br/>PdfRules"]
    end

    subgraph LAM["Lambda: src/Lambda/PdfProcessor"]
        FN["Function.HandleAsync"]
        IOST["IObjectStore"]
        S3OS["S3ObjectStore"]
    end

    subgraph EXT["External systems"]
        SQL[("SQL Server")]
        S3[("S3 bucket<br/>documents")]
        SM["Secrets Manager"]
        EQAPI["Equifax API"]
        TUAPI["TransUnion API"]
        LMB["Lambda runtime"]
        IDP["Cognito / OIDC"]
    end

    APP --> ROUTER
    ROUTER --> VLOGIN
    ROUTER --> VNEW
    ROUTER --> VDETAIL
    ROUTER --> VAUDIT
    ROUTER --> AUTHSTORE
    VNEW --> SSNUI
    VLOGIN --> APICLIENT
    VNEW --> APICLIENT
    VDETAIL --> APICLIENT
    VAUDIT --> APICLIENT
    APICLIENT --> AUTHSTORE

    APICLIENT -->|"HTTPS /api"| JWT
    JWT --> POL
    POL --> CAPP
    POL --> CCR
    POL --> CDOC
    POL --> CAUD
    JWT --> CAUTH
    CAUTH --> TOK
    CAUTH --> DB
    CAPP --> APPSVC
    CAPP --> BCS
    CAPP --> AUDLOG
    CAPP -.->|"JSON"| SSNJSON
    CCR --> BCS
    CCR --> DB
    CCR --> AUDLOG
    CDOC --> DOCSVC
    CDOC --> AUDLOG
    CDOC --> PDFR
    CAUD --> DB

    APPSVC --> SSNPROT
    APPSVC --> SSNVO
    APPSVC --> DB
    BCS --> APPSVC
    BCS --> ICB
    BCS --> DB
    SSNJSON --> SSNVO
    SSNPROT --> SSNVO
    DOCSVC --> IOS
    DOCSVC --> DB
    AUDLOG --> DB

    ICB -.->|"implemented by"| CBB
    EQ -->|"extends"| CBB
    TU -->|"extends"| CBB
    CBB --> ISP
    CBB -.->|"bureau errors"| ERR
    EQ --> EQAPI
    TU --> TUAPI
    ISP -.->|"implemented by"| CACHE
    CACHE --> CFG
    CACHE --> ASM
    ASM --> SM
    IOS -.->|"implemented by"| S3S
    S3S --> S3
    DB --> SQL
    SEED --> DB

    S3 -->|"ObjectCreated"| LMB
    LMB --> FN
    FN --> IOST
    FN --> PDFR
    IOST -.->|"implemented by"| S3OS
    S3OS --> S3
    CAUTH -.->|"planned: replace dev sign-in"| IDP
    JWT -.->|"planned: validate Cognito tokens"| IDP

    classDef planned stroke-dasharray: 5 5
    class IDP planned
```
