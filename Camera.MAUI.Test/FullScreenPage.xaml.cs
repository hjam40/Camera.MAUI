namespace Camera.MAUI.Test;

public partial class FullScreenPage : ContentPage
{
    bool playing = false;
	public FullScreenPage()
	{
		InitializeComponent();
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
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
            controlButton.Text = "Play";
        }
        else
        {
            var result = await cameraView.StartCameraAsync();
            controlButton.Text = "Stop";
        }
        playing = !playing;
    }
}