using Camera.MAUI.ZXing;

namespace Camera.MAUI.Test;

public partial class FullScreenPage : ContentPage
{
    bool playing = false;
	public FullScreenPage()
	{
		InitializeComponent();
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;
        cameraView.BarCodeDecoder = (IBarcodeDecoder)new ZXingBarcodeDecoder();
        cameraView.BarCodeOptions = new BarcodeDecodeOptions
        {
            AutoRotate = true,
            PossibleFormats = { BarcodeFormat.QR_CODE },
            ReadMultipleCodes = false,
            TryHarder = false,
            TryInverted = true
        };
        cameraView.BarCodeDetectionEnabled = true;
    }

    private void CameraView_BarcodeDetected(object sender, ZXingHelper.BarcodeEventArgs args)
    {
        barCodeText.Text = args.Result[0].Text;
        barCodeText.IsVisible = true;
        System.Diagnostics.Debug.WriteLine("QR Detected:  " + args.Result[0].Text);
    }

    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (cameraView.Cameras.Count > 0)
        {
            cameraView.Camera = cameraView.Cameras.First();
            /*
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (await cameraView.StartCameraAsync() == CameraResult.Success)
                {
                    controlButton.Text = "Stop";
                    playing = true;
                }
            });
            */
        }
    }
    private async void Button_Clicked(object sender, EventArgs e)
    {
        cameraView.Camera = cameraView.Cameras.First();
        if (playing)
        {
            var result = await cameraView.StopCameraAsync();
            if (result == CameraResult.Success)
                controlButton.Text = "Play";
        }
        else
        {
            var result = await cameraView.StartCameraAsync();
            if (result == CameraResult.Success)
                controlButton.Text = "Stop";
        }
        playing = !playing;
    }
}