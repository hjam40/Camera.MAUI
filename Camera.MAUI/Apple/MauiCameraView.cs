#if IOS || MACCATALYST
using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using MediaPlayer;
using System.IO;
using UIKit;

namespace Camera.MAUI.Platforms.Apple;

internal class MauiCameraView : UIView, IAVCaptureVideoDataOutputSampleBufferDelegate, IAVCaptureFileOutputRecordingDelegate, IAVCapturePhotoCaptureDelegate
{
    private CameraInfo Camera { get; set; }
    private readonly List<CameraInfo> Cameras = new();
    private readonly List<MicrophoneInfo> Micros = new();
    private MicrophoneInfo Microphone { get; set; }
    private AVCaptureDevice[] camDevices;
    private AVCaptureDevice[] micDevices;
    private readonly CameraView cameraView;
    private readonly AVCaptureVideoPreviewLayer PreviewLayer;
    private readonly AVCaptureVideoDataOutput videoDataOutput;
    private AVCaptureMovieFileOutput recordOutput;
    private AVCapturePhotoOutput photoOutput;
    private readonly AVCaptureSession captureSession;
    private AVCaptureDevice captureDevice;
    private AVCaptureDevice micDevice;
    private AVCaptureInput captureInput = null;
    private AVCaptureInput micInput = null;
    private bool started = false;
    private CIImage lastCapture;
    private readonly object lockCapture = new();
    private readonly DispatchQueue cameraDispacher;
    private int frames = 0;
    private bool initiated = false;
    private bool snapping = false;
    private bool photoTaken = false;
    private bool photoError = false;
    private UIImage photo;

    public MauiCameraView(CameraView cameraView)
    {
        this.cameraView = cameraView;

        captureSession = new AVCaptureSession
        {   
            SessionPreset = AVCaptureSession.PresetPhoto
        };
        PreviewLayer = new(captureSession)
        {
            VideoGravity = AVLayerVideoGravity.ResizeAspectFill
        };
        Layer.AddSublayer(PreviewLayer);
        videoDataOutput = new AVCaptureVideoDataOutput();
        var videoSettings = NSDictionary.FromObjectAndKey(
            new NSNumber((int)CVPixelFormatType.CV32BGRA),
            CVPixelBuffer.PixelFormatTypeKey);
        videoDataOutput.WeakVideoSettings = videoSettings;
        videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
        photoOutput = new AVCapturePhotoOutput();
        cameraDispacher = new DispatchQueue("CameraDispacher");

        videoDataOutput.AlwaysDiscardsLateVideoFrames = true;
        videoDataOutput.SetSampleBufferDelegate(this, cameraDispacher);
        NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, OrientationChanged);
        InitDevices();
    }
    private void OrientationChanged(NSNotification notification)
    {
        LayoutSubviews();
    }
    private void InitDevices()
    {
        if (!initiated)
        {
            try
            {
                var deviceDescoverySession = AVCaptureDeviceDiscoverySession.Create(new AVCaptureDeviceType[] { AVCaptureDeviceType.BuiltInWideAngleCamera }, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
                camDevices = deviceDescoverySession.Devices;
                foreach (var device in camDevices)
                {
                    CameraPosition position = device.Position switch
                    {
                        AVCaptureDevicePosition.Back => CameraPosition.Back,
                        AVCaptureDevicePosition.Front => CameraPosition.Front,
                        _ => CameraPosition.Unknow
                    };
                    Cameras.Add(new CameraInfo 
                    { 
                        Name = device.LocalizedName, DeviceId = device.UniqueID, Position = position,
                        HasFlashUnit = device.FlashAvailable,
                        MinZoomFactor = (float)device.MinAvailableVideoZoomFactor,
                        MaxZoomFactor = (float)device.MaxAvailableVideoZoomFactor
                    });
                }
                Camera = Cameras.FirstOrDefault();
                deviceDescoverySession.Dispose();
                var aSession = AVCaptureDeviceDiscoverySession.Create(new AVCaptureDeviceType[] { AVCaptureDeviceType.BuiltInMicrophone }, AVMediaTypes.Audio, AVCaptureDevicePosition.Unspecified);
                micDevices = aSession.Devices;
                foreach (var device in micDevices)
                    Micros.Add(new MicrophoneInfo { Name = device.LocalizedName, DeviceId = device.UniqueID });
                Microphone = Micros.FirstOrDefault();
                aSession.Dispose();
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
            }
        }
    }
    public async Task<CameraResult> StartRecordingAsync(string file)
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            if (started) StopCamera();
            if (await CameraView.RequestPermissions(true))
            {
                if (Camera != null && captureSession != null)
                {
                    try
                    {
                        frames = 0;
                        captureDevice = camDevices.First(d => d.UniqueID == Camera.DeviceId);
                        captureInput = new AVCaptureDeviceInput(captureDevice, out var err);
                        captureSession.AddInput(captureInput);
                        micDevice = micDevices.First(d => d.UniqueID == Microphone.DeviceId);
                        micInput = new AVCaptureDeviceInput(micDevice, out err);
                        captureSession.AddInput(micInput);

                        captureSession.AddOutput(videoDataOutput);
                        recordOutput = new AVCaptureMovieFileOutput();
                        captureSession.AddOutput(recordOutput);

                        var movieFileOutputConnection = recordOutput.Connections[0];
                        movieFileOutputConnection.VideoOrientation = (AVCaptureVideoOrientation)UIDevice.CurrentDevice.Orientation;
                        captureSession.StartRunning();
                        if (!File.Exists(file)) File.Create(file).Close();

                        recordOutput.StartRecordingToOutputFile(NSUrl.FromFilename(file), this);
                        UpdateMirroredImage();
                        SetZoomFactor(cameraView.ZoomFactor);
                        started = true;
                    }
                    catch
                    {
                        result = CameraResult.AccessError;
                    }
                }
                else
                    result = CameraResult.AccessError;
            }
            else
                result = CameraResult.AccessDenied;
        }
        else
            result = CameraResult.NotInitiated;
        return result;
    }
    public Task<CameraResult> StopRecordingAsync()
    {
        return StartCameraAsync();
    }

    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            if (started) StopCamera();
            if (await CameraView.RequestPermissions())
            {
                if (Camera != null && captureSession != null)
                {
                    try
                    {
                        frames = 0;
                        captureDevice = camDevices.First(d => d.UniqueID == Camera.DeviceId);
                        captureInput = new AVCaptureDeviceInput(captureDevice, out var err);
                        captureSession.AddInput(captureInput);
                        captureSession.AddOutput(videoDataOutput);
                        captureSession.AddOutput(photoOutput);
                        captureSession.StartRunning();
                        UpdateMirroredImage();
                        SetZoomFactor(cameraView.ZoomFactor);
                        started = true;
                    }
                    catch
                    {
                        result = CameraResult.AccessError;
                    }
                }
                else
                    result = CameraResult.AccessError;
            }
            else
                result = CameraResult.AccessDenied;
        }else
            result = CameraResult.NotInitiated;
        return result;
    }
    public CameraResult StopCamera()
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            try
            {
                if (captureSession != null)
                {
                    if (captureSession.Running)
                        captureSession.StopRunning();
                    if (recordOutput != null)
                    {
                        recordOutput.StopRecording();
                        captureSession.RemoveOutput(recordOutput);
                        recordOutput.Dispose();
                        recordOutput = null;
                    }
                    captureSession.RemoveOutput(videoDataOutput);
                    foreach (var input in captureSession.Inputs)
                    {
                        captureSession.RemoveInput(input);
                        input.Dispose();
                    }
                }
                started = false;
            }
            catch
            {
                result = CameraResult.AccessError;
            }
        }else
            result = CameraResult.NotInitiated;

        return result;
    }
    public void DisposeControl()
    {
        if (started) StopCamera();
        NSNotificationCenter.DefaultCenter.RemoveObserver(UIDevice.OrientationDidChangeNotification);
        PreviewLayer?.Dispose();
        captureSession?.Dispose();
        Dispose();
    }
    public async void UpdateCamera()
    {
        if (cameraView != null && cameraView.Camera != null)
        {
            if (started)
            {
                StopCamera();
                Camera = cameraView.Camera;
                await StartCameraAsync();
            }else
                Camera = cameraView.Camera;
        }
    }
    public void UpdateMirroredImage()
    {
        if (cameraView != null && PreviewLayer.Connection != null)
        {
            if (PreviewLayer.Connection.AutomaticallyAdjustsVideoMirroring)
                PreviewLayer.Connection.AutomaticallyAdjustsVideoMirroring = false;
            if (cameraView.MirroredImage)
                PreviewLayer.Connection.VideoMirrored = true;
            else
                PreviewLayer.Connection.VideoMirrored = false;

            UpdateTorch();
        }
    }
    public void SetZoomFactor(float zoom)
    {
        if (Camera != null && captureDevice != null)
        {
            captureDevice.LockForConfiguration(out NSError error);
            if (error == null)
            {
                captureDevice.VideoZoomFactor = Math.Max(Camera.MinZoomFactor, Math.Min(zoom, Camera.MaxZoomFactor));
                captureDevice.UnlockForConfiguration();
            }
        }
    }
    public void UpdateTorch()
    {
        if (captureDevice != null && cameraView != null)
        {
            captureDevice.LockForConfiguration(out NSError error);
            if (error == null)
            {
                if (captureDevice.HasTorch && captureDevice.TorchAvailable)
                    captureDevice.TorchMode = cameraView.TorchEnabled ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
                captureDevice.UnlockForConfiguration();
            }
        }
    }

    internal async Task<Stream> TakePhotoAsync(ImageFormat imageFormat)
    {
        photoError = photoTaken = false;
        var photoSettings = AVCapturePhotoSettings.Create();
        photoSettings.FlashMode = cameraView.FlashMode switch
        {
            FlashMode.Auto => AVCaptureFlashMode.Auto,
            FlashMode.Enabled => AVCaptureFlashMode.On,
            _ => AVCaptureFlashMode.Off
        };
        photoOutput.CapturePhoto(photoSettings, this);
        while(!photoTaken && !photoError) await Task.Delay(50);

        if (photoError || photo == null)
            return null;
        else
        {
            UIImageOrientation orientation = UIDevice.CurrentDevice.Orientation switch
            {
                UIDeviceOrientation.LandscapeRight => UIImageOrientation.Down,
                UIDeviceOrientation.LandscapeLeft => UIImageOrientation.Up,
                UIDeviceOrientation.PortraitUpsideDown => UIImageOrientation.Left,
                _ => UIImageOrientation.Right
            };
            if (photo.Orientation != orientation)
                photo = UIImage.FromImage(photo.CGImage, photo.CurrentScale, orientation);
            MemoryStream stream = new();
            switch (imageFormat)
            {
                case ImageFormat.JPEG:
                    photo.AsJPEG().AsStream().CopyTo(stream);
                    break;
                default:
                    photo.AsPNG().AsStream().CopyTo(stream);
                    break;
            }
            stream.Position = 0;
            return stream;
        }
    }
    public ImageSource GetSnapShot(ImageFormat imageFormat, bool auto = false)
    {
        ImageSource result = null;

        if (started && lastCapture != null && !snapping)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                snapping = true;
                try
                {
                    lock (lockCapture)
                    {
                        var ciContext = new CIContext();
                        CGImage cgImage = ciContext.CreateCGImage(lastCapture, lastCapture.Extent);
                        UIImageOrientation orientation = UIDevice.CurrentDevice.Orientation switch
                        {
                            UIDeviceOrientation.LandscapeRight => UIImageOrientation.Down,
                            UIDeviceOrientation.LandscapeLeft => UIImageOrientation.Up,
                            UIDeviceOrientation.PortraitUpsideDown => UIImageOrientation.Left,
                            _ => UIImageOrientation.Right
                        };
                        var image = UIImage.FromImage(cgImage, UIScreen.MainScreen.Scale, orientation);
                        var image2 = CropImage(image);
                        MemoryStream stream = new();
                        switch (imageFormat)
                        {
                            case ImageFormat.JPEG:
                                image2.AsJPEG().AsStream().CopyTo(stream);
                                break;
                            default:
                                image2.AsPNG().AsStream().CopyTo(stream);
                                break;
                        }
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
                    }
                }
                catch
                {
                }
                snapping = false;
            }).Wait();
        }

        return result;
    }
    public bool SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = true;

        if (started && lastCapture != null)
        {
            if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);            
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    lock (lockCapture)
                    {
                        var ciContext = new CIContext();
                        CGImage cgImage = ciContext.CreateCGImage(lastCapture, lastCapture.Extent);
                        UIImageOrientation orientation = UIDevice.CurrentDevice.Orientation switch
                        {
                            UIDeviceOrientation.LandscapeRight => UIImageOrientation.Down,
                            UIDeviceOrientation.LandscapeLeft => UIImageOrientation.Up,
                            UIDeviceOrientation.PortraitUpsideDown => UIImageOrientation.Left,
                            _ => UIImageOrientation.Right
                        };
                        var image = UIImage.FromImage(cgImage, UIScreen.MainScreen.Scale, orientation);
                        var image2 = CropImage(image);
                        switch (imageFormat)
                        {
                            case ImageFormat.PNG:
                                image2.AsPNG().Save(NSUrl.FromFilename(SnapFilePath), true);
                                break;
                            case ImageFormat.JPEG:
                                image2.AsJPEG().Save(NSUrl.FromFilename(SnapFilePath), true);
                                break;
                        }
                    }
                }
                catch
                {
                    result = false;
                }
            }).Wait();
        }
        else
            result = false;
        return result;
    }
    public UIImage CropImage(UIImage originalImage)
    {
        nfloat x, y, width, height;

        if (originalImage.Size.Width <= originalImage.Size.Height)
        {
            width = originalImage.Size.Width;
            height = (Frame.Size.Height * originalImage.Size.Width) / Frame.Size.Width;
        }
        else
        {
            height = originalImage.Size.Height;
            width = (Frame.Size.Width * originalImage.Size.Height) / Frame.Size.Height;
        }

        x = (nfloat)((originalImage.Size.Width - width) / 2.0);
        y = (nfloat)((originalImage.Size.Height - height) / 2.0);

        UIGraphics.BeginImageContextWithOptions(originalImage.Size, false, 1);
        if (cameraView.MirroredImage)
        {
            var context = UIGraphics.GetCurrentContext();
            context.ScaleCTM(-1, 1);
            context.TranslateCTM(-originalImage.Size.Width, 0);
        }
        originalImage.Draw(new CGPoint(0, 0));
        UIImage croppedImage = UIImage.FromImage(UIGraphics.GetImageFromCurrentImageContext().CGImage.WithImageInRect(new CGRect(new CGPoint(x, y), new CGSize(width, height))));
        UIGraphics.EndImageContext();

        return croppedImage;
    }
    private void ProccessQR()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                UIImage image2;
                lock (lockCapture)
                {
                    var ciContext = new CIContext();
                    CGImage cgImage = ciContext.CreateCGImage(lastCapture, lastCapture.Extent);
                    var image = UIImage.FromImage(cgImage, UIScreen.MainScreen.Scale, UIImageOrientation.Right);
                    image2 = CropImage(image);
                }
                cameraView.DecodeBarcode(image2);
            }
            catch
            {
            }
        });
    }
    private void ProcessImage(CIImage capture)
    {
        int currentFrames = frames;
        
        new Task(() =>
        {
            lock (lockCapture)
            {
                lastCapture?.Dispose();
                lastCapture = capture;
            }
            if (!snapping && cameraView != null && cameraView.AutoSnapShotSeconds > 0 && (DateTime.Now - cameraView.lastSnapshot).TotalSeconds >= cameraView.AutoSnapShotSeconds)
                cameraView.RefreshSnapshot(GetSnapShot(cameraView.AutoSnapShotFormat, true));
            else if (cameraView.BarCodeDetectionEnabled && currentFrames >= cameraView.BarCodeDetectionFrameRate)
                ProccessQR();
        }).Start();
    }

    [Export("captureOutput:didOutputSampleBuffer:fromConnection:")]
    public void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
    {
        frames++;
        if (frames >= Math.Min(10, cameraView.BarCodeDetectionFrameRate))
        {
            var capture = CIImage.FromImageBuffer(sampleBuffer.GetImageBuffer());
            ProcessImage(capture);
            sampleBuffer.Dispose();
            frames = 0;
            GC.Collect();
        }
        else
        {
            sampleBuffer?.Dispose();
        }
    }
    [Export("captureOutput:didFinishProcessingPhotoSampleBuffer:previewPhotoSampleBuffer:resolvedSettings:bracketSettings:error:")]
    void DidFinishProcessingPhoto(AVCapturePhotoOutput captureOutput, CMSampleBuffer photoSampleBuffer, CMSampleBuffer previewPhotoSampleBuffer, AVCaptureResolvedPhotoSettings resolvedSettings, AVCaptureBracketedStillImageSettings bracketSettings, NSError error)
    {
        if (photoSampleBuffer == null)
        {
            photoError = true;
            return;
        }

        NSData imageData = AVCapturePhotoOutput.GetJpegPhotoDataRepresentation(photoSampleBuffer, previewPhotoSampleBuffer);

        photo = new UIImage(imageData);
        photoTaken = true;
    }

    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        CATransform3D transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
        switch (UIDevice.CurrentDevice.Orientation)
        {
            case UIDeviceOrientation.Portrait:
                transform = CATransform3D.MakeRotation(0, 0, 0, 1.0f);
                break;
            case UIDeviceOrientation.PortraitUpsideDown:
                transform = CATransform3D.MakeRotation((nfloat)Math.PI, 0, 0, 1.0f);
                break;
            case UIDeviceOrientation.LandscapeLeft:
                transform = CATransform3D.MakeRotation((nfloat)(-Math.PI / 2), 0, 0, 1.0f);
                break;
            case UIDeviceOrientation.LandscapeRight:
                transform = CATransform3D.MakeRotation((nfloat)Math.PI / 2, 0, 0, 1.0f);
                break;
        }

        PreviewLayer.Transform = transform;
        PreviewLayer.Frame = Layer.Bounds;
    }

    public void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError error)
    {
        
    }
}
#endif
