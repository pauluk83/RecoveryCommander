namespace RecoveryCommander.Core;

public static class AppMetadata
{
    public const string ProductName = "RecoveryCommander";
    public const string Version = "1.2.9";
    public const string ReleaseTag = "v1.2.9";

    public static readonly string[] Modules =
    {
        "DISM",
        "REAgentc",
        "System File Checker",
        "Diagnostics",
        "Utilities",
        "System Prep",
        "Malware Removal",
        "Driver Manager",
        "Cloud Recovery"
    };

    public static readonly string[] Tools =
    {
        "Restore Point Manager",
        "Startup Manager",
        "Network Repair & Optimization",
        "Media Tools",
        "Module Builder"
    };
}
