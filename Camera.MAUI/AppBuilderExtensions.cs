namespace Camera.MAUI;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseMauiCameraView(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(h =>
        {
            h.AddHandler<CameraView, CameraViewHandler>();
        });

        return builder;
    }
}
