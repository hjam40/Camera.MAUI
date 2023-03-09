namespace Camera.MAUI;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseMauiCameraView(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(h =>
        {
            h.AddHandler(typeof(CameraView), typeof(CameraViewHandler));
        });
        return builder;
    }
}
