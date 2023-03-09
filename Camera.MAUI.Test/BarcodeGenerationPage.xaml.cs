namespace Camera.MAUI.Test;

public partial class BarcodeGenerationPage : ContentPage
{
	public BarcodeGenerationPage()
	{
		InitializeComponent();
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
		if (!string.IsNullOrEmpty(codeEntry.Text))
		{
			barcodeImage.Barcode = codeEntry.Text;
        }
    }
}