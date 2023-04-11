using Camera.MAUI.ZXingHelper;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ZXing;
using static Microsoft.Maui.ApplicationModel.Permissions;
#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else
using DecodeDataType = System.Object;
#endif

namespace Camera.MAUI;

public class CameraView : View, ICameraView
{
    public static readonly BindableProperty FlashModeProperty = BindableProperty.Create(nameof(FlashMode), typeof(FlashMode), typeof(CameraView), FlashMode.Disabled);
    public static readonly BindableProperty TorchEnabledProperty = BindableProperty.Create(nameof(TorchEnabled), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty CamerasProperty = BindableProperty.Create(nameof(Cameras), typeof(ObservableCollection<CameraInfo>), typeof(CameraView), new ObservableCollection<CameraInfo>());
    public static readonly BindableProperty NumCamerasDetectedProperty = BindableProperty.Create(nameof(NumCamerasDetected), typeof(int), typeof(CameraView), 0);
    public static readonly BindableProperty CameraProperty = BindableProperty.Create(nameof(Camera), typeof(CameraInfo), typeof(CameraView), null, propertyChanged:CameraChanged);
    public static readonly BindableProperty MicrophonesProperty = BindableProperty.Create(nameof(Microphones), typeof(ObservableCollection<MicrophoneInfo>), typeof(CameraView), new ObservableCollection<MicrophoneInfo>());
    public static readonly BindableProperty NumMicrophonesDetectedProperty = BindableProperty.Create(nameof(NumMicrophonesDetected), typeof(int), typeof(CameraView), 0);
    public static readonly BindableProperty MicrophoneProperty = BindableProperty.Create(nameof(Microphone), typeof(MicrophoneInfo), typeof(CameraView), null);
    public static readonly BindableProperty MirroredImageProperty = BindableProperty.Create(nameof(MirroredImage), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty BarCodeDetectionEnabledProperty = BindableProperty.Create(nameof(BarCodeDetectionEnabled), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty BarCodeDetectionFrameRateProperty = BindableProperty.Create(nameof(BarCodeDetectionFrameRate), typeof(int), typeof(CameraView), 10);
    public static readonly BindableProperty BarCodeOptionsProperty = BindableProperty.Create(nameof(BarCodeOptions), typeof(BarcodeDecodeOptions), typeof(CameraView), new BarcodeDecodeOptions(), propertyChanged:BarCodeOptionsChanged);
    public static readonly BindableProperty BarCodeResultsProperty = BindableProperty.Create(nameof(BarCodeResults), typeof(Result[]), typeof(CameraView), null, BindingMode.OneWayToSource);
    public static readonly BindableProperty ZoomFactorProperty = BindableProperty.Create(nameof(ZoomFactor), typeof(float), typeof(CameraView), 1f);
    public static readonly BindableProperty AutoSnapShotSecondsProperty = BindableProperty.Create(nameof(AutoSnapShotSeconds), typeof(float), typeof(CameraView), 0f);
    public static readonly BindableProperty AutoSnapShotFormatProperty = BindableProperty.Create(nameof(AutoSnapShotFormat), typeof(ImageFormat), typeof(CameraView), ImageFormat.PNG);
    public static readonly BindableProperty SnapShotProperty = BindableProperty.Create(nameof(SnapShot), typeof(ImageSource), typeof(CameraView), null, BindingMode.OneWayToSource);
    public static readonly BindableProperty SnapShotStreamProperty = BindableProperty.Create(nameof(SnapShotStream), typeof(Stream), typeof(CameraView), null, BindingMode.OneWayToSource);
    public static readonly BindableProperty TakeAutoSnapShotProperty = BindableProperty.Create(nameof(TakeAutoSnapShot), typeof(bool), typeof(CameraView), false, propertyChanged:TakeAutoSnapShotChanged);
    public static readonly BindableProperty AutoSnapShotAsImageSourceProperty = BindableProperty.Create(nameof(AutoSnapShotAsImageSource), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty AutoStartPreviewProperty = BindableProperty.Create(nameof(AutoStartPreview), typeof(bool), typeof(CameraView), false, propertyChanged: AutoStartPreviewChanged);
    public static readonly BindableProperty AutoRecordingFileProperty = BindableProperty.Create(nameof(AutoRecordingFile), typeof(string), typeof(CameraView), string.Empty);
    public static readonly BindableProperty AutoStartRecordingProperty = BindableProperty.Create(nameof(AutoStartRecording), typeof(bool), typeof(CameraView), false, propertyChanged: AutoStartRecordingChanged);

    /// <summary>
    /// Flash mode for take a photo. This is a bindable property.
    /// </summary>
    public FlashMode FlashMode
    {
        get { return (FlashMode)GetValue(FlashModeProperty); }
        set { SetValue(FlashModeProperty, value); }
    }
    /// <summary>
    /// Turns the camera torch on and off if available. This is a bindable property.
    /// </summary>
    public bool TorchEnabled
    {
        get { return (bool)GetValue(TorchEnabledProperty); }
        set { SetValue(TorchEnabledProperty, value); }
    }
    /// <summary>
    /// List of available cameras in the device. This is a bindable property.
    /// </summary>
    public ObservableCollection<CameraInfo> Cameras
    {
        get { return (ObservableCollection<CameraInfo>)GetValue(CamerasProperty); }
        set { SetValue(CamerasProperty, value); }
    }
    /// <summary>
    /// Indicates the number of available cameras in the device.
    /// </summary>
    public int NumCamerasDetected
    {
        get { return (int)GetValue(NumCamerasDetectedProperty); }
        set { SetValue(NumCamerasDetectedProperty, value); }
    }
    /// <summary>
    /// Set the camera to use by the controler. This is a bindable property.
    /// </summary>
    public CameraInfo Camera
    {
        get { return (CameraInfo)GetValue(CameraProperty); }
        set { SetValue(CameraProperty, value); }
    }
    /// <summary>
    /// List of available microphones in the device. This is a bindable property.
    /// </summary>
    public ObservableCollection<MicrophoneInfo> Microphones
    {
        get { return (ObservableCollection<MicrophoneInfo>)GetValue(MicrophonesProperty); }
        set { SetValue(MicrophonesProperty, value); }
    }
    /// <summary>
    /// Indicates the number of available microphones in the device.
    /// </summary>
    public int NumMicrophonesDetected
    {
        get { return (int)GetValue(NumMicrophonesDetectedProperty); }
        set { SetValue(NumMicrophonesDetectedProperty, value); }
    }
    /// <summary>
    /// Set the microphone to use by the controler. This is a bindable property.
    /// </summary>
    public MicrophoneInfo Microphone
    {
        get { return (MicrophoneInfo)GetValue(MicrophoneProperty); }
        set { SetValue(MicrophoneProperty, value); }
    }
    /// <summary>
    /// Turns a mirror image of the camera on and off. This is a bindable property.
    /// </summary>
    public bool MirroredImage
    {
        get { return (bool)GetValue(MirroredImageProperty); }
        set { SetValue(MirroredImageProperty, value); }
    }
    /// <summary>
    /// Turns on and off the barcode detection. This is a bindable property.
    /// </summary>
    public bool BarCodeDetectionEnabled
    {
        get { return (bool)GetValue(BarCodeDetectionEnabledProperty); }
        set { SetValue(BarCodeDetectionEnabledProperty, value); }
    }
    /// <summary>
    /// Indicates every how many frames the control tries to detect a barcode in the image. This is a bindable property.
    /// </summary>
    public int BarCodeDetectionFrameRate
    {
        get { return (int)GetValue(BarCodeDetectionFrameRateProperty); }
        set { SetValue(BarCodeDetectionFrameRateProperty, value); }
    }
    /// <summary>
    /// Options for the barcode detection. This is a bindable property.
    /// </summary>
    public BarcodeDecodeOptions BarCodeOptions
    {
        get { return (BarcodeDecodeOptions)GetValue(BarCodeOptionsProperty); }
        set { SetValue(BarCodeOptionsProperty, value); }
    }
    /// <summary>
    /// It refresh each time a barcode is detected if BarCodeDetectionEnabled porperty is true
    /// </summary>
    public Result[] BarCodeResults
    {
        get { return (Result[])GetValue(BarCodeResultsProperty); }
        set { SetValue(BarCodeResultsProperty, value); }
    }
    /// <summary>
    /// The zoom factor for the current camera in use. This is a bindable property.
    /// </summary>
    public float ZoomFactor
    {
        get { return (float)GetValue(ZoomFactorProperty); }
        set { SetValue(ZoomFactorProperty, value); }
    }
    /// <summary>
    /// Indicates the minimum zoom factor for the camera in use. This property is refreshed when the "Camera" property change.
    /// </summary>
    public float MinZoomFactor
    {
        get
        {
            if (Camera != null)
                return Camera.MinZoomFactor;
            else
                return 1f;
        }
    }
    /// <summary>
    /// Indicates the maximum zoom factor for the camera in use. This property is refreshed when the "Camera" property change.
    /// </summary>
    public float MaxZoomFactor
    {
        get
        {
            if (Camera != null)
                return Camera.MaxZoomFactor;
            else
                return 1f;
        }
    }
    /// <summary>
    /// Sets how often the SnapShot property is updated in seconds.
    /// Default 0: no snapshots are taken
    /// WARNING! A low frequency directly impacts over control performance and memory usage (with AutoSnapShotAsImageSource = true)
    /// </summary>
    public float AutoSnapShotSeconds
    {
        get { return (float)GetValue(AutoSnapShotSecondsProperty); }
        set { SetValue(AutoSnapShotSecondsProperty, value); }
    }
    /// <summary>
    /// Sets the snaphost image format
    /// </summary>
    public ImageFormat AutoSnapShotFormat
    {
        get { return (ImageFormat)GetValue(AutoSnapShotFormatProperty); }
        set { SetValue(AutoSnapShotFormatProperty, value); }
    }
    /// <summary>
    /// Refreshes according to the frequency set in the AutoSnapShotSeconds property (if AutoSnapShotAsImageSource is set to true)
    /// or when GetSnapShot is called or TakeAutoSnapShot is set to true
    /// </summary>
    public ImageSource SnapShot
    {
        get { return (ImageSource)GetValue(SnapShotProperty); }
        private set { SetValue(SnapShotProperty, value); }
    }
    /// <summary>
    /// Refreshes according to the frequency set in the AutoSnapShotSeconds property or when GetSnapShot is called.
    /// WARNING. Each time a snapshot is made, the previous stream is disposed.
    /// </summary>
    public Stream SnapShotStream
    {
        get { return (Stream)GetValue(SnapShotStreamProperty); }
        internal set { SetValue(SnapShotStreamProperty, value); }
    }
    /// <summary>
    /// Change from false to true refresh SnapShot property
    /// </summary>
    public bool TakeAutoSnapShot
    {
        get { return (bool)GetValue(TakeAutoSnapShotProperty); }
        set { SetValue(TakeAutoSnapShotProperty, value); }
    }
    /// <summary>
    /// If true SnapShot property is refreshed according to the frequency set in the AutoSnapShotSeconds property
    /// </summary>
    public bool AutoSnapShotAsImageSource
    {
        get { return (bool)GetValue(AutoSnapShotAsImageSourceProperty); }
        set { SetValue(AutoSnapShotAsImageSourceProperty, value); }
    }
    /// <summary>
    /// Starts/Stops the Preview if camera property has been set
    /// </summary>
    public bool AutoStartPreview
    {
        get { return (bool)GetValue(AutoStartPreviewProperty); }
        set { SetValue(AutoStartPreviewProperty, value); }
    }
    /// <summary>
    /// Full path to file where record video will be recorded.
    /// </summary>
    public string AutoRecordingFile
    {
        get { return (string)GetValue(AutoRecordingFileProperty); }
        set { SetValue(AutoRecordingFileProperty, value); }
    }
    /// <summary>
    /// Starts/Stops record video to AutoRecordingFile if camera and microphone properties have been set
    /// </summary>
    public bool AutoStartRecording
    {
        get { return (bool)GetValue(AutoStartRecordingProperty); }
        set { SetValue(AutoStartRecordingProperty, value); }
    }
    /// <summary>
    /// If true BarcodeDetected event will invoke only if a Results is diferent from preview Results
    /// </summary>
    public bool ControlBarcodeResultDuplicate { get; set; } = false;
    public delegate void BarcodeResultHandler(object sender, BarcodeEventArgs args);
    /// <summary>
    /// Event launched every time a code is detected in the image if "BarCodeDetectionEnabled" is set to true.
    /// </summary>
    public event BarcodeResultHandler BarcodeDetected;
    /// <summary>
    /// Event launched when "Cameras" property has been loaded.
    /// </summary>
    public event EventHandler CamerasLoaded;
    /// <summary>
    /// Event launched when "Microphones" property has been loaded.
    /// </summary>
    public event EventHandler MicrophonesLoaded;

    private readonly BarcodeReaderGeneric BarcodeReader;
    internal DateTime lastSnapshot = DateTime.Now;

    public CameraView()
    {
        BarcodeReader = new BarcodeReaderGeneric();
        HandlerChanged += CameraView_HandlerChanged;
    }
    private void CameraView_HandlerChanged(object sender, EventArgs e)
    {
        if (Handler != null)
        {
            CamerasLoaded?.Invoke(this, EventArgs.Empty);
            MicrophonesLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
    internal void RefreshSnapshot(ImageSource img)
    {
        if (AutoSnapShotAsImageSource)
        {
            SnapShot = img;
            OnPropertyChanged(nameof(SnapShot));
        }
        OnPropertyChanged(nameof(SnapShotStream));
        lastSnapshot = DateTime.Now;
    }

    internal void DecodeBarcode(DecodeDataType data)
    {
        LuminanceSource lumSource = default;
#if ANDROID
        lumSource = new Camera.MAUI.Platforms.Android.BitmapLuminanceSource(data);
#elif IOS || MACCATALYST
        lumSource = new Camera.MAUI.ZXingHelper.RGBLuminanceSource(data);
#elif WINDOWS
        lumSource = new Camera.MAUI.Platforms.Windows.SoftwareBitmapLuminanceSource(data);
#endif
        try
        {
            Result[] results = null;
            if (BarCodeOptions.ReadMultipleCodes)
                results = BarcodeReader.DecodeMultiple(lumSource);
            else
            {
                var result = BarcodeReader.Decode(lumSource);
                if (result != null) results = new Result[] { result };
            }
            if (results?.Length > 0)
            {
                bool refresh = true;
                if (ControlBarcodeResultDuplicate)
                {
                    if (BarCodeResults != null)
                    {
                        foreach (var result in results)
                        {
                            refresh = BarCodeResults.FirstOrDefault(b => b.Text == result.Text && b.BarcodeFormat == result.BarcodeFormat) == null;
                            if (refresh) break;
                        }
                    }
                }
                if (refresh)
                {
                    BarCodeResults = results;
                    OnPropertyChanged(nameof(BarCodeResults));
                    BarcodeDetected?.Invoke(this, new BarcodeEventArgs { Result = results });
                }
            }
        }
        catch {}
    }
    private static async void CameraChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != null && oldValue != newValue && bindable is CameraView cameraView && newValue is CameraInfo)
        {
            cameraView.OnPropertyChanged(nameof(MinZoomFactor));
            cameraView.OnPropertyChanged(nameof(MaxZoomFactor));
            if (cameraView.AutoStartPreview)
            {
                await cameraView.StartCameraAsync();
            }
        }
    }
    private static void TakeAutoSnapShotChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if ((bool)newValue && !(bool)oldValue && bindable is CameraView cameraView)
        {
            cameraView.RefreshSnapshot(cameraView.GetSnapShot(cameraView.AutoSnapShotFormat));
        }
    }
    private static async void AutoStartPreviewChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (oldValue != newValue && bindable is CameraView control)
        {
            try
            {
                if ((bool)newValue)
                    await control.StartCameraAsync();
                else
                    await control.StopCameraAsync();
            }
            catch { }
                    
        }
    }
    private static async void AutoStartRecordingChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (oldValue != newValue && bindable is CameraView control)
        {
            try
            {
                if ((bool)newValue)
                {
                    if (!string.IsNullOrEmpty(control.AutoRecordingFile))
                        await control.StartRecordingAsync(control.AutoRecordingFile);
                }
                else
                    await control.StopRecordingAsync();
            }
            catch { }

        }
    }
    private static void BarCodeOptionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != null && oldValue != newValue && bindable is CameraView cameraView && newValue is BarcodeDecodeOptions options)
        {
            cameraView.BarcodeReader.AutoRotate = options.AutoRotate;
            if (options.CharacterSet != string.Empty) cameraView.BarcodeReader.Options.CharacterSet = options.CharacterSet;
            cameraView.BarcodeReader.Options.PossibleFormats = options.PossibleFormats;
            cameraView.BarcodeReader.Options.TryHarder = options.TryHarder;
            cameraView.BarcodeReader.Options.TryInverted = options.TryInverted;
            cameraView.BarcodeReader.Options.PureBarcode = options.PureBarcode;
        }
    }
    /// <summary>
    /// Start playback of the selected camera async. "Camera" property must not be null.
    /// </summary>
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Camera != null)
        {
            if (Handler != null && Handler is CameraViewHandler handler)
            {
                result = await handler.StartCameraAsync();
                if (result == CameraResult.Success)
                {
                    BarCodeResults = null;
                    OnPropertyChanged(nameof(MinZoomFactor));
                    OnPropertyChanged(nameof(MaxZoomFactor));
                }
            }
        }
        else
            result = CameraResult.NoCameraSelected;

        return result;
    }
    /// <summary>
    /// Start recording a video async. "Camera" property must not be null.
    /// <paramref name="file"/> Full path to file where video will be stored.
    /// </summary>
    public async Task<CameraResult> StartRecordingAsync(string file)
    {
        CameraResult result = CameraResult.AccessError;
        if (Camera != null)
        {
            if (Handler != null && Handler is CameraViewHandler handler)
            {
                result = await handler.StartRecordingAsync(file);
                if (result == CameraResult.Success)
                {
                    BarCodeResults = null;
                    OnPropertyChanged(nameof(MinZoomFactor));
                    OnPropertyChanged(nameof(MaxZoomFactor));
                }
            }
        }
        else
            result = CameraResult.NoCameraSelected;

        return result;
    }
    /// <summary>
    /// Stop playback of the selected camera async.
    /// </summary>
    public async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.StopCameraAsync();
        }
        return result;
    }
    /// <summary>
    /// Stop recording a video async.
    /// </summary>
    public async Task<CameraResult> StopRecordingAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.StopRecordingAsync();
        }
        return result;
    }
    /// <summary>
    /// Takes a photo from the camera selected.
    /// </summary>
    /// <param name="imageFormat">The capture image format</param>
    /// <returns>A stream with the photo info</returns>
    public async Task<Stream> TakePhotoAsync(ImageFormat imageFormat = ImageFormat.JPEG)
    {
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            return await handler.TakePhotoAsync(imageFormat);
        }
        return null;
    }
    /// <summary>
    /// Takes a capture form the active camera playback.
    /// </summary>
    /// <param name="imageFormat">The capture image format</param>
    public ImageSource GetSnapShot(ImageFormat imageFormat = ImageFormat.PNG)
    {
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            SnapShot = handler.GetSnapShot(imageFormat);
        }
        return SnapShot;
    }
    /// <summary>
    /// Saves a capture form the active camera playback in a file
    /// </summary>
    /// <param name="imageFormat">The capture image format</param>
    /// <param name="SnapFilePath">Full path for the file</param>
    public async Task<bool> SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = false;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.SaveSnapShot(imageFormat, SnapFilePath);
        }
        return result;
    }
    internal void RefreshDevices()
    {
        Task.Run(() => { 
            OnPropertyChanged(nameof(Cameras)); 
            NumCamerasDetected = Cameras.Count;
            OnPropertyChanged(nameof(Microphones));
            NumMicrophonesDetected = Microphones.Count;
        });
    }
    public static async Task<bool> RequestPermissions(bool withMic = false, bool withStorageWrite = false)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted) return false;
        }
        if (withMic)
        {
            status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted) return false;
            }
        }
        if (withStorageWrite)
        {
            status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted) return false;
            }
        }
        return true;
    }
}