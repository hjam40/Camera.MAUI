using System.Collections.ObjectModel;

namespace Camera.MAUI;

public class CameraView : View, ICameraView
{
    public static readonly BindableProperty FlashModeProperty = BindableProperty.Create(nameof(FlashMode), typeof(FlashMode), typeof(CameraView), FlashMode.Disabled);
    public static readonly BindableProperty CamerasProperty = BindableProperty.Create(nameof(Cameras), typeof(ObservableCollection<CameraInfo>), typeof(CameraView), new ObservableCollection<CameraInfo>());
    public static readonly BindableProperty CameraProperty = BindableProperty.Create(nameof(Camera), typeof(CameraInfo), typeof(CameraView), null);
    public static readonly BindableProperty MirroredImageProperty = BindableProperty.Create(nameof(MirroredImage), typeof(bool), typeof(CameraView), false);

    public FlashMode FlashMode
    {
        get { return (FlashMode)GetValue(FlashModeProperty); }
        set { SetValue(FlashModeProperty, value); }
    }
    public ObservableCollection<CameraInfo> Cameras
    {
        get { return (ObservableCollection<CameraInfo>)GetValue(CamerasProperty); }
        set { SetValue(CamerasProperty, value); }
    }
    public CameraInfo Camera
    {
        get { return (CameraInfo)GetValue(CameraProperty); }
        set { SetValue(CameraProperty, value); }
    }
    public bool MirroredImage
    {
        get { return (bool)GetValue(MirroredImageProperty); }
        set { SetValue(MirroredImageProperty, value); }
    }
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Handler != null && Handler is CameraViewHandler handler) 
        {
            result = await handler.StartCameraAsync();
        }
        return result;
    }
    public async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.StopCameraAsync();
        }
        return result;
    }
    public async Task<bool> GetSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = false;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.GetSnapShot(imageFormat, SnapFilePath);
        }
        return result;
    }
}