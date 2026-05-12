// AUDIT NOTE (2026-05-02 / v1.3.0):
// The SystemUtilities.ErrorHandler class previously defined here was duplicated by an identical
// nested class inside UI/Theme.cs and had zero call sites in the codebase. Both copies were
// removed during the v1.3.0 hardening pass. Use Microsoft.Extensions.Logging.ILogger for
// structured logging and Forms-layer try/catch + MessageBox patterns where user feedback is
// required. This file is intentionally left empty rather than deleted to preserve git history.

namespace RecoveryCommander.UI
{
}
