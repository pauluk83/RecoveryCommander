/*
 * AUDIT HEADER
 * File: Program.cs
 * Module: RecoveryCommander.WinUI
 * Created: 2026-07-20
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-07-20 - 1.3.0 - Created custom entry point (Program.cs) with a global try-catch and 
 *                       Win32 MessageBox fallback to guarantee any bootstrap/launch/activation
 *                       crashes are shown in a pop up box.
 */

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace RecoveryCommanderWinUI;

public static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONERROR = 0x00000010;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        catch (Exception ex)
        {
            ShowFallbackCrashMessageBox(ex);
            throw;
        }
    }

    private static void ShowFallbackCrashMessageBox(Exception ex)
    {
#pragma warning disable CA1031 // Suppress broad catch warning for fallback crash reporting
        try
        {
            var inner = ex.InnerException != null
                ? $"\n\nInner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                : string.Empty;

            var text = $"RecoveryCommander failed to start due to a bootstrap error.\n\n" +
                       $"Error Type: {ex.GetType().FullName}\n" +
                       $"Message: {ex.Message}{inner}\n\n" +
                       $"Stack Trace:\n{ex.StackTrace}";

            _ = MessageBox(IntPtr.Zero, text, "RecoveryCommander — Bootstrap Error", MB_OK | MB_ICONERROR);
        }
        catch
        {
            // Ultimate fallback
        }
#pragma warning restore CA1031
    }
}
