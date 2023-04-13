using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Devices.Enumeration;
using Windows.Media.Core;
using Windows.Graphics.Imaging;
using Panel = Windows.Devices.Enumeration.Panel;
using Windows.Media.MediaProperties;

namespace Camera.MAUI.Platforms.Windows;

public sealed partial class MauiCameraView : UserControl, IDisposable
{
    private readonly List<CameraInfo> Cameras = new();
    private readonly List<MicrophoneInfo> Micros = new();
    private CameraInfo Camera { get; set; }
    private MicrophoneInfo Microphone { get; set; }

    private readonly MediaPlayerElement mediaElement;
    private MediaCapture mediaCapture;
    private MediaFrameSource frameSource;
    private MediaFrameReader frameReader;
    private LowLagMediaRecording mediaRecording;
    private List<MediaFrameSourceGroup> sGroups;
    private bool snapping = false;
    private bool started = false;
    private Microsoft.UI.Xaml.FlowDirection flowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;
    private int frames = 0;
    private bool initiated = false;
    private bool recording = false;
    private FileStream recordStream;

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
    internal async void UpdateCamera()
    {
        if (cameraView != null && cameraView.Camera != null)
        {
            if (started)
            {
                await StopCameraAsync();
                Camera = cameraView.Camera;
                await StartCameraAsync();
            }else
                Camera = cameraView.Camera;
        }
    }
    internal void UpdateMirroredImage()
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
    internal void SetZoomFactor(float zoom)
    {
        if (Camera != null && frameSource != null && frameSource.Controller.VideoDeviceController.ZoomControl.Supported)
        {
            frameSource.Controller.VideoDeviceController.ZoomControl.Value = Math.Max(Camera.MinZoomFactor, Math.Min(zoom, Camera.MaxZoomFactor));
        }
    }
    internal void UpdateFlashMode()
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
    internal void UpdateTorch()
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
                {
                    CameraPosition position = CameraPosition.Unknow;
                    var device = vDevices.FirstOrDefault(vd => vd.Id == s.Id);
                    if (device != null)
                    {
                        if (device.EnclosureLocation != null)
                            position = device.EnclosureLocation.Panel switch
                            {
                                Panel.Front => CameraPosition.Front,
                                Panel.Back => CameraPosition.Back,
                                _ => CameraPosition.Unknow
                            };
                    }
                    mediaCapture = new MediaCapture();
                    
                    mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        SourceGroup = s,
                        MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                        StreamingCaptureMode = StreamingCaptureMode.Video
                    }).GetAwaiter().GetResult();
                    frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                          && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
                    Cameras.Add(new CameraInfo
                    {
                        Name = s.DisplayName,
                        DeviceId = s.Id,
                        Position = position,
                        HasFlashUnit = frameSource.Controller.VideoDeviceController.FlashControl.Supported,
                        MinZoomFactor = frameSource.Controller.VideoDeviceController.ZoomControl.Supported ? frameSource.Controller.VideoDeviceController.ZoomControl.Min : 1f,
                        MaxZoomFactor = frameSource.Controller.VideoDeviceController.ZoomControl.Supported ? frameSource.Controller.VideoDeviceController.ZoomControl.Max : 1f
                    });
                }
                Camera = Cameras.FirstOrDefault();

                var aDevices = DeviceInformation.FindAllAsync(DeviceClass.AudioCapture).GetAwaiter().GetResult();
                foreach (var device in aDevices)
                    Micros.Add(new MicrophoneInfo { Name = device.Name, DeviceId = device.Id });
                Microphone = Micros.FirstOrDefault();

                initiated = true;
                if (cameraView != null)
                {
                    cameraView.Cameras.Clear();
                    foreach (var micro in Micros) cameraView.Microphones.Add(micro);
                    foreach (var cam in Cameras) cameraView.Cameras.Add(cam);
                    cameraView.RefreshDevices();
                }
            }
            catch
            {
                Camera = null;
            }
        }
    }
    internal async Task<CameraResult> StartRecordingAsync(string file)
    {
        CameraResult result = CameraResult.Success;

        if (initiated)
        {
            if (started) await StopCameraAsync();
            if (Camera != null && Microphone != null)
            {
                started = true;

                mediaCapture = new MediaCapture();
                try
                {
                    await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = Camera.DeviceId,
                        MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                        AudioDeviceId = Microphone.DeviceId
                    });

                    MediaEncodingProfile profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
                    recordStream = new(file, FileMode.Create);
                    mediaRecording = await mediaCapture.PrepareLowLagRecordToStreamAsync(profile, recordStream.AsRandomAccessStream());

                    frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                  && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
                    if (frameSource != null)
                    {
                        UpdateTorch();
                        SetZoomFactor(cameraView.ZoomFactor);

                        mediaElement.AutoPlay = true;
                        mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
                        await mediaRecording.StartAsync();
                        recording = true;
                    }
                    else
                        result = CameraResult.NoVideoFormatsAvailable;
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
                result = Camera == null ? CameraResult.NoCameraSelected : CameraResult.NoMicrophoneSelected;
        }
        else
            result = CameraResult.NotInitiated;

        return result;
    }
    internal async Task<CameraResult> StopRecordingAsync()
    {
        return await StartCameraAsync();
    }
    internal async Task<CameraResult> StartCameraAsync()
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
    private void RefreshSnapShot()
    {
        cameraView.RefreshSnapshot(GetSnapShot(cameraView.AutoSnapShotFormat, true));
    }

    private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (!snapping && cameraView != null && cameraView.AutoSnapShotSeconds > 0 && (DateTime.Now - cameraView.lastSnapshot).TotalSeconds >= cameraView.AutoSnapShotSeconds)
        {
            Task.Run(() => RefreshSnapShot());
        }
        else if (cameraView.BarCodeDetectionEnabled)
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

    internal async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            try
            {
                if (recording && mediaRecording != null)
                {
                    await mediaRecording.StopAsync();
                    recording = false;
                    recordStream?.Close();
                    recordStream?.Dispose();
                }
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
    internal void DisposeControl()
    {
        if (started) StopCameraAsync().Wait();
        Dispose();
    }
    internal async Task<Stream> TakePhotoAsync(ImageFormat imageFormat)
    {
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
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(iformat, stream.AsRandomAccessStream());
                var img = SoftwareBitmap.Convert(snapshot, BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied);
                encoder.SetSoftwareBitmap(img);
                try
                {
                    if (flowDirection == Microsoft.UI.Xaml.FlowDirection.RightToLeft)
                        encoder.BitmapTransform.Flip = BitmapFlip.Horizontal;
                    await encoder.FlushAsync();
                    stream.Position = 0;
                    img.Dispose();
                    snapshot.Dispose();
                    frame.Dispose();
                    snapping = false;
                    return stream;
                }
                catch (Exception)
                {
                }
            }
            snapping = false;
        }
        GC.Collect();
        return null;
    }
    internal ImageSource GetSnapShot(ImageFormat imageFormat, bool auto = false)
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
                    if (auto)
                    {
                        if (cameraView.AutoSnapShotAsImageSource)
                            result = ImageSource.FromStream(() => stream);
                        cameraView.SnapShotStream?.Dispose();
                        cameraView.SnapShotStream = stream;
                    }else
                        result = ImageSource.FromStream(() => stream);
                    img.Dispose();
                    snapshot.Dispose();
                    frame.Dispose();
                }
                catch (Exception)
                {
                }
            }
            snapping = false;
        }
        GC.Collect();
        return result;
    }

    internal async Task<bool> SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
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