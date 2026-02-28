using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RecoveryCommander.Contracts;

namespace RecoveryCommander.UI
{
    /// <summary>
    /// Unified system utilities - Consolidates error handling, animation, and async operations
    /// </summary>
    public static class SystemUtilities
    {
        #region Error Handling
        public static class ErrorHandler
        {
            public static void HandleError(Exception ex, string context = "")
            {
                var message = string.IsNullOrEmpty(context) 
                    ? ex.Message 
                    : $"{context}: {ex.Message}";
                
                Console.WriteLine($"ERROR: {message}");
                
                if (Application.OpenForms.Count > 0)
                {
                    MessageBox.Show(
                        Application.OpenForms[0], 
                        message, 
                        "Error", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            }

            public static void HandleWarning(string message, string context = "")
            {
                var fullMessage = string.IsNullOrEmpty(context) 
                    ? message 
                    : $"{context}: {message}";
                
                Console.WriteLine($"WARNING: {fullMessage}");
            }
        }
        #endregion
    }
}
