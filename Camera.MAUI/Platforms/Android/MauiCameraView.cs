using Android.Content;
using Android.Views;
using Android.Hardware.Camera2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content.PM;
using Java.Lang;
using Android.Util;
using Java.Util;
using Android.Graphics;
using Android.Media;
using Size = Android.Util.Size;
using static Android.Media.ImageReader;
using Android.Hardware.Camera2.Params;
using Android.App;
using Android.Runtime;
using Android.OS;
using Android.Widget;
using Java.Interop;
using Java.Nio;
using AndroidX.Core.App;

namespace Camera.MAUI.Platforms.Android;

internal class MauiCameraView : SurfaceView
{
    public CameraInfo Camera { get; set; }

    private readonly List<CameraInfo> Cameras = new List<CameraInfo>();
    private readonly CameraView cameraView;
    private CameraManager manager;
    private bool started = false;
    private Handler childHandler, mainHandler;
    private ImageView iv_show;
    private ImageReader mImageReader;
    private CameraCaptureSession mCameraCaptureSession;
    private CameraStateCallback stateCallback;
    private CameraDevice mCameraDevice;
    private ImageAvailableListener imageAvailableListener;
    private CaptureRequest.Builder previewRequestBuilder;
    private CameraCaptureStateCallback previewStateCallback;

    public MauiCameraView(Context context, CameraView cameraView) : base(context)
    {
        this.cameraView = cameraView;
        iv_show = new ImageView(context);
        iv_show.Visibility = ViewStates.Invisible;
        InitDevices();
        //Holder.SetKeepScreenOn(true);
    }

    private void InitDevices()
    {
        manager = (CameraManager)Context.GetSystemService(Context.CameraService);
        try
        {

            foreach (var camid in manager.GetCameraIdList())
            {
                var camProps = manager.GetCameraCharacteristics(camid);
                var facing = (Integer)camProps.Get(CameraCharacteristics.LensFacing);
                var map = (StreamConfigurationMap)camProps.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                if (map != null)
                    Cameras.Add(new CameraInfo { Name = facing != null && facing == (Integer.ValueOf((int)LensFacing.Front)) ? "Front Camera" : "Back Camera", DeviceId = camid });
            }
            Camera = Cameras.FirstOrDefault();
            if (cameraView != null)
            {
                cameraView.Cameras.Clear();
                foreach (var cam in Cameras) cameraView.Cameras.Add(cam);
            }
        }
        catch (System.Exception)
        {
            Camera = null;
        }
    }
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.Success;
        if (await RequestPermissions())
        {
            if (Camera != null)
            {
                HandlerThread handlerThread = new HandlerThread("Camera2");
                handlerThread.Start();
                childHandler = new Handler(handlerThread.Looper);
                mainHandler = new Handler(Looper.MainLooper);
                mImageReader = ImageReader.NewInstance(1080, 1920, ImageFormatType.Jpeg, 1);
                imageAvailableListener = new ImageAvailableListener(iv_show);
                mImageReader.SetOnImageAvailableListener(imageAvailableListener, mainHandler);
                stateCallback = new CameraStateCallback(this);
                manager.OpenCamera(Camera.DeviceId, stateCallback, mainHandler);

                started = true;
            }
        }
        else
            result = CameraResult.AccessDenied;
        return result;
    }
    private void StartPreview()
    {
        try
        {
            // CaptureRequest.Builder
            previewRequestBuilder = mCameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            // surface of SurfaceView will be the object of CaptureRequest.Builder
            previewRequestBuilder.AddTarget(Holder.Surface);
            // Create CameraCaptureSession to take care of preview and photo shooting.
            List<Surface> surfaces = new List<Surface>();
            surfaces.Add(Holder.Surface);
            surfaces.Add(mImageReader.Surface);
            previewStateCallback = new CameraCaptureStateCallback(this);
            //SessionConfiguration session=new SessionConfiguration()
            mCameraDevice.CreateCaptureSession(surfaces, previewStateCallback, childHandler);
        }
        catch (CameraAccessException e)
        {
            e.PrintStackTrace();
        }
    }
    public async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.Success;

        try
        {

        }
        catch (System.Exception)
        {
            result = CameraResult.AccessError;
        }
        started = false;

        return result;
    }
    private async Task<bool> RequestPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) return false;
        }
        return true;
    }
    class ImageAvailableListener : Java.Lang.Object, IOnImageAvailableListener
    {
        private ImageView iv_show;
        public ImageAvailableListener(ImageView iv_show)
        {
            this.iv_show = iv_show;
        }
        public void OnImageAvailable(ImageReader reader)
        {
            var image = reader.AcquireNextImage();
            ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            Bitmap bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
            if (bitmap != null)
            {
                iv_show.SetImageBitmap(bitmap);
            }
        }
    }
    class CameraStateCallback : CameraDevice.StateCallback
    {
        private MauiCameraView mainView;

        public CameraStateCallback(MauiCameraView mainView)
        {
            this.mainView = mainView;
        }
        public override void OnDisconnected(CameraDevice camera)
        {
            if (mainView.mCameraDevice != null) 
            {
                mainView.mCameraDevice.Close();
                mainView.mCameraDevice = null;
            }
        }
        public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
        {
        }
        public override void OnOpened(CameraDevice camera)
        {
            mainView.mCameraDevice = camera;
            mainView.StartPreview();
        }
    }
    class CameraCaptureStateCallback : CameraCaptureSession.StateCallback
    {
        private MauiCameraView mainView;

        public CameraCaptureStateCallback(MauiCameraView mainView)
        {
            this.mainView = mainView;
        }
        public override void OnConfigured(CameraCaptureSession session)
        {
            if (mainView.mCameraDevice == null) return;
            // Begin to preview
            mainView.mCameraCaptureSession = session;
            try
            {
                // Turn on Auto Focus
                mainView.previewRequestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
                // Turn on Flash
                mainView.previewRequestBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.OnAutoFlash);
                // Show up
                CaptureRequest previewRequest = mainView.previewRequestBuilder.Build();
                mainView.mCameraCaptureSession.SetRepeatingRequest(previewRequest, null, mainView.childHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
        }
    }
}


