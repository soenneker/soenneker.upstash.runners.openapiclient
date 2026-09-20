# Soenneker.Upstash.Runners.OpenApiClient

Regenerates the .NET 10 client from Upstash's official Developer API specification. It downloads YAML or JSON, normalizes the specification, generates with Kiota, and compiles the result in a temporary directory before replacing generated source. Generation and build failures produce a nonzero process exit code.

## Regenerate locally

From this repository, with the client repository checked out beside it:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Soenneker.Upstash.Runners.OpenApiClient --no-launch-profile -- --Upstash:TargetDirectory=../soenneker.upstash.openapiclient
```

This requires network access to the public specification and NuGet. No Upstash credentials are needed. Kiota is installed/updated as a global .NET tool. The local target must contain the expected client project.

Configuration:

| Key | Behavior |
| --- | --- |
| `Upstash:TargetDirectory` | Existing local client repository. Omit to use a fresh temporary GitHub clone. |
| `Upstash:ClientGenerationUrl` | Optional alternative specification URL. |
| `Upstash:Push` | Defaults to `false`. Set to `true` to commit and push a fresh clone after a successful build. Cannot be combined with a local target. |

Publishing requires `GH__TOKEN`, `GIT__NAME`, and `GIT__EMAIL`. The daily workflow explicitly enables `Upstash__Push`. Local generation does not push or publish packages.

## Build the suite

Check out the client, HTTP client, utility, and runner repositories beside each other, then run:

```powershell
./BuildSuite.ps1
./BuildSuite.ps1 -Test
```

The script builds the four solutions and packs the three libraries into `artifacts/packages`. `-Test` runs the suite's targeted tests through Microsoft Testing Platform. Local project references allow the initial build before the packages exist on NuGet. Publish the client and HTTP client before the utility.
