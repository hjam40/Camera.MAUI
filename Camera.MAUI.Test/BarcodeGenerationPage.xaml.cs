namespace Camera.MAUI.Test;

using Camera.MAUI.ZXing;

public partial class BarcodeGenerationPage : ContentPage
{
	public BarcodeGenerationPage()
	{
		InitializeComponent();
        barcodeImage.BarcodeEncoder = new ZXingBarcodeEncoder();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
		if (!string.IsNullOrEmpty(codeEntry.Text))
		{
			barcodeImage.Barcode = codeEntry.Text;
        }
    }
}