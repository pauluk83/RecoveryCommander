using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecoveryCommander.Forms;
using RecoveryCommander.UI;
using RecoveryCommander.Core;
using RecoveryCommander.Core.Logging;

namespace RecoveryCommander
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Modules Warm-Up: Force load module assemblies so reflection finds them in single-file EXE
            _ = typeof(RecoveryCommander.Modules.SfcModule);
            _ = typeof(RecoveryCommander.Modules.DismModule);
            _ = typeof(RecoveryCommander.Modules.ReagentcModule);
            _ = typeof(MalwareRemovalModule.MalwareRemovalModule);
            _ = typeof(RecoveryCommander.Modules.SystemPrepModule);
            _ = typeof(RecoveryCommander.Modules.UtilitiesModule);
            _ = typeof(RecoveryCommander.Modules.DiagnosticsModule);
            _ = typeof(RecoveryCommander.Modules.DriverManagerModule);
            _ = typeof(RecoveryCommander.Modules.CloudRecoveryModule);

            try
            {
                // Initialize dependency injection container
                ServiceContainer.Initialize(services =>
                {
                    services.AddTransient<MainForm>();
                });

                // Initialize global exception handling
                GlobalExceptionHandler.Initialize();

                // Get logger via ILoggerFactory
                var loggerFactory = ServiceContainer.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("RecoveryCommander");
                logger.LogInformation("App startup - Recovery Commander v{Version}, log dir = {LogDir}",
                    typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                    AppPaths.LogsDirectory);

                // Enable visual styles
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Create and configure main form
                var mainForm = ServiceContainer.GetOptionalService<MainForm>() ?? new MainForm();
                
                // Initialize theme colors for Core components
                ThemeProvider.BackgroundColor = Theme.Background;
                ThemeProvider.ForegroundColor = Theme.Text;
                
                mainForm.Show();
                
                logger.LogInformation("Application started successfully");
                
                // Run the application
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FATAL: {ex}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                
                var logDir = AppPaths.LogsDirectory;
                MessageBox.Show(
                    "Failed to start Recovery Commander.\n\n" +
                    $"Error: {ex.Message}\n\n" +
                    $"Logs are written to:\n{logDir}\n" +
                    $"(today's file is {Path.GetFileName(AppPaths.CurrentLogFile())}).",
                    "Startup Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                // Cleanup resources
                try
                {
                    ServiceContainer.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during cleanup: {ex}");
                }
            }
        }
    }
}
