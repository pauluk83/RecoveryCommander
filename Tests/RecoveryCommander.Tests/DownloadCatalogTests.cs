using System;
using FluentAssertions;
using RecoveryCommander.Core;
using Xunit;

namespace RecoveryCommander.Tests;

public class DownloadCatalogTests
{
    [Fact]
    public void Find_ReturnsNullForUnknownId()
    {
        DownloadCatalog.Find("definitely-not-real").Should().BeNull();
    }

    [Fact]
    public void Get_ThrowsForUnknownId()
    {
        var act = () => DownloadCatalog.Get("definitely-not-real");
        act.Should().Throw<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var lower = DownloadCatalog.Find("utilities.ccleaner");
        var upper = DownloadCatalog.Find("UTILITIES.CCleaner");
        lower.Should().NotBeNull();
        upper.Should().NotBeNull();
        upper!.Url.Should().Be(lower!.Url);
    }

    [Fact]
    public void All_HasNoDuplicateIds()
    {
        var ids = DownloadCatalog.All.Select(e => e.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_OnlyContainsHttpsOrFunctionalUrls()
    {
        // Every entry MUST be https; the ResolveDownloadUrlAsync layer rejects http://.
        foreach (var entry in DownloadCatalog.All)
        {
            entry.Url.Should().StartWith("https://", because: $"entry '{entry.Id}' must be HTTPS");
        }
    }

    [Fact]
    public void All_FileNamesAreNonEmptyAndSafe()
    {
        foreach (var entry in DownloadCatalog.All)
        {
            entry.FileName.Should().NotBeNullOrWhiteSpace(because: $"'{entry.Id}' must declare a FileName");
            entry.FileName.Should().NotContain("..", because: $"'{entry.Id}' must not allow traversal");
            entry.FileName.Should().NotContain("/").And.NotContain("\\");
        }
    }
}
