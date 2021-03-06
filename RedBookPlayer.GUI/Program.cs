using System;
#if WindowsDebug
using System.Runtime.InteropServices;
#endif
using Avalonia;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace RedBookPlayer.GUI
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
#if WindowsDebug
            AllocConsole();
#endif
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

#if WindowsDebug
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
#endif

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseReactiveUI().UsePlatformDetect().LogToDebug();
    }
}