using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RecoveryCommander.Core;
using Xunit;

namespace RecoveryCommander.Tests;

public class AsyncHelpersTests : IDisposable
{
    public AsyncHelpersTests()
    {
        // AsyncHelpers.DownloadFileAsync resolves HttpClient via ServiceContainer.
        ServiceContainer.Initialize();
    }

    public void Dispose()
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try { ServiceContainer.Dispose(); } catch { /* Suppress exceptions during cleanup */ }
#pragma warning restore CA1031 // Do not catch general exception types
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task DownloadFileAsync_BailsOutOnPreCancelledToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var act = async () => await AsyncHelpers.DownloadFileAsync("https://example.com/x.exe", temp, null, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            File.Exists(temp).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
