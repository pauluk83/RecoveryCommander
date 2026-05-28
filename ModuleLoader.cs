using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RecoveryCommander.Contracts;

namespace RecoveryCommander
{
    /// <summary>
    /// Simple module loader that discovers and loads IRecoveryModule implementations
    /// </summary>
    public static class ModuleLoader
    {
        public static List<IRecoveryModule> LoadModules(Action<string> logger)
        {
            var modules = new List<IRecoveryModule>();
            var loadedModuleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            logger.Invoke("Starting module discovery...");

            // Scan all assemblies loaded in the current AppDomain.
            try
            {
                string[] knownModules = { "DiagnosticsModule", "SFCModule", "DismModule", "ReagentcModule", "MalwareRemovalModule", "SystemPrepModule", "UtilitiesModule", "DriverManagerModule", "CloudRecoveryModule" };
                foreach (var moduleName in knownModules)
                {
                    try
                    {
                        Assembly.Load(moduleName);
                    }
                    catch (Exception ex)
                    {
                        // Failure here is usually because the module assembly is already embedded
                        // in single-file mode (the type warm-up in Program.cs handles that path).
                        // Surface as a debug write so we can diagnose deployment issues without
                        // spamming the user-visible output console.
                        System.Diagnostics.Debug.WriteLine($"[ModuleLoader] Assembly.Load('{moduleName}') failed: {ex.GetType().Name}: {ex.Message}");
                    }
                }

                var builtInTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .Where(t => !t.IsInterface && !t.IsAbstract && typeof(IRecoveryModule).IsAssignableFrom(t))
                    .ToList();

                foreach (var type in builtInTypes)
                {
                    try
                    {
                        var module = (IRecoveryModule)Activator.CreateInstance(type)!;
                        if (loadedModuleNames.Add(module.Name))
                        {
                            modules.Add(module);
                            logger.Invoke($"✓ Loaded module: {module.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Invoke($"✗ Error initializing {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Invoke($"✗ Error during AppDomain scan: {ex.Message}");
            }

            try
            {
                var baseDir = AppContext.BaseDirectory;
                var moduleDir = Path.Combine(baseDir, "Module");
                
                if (Directory.Exists(moduleDir))
                {
                    if (!IsExternalPluginLoadingEnabled())
                    {
                        logger.Invoke("External plugin loading is disabled. Set RC_ENABLE_EXTERNAL_PLUGINS=1 to enable signed plugins.");
                        logger.Invoke($"✓ Plugin system ready. {modules.Count} modules active.");
                        return modules;
                    }

                    // Get all DLL files recursively
                    var allDlls = Directory.GetFiles(moduleDir, "*.dll", SearchOption.AllDirectories);
                    
                    if (allDlls.Length > 0)
                    {
                        logger.Invoke($"Scanning and searching for plugins in: {moduleDir}");
                        foreach (var dllPath in allDlls)
                        {
                            try
                            {
                                if (!IsTrustedPlugin(moduleDir, dllPath, logger))
                                {
                                    continue;
                                }

                                var assembly = Assembly.LoadFrom(dllPath);
                                var moduleTypes = assembly.GetTypes()
                                    .Where(t => !t.IsInterface && !t.IsAbstract && (typeof(IRecoveryModule).IsAssignableFrom(t) || t.GetInterface("IRecoveryModule") != null))
                                    .ToList();

                                foreach (var type in moduleTypes)
                                {
                                    try
                                    {
                                        var module = (IRecoveryModule)Activator.CreateInstance(type)!;
                                        if (loadedModuleNames.Add(module.Name))
                                        {
                                            modules.Add(module);
                                            logger.Invoke($"✓ Loaded plugin: {module.Name} v{module.Version} from {Path.GetFileName(dllPath)}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Invoke($"✗ Error initializing {type.Name} from {Path.GetFileName(dllPath)}: {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.Invoke($"✗ Error loading assembly {Path.GetFileName(dllPath)}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Invoke($"✗ Fatal error in plugin system: {ex.Message}");
            }

            logger.Invoke($"✓ Plugin system ready. {modules.Count} modules active.");
            return modules;
        }

        private static bool IsExternalPluginLoadingEnabled()
            => string.Equals(
                Environment.GetEnvironmentVariable("RC_ENABLE_EXTERNAL_PLUGINS"),
                "1",
                StringComparison.Ordinal);

        private static bool IsTrustedPlugin(string moduleDir, string dllPath, Action<string> logger)
        {
            try
            {
                var root = Path.GetFullPath(moduleDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                var fullPath = Path.GetFullPath(dllPath);
                if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Invoke($"Skipped plugin outside trusted module directory: {Path.GetFileName(dllPath)}");
                    return false;
                }

                // Load certificate directly from the signed file path. Avoid obsolete CreateFromSignedFile API.
                using var certificate = new X509Certificate2(fullPath);
                using var chain = new X509Chain
                {
                    ChainPolicy =
                    {
                        RevocationMode = X509RevocationMode.Online,
                        RevocationFlag = X509RevocationFlag.ExcludeRoot,
                        VerificationFlags = X509VerificationFlags.NoFlag
                    }
                };

                if (!chain.Build(certificate))
                {
                    logger.Invoke($"Skipped unsigned or untrusted plugin: {Path.GetFileName(dllPath)}");
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (ex is FileNotFoundException ||
                                       ex is CryptographicException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is IOException)
            {
                logger.Invoke($"Skipped untrusted plugin {Path.GetFileName(dllPath)}: {ex.Message}");
                return false;
            }
        }
    }
}
