using System;
using System.Windows.Forms;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Interop;
using IdeaMemoryManager.UI.Views;

namespace IdeaMemoryManager
{
    /// <summary>
    /// Application entry point and global unhandled exception guards.
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Declare Per-Monitor V2 DPI awareness for crisp vector rendering on 2K/4K displays
            try
            {
                NativeMethods.SetProcessDpiAwareness(2); // PerMonitorV2
            }
            catch
            {
                try { NativeMethods.SetProcessDPIAware(); } catch { }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (s, e) =>
            {
                Logger.Error("Application thread exception captured", e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logger.Error("AppDomain unhandled exception captured", e.ExceptionObject as Exception);
            };

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Logger.Error("Fatal application termination", ex);
                MessageBox.Show("Fatal error encountered: " + ex.Message, "IdeaMemoryManager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}