using System.Diagnostics;

namespace Camera.MAUI.Test
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
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
        private async void OnSnapClicked(object sender, EventArgs e)
        {
            string filename = Path.Combine(FileSystem.CacheDirectory, "snap.png");
            var result = await cameraView.GetSnapShot(ImageFormat.PNG, filename);
            if (result)
                snapPreview.Source = ImageSource.FromStream(() => { return new FileStream(filename, FileMode.Open); });
        }

        private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            cameraView.MirroredImage = e.Value;
        }
    }
}