
# Camera.MAUI

A Camera View control and a Barcode Endode/Decode control (based on ZXing.Net) for .NET MAUI applications.

## CameraView

A ContetView control for camera management with the next properties:

|   | Android  | iOS/Mac  | Windows  |
|---|---|---|---|
| Preview  |  ✅ | ✅  | ✅  |
| Mirror preview  | ✅  | ✅  | ✅  |
| Flash  | ✅  | ✅  | ✅  |
| Torch  | ✅  | ✅  | ✅  |
| Zoom  | ✅  | ✅  | ✅  |
| Take snapshot  | ✅  | ✅  | ✅  |
| Save snapshot  | ✅  | ✅  | ✅  |
| Barcode detection/decode  | ✅  | ✅  | ✅  |
| Video/audio recording  | ✅  | ✅  | ✅  |
| Take Photo  | ✅  | ✅  | ✅  |

### Install and configure CameraView

1. Download and Install [Camera.MAUI](https://www.nuget.org/packages/Camera.MAUI) NuGet package on your application.

1. Initialize the plugin in your `MauiProgram.cs`:

    ```csharp
    // Add the using to the top
    using Camera.MAUI;
    
    public static MauiApp CreateMauiApp()
    {
    	var builder = MauiApp.CreateBuilder();
    
    	builder
    		.UseMauiApp<App>()
    		.UseMauiCameraView(); // Add the use of the plugging
    
    	return builder.Build();
    }
    ```
1. Add camera/microphone permissions to your application:

#### Android

In your `AndroidManifest.xml` file (Platforms\Android) add the following permission:

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.RECORD_VIDEO" />
<uses-permission android:name="android.permission.VIBRATE" />

```

#### iOS/MacCatalyst

In your `info.plist` file (Platforms\iOS / Platforms\MacCatalyst) add the following permission:

```xml
<key>NSCameraUsageDescription</key>
<string>This app uses camera for...</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to the microphone for record videos</string>
```
Make sure that you enter a clear and valid reason for your app to access the camera. This description will be shown to the user.

#### Windows

In your Package.appxmanifest file (Platforms\Windows) go to Capabilities and mark Web Camera and Microphone.

For more information on permissions, see the [Microsoft Docs](https://docs.microsoft.com/dotnet/maui/platform-integration/appmodel/permissions).

### Using CameraView

In XAML, make sure to add the right XML namespace:

`xmlns:cv="clr-namespace:Camera.MAUI;assembly=Camera.MAUI"`  
`xmlns:bc="clr-namespace:Camera.MAUI.Plugin.ZXing;assembly=Camera.MAUI.Plugin.ZXing"`, or  
`xmlns:bc="clr-namespace:Camera.MAUI.Plugin.MLKit;assembly=Camera.MAUI.Plugin.MLKit"`

Use the control:
```xaml
<cv:CameraView x:Name="cameraView" WidthRequest="300" HeightRequest="200"/>
```

Configure the events:
```csharp
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
```

Configure plugin:
```csharp
    // ZXing
    cameraView.PluginDecoder = new Camera.MAUI.Plugin.ZXing.ZXingDecoder();
    cameraView.PluginDecoder.Decoded += CameraView_BarcodeDetected;
```
or
```csharp
    // MLKit
    cameraView.PluginDecoder = new Camera.MAUI.Plugin.MLKit.MLKitBarcodeDecoder();
    cameraView.PluginDecoder.Decoded += CameraView_BarcodeDetected;
```

Configure a collection of plugins:
```csharp
    var zxing = new Camera.MAUI.Plugin.ZXing.ZXingDecoder();
    zxing.Decoded += CameraView_BarcodeDetected;

    cameraView.PluginDecoders = new PluginDecoderCollection
    {
        zxing
    };
```

Configure the camera and microphone to use:
```csharp
    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (cameraView.NumCamerasDetected > 0)
        {
            if (cameraView.NumMicrophonesDetected > 0)
                cameraView.Microphone = cameraView.Microphones.First();
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (await cameraView.StartCameraAsync() == CameraResult.Success)
                {
                    controlButton.Text = "Stop";
                    playing = true;
                }
            });
        }
    }
```
CameraInfo type (Camera Property):
CameraInfo has the next properties:
```csharp
    public string Name
    public string DeviceId
    public CameraPosition Position
    public bool HasFlashUnit
    public float MinZoomFactor
    public float MaxZoomFactor
    public List<Size> AvailableResolutions
```
Start camera playback:
```csharp
         if (await cameraView.StartCameraAsync(new Size(1280, 720)) == CameraResult.Success)
         {
             playing = true;
         }
```
Stop camera playback:
```csharp
         if (await cameraView.StopCameraAsync() == CameraResult.Success)
         {
             playing = false;
         }
```
Set Flash mode
```csharp
cameraView.FlashMode = FlashMode.Auto;
```
Toggle Torch
```csharp
cameraView.TorchEnabled = !cameraView.TorchEnabled;
```
Set mirrored mode
```csharp
cameraView.MirroredImage = true;
```
Set zoom factor
```csharp
if (cameraView.MaxZoomFactor >= 2.5f)
    cameraView.ZoomFactor = 2.5f;
```
Get a snapshot from the playback
```csharp
ImageSource imageSource = cameraView.GetSnapShot(ImageFormat.PNG);
bool result = cameraView.SaveSnapShot(ImageFormat.PNG, filePath);
```
Record a video:
```csharp
var result = await cameraView.StartRecordingAsync(Path.Combine(FileSystem.Current.CacheDirectory, "Video.mp4", new Size(1920, 1080)));
....
result = cameraView.StopRecordingAsync();
```
Take a photo
```csharp
var stream = await cameraView.TakePhotoAsync();
if (stream != null)
{
    var result = ImageSource.FromStream(() => stream);
    snapPreview.Source = result;
}
```

**Use Control with MVVM:**
The control has several binding properties for take an snapshot:
```csharp
    /// Binding property for use this control in MVVM.
    public CameraView Self

    /// Sets how often the SnapShot property is updated in seconds.
    /// Default 0: no snapshots are taken
    /// WARNING! A low frequency directly impacts over control performance and memory usage (with AutoSnapShotAsImageSource = true)
    /// </summary>
    public float AutoSnapShotSeconds
    
    /// Sets the snaphost image format
    public ImageFormat AutoSnapShotFormat

    /// Refreshes according to the frequency set in the AutoSnapShotSeconds property (if AutoSnapShotAsImageSource is set to true) or when GetSnapShot is called or TakeAutoSnapShot is set to true
    public ImageSource SnapShot
    
    /// Refreshes according to the frequency set in the AutoSnapShotSeconds property or when GetSnapShot is called.
    /// WARNING. Each time a snapshot is made, the previous stream is disposed.
    public Stream SnapShotStream
    
    /// Change from false to true refresh SnapShot property
    public bool TakeAutoSnapShot
    
    /// If true SnapShot property is refreshed according to the frequency set in the AutoSnapShotSeconds property
    public bool AutoSnapShotAsImageSource
    /// Starts/Stops the Preview if camera property has been set
    public bool AutoStartPreview
    {
        get { return (bool)GetValue(AutoStartPreviewProperty); }
        set { SetValue(AutoStartPreviewProperty, value); }
    }
    /// Full path to file where record video will be recorded.
    public string AutoRecordingFile
    {
        get { return (string)GetValue(AutoRecordingFileProperty); }
        set { SetValue(AutoRecordingFileProperty, value); }
    }
    /// Starts/Stops record video to AutoRecordingFile if camera and microphone properties have been set
    public bool AutoStartRecording
    {
        get { return (bool)GetValue(AutoStartRecordingProperty); }
        set { SetValue(AutoStartRecordingProperty, value); }
    }
```
```xaml
<cv:CameraView x:Name="cameraView" WidthRequest="300" HeightRequest="200"
               Cameras="{Binding Cameras, Mode=OneWayToSource}" Camera="{Binding Camera}"
               AutoStartPreview="{Binding AutoStartPreview}"
               NumCamerasDetected="{Binding NumCameras, Mode=OneWayToSource}"
               AutoSnapShotAsImageSource="True" AutoSnapShotFormat="PNG" 
               TakeAutoSnapShot="{Binding TakeSnapshot}" AutoSnapShotSeconds="{Binding SnapshotSeconds}"
               Microphones="{Binding Microphones, Mode=OneWayToSource}" Microphone="{Binding Microphone}"
               NumMicrophonesDetected="{Binding NumMicrophones, Mode=OneWayToSource}"
               AutoRecordingFile="{Binding RecordingFile}" 
               AutoStartRecording="{Binding AutoStartRecording}">
    <cv:CameraView.PluginDecoder>
        <bc:ZXingDecoder Options="{Binding BarCodeOptions}"
                         Results="{Binding BarCodeResults, Mode=OneWayToSource}" />
    </cv:CameraView.PluginDecoder>
</cv:CameraView>
```

To bind ```CameraView.PluginDecoders``` instead of ```CameraView.PluginDecoder```
```xaml
<cv:CameraView x:Name="cameraView" WidthRequest="300" HeightRequest="200"
               Cameras="{Binding Cameras, Mode=OneWayToSource}" Camera="{Binding Camera}"
               AutoStartPreview="{Binding AutoStartPreview}"
               NumCamerasDetected="{Binding NumCameras, Mode=OneWayToSource}"
               AutoSnapShotAsImageSource="True" AutoSnapShotFormat="PNG" 
               TakeAutoSnapShot="{Binding TakeSnapshot}" AutoSnapShotSeconds="{Binding SnapshotSeconds}"
               Microphones="{Binding Microphones, Mode=OneWayToSource}" Microphone="{Binding Microphone}"
               NumMicrophonesDetected="{Binding NumMicrophones, Mode=OneWayToSource}"
               AutoRecordingFile="{Binding RecordingFile}" 
               AutoStartRecording="{Binding AutoStartRecording}">
    <cv:CameraView.PluginDecoder>
        <plugin:PluginDecoderCollection xmlns:plugin="clr-namespace:Camera.MAUI.Plugin;assembly=Camera.MAUI.Plugin">
            <bc:ZXingDecoder Options="{Binding BarCodeOptions}"
                             Results="{Binding BarCodeResults, Mode=OneWayToSource}" />
        </plugin:PluginDecoderCollection>
    </cv:CameraView.PluginDecoder>
</cv:CameraView>
```


You have a complete example of MVVM in [MVVM Example](https://github.com/hjam40/Camera.MAUI/tree/master/Camera.MAUI.Test/MVVM)


Enable and Handle barcodes detection:
```csharp
    cameraView.PluginDecoder.Decoded += CameraView_BarcodeDetected;
    if (cameraView.PluginDecoder is ZXingDecoder zxingDecoder)
    {
        zxingDecoder.Options = new ZXingDecoderOptions
        {
            AutoRotate = true,
            PossibleFormats = { Plugin.BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = false,
            TryInverted = true
        };
    }
    if (cameraView.PluginDecoder is MLKitBarcodeDecoder mlkitDecoder)
    {
        mlkitDecoder.Options = new MLKitBarcodeDecoderOptions
        {
            PossibleFormats = { Plugin.BarcodeFormat.QR_CODE },
        };
    }
    cameraView.BarCodeDetectionFrameRate = 10;
    cameraView.BarCodeDetectionMaxThreads = 5;
    cameraView.ControlBarcodeResultDuplicate = true;
    cameraView.BarCodeDetectionEnabled = true;

    private void CameraView_BarcodeDetected(object sender, ZXingHelper.BarcodeEventArgs args)
    {
        Debug.WriteLine("BarcodeText=" + args.Result[0].Text);
    }
```
Use the event or the bindable property BarCodeResults
```csharp
    /// Event launched every time a successful decode occurs in the image if "Camera.MAUI.CameraView.BarCodeDetectionEnabled" is set to true.
    public event PluginDecoderResultHandler Decoded;
    /// It refresh each time a successful decode occurs, if "Camera.MAUI.CameraView.BarCodeDetectionEnabled" property is true.
    public IPluginResult[] Results
```

## BarcodeImage

A ContentView control for generate codebars images. 

In XAML, make sure to add the right XML namespace:

`xmlns:cv="clr-namespace:Camera.MAUI;assembly=Camera.MAUI"`  
`xmlns:bc="clr-namespace:Camera.MAUI.Plugin.ZXing;assembly=Camera.MAUI.Plugin.ZXing"`

Use the control and its bindable properties:
```xaml
<cv:BarcodeImage x:Name="barcodeImage" Aspect="AspectFit"
                 WidthRequest="400" HeightRequest="400" 
                 BarcodeWidth="200" BarcodeHeight="200" BarcodeMargin="5"
                 BarcodeBackground="White" BarcodeForeground="Blue"
                 BarcodeFormat="QR_CODE">
    <cv:BarcodeImage.BarcodeRenderer>
        <bc:ZXingRenderer />
    </cv:BarcodeImage.BarcodeRenderer>
</cv:BarcodeImage
```
Set the barcode property to generate the image:
```csharp
barcodeImage.Barcode = "https://github.com/hjam40/Camera.MAUI";
```
