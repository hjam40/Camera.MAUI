using System.Diagnostics;

namespace Camera.MAUI.Test;

public partial class SizedPage : ContentPage
{
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
    }

    private void CameraView_BarcodeDetected(object sender, ZXingHelper.BarcodeEventArgs args)
    {
        Debug.WriteLine("BarcodeText=" + args.Result[0].Text);
    }

    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        cameraPicker.ItemsSource = cameraView.Cameras;
        cameraPicker.SelectedIndex = 0;
        if (cameraView.MaxZoomFactor > 1) zoomStepper.Maximum = cameraView.MaxZoomFactor;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if (cameraPicker.SelectedItem != null && cameraPicker.SelectedItem is CameraInfo camera)
        {
            cameraLabel.BackgroundColor = Colors.White;
            cameraView.Camera = camera;
            var result = await cameraView.StartCameraAsync();
            if (cameraView.MaxZoomFactor > 1) zoomStepper.Maximum = cameraView.MaxZoomFactor;
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
    private void CheckBox2_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        cameraView.FlashMode = e.Value ? FlashMode.Auto : FlashMode.Disabled;
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
}