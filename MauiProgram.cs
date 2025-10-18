using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace LocationTracker
{
    /// <summary>
    /// Configures and creates the MAUI application
    /// Registers services, handlers, and app-wide configuration
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Creates and configures the MAUI app
        /// Called by the platform-specific entry points
        /// </summary>
        /// <returns>Configured MauiApp instance</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            builder
                .UseMauiApp<App>()
                .UseMauiMaps() // Enable MAUI Maps support
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            // Enable debug logging for development
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}