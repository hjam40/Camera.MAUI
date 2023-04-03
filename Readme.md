
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
1. Add camera permissions to your application:

#### Android

In your `AndroidManifest.xml` file (Platforms\Android) add the following permission:

```xml
<uses-permission android:name="android.permission.CAMERA" />
```

#### iOS/MacCatalyst

In your `info.plist` file (Platforms\iOS / Platforms\MacCatalyst) add the following permission:

```xml
<key>NSCameraUsageDescription</key>
<string>This app uses camera for...</string>
```
Make sure that you enter a clear and valid reason for your app to access the camera. This description will be shown to the user.

#### Windows

In your Package.appxmanifest file (Platforms\Windows) go to Capabilities and mark Web Camera.

For more information on permissions, see the [Microsoft Docs](https://docs.microsoft.com/dotnet/maui/platform-integration/appmodel/permissions).

### Using CameraView

In XAML, make sure to add the right XML namespace:

`xmlns:cv="clr-namespace:Camera.MAUI;assembly=Camera.MAUI"`

Use the control:
```xaml
<cv:CameraView x:Name="cameraView" WidthRequest="300" HeightRequest="200"/>
```

Configure the events:
```csharp
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;
```
Configure the camera to use:
```csharp
    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (cameraView.Cameras.Count > 0)
        {
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
```
Start camera playback:
```csharp
         if (await cameraView.StartCameraAsync() == CameraResult.Success)
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
```
Get a snapshot MVVM:
The control has several binding properties for take an snapshot:
```csharp
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
```

Enable and Handle barcodes detection:
```csharp
		cameraView.BarcodeDetected += CameraView_BarcodeDetected;
        cameraView.BarCodeOptions = new ZXingHelper.BarcodeDecodeOptions
        {
            AutoRotate = true,
            PossibleFormats = { ZXing.BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = true,
            TryInverted = true
        };
		cameraView.BarCodeDetectionFrameRate = 10;
		cameraView.BarCodeDetectionEnabled = true;

    private void CameraView_BarcodeDetected(object sender, ZXingHelper.BarcodeEventArgs args)
    {
        Debug.WriteLine("BarcodeText=" + args.Result[0].Text);
    }
```
Use the event or the bindable property BarCodeResults
```csharp
    /// Event launched every time a code is detected in the image if "BarCodeDetectionEnabled" is set to true.
    public event BarcodeResultHandler BarcodeDetected;
    /// It refresh each time a barcode is detected if BarCodeDetectionEnabled porperty is true
    public Result[] BarCodeResults
```

## BarcodeImage

A ContentView control for generate codebars images. 

In XAML, make sure to add the right XML namespace:

`xmlns:cv="xmlns:cv="clr-namespace:Camera.MAUI;assembly=Camera.MAUI"`

Use the control and its bindable properties:
```xaml
<cv:BarcodeImage x:Name="barcodeImage" Aspect="AspectFit"
                 WidthRequest="400" HeightRequest="400" 
                 BarcodeWidth="200" BarcodeHeight="200" BarcodeMargin="5"
                 BarcodeBackground="White" BarcodeForeground="Blue"
                 BarcodeFormat="QR_CODE" />
```
Set the barcode property to generate the image:
```csharp
barcodeImage.Barcode = "https://github.com/hjam40/Camera.MAUI";
```
