using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util.Concurrent;
using CameraCharacteristics = Android.Hardware.Camera2.CameraCharacteristics;
using Class = Java.Lang.Class;
using Rect = Android.Graphics.Rect;
using RectF = Android.Graphics.RectF;
using Size = Android.Util.Size;
using SizeF = Android.Util.SizeF;

namespace Camera.MAUI;

internal class MauiCameraView : GridLayout
{
    private readonly CameraView cameraView;
    private IExecutorService executorService;
    private bool started = false;
    private int frames = 0;
    private bool initiated = false;
    private bool snapping = false;
    private bool recording = false;
    private readonly Context context;
    private readonly object changeCameraStateLocker = new();

    private readonly TextureView textureView;
    public CameraCaptureSession previewSession;
    public MediaRecorder mediaRecorder;
    private CaptureRequest.Builder previewBuilder;
    private CameraDevice cameraDevice;
    private readonly MyCameraStateCallback stateListener;
    private Size videoSize;
    private CameraManager cameraManager;
    private AudioManager audioManager;
    private readonly System.Timers.Timer timer;
    private readonly SparseIntArray ORIENTATIONS = new();
    private CameraCharacteristics camChars;
    private PreviewCaptureStateCallback sessionCallback;
    private byte[] capturePhoto = null;
    private bool captureDone = false;
    private readonly ImageAvailableListener photoListener;
    private HandlerThread backgroundThread;
    private Handler backgroundHandler;
    private ImageReader imgReader;

    public MauiCameraView(Context context, CameraView cameraView) : base(context)
    {
        this.context = context;
        this.cameraView = cameraView;

        textureView = new(context);
        //timer = new(33.3);
        timer = new(TimeSpan.FromSeconds(1));
        timer.Elapsed += Timer_Elapsed;
        stateListener = new MyCameraStateCallback(this);
        photoListener = new ImageAvailableListener(this);
        AddView(textureView);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
        ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
        InitDevices();
    }

    private void InitDevices()
    {
        if (!initiated && cameraView != null)
        {
            cameraManager = (CameraManager)context.GetSystemService(Context.CameraService);
            audioManager = (AudioManager)context.GetSystemService(Context.AudioService);
            cameraView.Cameras.Clear();
            foreach (var id in cameraManager.GetCameraIdList())
            {
                var cameraInfo = new CameraInfo { DeviceId = id, MinZoomFactor = 1 };
                var chars = cameraManager.GetCameraCharacteristics(id);
                if ((int)(chars.Get(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Back)
                {
                    cameraInfo.Name = "Back Camera";
                    cameraInfo.Position = CameraPosition.Back;
                }
                else if ((int)(chars.Get(CameraCharacteristics.LensFacing) as Java.Lang.Number) == (int)LensFacing.Front)
                {
                    cameraInfo.Name = "Front Camera";
                    cameraInfo.Position = CameraPosition.Front;
                }
                else
                {
                    cameraInfo.Name = "Camera " + id;
                    cameraInfo.Position = CameraPosition.Unknow;
                }
                cameraInfo.MaxZoomFactor = (float)(chars.Get(CameraCharacteristics.ScalerAvailableMaxDigitalZoom) as Java.Lang.Number);
                cameraInfo.HasFlashUnit = (bool)(chars.Get(CameraCharacteristics.FlashInfoAvailable) as Java.Lang.Boolean);
                cameraInfo.AvailableResolutions = new();
                try
                {
                    float[] maxFocus = (float[])chars.Get(CameraCharacteristics.LensInfoAvailableFocalLengths);
                    SizeF size = (SizeF)chars.Get(CameraCharacteristics.SensorInfoPhysicalSize);
                    cameraInfo.HorizontalViewAngle = (float)(2 * Math.Atan(size.Width / (maxFocus[0] * 2)));
                    cameraInfo.VerticalViewAngle = (float)(2 * Math.Atan(size.Height / (maxFocus[0] * 2)));
                }
                catch { }
                try
                {
                    StreamConfigurationMap map = (StreamConfigurationMap)chars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                    foreach (var s in map.GetOutputSizes(Class.FromType(typeof(ImageReader))))
                        cameraInfo.AvailableResolutions.Add(new(s.Width, s.Height));
                }
                catch
                {
                    if (cameraInfo.Position == CameraPosition.Back)
                        cameraInfo.AvailableResolutions.Add(new(1920, 1080));
                    cameraInfo.AvailableResolutions.Add(new(1280, 720));
                    cameraInfo.AvailableResolutions.Add(new(640, 480));
                    cameraInfo.AvailableResolutions.Add(new(352, 288));
                }
                cameraView.Cameras.Add(cameraInfo);
            }
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                cameraView.Microphones.Clear();
                foreach (var device in audioManager.Microphones)
                {
                    cameraView.Microphones.Add(new MicrophoneInfo { Name = "Microphone " + device.Type.ToString() + " " + device.Address, DeviceId = device.Id.ToString() });
                }
            }
            //Microphone = Micros.FirstOrDefault();
            executorService = Executors.NewSingleThreadExecutor();

            initiated = true;
            cameraView.RefreshDevices();
        }
    }

    internal async Task<CameraResult> StartRecordingAsync(string file, Microsoft.Maui.Graphics.Size Resolution)
    {
        var result = CameraResult.Success;
        if (initiated && !recording)
        {
            if (await CameraView.RequestPermissions(true, true))
            {
                if (started) StopCamera();
                if (cameraView.Camera != null)
                {
                    try
                    {
                        camChars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);

                        StreamConfigurationMap map = (StreamConfigurationMap)camChars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                        videoSize = ChooseVideoSize(map.GetOutputSizes(Class.FromType(typeof(ImageReader))));
                        recording = true;

                        if (OperatingSystem.IsAndroidVersionAtLeast(31))
                            mediaRecorder = new MediaRecorder(context);
                        else
                            mediaRecorder = new MediaRecorder();
                        audioManager.Mode = Mode.Normal;
                        mediaRecorder.SetAudioSource(AudioSource.Mic);
                        mediaRecorder.SetVideoSource(VideoSource.Surface);
                        mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
                        mediaRecorder.SetOutputFile(file);
                        mediaRecorder.SetVideoEncodingBitRate(10000000);
                        mediaRecorder.SetVideoFrameRate(30);

                        var maxVideoSize = ChooseMaxVideoSize(map.GetOutputSizes(Class.FromType(typeof(ImageReader))));
                        if (Resolution.Width != 0 && Resolution.Height != 0)
                            maxVideoSize = new((int)Resolution.Width, (int)Resolution.Height);
                        mediaRecorder.SetVideoSize(maxVideoSize.Width, maxVideoSize.Height);

                        mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
                        mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
                        IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
                        int rotation = (int)windowManager.DefaultDisplay.Rotation;
                        int orientation = ORIENTATIONS.Get(rotation);
                        mediaRecorder.SetOrientationHint(orientation);
                        mediaRecorder.Prepare();

                        if (OperatingSystem.IsAndroidVersionAtLeast(28))
                            cameraManager.OpenCamera(cameraView.Camera.DeviceId, executorService, stateListener);
                        else
                            cameraManager.OpenCamera(cameraView.Camera.DeviceId, stateListener, null);
                        started = true;
                    }
                    catch
                    {
                        result = CameraResult.AccessError;
                    }
                }
                else
                    result = CameraResult.NoCameraSelected;
            }
            else
                result = CameraResult.AccessDenied;
        }
        else
            result = CameraResult.NotInitiated;

        return result;
    }

    private void StartPreview()
    {
        while (textureView.SurfaceTexture == null) Thread.Sleep(100);
        SurfaceTexture texture = textureView.SurfaceTexture;
        texture.SetDefaultBufferSize(videoSize.Width, videoSize.Height);

        try
        {
            previewBuilder = cameraDevice.CreateCaptureRequest(recording ? CameraTemplate.Record : CameraTemplate.Preview);
            var surfaces = new List<OutputConfiguration>();
            var surfaces26 = new List<Surface>();
            var previewSurface = new Surface(texture);
            surfaces.Add(new OutputConfiguration(previewSurface));
            surfaces26.Add(previewSurface);
            previewBuilder.AddTarget(previewSurface);
            if (imgReader != null)
            {
                surfaces.Add(new OutputConfiguration(imgReader.Surface));
                surfaces26.Add(imgReader.Surface);
            }
            if (mediaRecorder != null)
            {
                surfaces.Add(new OutputConfiguration(mediaRecorder.Surface));
                surfaces26.Add(mediaRecorder.Surface);
                previewBuilder.AddTarget(mediaRecorder.Surface);
            }

            sessionCallback = new PreviewCaptureStateCallback(this);
            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                SessionConfiguration config = new((int)SessionType.Regular, surfaces, executorService, sessionCallback);
                cameraDevice.CreateCaptureSession(config);
            }
            else
            {
#pragma warning disable CS0618 // El tipo o el miembro están obsoletos
                cameraDevice.CreateCaptureSession(surfaces26, sessionCallback, null);
#pragma warning restore CS0618 // El tipo o el miembro están obsoletos
            }
        }
        catch (CameraAccessException)
        {
        }
    }

    private void UpdatePreview()
    {
        lock (changeCameraStateLocker)
        {
            if (null == cameraDevice || previewBuilder == null)
                return;

            try
            {
                previewBuilder.Set(CaptureRequest.ControlMode, Java.Lang.Integer.ValueOf((int)ControlMode.Auto));
                //Rect m = (Rect)camChars.Get(CameraCharacteristics.SensorInfoActiveArraySize);
                //videoSize = new Size(m.Width(), m.Height());
                //AdjustAspectRatio(videoSize.Width, videoSize.Height);
                AdjustAspectRatio(videoSize.Width, videoSize.Height);
                SetZoomFactor(cameraView.ZoomFactor);
                //previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                if (recording)
                    mediaRecorder?.Start();
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }
    }
    internal async Task<CameraResult> StartCameraAsync(Microsoft.Maui.Graphics.Size PhotosResolution)
    {
        var result = CameraResult.Success;
        if (initiated)
        {
            if (await CameraView.RequestPermissions())
            {
                lock (changeCameraStateLocker)
                {
                    if (started) StopCamera();
                    if (cameraView.Camera != null)
                    {
                        try
                        {
                            camChars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);

                            StreamConfigurationMap map = (StreamConfigurationMap)camChars.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                            videoSize = ChooseVideoSize(map.GetOutputSizes(Class.FromType(typeof(ImageReader))));
                            var maxVideoSize = ChooseMaxVideoSize(map.GetOutputSizes(Class.FromType(typeof(ImageReader))));
                            if (PhotosResolution.Width != 0 && PhotosResolution.Height != 0)
                                maxVideoSize = new((int)PhotosResolution.Width, (int)PhotosResolution.Height);
                            imgReader = ImageReader.NewInstance(maxVideoSize.Width, maxVideoSize.Height, ImageFormatType.Jpeg, 1);
                            backgroundThread = new HandlerThread("CameraBackground");
                            backgroundThread.Start();
                            backgroundHandler = new Handler(backgroundThread.Looper);
                            imgReader.SetOnImageAvailableListener(photoListener, backgroundHandler);

                            if (OperatingSystem.IsAndroidVersionAtLeast(28))
                                cameraManager.OpenCamera(cameraView.Camera.DeviceId, executorService, stateListener);
                            else
                                cameraManager.OpenCamera(cameraView.Camera.DeviceId, stateListener, null);
                            timer.Start();

                            started = true;
                        }
                        catch
                        {
                            result = CameraResult.AccessError;
                        }
                    }
                    else
                        result = CameraResult.NoCameraSelected;
                }
            }
            else
                result = CameraResult.AccessDenied;
        }
        else
            result = CameraResult.NotInitiated;

        return result;
    }
    internal Task<CameraResult> StopRecordingAsync()
    {
        recording = false;
        return StartCameraAsync(cameraView.PhotosResolution);
    }

    internal CameraResult StopCamera()
    {
        CameraResult result = CameraResult.Success;
        if (initiated)
        {
            lock (changeCameraStateLocker)
            {
                timer.Stop();
                try
                {
                    mediaRecorder?.Stop();
                    mediaRecorder?.Dispose();
                }
                catch { }
                try
                {
                    backgroundThread?.QuitSafely();
                    backgroundThread?.Join();
                    backgroundThread = null;
                    backgroundHandler = null;
                    imgReader?.Dispose();
                    imgReader = null;
                }
                catch { }
                try
                {
                    previewSession?.StopRepeating();
                    previewSession?.Dispose();
                }
                catch { }
                try
                {
                    cameraDevice?.Close();
                    cameraDevice?.Dispose();
                }
                catch { }
                previewSession = null;
                cameraDevice = null;
                previewBuilder = null;
                mediaRecorder = null;
                started = false;
                recording = false;
            }
        }
        else
            result = CameraResult.NotInitiated;
        return result;
    }
    internal void DisposeControl()
    {
        try
        {
            if (started) StopCamera();
            executorService?.Shutdown();
            executorService?.Dispose();
            RemoveAllViews();
            textureView?.Dispose();
            timer?.Dispose();
            Dispose();
        }
        catch { }
    }
    private void ProcessPlugin()
    {
        Task.Run(() =>
        {
            if (cameraView.PluginDecoder != null || cameraView.PluginDecoders?.Count > 0)
            {
                Bitmap bitmap = TakeSnap();
                if (bitmap != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Processing Plugin ({bitmap.Width}x{bitmap.Height}) " + DateTime.Now.ToString("mm:ss:fff"));
                    cameraView.PluginDecoder?.Decode(bitmap);
                    cameraView.PluginDecoders?.ToList().ForEach(x => x.Decode(bitmap));
                    bitmap.Dispose();
                    System.Diagnostics.Debug.WriteLine("Plugin Processed " + DateTime.Now.ToString("mm:ss:fff"));
                    GC.Collect();
                }
                lock (cameraView.currentThreadsLocker) cameraView.currentThreads--;
            }
        });
    }
    private void RefreshSnapShot()
    {
        cameraView.RefreshSnapshot(GetSnapShot(cameraView.AutoSnapShotFormat, true));
    }

    private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
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
                bool processPlugin = false;
                lock (cameraView.currentThreadsLocker)
                {
                    if (cameraView.currentThreads < cameraView.BarCodeDetectionMaxThreads)
                    {
                        cameraView.currentThreads++;
                        processPlugin = true;
                    }
                }
                if (processPlugin)
                {
                    ProcessPlugin();
                    frames = 0;
                }
            }
        }
    }

    private Bitmap TakeSnap()
    {
        Bitmap bitmap = null;
        try
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                //bitmap = textureView.GetBitmap(null);
                bitmap = textureView.Bitmap;
            }).Wait();
            if (bitmap != null)
            {
                var oriWidth = bitmap.Width;
                var oriHeight = bitmap.Height;
                bitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, textureView.GetTransform(null), false);

                Matrix matrix;
                if (textureView.ScaleX == -1)
                {
                    matrix = new();
                    matrix.PreScale(-1, 1);
                }
                else
                {
                    matrix = null;
                }

                var x = (float)((oriWidth - bitmap.Width) / 2.0);
                var y = (float)((oriHeight - bitmap.Height) / 2.0);

                bitmap = Bitmap.CreateBitmap(bitmap, (int)Math.Abs(x), (int)Math.Abs(y), oriWidth, oriHeight, matrix, false);
            }
        }
        catch
        { }
        return bitmap;
    }
    internal async Task<System.IO.Stream> TakePhotoAsync(ImageFormat imageFormat)
    {
        MemoryStream stream = null;
        if (started && !recording)
        {
            CaptureRequest.Builder singleRequest = cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
            captureDone = false;
            capturePhoto = null;
            if (cameraView.Camera.HasFlashUnit)
            {
                switch (cameraView.FlashMode)
                {
                    case FlashMode.Auto:
                        singleRequest.Set(CaptureRequest.FlashMode, (int)ControlAEMode.OnAutoFlash);
                        break;

                    case FlashMode.Enabled:
                        singleRequest.Set(CaptureRequest.FlashMode, (int)ControlAEMode.On);
                        break;

                    case FlashMode.Disabled:
                        singleRequest.Set(CaptureRequest.FlashMode, (int)ControlAEMode.Off);
                        break;
                }
            }
            int rotation = GetJpegOrientation();
            singleRequest.Set(CaptureRequest.JpegOrientation, rotation);

            var destZoom = Math.Clamp(cameraView.ZoomFactor, 1, Math.Min(6, cameraView.Camera.MaxZoomFactor)) - 1;
            Rect m = (Rect)camChars.Get(CameraCharacteristics.SensorInfoActiveArraySize);
            int minW = (int)(m.Width() / (cameraView.Camera.MaxZoomFactor));
            int minH = (int)(m.Height() / (cameraView.Camera.MaxZoomFactor));
            int newWidth = (int)(m.Width() - (minW * destZoom));
            int newHeight = (int)(m.Height() - (minH * destZoom));
            Rect zoomArea = new((m.Width() - newWidth) / 2, (m.Height() - newHeight) / 2, newWidth, newHeight);
            singleRequest.Set(CaptureRequest.ScalerCropRegion, zoomArea);

            singleRequest.AddTarget(imgReader.Surface);
            try
            {
                previewSession.Capture(singleRequest.Build(), null, null);
                while (!captureDone) await Task.Delay(50);
                if (capturePhoto != null)
                {
                    if (textureView.ScaleX == -1 || imageFormat != ImageFormat.JPEG)
                    {
                        Bitmap bitmap = BitmapFactory.DecodeByteArray(capturePhoto, 0, capturePhoto.Length);
                        if (bitmap != null)
                        {
                            if (textureView.ScaleX == -1)
                            {
                                Matrix matrix = new();
                                matrix.PreRotate(rotation);
                                matrix.PostScale(-1, 1);
                                bitmap = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matrix, false);
                            }
                            var iformat = imageFormat switch
                            {
                                ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                                _ => Bitmap.CompressFormat.Png
                            };
                            stream = new();
                            bitmap.Compress(iformat, 100, stream);
                            stream.Position = 0;
                        }
                    }
                    else
                    {
                        stream = new();
                        stream.Write(capturePhoto);
                        stream.Position = 0;
                    }
                }
            }
            catch (Java.Lang.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
        }
        return stream;
    }
    internal ImageSource GetSnapShot(ImageFormat imageFormat, bool auto = false)
    {
        ImageSource result = null;

        if (started && !snapping)
        {
            snapping = true;
            Bitmap bitmap = TakeSnap();

            if (bitmap != null)
            {
                var iformat = imageFormat switch
                {
                    ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                    _ => Bitmap.CompressFormat.Png
                };
                MemoryStream stream = new();
                bitmap.Compress(iformat, 100, stream);
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
                bitmap.Dispose();
            }
            snapping = false;
        }
        return result;
    }

    internal bool SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = true;

        if (started && !snapping)
        {
            snapping = true;
            Bitmap bitmap = TakeSnap();
            if (bitmap != null)
            {
                if (File.Exists(SnapFilePath)) File.Delete(SnapFilePath);
                var iformat = imageFormat switch
                {
                    ImageFormat.JPEG => Bitmap.CompressFormat.Jpeg,
                    _ => Bitmap.CompressFormat.Png
                };
                using FileStream stream = new(SnapFilePath, FileMode.OpenOrCreate);
                bitmap.Compress(iformat, 80, stream);
                stream.Close();
            }
            snapping = false;
        }
        else
            result = false;

        return result;
    }
    public void UpdateMirroredImage()
    {
        if (cameraView != null && textureView != null)
        {
            if (cameraView.MirroredImage)
                textureView.ScaleX = -1;
            else
                textureView.ScaleX = 1;
        }
    }
    internal void UpdateTorch()
    {
        if (cameraView.Camera != null && cameraView.Camera.HasFlashUnit)
        {
            if (started && previewBuilder != null && previewSession != null)
            {
                try
                {
                    previewBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.On);
                    previewBuilder.Set(CaptureRequest.FlashMode, cameraView.TorchEnabled ? (int)ControlAEMode.OnAutoFlash : (int)ControlAEMode.Off);
                    previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                }
                catch (CameraAccessException)
                { }
            }
            else if (initiated)
                cameraManager.SetTorchMode(cameraView.Camera.DeviceId, cameraView.TorchEnabled);
        }
    }
    internal void UpdateFlashMode()
    {
        lock (changeCameraStateLocker)
        {
            if (previewSession != null && previewBuilder != null && cameraView.Camera != null && cameraView != null)
            {
                try
                {
                    if (cameraView.Camera.HasFlashUnit)
                    {
                        switch (cameraView.FlashMode)
                        {
                            case FlashMode.Auto:
                                previewBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.OnAutoFlash);
                                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                                break;

                            case FlashMode.Enabled:
                                previewBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.On);
                                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                                break;

                            case FlashMode.Disabled:
                                previewBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.Off);
                                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                                break;
                        }
                    }
                }
                catch (System.Exception)
                {
                }
            }
        }
    }
    internal void SetZoomFactor(float zoom)
    {
        lock (changeCameraStateLocker)
        {
            if (previewSession != null && previewBuilder != null && cameraView.Camera != null)
            {
                try
                {
                    //if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    //{
                    //previewBuilder.Set(CaptureRequest.ControlZoomRatio, Math.Max(Camera.MinZoomFactor, Math.Min(zoom, Camera.MaxZoomFactor)));
                    //}
                    var destZoom = Math.Clamp(zoom, 1, Math.Min(6, cameraView.Camera.MaxZoomFactor)) - 1;
                    Rect m = (Rect)camChars.Get(CameraCharacteristics.SensorInfoActiveArraySize);
                    int minW = (int)(m.Width() / (cameraView.Camera.MaxZoomFactor));
                    int minH = (int)(m.Height() / (cameraView.Camera.MaxZoomFactor));
                    int newWidth = (int)(m.Width() - (minW * destZoom));
                    int newHeight = (int)(m.Height() - (minH * destZoom));
                    Rect zoomArea = new((m.Width() - newWidth) / 2, (m.Height() - newHeight) / 2, newWidth, newHeight);
                    previewBuilder.Set(CaptureRequest.ScalerCropRegion, zoomArea);
                    previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                }
                catch (Java.Lang.IllegalStateException) { }
            }
        }
    }
    internal void ForceAutoFocus()
    {
        lock (changeCameraStateLocker)
        {
            if (previewSession != null && previewBuilder != null && cameraView.Camera != null)
            {
                previewBuilder.Set(CaptureRequest.ControlAfMode, Java.Lang.Integer.ValueOf((int)ControlAFMode.Off));
                previewBuilder.Set(CaptureRequest.ControlAfTrigger, Java.Lang.Integer.ValueOf((int)ControlAFTrigger.Cancel));
                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
                previewBuilder.Set(CaptureRequest.ControlAfMode, Java.Lang.Integer.ValueOf((int)ControlAFMode.Auto));
                previewBuilder.Set(CaptureRequest.ControlAfTrigger, Java.Lang.Integer.ValueOf((int)ControlAFTrigger.Start));
                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
            }
        }
    }
    private static Size ChooseMaxVideoSize(Size[] choices)
    {
        Size result = choices[0];
        int diference = 0;

        foreach (Size size in choices)
        {
            if (size.Width == size.Height * 4 / 3 && size.Width * size.Height > diference)
            {
                result = size;
                diference = size.Width * size.Height;
            }
        }

        return result;
    }
    private Size ChooseVideoSize(Size[] choices)
    {
        Size result = choices[0];
        int diference = int.MaxValue;
        bool swapped = IsDimensionSwapped();
        foreach (Size size in choices)
        {
            int w = swapped ? size.Height : size.Width;
            int h = swapped ? size.Width : size.Height;
            if (size.Width == size.Height * 4 / 3 && w >= Width && h >= Height && size.Width * size.Height < diference)
            {
                result = size;
                diference = size.Width * size.Height;
            }
        }

        return result;
    }

    private void AdjustAspectRatio(int videoWidth, int videoHeight)
    {
        Matrix txform = new();
        /*
        float scaleX = (float)videoWidth / Width;
        float scaleY = (float)videoHeight / Height;
        bool swapped = IsDimensionSwapped();
        if (swapped)
        {
            scaleX = (float)videoHeight / Width;
            scaleY = (float)videoWidth / Height;
        }
        if (scaleX <= scaleY)
        {
            scaleY /= scaleX;
            scaleX = 1;
        }
        else
        {
            scaleX /= scaleY;
            scaleY = 1;
        }
        */
        RectF viewRect = new(0, 0, Width, Height);
        float centerX = viewRect.CenterX();
        float centerY = viewRect.CenterY();
        RectF bufferRect = new(0, 0, videoHeight, videoWidth);
        bufferRect.Offset(centerX - bufferRect.CenterX(), centerY - bufferRect.CenterY());
        txform.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
        float scale = Math.Max(
                (float)Height / videoHeight,
                (float)Width / videoWidth);
        txform.PostScale(scale, scale, centerX, centerY);

        //txform.PostScale(scaleX, scaleY, centerX, centerY);
        IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var rotation = windowManager.DefaultDisplay.Rotation;
        if (SurfaceOrientation.Rotation90 == rotation || SurfaceOrientation.Rotation270 == rotation)
        {
            txform.PostRotate(90 * ((int)rotation - 2), centerX, centerY);
        }
        else if (SurfaceOrientation.Rotation180 == rotation)
        {
            txform.PostRotate(180, centerX, centerY);
        }
        textureView.SetTransform(txform);
    }

    protected override async void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        if (started && !recording)
            await StartCameraAsync(cameraView.PhotosResolution);
    }

    private bool IsDimensionSwapped()
    {
        IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var displayRotation = windowManager.DefaultDisplay.Rotation;
        var chars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        int sensorOrientation = (int)(chars.Get(CameraCharacteristics.SensorOrientation) as Java.Lang.Integer);
        bool swappedDimensions = false;
        switch (displayRotation)
        {
            case SurfaceOrientation.Rotation0:
            case SurfaceOrientation.Rotation180:
                if (sensorOrientation == 90 || sensorOrientation == 270)
                {
                    swappedDimensions = true;
                }
                break;

            case SurfaceOrientation.Rotation90:
            case SurfaceOrientation.Rotation270:
                if (sensorOrientation == 0 || sensorOrientation == 180)
                {
                    swappedDimensions = true;
                }
                break;
        }
        return swappedDimensions;
    }
    private int GetJpegOrientation()
    {
        IWindowManager windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
        var displayRotation = windowManager.DefaultDisplay.Rotation;
        var chars = cameraManager.GetCameraCharacteristics(cameraView.Camera.DeviceId);
        int sensorOrientation = (int)(chars.Get(CameraCharacteristics.SensorOrientation) as Java.Lang.Integer);
        int deviceOrientation = displayRotation switch
        {
            SurfaceOrientation.Rotation90 => 0,
            SurfaceOrientation.Rotation180 => 270,
            SurfaceOrientation.Rotation270 => 180,
            _ => 90
        };
        // Round device orientation to a multiple of 90
        //deviceOrientation = (deviceOrientation + 45) / 90 * 90;

        // Reverse device orientation for front-facing cameras
        //if (cameraView.Camera.Position == CameraPosition.Front) deviceOrientation = -deviceOrientation;

        // Calculate desired JPEG orientation relative to camera orientation to make
        // the image upright relative to the device orientation
        int jpegOrientation = (sensorOrientation + deviceOrientation + 270) % 360;

        return jpegOrientation;
    }
    private class MyCameraStateCallback : CameraDevice.StateCallback
    {
        private readonly MauiCameraView cameraView;
        public MyCameraStateCallback(MauiCameraView camView)
        {
            cameraView = camView;
        }
        public override void OnOpened(CameraDevice camera)
        {
            lock (cameraView.changeCameraStateLocker)
            {
                if (camera != null)
                {
                    cameraView.cameraDevice = camera;
                    cameraView.StartPreview();
                }
            }
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            camera.Close();
            cameraView.cameraDevice = null;
        }

        public override void OnError(CameraDevice camera, CameraError error)
        {
            camera?.Close();
            cameraView.cameraDevice = null;
        }
    }

    private class PreviewCaptureStateCallback : CameraCaptureSession.StateCallback
    {
        private readonly MauiCameraView cameraView;
        public PreviewCaptureStateCallback(MauiCameraView camView)
        {
            cameraView = camView;
        }
        public override void OnConfigured(CameraCaptureSession session)
        {
            cameraView.previewSession = session;
            cameraView.UpdatePreview();
            if (cameraView.started)
            {
                cameraView.UpdateTorch();
            }
        }
        public override void OnConfigureFailed(CameraCaptureSession session)
        {
        }
    }
    class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        private readonly MauiCameraView cameraView;

        public ImageAvailableListener(MauiCameraView camView)
        {
            cameraView = camView;
        }
        public void OnImageAvailable(ImageReader reader)
        {
            try
            {
                var image = reader?.AcquireNextImage();
                if (image == null)
                    return;

                var buffer = image.GetPlanes()?[0].Buffer;
                if (buffer == null)
                    return;

                var imageData = new byte[buffer.Capacity()];
                buffer.Get(imageData);
                cameraView.capturePhoto = imageData;
                buffer.Clear();
                image.Close();
            }
            catch
            {
            }
            cameraView.captureDone = true;
        }
    }
}