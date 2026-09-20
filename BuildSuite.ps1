[CmdletBinding()]
param([switch]$Test)

$ErrorActionPreference = 'Stop'
$suiteRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $PSScriptRoot 'artifacts/packages'
$projects = @('OpenApiClient', 'HttpClients', 'OpenApiClientUtil', 'Runners.OpenApiClient')
$classes = @('UpstashOpenApiClientTests', 'UpstashOpenApiHttpClientTests', 'UpstashOpenApiClientUtilTests', 'UpstashOpenApiClientRunnerTests')

for ($index = 0; $index -lt $projects.Count; $index++) {
    $name = 'Soenneker.Upstash.' + $projects[$index]
    $repository = Join-Path $suiteRoot $name.ToLowerInvariant()
    $solution = Join-Path $repository "$name.slnx"
    if (-not (Test-Path -LiteralPath $solution)) { throw "Missing sibling solution: $solution" }
    & dotnet build $solution -c Release -p:UseLocalUpstashProjects=true --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed: $name" }

    if ($Test) {
        $testProject = Join-Path $repository "test/$name.Tests/$name.Tests.csproj"
        # Run from the repository so its global.json selects Microsoft Testing Platform.
        Push-Location $repository
        try {
            & dotnet test --project $testProject -c Release --no-build --no-restore -p:UseLocalUpstashProjects=true -- --treenode-filter "/*/*/$($classes[$index])/*"
            if ($LASTEXITCODE -ne 0) { throw "Tests failed: $name" }
        }
        finally { Pop-Location }
    }

    if ($projects[$index] -ne 'Runners.OpenApiClient') {
        $project = Join-Path $repository "src/$name/$name.csproj"
        & dotnet pack $project -c Release --no-build --no-restore -p:UseLocalUpstashProjects=true -o $outputDirectory --nologo
        if ($LASTEXITCODE -ne 0) { throw "Pack failed: $name" }
    }
}
