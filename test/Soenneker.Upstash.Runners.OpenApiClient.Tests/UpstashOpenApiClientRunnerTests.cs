using System;
using System.Threading.Tasks;
using Soenneker.Tests.HostedUnit;
using Soenneker.Upstash.Runners.OpenApiClient.Utils.Abstract;

namespace Soenneker.Upstash.Runners.OpenApiClient.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class UpstashOpenApiClientRunnerTests(Host host) : HostedUnitTest(host)
{
    [Test]
    public async Task Process_rejects_a_target_without_the_client_project()
    {
        InvalidOperationException? failure = null;
        try
        {
            await Resolve<IFileOperationsUtil>().Process();
        }
        catch (InvalidOperationException exception)
        {
            failure = exception;
        }
        await Assert.That(failure).IsNotNull();
        await Assert.That(failure!.Message.StartsWith("Target must contain ")).IsTrue();
    }
}
