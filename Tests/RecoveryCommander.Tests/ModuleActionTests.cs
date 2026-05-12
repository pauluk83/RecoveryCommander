using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RecoveryCommander.Contracts;
using Xunit;

namespace RecoveryCommander.Tests;

public class ModuleActionTests
{
    [Fact]
    public void DefaultsAreSensibleForNewAction()
    {
        var action = new ModuleAction("Run Scan", "Run Scan");
        action.Name.Should().Be("Run Scan");
        action.DisplayName.Should().Be("Run Scan");
        action.IsAsync.Should().BeTrue();
        action.RequiresAdmin.Should().BeFalse();
        action.IsDestructive.Should().BeFalse();
        action.Highlight.Should().BeFalse();
        action.IsHeader.Should().BeFalse();
    }

    [Fact]
    public void DisplayNameFallsBackToName()
    {
        var action = new ModuleAction("ScanOnly");
        action.DisplayName.Should().Be("ScanOnly");
    }

    [Fact]
    public async Task ExecuteAction_WhenSet_RunsAndCompletes()
    {
        var executed = false;
        var action = new ModuleAction("X", "X", async (p, o, c) =>
        {
            await Task.Yield();
            executed = true;
        });

        await action.ExecuteAction!(new Progress<ProgressReport>(_ => { }), _ => { }, CancellationToken.None);
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAction_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = new ModuleAction("X", "X", async (p, o, c) =>
        {
            c.ThrowIfCancellationRequested();
            await Task.Yield();
        });

        var act = async () => await action.ExecuteAction!(new Progress<ProgressReport>(_ => { }), _ => { }, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
