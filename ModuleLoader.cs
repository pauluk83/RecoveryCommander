using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                    catch { }
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
                    // Get all DLL files recursively
                    var allDlls = Directory.GetFiles(moduleDir, "*.dll", SearchOption.AllDirectories);
                    
                    if (allDlls.Length > 0)
                    {
                        logger.Invoke($"Scanning and searching for plugins in: {moduleDir}");
                        foreach (var dllPath in allDlls)
                        {
                            try
                            {
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
    }
}
