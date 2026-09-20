using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Soenneker.TestHosts.Unit;
using System.Collections.Generic;
using System.IO;
using Soenneker.Kiota.Util.Registrars;
using Soenneker.Managers.Runners.Registrars;
using Soenneker.OpenApi.Fixer.Registrars;
using Soenneker.Upstash.Runners.OpenApiClient.Utils;
using Soenneker.Upstash.Runners.OpenApiClient.Utils.Abstract;
using Soenneker.Utils.File.Download.Registrars;
using Soenneker.Utils.Yaml.Registrars;

namespace Soenneker.Upstash.Runners.OpenApiClient.Tests;

public sealed class Host : UnitTestHost
{
    public override Task InitializeAsync()
    {
        SetupIoC(Services);

        return base.InitializeAsync();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: false);
        });

        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Upstash:TargetDirectory"] = Path.Combine(Path.GetTempPath(), "upstash-missing-project-test", System.Guid.NewGuid().ToString("N"))
        }).Build();
        services.AddSingleton(config);
        services.AddSingleton<IFileOperationsUtil, FileOperationsUtil>()
            .AddRunnersManagerAsSingleton()
            .AddFileDownloadUtilAsSingleton()
            .AddOpenApiFixerAsSingleton()
            .AddYamlUtilAsSingleton()
            .AddKiotaUtilAsSingleton();
    }
}
