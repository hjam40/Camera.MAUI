using Microsoft.Maui.Handlers;
#if IOS || MACCATALYST
using PlatformView = Camera.MAUI.Platforms.Apple.MauiCameraView;
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
        [nameof(CameraView.TorchEnabled)] = MapTorch,
        [nameof(CameraView.MirroredImage)] = MapMirroredImage,
        [nameof(CameraView.ZoomFactor)] = MapZoomFactor,
    };
    public static CommandMapper<CameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
    {
    };
    public CameraViewHandler() : base(PropertyMapper, CommandMapper)
    {
    }
#if ANDROID
    protected override PlatformView CreatePlatformView() => new(Context, VirtualView);
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
#if WINDOWS || IOS || MACCATALYST || ANDROID
        platformView.DisposeControl();
#endif
        base.DisconnectHandler(platformView);
    }
    public static void MapTorch(CameraViewHandler handler, CameraView cameraView)
    {
#if WINDOWS || ANDROID || IOS || MACCATALYST
        handler.PlatformView?.UpdateTorch();
#endif
    }
    public static void MapMirroredImage(CameraViewHandler handler, CameraView cameraView)
    {
#if WINDOWS || ANDROID || IOS || MACCATALYST
        handler.PlatformView?.UpdateMirroredImage();
#endif
    }
    public static void MapZoomFactor(CameraViewHandler handler, CameraView cameraView)
    {
#if WINDOWS || ANDROID || IOS || MACCATALYST
        handler.PlatformView?.SetZoomFactor(cameraView.ZoomFactor);
#endif
    }

    public Task<CameraResult> StartCameraAsync(Size PhotosResolution)
    {
        if (PlatformView != null)
        {
#if WINDOWS || ANDROID || IOS || MACCATALYST
            return PlatformView.StartCameraAsync(PhotosResolution);
#endif
        }
        return Task.Run(() => { return CameraResult.AccessError; });
    }
    public Task<CameraResult> StartRecordingAsync(string file, Size Resolution)
    {
        if (PlatformView != null)
        {
#if WINDOWS || ANDROID || IOS
            return PlatformView.StartRecordingAsync(file, Resolution);
#endif
        }
        return Task.Run(() => { return CameraResult.AccessError; });
    }
    public Task<CameraResult> StopCameraAsync()
    {
        if (PlatformView != null)
        {
#if WINDOWS
            return PlatformView.StopCameraAsync();
#elif ANDROID || IOS || MACCATALYST
            var task = new Task<CameraResult>(() => { return PlatformView.StopCamera(); });
            task.Start();
            return task;
#endif
        }
        return Task.Run(() => { return CameraResult.AccessError; });
    }
    public Task<CameraResult> StopRecordingAsync()
    {
        if (PlatformView != null)
        {
#if WINDOWS || ANDROID || IOS || MACCATALYST
            return PlatformView.StopRecordingAsync();
#endif
        }
        return Task.Run(() => { return CameraResult.AccessError; });
    }
    public ImageSource GetSnapShot(ImageFormat imageFormat)
    {
        if (PlatformView != null)
        {
#if WINDOWS || ANDROID || IOS || MACCATALYST
            return PlatformView.GetSnapShot(imageFormat);
#endif
        }
        return null;
    }
    public Task<Stream> TakePhotoAsync(ImageFormat imageFormat)
    {
        if (PlatformView != null)
        {
#if  IOS || MACCATALYST || WINDOWS
            return PlatformView.TakePhotoAsync(imageFormat);
#elif ANDROID
            return Task.Run(() => { return PlatformView.TakePhotoAsync(imageFormat); });
#endif
        }
        return Task.Run(() => { Stream result = null; return result; });
    }
    public Task<bool> SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        if (PlatformView != null)
        {
#if WINDOWS
            return PlatformView.SaveSnapShot(imageFormat, SnapFilePath);
#elif ANDROID || IOS || MACCATALYST
            var task = new Task<bool>(() => { return PlatformView.SaveSnapShot(imageFormat, SnapFilePath); });
            task.Start();
            return task;
#endif
        }
        return Task.Run(() => { return false; });
    }
    public void ForceAutoFocus()
    {
#if ANDROID || WINDOWS || IOS || MACCATALYST
        PlatformView?.ForceAutoFocus();
#endif
    }
    public void ForceDispose()
    {
#if ANDROID || WINDOWS || IOS || MACCATALYST
        PlatformView?.DisposeControl();
#endif
    }
}
