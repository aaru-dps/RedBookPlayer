using Avalonia;
using Avalonia.Logging.Serilog;

namespace RedBookPlayer
{
    class Program
    {
        public static void Main(string[] args)
        {
#if Windows
            AllocConsole();
#endif
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

#if Windows
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
#endif

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug();
    }
}
