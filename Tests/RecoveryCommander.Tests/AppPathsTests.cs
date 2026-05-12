using System;
using System.IO;
using FluentAssertions;
using RecoveryCommander.Core.Logging;
using Xunit;

namespace RecoveryCommander.Tests;

public class AppPathsTests
{
    [Fact]
    public void RootDirectory_LandsUnderLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        AppPaths.RootDirectory.Should().StartWith(localAppData);
        AppPaths.RootDirectory.Should().EndWith("RecoveryCommander");
    }

    [Fact]
    public void LogsDirectory_NestsBeneathRoot()
    {
        AppPaths.LogsDirectory.Should().StartWith(AppPaths.RootDirectory);
        AppPaths.LogsDirectory.Should().EndWith("logs");
    }

    [Fact]
    public void CurrentLogFile_FormatsAsPrefixDateLog()
    {
        var path = AppPaths.CurrentLogFile("rc-");
        var name = Path.GetFileName(path);
        name.Should().StartWith("rc-");
        name.Should().EndWith(".log");
        // 8 digits between prefix and ".log".
        name.Length.Should().Be("rc-".Length + 8 + ".log".Length);
    }
}
