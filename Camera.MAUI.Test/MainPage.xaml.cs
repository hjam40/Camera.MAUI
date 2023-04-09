namespace Camera.MAUI.Test;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }
    private async void Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new SizedPage());
    }

    private async void Button2_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FullScreenPage());
    }

    private async void Button_Clicked_1(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new BarcodeGenerationPage());
    }

    private async void Button_Clicked_2(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MVVMPage());
    }
}