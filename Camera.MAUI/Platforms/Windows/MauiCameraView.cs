using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Devices.Enumeration;
using Windows.Media.Core;
using Windows.Graphics.Imaging;
using Windows.Media.Devices;

namespace Camera.MAUI.Platforms.Windows;

public sealed partial class MauiCameraView : UserControl, IDisposable
{
    private readonly List<CameraInfo> Cameras = new();
    public CameraInfo Camera { get; set; }
    public float MinZoomFactor
    {
        get
        {
            if (frameSource != null && frameSource.Controller.VideoDeviceController.ZoomControl.Supported)
                return frameSource.Controller.VideoDeviceController.ZoomControl.Min;
            else
                return 1f;
        }
    }
    public float MaxZoomFactor { get
        {
            if (frameSource != null && frameSource.Controller.VideoDeviceController.ZoomControl.Supported)
                return frameSource.Controller.VideoDeviceController.ZoomControl.Max;
            else
                return 1f;
        } 
    }

    private readonly MediaPlayerElement mediaElement;
    private MediaCapture mediaCapture;
    private MediaFrameSource frameSource;
    private MediaFrameReader frameReader;
    private List<MediaFrameSourceGroup> sGroups;
    private bool snapping = false;
    private bool started = false;
    private Microsoft.UI.Xaml.FlowDirection flowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;
    private int frames = 0;
    private bool initiated = false;

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
    public void SetZoomFactor(float zoom)
    {
        if (frameSource != null && frameSource.Controller.VideoDeviceController.ZoomControl.Supported)
        {
            frameSource.Controller.VideoDeviceController.ZoomControl.Value = Math.Max(MinZoomFactor, Math.Min(zoom, MaxZoomFactor));
        }
    }
    public void UpdateFlashMode()
    {
        if (frameSource != null && cameraView != null)
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
    }
    public void UpdateTorch()
    {
        if (frameSource != null && cameraView != null)
        {
            if (frameSource.Controller.VideoDeviceController.TorchControl.Supported)
                frameSource.Controller.VideoDeviceController.TorchControl.Enabled = cameraView.TorchEnabled;
        }
    }
    private void InitDevices()
    {
        if (!initiated)
        {
            try
            {
                var vDevices = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).GetAwaiter().GetResult();
                var mediaGroups = MediaFrameSourceGroup.FindAllAsync().GetAwaiter().GetResult();
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
                initiated = true;
            }
            catch
            {
                Camera = null;
            }
        }
    }
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.Success;

        if (initiated)
        {
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
                        frames = 0;
                        UpdateTorch();
                        UpdateMirroredImage();
                        SetZoomFactor(cameraView.ZoomFactor);
                        
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
                                frameReader.FrameArrived += FrameReader_FrameArrived;
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
                    frameReader.FrameArrived -= FrameReader_FrameArrived;
                    frameReader.Dispose();
                    frameReader = null;
                }
                mediaCapture.Dispose();
                mediaCapture = null;
            }
        }else
            result = CameraResult.NotInitiated;
        return result;
    }
    private void ProcessQRImage(SoftwareBitmap simg)
    {
        if (simg != null)
        {
            Task.Run(() =>
            {
                var img = SoftwareBitmap.Convert(simg, BitmapPixelFormat.Gray8, BitmapAlphaMode.Ignore);
                if (img != null)
                {
                    if (img.PixelWidth > 0 && img.PixelHeight > 0)
                        cameraView.DecodeBarcode(img);
                    img.Dispose();
                }
                GC.Collect();
            });
        }
    }

    private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (cameraView.BarCodeDetectionEnabled)
        {
            frames++;
            if (frames >= cameraView.BarCodeDetectionFrameRate)
            {
                var frame = sender.TryAcquireLatestFrame();
                ProcessQRImage(frame.VideoMediaFrame.SoftwareBitmap);
                frames = 0;
            }
        }
    }

    public async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            try
            {
                if (frameReader != null)
                {
                    await frameReader.StopAsync();
                    frameReader.FrameArrived -= FrameReader_FrameArrived;
                    frameReader?.Dispose();
                    frameReader = null;
                }
                mediaElement.Source = null;
                if (mediaCapture != null)
                {
                    mediaCapture.Dispose();
                    mediaCapture = null;
                }
            }
            catch
            {
                result = CameraResult.AccessError;
            }
        }else
            result = CameraResult.NotInitiated;
        started = false;

        return result;
    }
    public void DisposeControl()
    {
        if (started) StopCameraAsync().Wait();
        Dispose();
    }
    public ImageSource GetSnapShot(ImageFormat imageFormat)
    {
        ImageSource result = null;
        if (started && !snapping && frameReader != null)
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
                    _ => BitmapEncoder.PngEncoderId
                };
                MemoryStream stream = new();
                BitmapEncoder encoder = BitmapEncoder.CreateAsync(iformat, stream.AsRandomAccessStream()).GetAwaiter().GetResult();
                var img = SoftwareBitmap.Convert(snapshot, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                encoder.SetSoftwareBitmap(img);
                try
                {
                    if (flowDirection == Microsoft.UI.Xaml.FlowDirection.RightToLeft)
                        encoder.BitmapTransform.Flip = BitmapFlip.Horizontal;
                    encoder.FlushAsync().GetAwaiter().GetResult();
                    stream.Position = 0;
                    result = ImageSource.FromStream(() => stream);
                }
                catch (Exception)
                {
                }
            }
            snapping = false;
        }
        return result;
    }

    public async Task<bool> SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = true;
        if (started && !snapping && frameReader != null)
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
                    _ => BitmapEncoder.PngEncoderId
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
        }else
            result = false;
        return result;
    }

    public void Dispose()
    {
        StopCameraAsync().Wait();
    }
}