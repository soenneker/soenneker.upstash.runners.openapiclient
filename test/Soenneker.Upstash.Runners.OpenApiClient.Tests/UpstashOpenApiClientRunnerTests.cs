using System;
using System.Threading.Tasks;
using Soenneker.Tests.HostedUnit;
using Soenneker.Upstash.Runners.OpenApiClient.Utils.Abstract;
using System.Threading;

namespace Soenneker.Upstash.Runners.OpenApiClient.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class UpstashOpenApiClientRunnerTests(Host host) : HostedUnitTest(host)
{
    [Test]
    public async ValueTask Process_rejects_a_target_without_the_client_project(CancellationToken cancellationToken)
    {
        InvalidOperationException? failure = null;
        try
        {
            await Resolve<IFileOperationsUtil>().Process(cancellationToken: cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            failure = exception;
        }
        await Assert.That(failure).IsNotNull();
        await Assert.That(failure!.Message.StartsWith("Target must contain ")).IsTrue();
    }
}
