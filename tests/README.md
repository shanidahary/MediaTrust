# MediaTrust automated tests

Run from the solution root with the .NET 10 SDK:

```powershell
dotnet test MediaTrust.sln
dotnet build MediaTrust.sln -c Release
```

Run a single group using its project path, for example:

```powershell
dotnet test tests/MediaTrust.Report.Tests/MediaTrust.Report.Tests.csproj
```

The four xUnit projects reference their corresponding production projects.
Common test settings and pinned test-runner dependencies live in
`tests/Directory.Build.props`. Moq is used only in the Report and Ingest tests.

## Coverage

- **Detectors:** content-integrity scoring for both header states, entropy at and
  above the 7.2 boundary, score bounds, and entropy for empty, constant, and
  uniformly distributed bytes. The production manager calls the extracted
  `ContentIntegrityScoring.CalculateScore` method.
- **Report:** job-specific lookup, isolation between jobs for the same media,
  newest-result selection within a job, AI argument forwarding and response
  mapping, missing-result and AI-failure behavior, media risk-status boundaries,
  maximum-score aggregation, and an empty media report.
- **Orchestrator:** the real repository's latest-job query, including unordered
  jobs, filtering out other media, creation time rather than update time, and
  missing jobs. Each test uses an isolated EF Core in-memory database.
- **Ingest:** duplicate uploads return existing metadata without inserting a
  `MediaItem`, uploading to storage, or creating an HTTP client for another job.
  Also covers filename normalization and invalid/missing/empty uploads.

## Isolation and limits

These tests do not start any application host, load application configuration,
read a Gemini API key, or make network calls. No Docker, PostgreSQL, RabbitMQ,
MinIO, or Gemini instance is needed. NuGet access is needed for the initial
dependency restore unless packages are already cached.

The Orchestrator tests intentionally exercise the repository because that is
where job filtering and ordering live; the manager simply delegates its lookup.
EF Core's in-memory provider does not validate PostgreSQL SQL translation,
constraints, transactions, or migrations.

Not covered here: HTTP routing and serialization, gateway proxying, real database
integration, message delivery/retries, storage integration, the complete detector
worker pipeline and header parsing, new-file ingest orchestration, Gemini HTTP
integration, or React/browser flows. Those require separate integration or
end-to-end suites with appropriately isolated dependencies.
