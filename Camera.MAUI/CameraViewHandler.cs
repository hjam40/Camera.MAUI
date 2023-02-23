using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;

#if IOS
using PlatformView = Camera.MAUI.Platforms.iOS.MauiCameraView;
#elif MACCATALYST
using PlatformView = Camera.MAUI.Platforms.MacCatalyst.MauiCameraView;
#elif ANDROID
using PlatformView = Camera.MAUI.Platforms.Android.MauiCameraView;
#elif WINDOWS
using PlatformView = Camera.MAUI.Platforms.Windows.MauiCameraView;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace Camera.MAUI;

internal partial class CameraViewHandler : ViewHandler<CameraView, PlatformView>
{
    public static IPropertyMapper<CameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<CameraView, CameraViewHandler>(ViewMapper)
    {
        [nameof(CameraView.FlashMode)] = MapFlashMode,
        [nameof(CameraView.Camera)] = MapCamera,
        [nameof(CameraView.MirroredImage)] = MapMirroredImage,
    };
    public static CommandMapper<CameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
    {
    };
    public CameraViewHandler() : base(PropertyMapper, CommandMapper)
    {
    }
#if ANDROID
    protected override PlatformView CreatePlatformView() => new PlatformView(Context, VirtualView);
#elif IOS || MACCATALYST || WINDOWS
    protected override PlatformView CreatePlatformView() => new(VirtualView);
#else
    protected override PlatformView CreatePlatformView() => new();
#endif
    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);

        // Perform any control setup here
    }

    protected override void DisconnectHandler(PlatformView platformView)
    {
#if WINDOWS || IOS || ANDROID || MACCATALYST
        platformView.Dispose();
#endif
        base.DisconnectHandler(platformView);
    }
    public static void MapFlashMode(CameraViewHandler handler, CameraView cameraView)
    {
#if WINDOWS
        handler.PlatformView?.UpdateFlashMode();
#endif
    }
    public static void MapCamera(CameraViewHandler handler, CameraView cameraView)
    {
#if WINDOWS
        handler.PlatformView?.UpdateCamera();
#endif
    }
    public static void MapMirroredImage(CameraViewHandler handler, CameraView cameraView)
    {
#if WINDOWS
        handler.PlatformView?.UpdateMirroredImage();
#endif
    }
    public Task<CameraResult> StartCameraAsync()
    {
        if (PlatformView != null)
        {
#if WINDOWS || ANDROID
            return PlatformView.StartCameraAsync();
#endif
        }
        return new Task<CameraResult>(() => { return CameraResult.AccessError; });
    }
    public Task<CameraResult> StopCameraAsync()
    {
        if (PlatformView != null)
        {
#if WINDOWS
            return PlatformView.StopCameraAsync();
#endif
        }
        return new Task<CameraResult>(() => { return CameraResult.AccessError; });
    }
    public Task<bool> GetSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        if (PlatformView != null)
        {
#if WINDOWS
            return PlatformView.GetSnapShot(imageFormat, SnapFilePath);
#endif
        }
        return new Task<bool>(() => { return false; });
    }
}
