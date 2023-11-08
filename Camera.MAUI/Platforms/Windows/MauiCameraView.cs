using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Devices.Enumeration;
using Windows.Media.Core;
using Windows.Graphics.Imaging;
using Panel = Windows.Devices.Enumeration.Panel;
using Windows.Media.MediaProperties;
using Windows.Media.Devices;

namespace Camera.MAUI.Platforms.Windows;

public sealed partial class MauiCameraView : UserControl, IDisposable
{
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
    bool mediaLoaded = false;

    private readonly CameraView cameraView;
    public MauiCameraView(CameraView cameraView)
    {
        this.cameraView = cameraView;
        mediaElement = new MediaPlayerElement
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch
        };
        mediaElement.Loaded += MediaElement_Loaded;
        Content = mediaElement;
        InitDevices();
    }

    private void MediaElement_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        mediaLoaded = true;
    }

    internal void UpdateMirroredImage()
    {
        if (cameraView != null)
        {
            if (cameraView.MirroredImage)
                flowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft;
            else
                flowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;
            if (mediaElement != null) mediaElement.FlowDirection = flowDirection;
        }
    }
    internal void SetZoomFactor(float zoom)
    {
        if (cameraView.Camera != null && frameSource != null && frameSource.Controller.VideoDeviceController.ZoomControl.Supported)
        {
            frameSource.Controller.VideoDeviceController.ZoomControl.Value = Math.Clamp(zoom, cameraView.Camera.MinZoomFactor, cameraView.Camera.MaxZoomFactor);
        }
    }
    internal void ForceAutoFocus()
    {
        if (cameraView.Camera != null && frameSource != null && frameSource.Controller.VideoDeviceController.FocusControl.Supported)
        {
            frameSource.Controller.VideoDeviceController.FocusControl.SetPresetAsync(FocusPreset.Manual).GetAwaiter().GetResult();
            frameSource.Controller.VideoDeviceController.FocusControl.SetPresetAsync(FocusPreset.Auto).GetAwaiter().GetResult();
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
                cameraView.Cameras.Clear();
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
                    var camInfo = new CameraInfo
                    {
                        Name = s.DisplayName,
                        DeviceId = s.Id,
                        Position = position,
                        HasFlashUnit = frameSource.Controller.VideoDeviceController.FlashControl.Supported,
                        MinZoomFactor = frameSource.Controller.VideoDeviceController.ZoomControl.Supported ? frameSource.Controller.VideoDeviceController.ZoomControl.Min : 1f,
                        MaxZoomFactor = frameSource.Controller.VideoDeviceController.ZoomControl.Supported ? frameSource.Controller.VideoDeviceController.ZoomControl.Max : 1f,
                        AvailableResolutions = new()
                    };
                    foreach (var profile in MediaCapture.FindAllVideoProfiles(s.Id))
                    {
                        foreach (var recordMediaP in profile.SupportedRecordMediaDescription)
                        {
                            if (!camInfo.AvailableResolutions.Any(s => s.Width == recordMediaP.Width && s.Height == recordMediaP.Height))
                                camInfo.AvailableResolutions.Add(new(recordMediaP.Width, recordMediaP.Height));
                        }
                    }
                    cameraView.Cameras.Add(camInfo);
                }

                var aDevices = DeviceInformation.FindAllAsync(DeviceClass.AudioCapture).GetAwaiter().GetResult();
                cameraView.Microphones.Clear();
                foreach (var device in aDevices)
                    cameraView.Microphones.Add(new MicrophoneInfo { Name = device.Name, DeviceId = device.Id });

                initiated = true;
                cameraView.RefreshDevices();
            }
            catch
            {
            }
        }
    }
    internal async Task<CameraResult> StartRecordingAsync(string file, Size Resolution)
    {
        CameraResult result = CameraResult.Success;

        if (initiated)
        {
            if (started) await StopCameraAsync();
            if (cameraView.Camera != null && cameraView.Microphone != null)
            {
                while (!mediaLoaded) await Task.Delay(50);
                started = true;

                mediaCapture = new MediaCapture();
                try
                {
                    await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        VideoDeviceId = cameraView.Camera.DeviceId,
                        MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                        AudioDeviceId = cameraView.Microphone.DeviceId
                    });

                    MediaEncodingProfile profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
                    recordStream = new(file, FileMode.Create);
                    mediaRecording = await mediaCapture.PrepareLowLagRecordToStreamAsync(profile, recordStream.AsRandomAccessStream());

                    frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord
                                                                                  && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;
                    if (frameSource != null)
                    {
                        MediaFrameFormat frameFormat;
                        if (Resolution.Width <= 0 || Resolution.Height <= 0)
                            frameFormat = frameSource.SupportedFormats.OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height).FirstOrDefault();
                        else
                            frameFormat = frameSource.SupportedFormats.First(f => f.VideoFormat.Width == Resolution.Width && f.VideoFormat.Height == Resolution.Height);

                        if (frameFormat != null)
                        {
                            await frameSource.SetFormatAsync(frameFormat);
                            UpdateTorch();
                            SetZoomFactor(cameraView.ZoomFactor);
                            mediaElement.AutoPlay = true;
                            mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
                            await mediaRecording.StartAsync();
                            recording = true;
                        }
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
                result = cameraView.Camera == null ? CameraResult.NoCameraSelected : CameraResult.NoMicrophoneSelected;
        }
        else
            result = CameraResult.NotInitiated;

        return result;
    }
    internal async Task<CameraResult> StopRecordingAsync()
    {
        return await StartCameraAsync(cameraView.PhotosResolution);
    }
    internal async Task<CameraResult> StartCameraAsync(Size PhotosResolution)
    {
        CameraResult result = CameraResult.Success;

        if (initiated)
        {
            if (started) await StopCameraAsync();
            if (cameraView.Camera != null)
            {
                while (!mediaLoaded) await Task.Delay(50);
                started = true;
                mediaCapture = new MediaCapture();
                try
                {
                    await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
                    {
                        SourceGroup = sGroups.First(s => s.Id == cameraView.Camera.DeviceId),
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

                        MediaFrameFormat frameFormat;
                        if (PhotosResolution.Width <= 0 || PhotosResolution.Height <= 0)
                            frameFormat = frameSource.SupportedFormats.OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height).FirstOrDefault();
                        else
                            frameFormat = frameSource.SupportedFormats.First(f => f.VideoFormat.Width == PhotosResolution.Width && f.VideoFormat.Height == PhotosResolution.Height);

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
        }
        else
            result = CameraResult.NotInitiated;
        return result;
    }
    private void ProcessQRImage(SoftwareBitmap simg)
    {
        if (simg != null)
        {
            Task.Run(() =>
            {
                if (cameraView.BarcodeDecoder != null)
                {
                    var img = SoftwareBitmap.Convert(simg, BitmapPixelFormat.Gray8, BitmapAlphaMode.Ignore);
                    if (img != null)
                    {
                        if (img.PixelWidth > 0 && img.PixelHeight > 0)
                            cameraView.BarcodeDecoder.Decode(img);
                        img.Dispose();
                    }
                    lock (cameraView.currentThreadsLocker) cameraView.currentThreads--;
                    GC.Collect();
                }
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
                bool processQR = false;
                lock (cameraView.currentThreadsLocker)
                {
                    if (cameraView.currentThreads < cameraView.BarCodeDetectionMaxThreads)
                    {
                        cameraView.currentThreads++;
                        processQR = true;
                    }
                }
                if (processQR)
                {
                    var frame = sender.TryAcquireLatestFrame();
                    ProcessQRImage(frame.VideoMediaFrame.SoftwareBitmap);
                    frames = 0;
                }
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
        }
        else
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
        /*
        CameraCaptureUI cameraCaptureUI = new CameraCaptureUI();
        cameraCaptureUI.PhotoSettings.Format = imageFormat switch
        {
            ImageFormat.PNG => CameraCaptureUIPhotoFormat.Png,
            _=> CameraCaptureUIPhotoFormat.Jpeg
        };
        if (cameraView.PhotosResolution.Width != 0 && cameraView.PhotosResolution.Height != 0)
            cameraCaptureUI.PhotoSettings.CroppedSizeInPixels = new(cameraView.PhotosResolution.Width, cameraView.PhotosResolution.Height);
        try
        {
            var photo = await cameraCaptureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);
            var rstr = await photo.OpenReadAsync();
            MemoryStream stream = new();
            rstr.AsStreamForRead().CopyTo(stream);
            rstr.Dispose();
            await photo.DeleteAsync();
            return stream;
        }
        catch { }
        */
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
                    /*
                    if (cameraView.PhotosResolution.Width != 0 && cameraView.PhotosResolution.Height != 0)
                    {
                        encoder.BitmapTransform.ScaledWidth = (uint)cameraView.PhotosResolution.Width;
                        encoder.BitmapTransform.ScaledHeight = (uint)cameraView.PhotosResolution.Height;
                    }
                    */
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
                    }
                    else
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
        }
        else
            result = false;
        return result;
    }

    public void Dispose()
    {
        StopCameraAsync().Wait();
    }
}