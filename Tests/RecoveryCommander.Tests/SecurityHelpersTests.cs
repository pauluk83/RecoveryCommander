using FluentAssertions;
using RecoveryCommander.Core;
using Xunit;

namespace RecoveryCommander.Tests;

public class SecurityHelpersTests
{
    [Theory]
    [InlineData("https://github.com/owner/repo/releases/latest", true)]
    [InlineData("https://example.com/file.exe", true)]
    [InlineData("http://example.com/file.exe", false)]   // http rejected
    [InlineData("ftp://example.com/file.exe", false)]    // non-http schemes rejected
    [InlineData("https://localhost/file.exe", false)]    // SSRF guard
    [InlineData("https://127.0.0.1/file.exe", false)]
    [InlineData("https://192.168.1.1/file.exe", false)]
    [InlineData("https://10.0.0.5/file.exe", false)]
    [InlineData("https://169.254.0.1/file.exe", false)]
    [InlineData("https://172.16.0.1/file.exe", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    public void IsValidDownloadUrl_AcceptsHttpsAndRejectsLocal(string url, bool expected)
    {
        var ok = SecurityHelpers.IsValidDownloadUrl(url, out var uri);
        ok.Should().Be(expected);
        if (expected) uri.Should().NotBeNull();
        else uri.Should().BeNull();
    }

    [Theory]
    [InlineData("file.exe", true)]
    [InlineData("My File 2.0.0.exe", true)]
    [InlineData("setup.msi", true)]
    [InlineData("", false)]
    [InlineData("..\\evil.exe", false)]      // path traversal
    [InlineData("../evil.exe", false)]
    [InlineData("CON", false)]               // reserved Windows name
    [InlineData("PRN.txt", false)]
    [InlineData("file<>.exe", true)]         // sanitized: invalid chars stripped
    public void IsValidFileName_BlocksTraversalAndReservedNames(string name, bool expected)
    {
        var ok = SecurityHelpers.IsValidFileName(name, out var sanitized);
        ok.Should().Be(expected);
        if (expected)
        {
            sanitized.Should().NotBeNullOrWhiteSpace();
            sanitized.Should().NotContain("<");
            sanitized.Should().NotContain(">");
        }
    }

    [Theory]
    [InlineData("setup.exe", new[] { "exe", "msi" }, true)]
    [InlineData("setup.MSI", new[] { "exe", "msi" }, true)]
    [InlineData("script.ps1", new[] { "exe", "msi" }, false)]
    [InlineData("noext", new[] { "exe" }, false)]
    [InlineData("", new[] { "exe" }, false)]
    public void IsAllowedFileExtension_HonorsAllowList(string fileName, string[] allowed, bool expected)
    {
        SecurityHelpers.IsAllowedFileExtension(fileName, allowed).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo & bar", "foo  bar")]
    [InlineData("foo | bar", "foo  bar")]
    [InlineData("foo; bar", "foo bar")]
    [InlineData("`whoami`", "whoami")]
    [InlineData("normal value", "normal value")]
    [InlineData("", "")]
    public void SanitizeCommandArguments_StripsShellMetacharacters(string input, string expected)
    {
        SecurityHelpers.SanitizeCommandArguments(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("with space", "\"with space\"")]
    [InlineData("with\"quote", "\"with\\\"quote\"")]
    [InlineData("", "\"\"")]
    public void EscapeProcessArgument_FollowsCreateProcessRules(string input, string expected)
    {
        SecurityHelpers.EscapeProcessArgument(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("simple", "\"simple\"")]
    [InlineData("c:\\path with spaces\\file.ps1", "\"c:\\path with spaces\\file.ps1\"")]
    [InlineData("has\"quote", "\"has\"\"quote\"")]
    public void EscapePowerShellArgument_DoublesEmbeddedQuotes(string input, string expected)
    {
        SecurityHelpers.EscapePowerShellArgument(input).Should().Be(expected);
    }
}
