#if IOS || MACCATALYST
using AVFoundation;
using CoreAnimation;
using CoreFoundation;
using CoreGraphics;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;

namespace Camera.MAUI.Platforms.Apple;

internal class MauiCameraView : UIView, IAVCaptureVideoDataOutputSampleBufferDelegate
{
    public CameraInfo Camera { get; set; }
    public float MinZoomFactor { get; } = 1f;
    public float MaxZoomFactor
    {
        get
        {
            if (captureDevice != null)
                return (float)captureDevice.ActiveFormat.VideoMaxZoomFactor;
            else
                return 1f;
        }
    }

    private readonly List<CameraInfo> Cameras = new();
    private AVCaptureDevice[] devices;
    private readonly CameraView cameraView;
    private readonly AVCaptureVideoPreviewLayer PreviewLayer;
    private readonly AVCaptureVideoDataOutput videoDataOutput;
    private readonly AVCaptureSession captureSession;
    private AVCaptureDevice captureDevice;
    private AVCaptureInput captureInput = null;
    private bool started = false;
    private CIImage lastCapture;
    private readonly object lockCapture = new();
    private readonly DispatchQueue cameraDispacher;
    private int frames = 0;
    private bool initiated = false;

    public MauiCameraView(CameraView cameraView)
    {
        this.cameraView = cameraView;

        captureSession = new AVCaptureSession
        {   
            SessionPreset = AVCaptureSession.PresetHigh
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
                devices = deviceDescoverySession.Devices;
                foreach (var device in devices)
                {
                    CameraPosition position = device.Position switch
                    {
                        AVCaptureDevicePosition.Back => CameraPosition.Back,
                        AVCaptureDevicePosition.Front => CameraPosition.Front,
                        _ => CameraPosition.Unknow
                    };
                    Cameras.Add(new CameraInfo { Name = device.LocalizedName, DeviceId = device.UniqueID, Position = position });
                }
                Camera = Cameras.FirstOrDefault();
                if (cameraView != null)
                {
                    cameraView.Cameras.Clear();
                    foreach (var cam in Cameras) cameraView.Cameras.Add(cam);
                }
                deviceDescoverySession.Dispose();
                initiated = true;
            }
            catch
            {
            }
        }
    }
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            if (started) StopCamera();
            if (await RequestPermissions())
            {
                if (Camera != null && captureSession != null)
                {
                    try
                    {
                        frames = 0;
                        captureDevice = devices.First(d => d.UniqueID == Camera.DeviceId);
                        captureInput = new AVCaptureDeviceInput(captureDevice, out var err);
                        captureSession.AddInput(captureInput);
                        captureSession.AddOutput(videoDataOutput);
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
                    captureSession.RemoveOutput(videoDataOutput);

                    if (captureInput != null && captureSession.Inputs.Length > 0 && captureSession.Inputs.Contains(captureInput))
                    {
                        captureSession.RemoveInput(captureInput);
                        captureInput.Dispose();
                        captureInput = null;
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
            if (started) StopCamera();
            Camera = cameraView.Camera;
            if (started) await StartCameraAsync();
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
        if (captureDevice != null)
        {
            captureDevice.LockForConfiguration(out NSError error);
            if (error == null)
            {
                captureDevice.VideoZoomFactor = Math.Max(MinZoomFactor, Math.Min(zoom, MaxZoomFactor));
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
    public void UpdateFlashMode()
    {
        if (captureDevice != null && cameraView != null)
        {
            try
            {
                captureDevice.LockForConfiguration(out NSError error);
                if (captureDevice.HasTorch && captureDevice.TorchAvailable)
                {
                    switch (cameraView.FlashMode)
                    {
                        case FlashMode.Auto:
                            captureDevice.TorchMode =  AVCaptureTorchMode.Auto;
                            break;
                        case FlashMode.Enabled:
                            captureDevice.TorchMode = AVCaptureTorchMode.On;
                            break;
                        case FlashMode.Disabled:
                            captureDevice.TorchMode = AVCaptureTorchMode.Off;
                            break;
                    }
                }
                captureDevice.UnlockForConfiguration();
            }
            catch
            {

            }
        }
    }
    public ImageSource GetSnapShot(ImageFormat imageFormat)
    {
        ImageSource result = null;

        if (started && lastCapture != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
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
                        result = ImageSource.FromStream(() => stream);
                    }
                }
                catch
                {
                }
            });
        }

        return result;
    }
    public bool SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = true;

        if (started && lastCapture != null)
        {
            if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
            MainThread.BeginInvokeOnMainThread(() =>
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
            });
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
            if (cameraView.BarCodeDetectionEnabled && currentFrames >= cameraView.BarCodeDetectionFrameRate)
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

    private static async Task<bool> RequestPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) return false;
        }
        return true;
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
}
#endif
