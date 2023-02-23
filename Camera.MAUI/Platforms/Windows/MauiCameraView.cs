using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using System.Diagnostics;
using Windows.Devices.Enumeration;
using Windows.Media.Core;
using Windows.Graphics.Imaging;
using Windows.Media.Devices;

namespace Camera.MAUI.Platforms.Windows;

public sealed partial class MauiCameraView : UserControl, IDisposable
{
    private readonly List<CameraInfo> Cameras = new List<CameraInfo>();
    public CameraInfo Camera { get; set; }

    private readonly MediaPlayerElement mediaElement;
    private MediaCapture mediaCapture;
    private MediaFrameSource frameSource;
    private MediaFrameReader frameReader;
    private List<MediaFrameSourceGroup> sGroups;
    private bool snapping = false;
    private bool started = false;
    private Microsoft.UI.Xaml.FlowDirection flowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;

    private readonly CameraView cameraView;
    public MauiCameraView(CameraView cameraView)
    {
        this.cameraView = cameraView;
        mediaElement = new MediaPlayerElement
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch
        };
        Content = mediaElement;
        InitDevices();
    }
    public async void UpdateCamera()
    {
        if (cameraView != null && cameraView.Camera != null)
        {
            if(started) await StopCameraAsync();
            Camera = cameraView.Camera;
            if (started) await StartCameraAsync();
        }
    }
    public void UpdateMirroredImage()
    {
        if (cameraView != null)
        {
            if(cameraView.MirroredImage)
                flowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft;
            else
                flowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;
            if (mediaElement != null) mediaElement.FlowDirection = flowDirection;
        }
    }
    public void UpdateFlashMode()
    {
        if (frameSource != null && cameraView != null)
        {
            try
            {
                if (frameSource.Controller.VideoDeviceController.FlashControl.Supported)
                {
                    switch (cameraView.FlashMode)
                    {
                        case FlashMode.Auto:
                            frameSource.Controller.VideoDeviceController.FlashControl.Auto = true;
                            break;
                        case FlashMode.Enabled:
                            frameSource.Controller.VideoDeviceController.FlashControl.Auto = false;
                            frameSource.Controller.VideoDeviceController.FlashControl.Enabled = true;
                            break;
                        case FlashMode.Disabled:
                            frameSource.Controller.VideoDeviceController.FlashControl.Auto = false;
                            frameSource.Controller.VideoDeviceController.FlashControl.Enabled = false;
                            break;
                    }
                }
            }
            catch(Exception)
            {
            }
        }

    }
    private async void InitDevices()
    {
        try
        {
            var vDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            var mediaGroups = await MediaFrameSourceGroup.FindAllAsync();
            sGroups = mediaGroups.Where(g => g.SourceInfos.Any(s => s.SourceKind == MediaFrameSourceKind.Color &&
                                                                            (s.MediaStreamType == MediaStreamType.VideoPreview || s.MediaStreamType == MediaStreamType.VideoRecord))
                                                                            && g.SourceInfos.All(sourceInfo => vDevices.Any(vd => vd.Id == sourceInfo.DeviceInformation.Id))).ToList();
            foreach (var s in sGroups)
                Cameras.Add(new CameraInfo { Name = s.DisplayName, DeviceId = s.Id });

            Camera = Cameras.FirstOrDefault();
            if (cameraView != null)
            {
                cameraView.Cameras.Clear();
                foreach (var cam in Cameras) cameraView.Cameras.Add(cam);
            }
        }
        catch(Exception) 
        {
            Camera = null;
        }
    }
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.Success;

        if (started) await StopCameraAsync();
        if (Camera != null)
        {
            started = true;
            mediaCapture = new MediaCapture();
            try
            {
                await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                {
                    SourceGroup = sGroups.First(s => s.Id == Camera.DeviceId),
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                });
                frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                      && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
                if (frameSource != null)
                {
                    UpdateFlashMode();
                    var frameFormat = frameSource.SupportedFormats.OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height).FirstOrDefault();

                    if (frameFormat != null)
                    {
                        await frameSource.SetFormatAsync(frameFormat);
                        mediaElement.AutoPlay = true;
                        mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
                        mediaElement.FlowDirection = flowDirection;

                        frameReader = await mediaCapture.CreateFrameReaderAsync(frameSource);
                        frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
                        if (frameReader == null)
                        {
                            result = CameraResult.AccessError;
                        }
                        else
                        {
                            var fResult = await frameReader.StartAsync();
                            if (fResult != MediaFrameReaderStartStatus.Success)
                            {
                                result = CameraResult.AccessError;
                            }

                        }

                    }
                    else
                        result = CameraResult.NoVideoFormatsAvailable;
                }
                else
                    result = CameraResult.AccessError;
            }
            catch (UnauthorizedAccessException)
            {
                result = CameraResult.AccessDenied;
            }
            catch (Exception)
            {
                result = CameraResult.AccessError;
            }
        }
        else
            result = CameraResult.NoCameraSelected;

        if (result != CameraResult.Success && mediaCapture != null)
        {
            if (frameReader != null)
            {
                frameReader.Dispose();
                frameReader = null;
            }
            mediaCapture.Dispose();
            mediaCapture = null;
        }
        return result;
    }

    public async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.Success;

        try
        {
            if (frameReader != null)
            {
                await frameReader.StopAsync();
                frameReader.Dispose();
                frameReader = null;
            }
            mediaElement.Source = null;
            if (mediaCapture != null)
            {
                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }
        catch (Exception)
        {
            result = CameraResult.AccessError;
        }
        started = false;

        return result;
    }
    public async Task<bool> GetSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = true;
        if (!snapping && frameReader != null)
        {
            snapping = true;
            SoftwareBitmap snapshot = null;

            var frame = frameReader.TryAcquireLatestFrame();
            if (frame != null && frame.VideoMediaFrame != null)
            {
                snapshot = frame.VideoMediaFrame.SoftwareBitmap;
            }
            if (snapshot != null)
            {
                var iformat = imageFormat switch
                {
                    ImageFormat.JPEG => BitmapEncoder.JpegEncoderId,
                    ImageFormat.PNG => BitmapEncoder.PngEncoderId,
                    _ => BitmapEncoder.BmpEncoderId,
                };
                if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
                using FileStream stream = new(SnapFilePath, FileMode.OpenOrCreate);
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(iformat, stream.AsRandomAccessStream());
                var img = SoftwareBitmap.Convert(snapshot, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                encoder.SetSoftwareBitmap(img);
                try
                {
                    if (flowDirection == Microsoft.UI.Xaml.FlowDirection.RightToLeft)
                        encoder.BitmapTransform.Flip = BitmapFlip.Horizontal;
                    await encoder.FlushAsync();
                }
                catch (Exception)
                {
                    result = false;
                }
                stream.Close();
            }
            snapping = false;
        }
        return result;
    }

    public void Dispose()
    {
        StopCameraAsync().Wait();
    }
}