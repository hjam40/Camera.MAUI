//using Camera.MAUI.MLKit;
using Camera.MAUI.ZXing;
using CommunityToolkit.Maui.Views;
using System.Diagnostics;

namespace Camera.MAUI.Test;

public partial class SizedPage : ContentPage
{
    public static readonly BindableProperty StreamProperty = BindableProperty.Create(nameof(Stream), typeof(Stream), typeof(SizedPage), null, propertyChanged: SetStream);

    private static void SetStream(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != oldValue && newValue is Stream str)
        {
            var control = bindable as SizedPage;
            str.Position = 0;
            control.snapPreview.RemoveBinding(Image.SourceProperty);
            control.snapPreview.Source = ImageSource.FromStream(() => str);
        }
    }
    public string BarcodeText { get; set; } = "No barcode detected";
    public Stream Stream {
        get { return (Stream)GetValue(StreamProperty); }
        set { SetValue(StreamProperty, value); } 
    }
	public SizedPage()
	{
		InitializeComponent();
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        cameraView.MicrophonesLoaded += CameraView_MicrophonesLoaded;
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;
        cameraView.BarCodeDecoder = new ZXingBarcodeDecoder();
        //cameraView.BarCodeDecoder = new MLKitBarcodeDecoder();
        cameraView.BarCodeOptions = new BarcodeDecodeOptions
        {
            AutoRotate = true,
            PossibleFormats = { BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = false,
            TryInverted = true
        };
        BindingContext = cameraView;
    }

    private void CameraView_MicrophonesLoaded(object sender, EventArgs e)
    {
        microPicker.ItemsSource = cameraView.Microphones;
        microPicker.SelectedIndex = 0;
    }

    private void CameraView_BarcodeDetected(object sender, ZXingHelper.BarcodeEventArgs args)
    {
        BarcodeText = "Barcode: " + args.Result[0].Text;
        OnPropertyChanged(nameof(BarcodeText));
        Debug.WriteLine("BarcodeText=" + args.Result[0].Text);
    }

    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        cameraPicker.ItemsSource = cameraView.Cameras;
        cameraPicker.SelectedIndex = 0;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (cameraPicker.SelectedItem != null && cameraPicker.SelectedItem is CameraInfo camera)
        {
            cameraLabel.BackgroundColor = Colors.White;
            cameraView.Camera = camera;
            var result = await cameraView.StartCameraAsync();
            Debug.WriteLine("Start camera result " + result);
        }
        else
        {
            cameraLabel.BackgroundColor = Colors.Red;
        }
    }
    private async void OnStartRecordingClicked(object sender, EventArgs e)
    {
        if (cameraPicker.SelectedItem != null && cameraPicker.SelectedItem is CameraInfo camera)
        {
            //if (microPicker.SelectedItem != null && microPicker.SelectedItem is MicrophoneInfo micro)
            //{
                cameraLabel.BackgroundColor = Colors.White;
                microLabel.BackgroundColor = Colors.White;
                cameraView.Camera = camera;
            //cameraView.Microphone = micro;
#if IOS
            var result = await cameraView.StartRecordingAsync(Path.Combine(FileSystem.Current.CacheDirectory, "Video.mov"));
#else
            var result = await cameraView.StartRecordingAsync(Path.Combine(FileSystem.Current.CacheDirectory, "Video.mp4"), new Size(1280, 720));
#endif
            Debug.WriteLine("Start recording result " + result);
            //}
            //else
            //    microLabel.BackgroundColor = Colors.Red;
        }
        else
            cameraLabel.BackgroundColor = Colors.Red;
    }
    private async void OnStopRecordingClicked(object sender, EventArgs e)
    {
        var result = await cameraView.StopRecordingAsync();
        Debug.WriteLine("Stop recording result " + result);
#if IOS
        player.Source = MediaSource.FromFile(Path.Combine(FileSystem.Current.CacheDirectory, "Video.mov"));
#else
        player.Source = MediaSource.FromFile(Path.Combine(FileSystem.Current.CacheDirectory, "Video.mp4"));
#endif
    }
    private async void OnStopClicked(object sender, EventArgs e)
    {
        var result = await cameraView.StopCameraAsync();
        Debug.WriteLine("Stop camera result " + result);
    }
    private void OnSnapClicked(object sender, EventArgs e)
    {        
        var result = cameraView.GetSnapShot(ImageFormat.PNG);
        if (result != null)
            snapPreview.Source = result;
    }

    private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        cameraView.MirroredImage = e.Value;
    }
    private void CheckBox4_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        cameraView.TorchEnabled = e.Value;
    }
    private void CheckBox3_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        cameraView.BarCodeDetectionEnabled = e.Value;
    }

    private void Stepper_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (cameraView != null) cameraView.ZoomFactor = (float)e.NewValue;
    }

    private void CameraPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cameraPicker.SelectedItem != null && cameraPicker.SelectedItem is CameraInfo camera)
        {
            torchLabel.IsEnabled = torchCheck.IsEnabled = camera.HasFlashUnit;
            if (camera.MaxZoomFactor > 1)
            {
                zoomLabel.IsEnabled = zoomStepper.IsEnabled = true;
                zoomStepper.Maximum = camera.MaxZoomFactor;
            }else
                zoomLabel.IsEnabled = zoomStepper.IsEnabled = true;
            cameraView.Camera = camera;
        }
    }

    private void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (float.TryParse(e.NewTextValue, out float value))
        {
            cameraView.AutoSnapShotSeconds = value;
            if (value <= 0)
                snapPreview.RemoveBinding(Image.SourceProperty);
            else
                snapPreview.SetBinding(Image.SourceProperty, nameof(cameraView.SnapShot));
        }
    }

    private void CheckBox_CheckedChanged_1(object sender, CheckedChangedEventArgs e)
    {
        cameraView.FlashMode = e.Value ? FlashMode.Enabled : FlashMode.Disabled;
    }

    private void MicroPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (microPicker.SelectedItem != null && microPicker.SelectedItem is MicrophoneInfo micro)
        {
            cameraView.Microphone = micro;
        }
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var stream = await cameraView.TakePhotoAsync();
        if (stream != null)
        {
            var result = ImageSource.FromStream(() => stream);
            snapPreview.Source = result;
        }
    }

    private void Button_Clicked_1(object sender, EventArgs e)
    {
        cameraView.ForceAutoFocus();
    }
}