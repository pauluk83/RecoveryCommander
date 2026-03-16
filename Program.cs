using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RecoveryCommander.Forms;
using RecoveryCommander.UI;
using RecoveryCommander.Core;

namespace RecoveryCommander
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Modules Warm-Up: Force load module assemblies so reflection finds them in single-file EXE
            _ = typeof(RecoveryCommander.Module.SfcModule);
            _ = typeof(RecoveryCommander.Module.DismModule);
            _ = typeof(RecoveryCommander.Module.ReagentcModule);
            _ = typeof(MalwareRemovalModule.MalwareRemovalModule);
            _ = typeof(RecoveryCommander.Module.SystemPrepModule);
            _ = typeof(UtilitiesModule.UtilitiesModule);

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
                logger.LogInformation("Recovery Commander starting up");

                // Enable visual styles
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Create and configure main form
                var mainForm = ServiceContainer.GetOptionalService<MainForm>() ?? new MainForm();
                
                // Initialize theme colors for Core components
                ThemeProvider.BackgroundColor = Theme.Background;
                ThemeProvider.ForegroundColor = Theme.Text;
                
                // Apply animations and transitions
                try
                {
                    // Theme.Animations.AnimateForm(mainForm, Theme.Animations.AnimationType.Blend, show: true);
                    mainForm.Show();
                }
                catch
                {
                    // Animation failed, continue without it
                    mainForm.Show();
                }
                
                logger.LogInformation("Application started successfully");
                
                // Run the application
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                // Last resort error handling
                try
                {
                    // GlobalExceptionHandler is static, can't be used as type parameter
                    Console.WriteLine($"ERROR: {ex}");
                }
                catch
                {
                    // If even logging fails, write to console with full stack trace
                    Console.WriteLine($"FATAL: {ex}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException}");
                        Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }
                }
                
                try
                {
                    MessageBox.Show(
                        "Failed to start Recovery Commander.\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        "Please check the log files for more details.",
                        "Startup Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                catch
                {
                    // If even MessageBox fails, write to console
                    Console.WriteLine($"FATAL: {ex}");
                }
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
