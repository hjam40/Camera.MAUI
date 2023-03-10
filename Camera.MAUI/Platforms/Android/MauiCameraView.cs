using Android.Content;
using Android.Widget;
using AndroidX.Camera.View;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.Core;
using Java.Util.Concurrent;
using Android.Graphics;
using AndroidX.Camera.Core.Impl;
using AndroidX.Camera.Camera2.InterOp;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Android.Hardware.Camera2;

namespace Camera.MAUI.Platforms.Android;

internal class MauiCameraView: GridLayout
{
    public CameraInfo Camera { get; set; }
    public float MinZoomFactor
    {
        get
        {
            if (camera != null)
                return (camera.CameraInfo.ZoomState.Value as IZoomState).MinZoomRatio;
            else
                return 1f;
        }
    }
    public float MaxZoomFactor
    {
        get
        {
            if (camera != null)
                return (camera.CameraInfo.ZoomState.Value as IZoomState).MaxZoomRatio;
            else
                return 1f;
        }
    }
    private record InternalCameraInfo
    {
        public CameraSelector CameraSelector { get; set; }
        public string CameraId { get; set; }
    }
    private readonly List<CameraInfo> Cameras = new();
    private readonly List<InternalCameraInfo> InternalCameras = new();
    private readonly CameraView cameraView;
    private readonly PreviewView previewView;
    private IExecutorService executorService;
    private ImageAnalysis imageAnalyzer;
    private ImageAnalyzer frameAnalyzer;
    private ProcessCameraProvider cameraProvider;
    private ICamera camera;
    private Preview cameraPreview;
    private bool started = false;
    private PreviewView.ImplementationMode currentImplementationMode = PreviewView.ImplementationMode.Performance;
    private int frames = 0;
    private bool initiated = false;

    public MauiCameraView(Context context, CameraView cameraView) : base(context)
    {
        this.cameraView = cameraView;
        previewView = new PreviewView(context);
        previewView.SetScaleType(PreviewView.ScaleType.FillCenter);
        AddView(previewView);
        InitDevices();
    }

    private void InitDevices()
    {
        if (!initiated)
        {
            try
            {
                var initCameraProvider = ProcessCameraProvider.GetInstance(Context);
                cameraProvider = (ProcessCameraProvider)initCameraProvider.Get();
                cameraPreview = new Preview.Builder().Build();
                cameraPreview.SetSurfaceProvider(previewView.SurfaceProvider);
                executorService = Executors.NewSingleThreadExecutor();
                frameAnalyzer = new ImageAnalyzer();
                frameAnalyzer.FrameReady += FrameAnalyzer_FrameReady;
                imageAnalyzer = new ImageAnalysis.Builder().SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest).Build();
                imageAnalyzer.SetAnalyzer(executorService, frameAnalyzer);

                if (cameraProvider != null)
                {
                    foreach (var camInfo in cameraProvider.AvailableCameraInfos)
                    {
                        var c2cInfo = Camera2CameraInfo.From(camInfo);
                        if ((int)(c2cInfo.GetCameraCharacteristic(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Back)
                            Cameras.Add(new CameraInfo 
                            { 
                                Name = "Back Camera", DeviceId = c2cInfo.CameraId, Position = CameraPosition.Back,
                                HasFlashUnit = camInfo.HasFlashUnit,
                                MinZoomFactor = (camInfo.ZoomState.Value as IZoomState).MinZoomRatio,
                                MaxZoomFactor = (camInfo.ZoomState.Value as IZoomState).MaxZoomRatio
                            });
                        else if ((int)(c2cInfo.GetCameraCharacteristic(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Front)
                            Cameras.Add(new CameraInfo 
                            { 
                                Name = "Front Camera", DeviceId = c2cInfo.CameraId, Position = CameraPosition.Front,
                                HasFlashUnit = camInfo.HasFlashUnit,
                                MinZoomFactor = (camInfo.ZoomState.Value as IZoomState).MinZoomRatio,
                                MaxZoomFactor = (camInfo.ZoomState.Value as IZoomState).MaxZoomRatio
                            });
                        else
                        {
                            Cameras.Add(new CameraInfo 
                            {
                                Name = "Camera " + c2cInfo.CameraId, DeviceId = c2cInfo.CameraId, Position = CameraPosition.Unknow,
                                HasFlashUnit = camInfo.HasFlashUnit,
                                MinZoomFactor = (camInfo.ZoomState.Value as IZoomState).MinZoomRatio,
                                MaxZoomFactor = (camInfo.ZoomState.Value as IZoomState).MaxZoomRatio
                            });
                        }
                        InternalCameras.Add(new InternalCameraInfo { CameraSelector = camInfo.CameraSelector, CameraId = c2cInfo.CameraId });
                    }
                    Camera = Cameras.FirstOrDefault();
                    if (cameraView != null)
                    {
                        cameraView.Cameras.Clear();
                        foreach (var cam in Cameras) cameraView.Cameras.Add(cam);
                    }
                }
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
            previewView.LayoutParameters = new LayoutParams { Width = Width, Height = Height };
            if (started) StopCamera();
            if (await RequestPermissions())
            {
                if (Camera != null && cameraProvider != null)
                {
                    try
                    {
                        if (previewView.ScaleX == -1)
                            SetImplementationMode(PreviewView.ImplementationMode.Compatible);
                        else
                            SetImplementationMode(PreviewView.ImplementationMode.Performance);
                        CameraSelector cameraSelector = InternalCameras.First(c => c.CameraId == Camera.DeviceId).CameraSelector;
                        frames = 0;
                        
                        if (Context is AndroidX.Lifecycle.ILifecycleOwner lifecycleOwner)
                            camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, cameraPreview, imageAnalyzer);
                        else if (Platform.CurrentActivity is AndroidX.Lifecycle.ILifecycleOwner maLifecycleOwner)
                            camera = cameraProvider.BindToLifecycle(maLifecycleOwner, cameraSelector, cameraPreview, imageAnalyzer);
                        
                        UpdateTorch();
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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    cameraProvider?.UnbindAll();
                    camera?.Dispose();
                }
                catch (System.Exception)
                {
                    result = CameraResult.AccessError;
                }
            });
            started = false;
        }
        else
            result = CameraResult.NotInitiated;
        return result;
    }
    public void DisposeControl()
    {
        if (started) StopCamera();
        cameraPreview?.Dispose();
        executorService?.Shutdown();
        executorService?.Dispose();
        frameAnalyzer?.Dispose();
        imageAnalyzer?.Dispose();
        RemoveAllViews();
        previewView?.Dispose();
        Dispose();
    }
    private void ProccessQR()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var bitmap = previewView.Bitmap;
            Task.Run(() =>
            {
                cameraView.DecodeBarcode(bitmap);
                bitmap.Dispose();
                GC.Collect();
            });
        });
    }
    private void FrameAnalyzer_FrameReady(object sender, EventArgs e)
    {
        if (cameraView.BarCodeDetectionEnabled)
        {
            frames++;
            if (frames >= cameraView.BarCodeDetectionFrameRate)
            {
                ProccessQR();
                frames = 0;
            }
        }
    }
    public ImageSource GetSnapShot(ImageFormat imageFormat)
    {
        ImageSource result = null;

        if (started)
        {
            Bitmap bitmap = null;
            float scale = 1;
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                bitmap = previewView.Bitmap;
                scale = previewView.ScaleX;
            }).Wait();
            if (bitmap != null)
            {
                try
                {
                    if (scale == -1)
                    {
                        Matrix matrix = new();
                        matrix.PreScale(-1, 1);
                        bitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, false);
                    }

                    var iformat = imageFormat switch
                    {
                        ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                        _ => Bitmap.CompressFormat.Png
                    };
                    MemoryStream stream = new();
                    bitmap.Compress(iformat, 100, stream);
                    stream.Position = 0;
                    result = ImageSource.FromStream(() => stream);
                }
                catch
                {
                }
            }
        }
        GC.Collect();
        return result;
    }

    public bool SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = true;

        if (started)
        {
            Bitmap bitmap = null;
            float scale = 1;
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                bitmap = previewView.Bitmap;
                scale = previewView.ScaleX;
            }).Wait();
            if (bitmap != null)
            {
                try
                {
                    if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
                    if (scale == -1)
                    {
                        Matrix matrix = new();
                        matrix.PreScale(-1, 1);
                        bitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, false);
                    }

                    var iformat = imageFormat switch
                    {
                        ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                        _ => Bitmap.CompressFormat.Png
                    };
                    using FileStream stream = new(SnapFilePath, FileMode.OpenOrCreate);
                    bitmap.Compress(iformat, 80, stream);
                    stream.Close();
                }
                catch (System.Exception)
                {
                    result = false;
                }
            }
        }else
            result = false;

        GC.Collect();
        return result;
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
        if (cameraView != null)
        {
            if (cameraView.MirroredImage)
            {
                previewView.ScaleX = -1;
                SetImplementationMode(PreviewView.ImplementationMode.Compatible);
            }
            else
            {
                previewView.ScaleX = 1;
            }
        }
    }
    public void UpdateTorch()
    {
        if (camera != null && cameraView != null)
        {
            if (camera.CameraInfo.HasFlashUnit)
                camera.CameraControl.EnableTorch(cameraView.TorchEnabled);
        }
    }

    public void UpdateFlashMode()
    {
        if (camera != null && cameraView != null)
        {
            try
            {
                if (camera.CameraInfo.HasFlashUnit)
                {
                    switch (cameraView.FlashMode)
                    {
                        case FlashMode.Auto:
                            camera.CameraControl.EnableTorch(true);
                            break;
                        case FlashMode.Enabled:
                            camera.CameraControl.EnableTorch(true);
                            break;
                        case FlashMode.Disabled:
                            camera.CameraControl.EnableTorch(false);
                            break;
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }
    }
    public void SetZoomFactor(float zoom)
    {
        if (camera != null)
            camera.CameraControl.SetZoomRatio(Math.Max(MinZoomFactor, Math.Min(zoom, MaxZoomFactor)));
    }
    private async void SetImplementationMode(PreviewView.ImplementationMode mode)
    {

        if (started && currentImplementationMode != mode)
        {
            StopCamera();
            previewView.SetImplementationMode(mode);
            await StartCameraAsync();
        }else
            previewView.SetImplementationMode(mode);

        currentImplementationMode = mode;
    }
    class ImageAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        public IImageProxy Image { get; private set; }
        public event EventHandler FrameReady;
        public void Analyze(IImageProxy image)
        {
            Image = image;
            FrameReady?.Invoke(this, EventArgs.Empty);
            image.Close();
        }
    }
}


