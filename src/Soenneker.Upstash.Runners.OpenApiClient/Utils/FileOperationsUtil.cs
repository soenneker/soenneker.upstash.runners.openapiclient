using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Soenneker.Git.Util.Abstract;
using Soenneker.Kiota.Util.Abstract;
using Soenneker.OpenApi.Fixer.Abstract;
using Soenneker.Upstash.Runners.OpenApiClient.Utils.Abstract;
using Soenneker.Utils.Dotnet.Abstract;
using Soenneker.Utils.Environment;
using Soenneker.Utils.File.Download.Abstract;
using Soenneker.Utils.Yaml.Abstract;

namespace Soenneker.Upstash.Runners.OpenApiClient.Utils;

public sealed class FileOperationsUtil(
    ILogger<FileOperationsUtil> logger,
    IConfiguration configuration,
    IServiceProvider services,
    IDotnetUtil dotnetUtil,
    IFileDownloadUtil fileDownloadUtil,
    IKiotaUtil kiotaUtil,
    IOpenApiFixer openApiFixer,
    IYamlUtil yamlUtil) : IFileOperationsUtil
{
    public async ValueTask Process(CancellationToken cancellationToken = default)
    {
        string? localDirectory = configuration["Upstash:TargetDirectory"];
        bool push = configuration.GetValue("Upstash:Push", false);
        if (push && !string.IsNullOrWhiteSpace(localDirectory))
            throw new InvalidOperationException("Upstash:Push cannot be used with Upstash:TargetDirectory. Publishing uses a fresh clone.");

        string gitDirectory = string.IsNullOrWhiteSpace(localDirectory)
            ? await services.GetRequiredService<IGitUtil>().CloneToTempDirectory($"https://github.com/soenneker/{Constants.Library.ToLowerInvariant()}", cancellationToken: cancellationToken)
            : Path.GetFullPath(localDirectory);
        string srcDirectory = Path.Combine(gitDirectory, "src", Constants.Library);
        string projectPath = Path.Combine(srcDirectory, $"{Constants.Library}.csproj");
        if (!File.Exists(projectPath))
            throw new InvalidOperationException($"Target must contain {projectPath}.");

        // Generate and compile separately so a failed download, generation, or build preserves the existing client.
        string stagingDirectory = Path.Combine(Path.GetTempPath(), "soenneker-upstash", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingDirectory);
        try
        {
            string documentUrl = configuration["Upstash:ClientGenerationUrl"] ??
                "https://raw.githubusercontent.com/upstash/docs/main/devops/developer-api/openapi.yaml";
            string? documentPath = await fileDownloadUtil.Download(documentUrl, Path.Combine(stagingDirectory, "openapi.json"),
                fileExtension: ".json", cancellationToken: cancellationToken);
            if (documentPath is null)
                throw new InvalidOperationException("Upstash OpenAPI document download failed.");

            string document = await File.ReadAllTextAsync(documentPath, cancellationToken);
            if (!document.TrimStart().StartsWith('{'))
            {
                string convertedPath = Path.Combine(stagingDirectory, "openapi.converted.json");
                await yamlUtil.SaveAsJson(documentPath, convertedPath, cancellationToken: cancellationToken);
                documentPath = convertedPath;
            }

            string fixedPath = Path.Combine(stagingDirectory, "openapi.fixed.json");
            await openApiFixer.Fix(documentPath, fixedPath, cancellationToken);
            await kiotaUtil.EnsureInstalled(cancellationToken);
            await kiotaUtil.Generate(fixedPath, "UpstashOpenApiClient", Constants.Library, stagingDirectory, cancellationToken);

            string generatedDirectory = Path.Combine(stagingDirectory, "src", Constants.Library);
            if (!File.Exists(Path.Combine(generatedDirectory, "UpstashOpenApiClient.cs")))
                throw new InvalidOperationException("Kiota did not generate UpstashOpenApiClient.cs.");

            string stagedProjectPath = Path.Combine(generatedDirectory, $"{Constants.Library}.csproj");
            File.Copy(projectPath, stagedProjectPath);
            await dotnetUtil.Restore(stagedProjectPath, cancellationToken: cancellationToken);
            if (!await dotnetUtil.Build(stagedProjectPath, true, "Release", false, cancellationToken: cancellationToken))
                throw new InvalidOperationException("Generated Upstash client failed to build; existing source was preserved.");

            // The client project contains generated C# only. Keep project files, build output, and metadata intact.
            EnsureNoLinkedDirectories(srcDirectory);
            foreach (string file in Directory.EnumerateFiles(srcDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(srcDirectory, file))
                    continue;
                File.Delete(file);
            }

            foreach (string file in Directory.EnumerateFiles(generatedDirectory, "*", SearchOption.AllDirectories))
            {
                if (IsBuildOutput(generatedDirectory, file) || file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    continue;
                string destination = Path.Combine(srcDirectory, Path.GetRelativePath(generatedDirectory, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, true);
            }
            File.Copy(fixedPath, Path.Combine(gitDirectory, "openapi.fixed.json"), true);

            if (push)
            {
                await services.GetRequiredService<IGitUtil>().CommitAndPush(gitDirectory, "Regenerate Upstash Developer API client",
                    EnvironmentUtil.GetVariableStrict("GH__TOKEN"), EnvironmentUtil.GetVariableStrict("GIT__NAME"),
                    EnvironmentUtil.GetVariableStrict("GIT__EMAIL"), cancellationToken);
            }
            logger.LogInformation("Generated and built Upstash client at {Directory}. Pushed: {Push}", gitDirectory, push);
        }
        finally
        {
            Directory.Delete(stagingDirectory, recursive: true);
        }
    }

    private static bool IsBuildOutput(string root, string file)
    {
        string relative = Path.GetRelativePath(root, file);
        return relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureNoLinkedDirectories(string directory)
    {
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("The generated source directory cannot contain directory links.");
        foreach (string child in Directory.EnumerateDirectories(directory))
            EnsureNoLinkedDirectories(child);
    }
}
