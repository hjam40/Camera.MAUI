using System.Diagnostics;
using ZXing.QrCode.Internal;

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
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;
        cameraView.BarCodeOptions = new ZXingHelper.BarcodeDecodeOptions
        {
            AutoRotate = true,
            PossibleFormats = { ZXing.BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = true,
            TryInverted = true
        };
        BindingContext = cameraView;
        //this.SetBinding(StreamProperty, nameof(cameraView.SnapShotStream));
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
        if (e.Value && cameraView.AutoSnapShotSeconds <= 0 || !cameraView.AutoSnapShotAsImageSource)
            snapPreview.SetBinding(Image.SourceProperty, nameof(cameraView.SnapShot));
        else if (cameraView.AutoSnapShotSeconds <= 0)
            snapPreview.RemoveBinding(Image.SourceProperty);
        cameraView.TakeAutoSnapShot = e.Value;
    }
}