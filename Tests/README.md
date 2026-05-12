# RecoveryCommander Tests

Unit tests for the Core layer of RecoveryCommander, introduced in v1.3.0.

## What's covered

| File | Surface under test |
|------|--------------------|
| `SecurityHelpersTests.cs` | URL validation, file-name validation, extension allow-list, command-arg sanitization, process/PowerShell argument escaping |
| `DownloadCatalogTests.cs` | Catalog lookup (case-insensitive), uniqueness of ids, HTTPS-only invariant, safe filenames |
| `ModuleActionTests.cs` | `ModuleAction` defaults, display-name fallback, cancellation propagation through `ExecuteAction` |
| `AppPathsTests.cs` | LocalAppData rooting, daily log filename format |
| `AsyncHelpersTests.cs` | `DownloadFileAsync` rejects http://, localhost, and pre-cancelled tokens without writing files |

## Running

```bash
dotnet test RecoveryCommander.sln --configuration Release
```

CI runs the same command in `.github/workflows/dotnet-desktop.yml` (Release matrix only — Debug skips tests to keep PR feedback fast). Coverage is collected via the `coverlet.collector` package and uploaded as the `test-results` artifact.

## Adding a test

1. Drop a new `.cs` file under `Tests/RecoveryCommander.Tests/`.
2. xUnit auto-discovers any `[Fact]` / `[Theory]` method.
3. Prefer `FluentAssertions` over `Assert.*` for readable failure messages.
4. Use `Moq` for collaborator boundaries (e.g. `IDialogService`, `ILogger`).
5. Keep tests fast and offline. Network/filesystem-bound paths should be exercised with the smallest possible setup, never with real production URLs.

## Out of scope (for now)

- Forms-level UI tests. WinForms is not test-friendly without an interactive desktop session; we instead exercise the headless Core surface and trust manual smoke checks for the UI.
- Process-launch end-to-end tests. We rely on the security-helper unit tests to guarantee the inputs to `Process.Start` are sanitized.
