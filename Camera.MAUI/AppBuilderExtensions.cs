using Microsoft.Maui.LifecycleEvents;

namespace Camera.MAUI;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseMauiCameraView(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(h =>
        {
            h.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
        });
        builder.ConfigureLifecycleEvents(events =>
        {
#if ANDROID
            events.AddAndroid(android => android
                .OnPause((activity) => CameraViewHandler.Current?.StopCameraAsync(false))
                .OnResume((activity) => CameraViewHandler.Current?.StartCameraAsync(CameraViewHandler.CurrentResolution)));
#elif IOS || MACCATALYST
            events.AddiOS(ios => ios
                .OnResignActivation((app) => CameraViewHandler.Current?.StopCameraAsync(false))
                .WillEnterForeground((app) => CameraViewHandler.Current?.StartCameraAsync(CameraViewHandler.CurrentResolution)));
#elif WINDOWS
#endif
        });
        return builder;
    }
}