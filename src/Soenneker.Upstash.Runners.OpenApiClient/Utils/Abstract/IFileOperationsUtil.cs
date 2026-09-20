using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Upstash.Runners.OpenApiClient.Utils.Abstract;

/// <summary>Regenerates and validates the Upstash Developer API client from its official specification.</summary>
public interface IFileOperationsUtil
{
    /// <summary>
    /// Generates and builds in a temporary directory before replacing generated source.
    /// Upstash:TargetDirectory selects an existing local client repository; otherwise a fresh clone is used.
    /// Upstash:Push explicitly enables publishing from a fresh clone and requires Git credentials.
    /// Throws if generation or compilation fails.
    /// </summary>
    ValueTask Process(CancellationToken cancellationToken = default);
}
