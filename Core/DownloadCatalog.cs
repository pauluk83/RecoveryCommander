/*
 * AUDIT HEADER
 * File: DownloadCatalog.cs
 * Module: Core / Supply Chain
 * Created: 2026-05-02
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-05-02 - 1.3.0 - Centralized third-party download URLs into a single catalog.
 *                       Each entry carries optional SHA-256 + version metadata so we can
 *                       gradually move from "trust the URL" to "verify before execute".
 *                       Modules look entries up by id and download via DownloadVerifiedAsync,
 *                       which logs a clear warning whenever Sha256 is null.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.Core
{
    /// <summary>
    /// A single third-party artifact we may download and run.
    /// </summary>
    /// <param name="Id">Stable identifier used by callers (e.g. "Utilities.CCleaner").</param>
    /// <param name="Url">HTTPS URL or `.txt` indirection file (resolved by AsyncHelpers).</param>
    /// <param name="FileName">Local filename to save as.</param>
    /// <param name="Sha256">Optional hex SHA-256. When set, the download is verified before execute.</param>
    /// <param name="Version">Vendor-declared version string (advisory).</param>
    /// <param name="Vendor">Human-friendly vendor name (advisory).</param>
    public sealed record DownloadEntry(
        string Id,
        string Url,
        string FileName,
        string? Sha256 = null,
        string? Version = null,
        string? Vendor = null);

    /// <summary>
    /// Central registry of every third-party download the app may fetch.
    ///
    /// Supply-chain policy (v1.3.0):
    /// - Every entry SHOULD carry a SHA-256 once a stable artifact version is pinned.
    /// - Entries with Sha256 == null log a runtime warning. They are still allowed to run
    ///   for now to avoid breaking the user-facing toolkit, but the warning makes the gap
    ///   visible during operation and in audit logs.
    /// - URLs MUST be HTTPS. AsyncHelpers.ResolveDownloadUrlAsync will reject http://.
    /// - When updating a vendor binary, bump Version + Sha256 together. Never replace a URL
    ///   without re-pinning the hash.
    /// </summary>
    public static class DownloadCatalog
    {
        private static readonly Dictionary<string, DownloadEntry> _entries = new(StringComparer.OrdinalIgnoreCase)
        {
            // ===== Utilities Module =====
            ["Utilities.CompactGUI"] = new("Utilities.CompactGUI",
                "https://github.com/IridiumIO/CompactGUI/releases/latest/download/CompactGUI.exe",
                "CompactGUI.exe", Vendor: "IridiumIO"),
            ["Utilities.CCleaner"] = new("Utilities.CCleaner",
                "https://www.dropbox.com/scl/fi/7op61jbtwy3nc50i3qu0v/CCleaner-6.40.115.62.txt?rlkey=4vel6tocnd3hmpucb1lsmu8s9&st=l1ox1unw&raw=1",
                "CCleaner 6.40.115.62.exe", Sha256: "13d3834e5951695178d1a2742ffc3cbaa08dd6222931555872a8baee9d1b9e62", Version: "6.40.115.62", Vendor: "CCleaner"),
            ["Utilities.Defragger"] = new("Utilities.Defragger",
                "https://drive.google.com/uc?export=download&id=1y-kGi-voJGMaT0KP8nzJ6Y5nuM9rIj4l",
                "Defragger.exe"),
            ["Utilities.Ninite"] = new("Utilities.Ninite",
                "https://drive.google.com/uc?export=download&id=1qIF8HXRBi7fdxI-ryOxJwG5UioROfMgx",
                "Ninite.exe", Vendor: "Ninite"),
            ["Utilities.RufusReleaseApi"] = new("Utilities.RufusReleaseApi",
                "https://api.github.com/repos/pbatard/rufus/releases/latest",
                "rufus-latest.json", Vendor: "pbatard"),
            ["Utilities.MacriumPortable"] = new("Utilities.MacriumPortable",
                "https://www.dropbox.com/scl/fi/qj5pa6ykrara7jcb8m3k1/Macrium.txt?rlkey=9gl0l7pgsuniits2gmovb44oi&st=escodys6&raw=1",
                "Macrium Reflect X 10.0.8843.exe", Sha256: "b530583e8614b273823622fb85cd128f0d9afc5f8f479944a9b66000bc88ffe4", Version: "10.0.8843", Vendor: "Macrium"),
            ["Utilities.Win11DebloatZip"] = new("Utilities.Win11DebloatZip",
                "https://github.com/Raphire/Win11Debloat/archive/refs/heads/master.zip",
                "Win11Debloat.zip", Vendor: "Raphire"),
            ["Utilities.VCRedistApi"] = new("Utilities.VCRedistApi",
                "https://api.github.com/repos/abbodi1406/vcredist/releases/latest",
                "vcredist-latest.json", Vendor: "abbodi1406"),
            ["Utilities.PCRepairSuite"] = new("Utilities.PCRepairSuite",
                "https://www.dropbox.com/scl/fi/77a4s205yz7qjocfmev9r/PCRepairSuite.txt?rlkey=us4dw96qvw0bpqhemrghs5ru8&st=q6d07jhd&raw=1",
                "PC Repair Suite 2.0.0.exe", Sha256: "c1aab710605008b8985ed47ead330bdfb12d83d71edb1b77254f5afac6d00dca", Version: "2.0.0"),
            ["Utilities.IObitDriverBooster"] = new("Utilities.IObitDriverBooster",
                "https://www.dropbox.com/scl/fi/2paq4t1yevyprkw5jrp0a/DriverBoosterPortable.txt?rlkey=p6j6ofauo26tp5xnunqhc3pxb&st=bvyegici&raw=1",
                "Driver Booster PRO 13.4.0.234.exe", Sha256: "fe3e74163f15ea89f5f540ed936b58fd90d4ab8c04acbb6045bfddb67cd9b364", Version: "13.4.0.234", Vendor: "IObit"),
            ["Utilities.DellOSRecoveryTool"] = new("Utilities.DellOSRecoveryTool",
                "https://www.dropbox.com/scl/fi/n7e76lxvy7r157bifklbl/Dell-OS-Recovery-Toolv2.3.4.3569.txt?rlkey=m5ir6s1ahh77yll82iyenj6j5&raw=1",
                "Dell OS Recovery Tool 2.3.4.3569.exe", Sha256: "530ad2c493958fdd7761c9dd8a8f666ad15e7f9130fbd9b82d8fd410d0666bfd", Version: "2.3.4.3569", Vendor: "Dell"),
            ["Utilities.Office2024"] = new("Utilities.Office2024",
                "https://www.dropbox.com/scl/fi/i6ds50lst2edbuc2ildlv/office2024..txt?rlkey=dbhl2bo5y3tj5a7sfjnadeu9y&st=oesmjnyr&raw=1",
                "Office 2024 (Build 2024).ps1", Sha256: "6de58f6f6ce4f5097b4613b858a6ee997a4c5007025a61d37663808789f978f4", Vendor: "Microsoft"),
            ["Utilities.ActivationPublic"] = new("Utilities.ActivationPublic",
                "https://get.activated.win", "Activate.ps1"),
            ["Utilities.OfficeC2RPublic"] = new("Utilities.OfficeC2RPublic",
                "https://c2rsetup.officeapps.live.com/c2r/download.aspx?ProductreleaseID=O365ProPlusRetail&platform=x64&language=en-us&version=O16GA",
                "OfficeSetup.exe", Vendor: "Microsoft"),
            ["Utilities.BackupRestoreActivation"] = new("Utilities.BackupRestoreActivation",
                "https://www.dropbox.com/scl/fi/y3a8j6osto9rpip7buveh/Backup-Activation.txt?rlkey=dd1ms4f8wjz3iav9g13ewnzbw&st=6ex79f52&raw=1",
                "Backup and Restore Activation State 1.0.0.bat", Sha256: "d47a63b5135008949f847e78141333ef62269aaa7f4ee849cd976c1641552003", Version: "1.0.0"),
            ["Utilities.CleanMyPc"] = new("Utilities.CleanMyPc",
                "https://www.dropbox.com/scl/fi/bowosjzxmkr16qw5zcfnc/CleanMyPC.txt?rlkey=sz2tafebh67sp6aod6hxjd0pp&st=2be8vsyw&raw=1",
                "MacPaw CleanMyPC 1.11.1.2079.exe", Sha256: "1b9662dda477e160903f832af87a01ccf8a87325212cc4f09b45f8a0954721f5", Version: "1.11.1.2079", Vendor: "MacPaw"),
            ["Utilities.ChrisTitusUtility"] = new("Utilities.ChrisTitusUtility",
                "https://christitus.com/win", "Christitus.ps1", Vendor: "Chris Titus Tech"),
            ["Utilities.EaseUSPartitionMaster"] = new("Utilities.EaseUSPartitionMaster",
                "https://www.dropbox.com/scl/fi/dzuyng0lf4shqabcyrcwj/EaseUS-Partition-Master-v20.3.0-Build-202604081519.txt?rlkey=hqssm9cvt6qx7mv8w9brdliza&st=qpojqmee&raw=1",
                "EaseUS Partition Master 20.3.0 Build 202604081519.exe", Sha256: "f47560eab86071f27a7b0e36e200580d0aba334ad76f0c511fe988b38fc9fbb1", Version: "20.3.0", Vendor: "EaseUS"),
            ["Utilities.UniGetUI"] = new("Utilities.UniGetUI",
                "https://github.com/Devolutions/UniGetUI/releases/download/v2026.1.9/UniGetUI.x64.zip",
                "UniGetUI.x64.zip", Version: "2026.1.9", Vendor: "Devolutions"),

            // ===== Driver Manager Module =====
            // (Aliased to UtilitiesModule entry to avoid drift.)

            // ===== Malware Removal Module =====
            ["Malware.EmsisoftEmergencyKit"] = new("Malware.EmsisoftEmergencyKit",
                "https://dl.emsisoft.com/EmsisoftEmergencyKit.exe", "EmsisoftEmergencyKit.exe", Vendor: "Emsisoft"),
            ["Malware.Kvrt"] = new("Malware.Kvrt",
                "https://devbuilds.s.kaspersky-labs.com/devbuilds/KVRT/latest/full/KVRT.exe", "KVRT.exe", Vendor: "Kaspersky"),
            ["Malware.AdwCleaner"] = new("Malware.AdwCleaner",
                "https://downloads.malwarebytes.com/file/adwcleaner", "adwcleaner.exe", Vendor: "Malwarebytes"),
            ["Malware.MicrosoftMsrt"] = new("Malware.MicrosoftMsrt",
                "https://go.microsoft.com/fwlink/?LinkId=212732", "Windows-KB890830-x64.exe", Vendor: "Microsoft"),
            ["Malware.EsetOnlineScanner"] = new("Malware.EsetOnlineScanner",
                "https://download.eset.com/com/eset/tools/online_scanner/latest/esetonlinescanner.exe", "esetonlinescanner.exe", Vendor: "ESET"),
            ["Malware.NortonPowerEraser"] = new("Malware.NortonPowerEraser",
                "https://www.norton.com/npe_latest", "NPE.exe", Vendor: "Norton"),
            ["Malware.HitmanPro"] = new("Malware.HitmanPro",
                "https://dl.surfright.nl/HitmanPro_x64.exe", "HitmanPro_x64.exe", Vendor: "HitmanPro"),
            ["Malware.ClamWinPortable"] = new("Malware.ClamWinPortable",
                "https://portableapps.com/redir2/?a=ClamWinPortable&s=s&d=pa&f=ClamWinPortable_0.103.2.1_Build_1.4.3_English.paf.exe",
                "ClamWinPortable.paf.exe", Vendor: "ClamWin"),
            ["Malware.FSecureOnlineScanner"] = new("Malware.FSecureOnlineScanner",
                "https://download.sp.f-secure.com/tools/F-SecureOnlineScanner.exe", "F-SecureOnlineScanner.exe", Vendor: "F-Secure"),
            ["Malware.TrendMicroHouseCall"] = new("Malware.TrendMicroHouseCall",
                "https://go.trendmicro.com/housecall8/r2/HousecallLauncher64.exe", "HousecallLauncher64.exe", Vendor: "Trend Micro"),
            ["Malware.SophosScanAndClean"] = new("Malware.SophosScanAndClean",
                "https://s.home.sophos.com/download/SophosInstall.exe", "SophosInstall.exe", Vendor: "Sophos"),
            ["Malware.ComodoCleaningEssentials"] = new("Malware.ComodoCleaningEssentials",
                "https://download.comodo.com/cce/download/setups/cce_public_x64.zip", "cce_public_x64.zip", Vendor: "Comodo"),
            ["Malware.DrWebCureIt"] = new("Malware.DrWebCureIt",
                "https://download.geo.drweb.com/pub/drweb/cureit/setup.exe", "CureIt.exe", Vendor: "Dr.Web"),
            ["Malware.SuperAntiSpyware"] = new("Malware.SuperAntiSpyware",
                "https://secure.superantispyware.com/SUPERAntiSpyware.exe", "SUPERAntiSpyware.exe", Vendor: "SuperAntiSpyware"),
        };

        /// <summary>
        /// Look up a download by id. Returns null when the id is unknown.
        /// </summary>
        public static DownloadEntry? Find(string id)
            => _entries.TryGetValue(id, out var entry) ? entry : null;

        /// <summary>
        /// Look up a download by id and throw a clear exception if missing. For call sites
        /// that want a hard failure rather than a silent null.
        /// </summary>
        public static DownloadEntry Get(string id)
            => Find(id) ?? throw new KeyNotFoundException($"DownloadCatalog has no entry for id '{id}'.");

        /// <summary>
        /// All registered entries. Useful for tests + diagnostic dialogs.
        /// </summary>
        public static IReadOnlyCollection<DownloadEntry> All => _entries.Values;

        /// <summary>
        /// Resolve a catalog entry, log a clear warning if it is missing a SHA-256 hash, and then
        /// download + execute via the standard secure path. Equivalent to the previous
        /// AsyncHelpers.DownloadAndExecuteAsync(url, fileName, ...) but with one place to add
        /// integrity checks later.
        /// </summary>
        public static Task DownloadAndExecuteFromCatalogAsync(
            string id,
            IProgress<ProgressReport> progress,
            Action<string> reportOutput,
            CancellationToken cancellationToken,
            string[]? allowedExtensions = null)
        {
            var entry = Get(id);
            if (entry.Sha256 is null)
            {
                reportOutput?.Invoke($"[supply-chain] WARN: '{entry.Id}' is unverified (no SHA-256 pinned). Proceeding by URL trust.");
            }
            return AsyncHelpers.DownloadAndExecuteAsync(
                entry.Url,
                entry.FileName,
                progress,
                reportOutput,
                cancellationToken,
                allowedExtensions: allowedExtensions,
                expectedHash: entry.Sha256);
        }

        /// <summary>
        /// Download (without executing) a catalog entry to a specific path, verifying the
        /// SHA-256 if one is registered. Emits a clear log line either way.
        /// </summary>
        public static async Task DownloadVerifiedAsync(
            string id,
            string destinationPath,
            IProgress<ProgressReport> progress,
            Action<string> reportOutput,
            CancellationToken cancellationToken)
        {
            var entry = Get(id);
            if (entry.Sha256 is null)
            {
                reportOutput?.Invoke($"[supply-chain] WARN: '{entry.Id}' is unverified (no SHA-256 pinned). Proceeding by URL trust.");
            }
            else
            {
                reportOutput?.Invoke($"[supply-chain] verifying '{entry.Id}' (SHA-256 pinned).");
            }
            await AsyncHelpers.DownloadFileAsync(entry.Url, destinationPath, progress, cancellationToken, entry.Sha256);
        }
    }
}
