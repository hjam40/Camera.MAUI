namespace Camera.MAUI.Barcode
{
    public interface IBarcodeRenderer
    {
        #region Public Properties

        Color Background { get; set; }
        Color Foreground { get; set; }

        #endregion Public Properties

        #region Public Methods

        ImageSource EncodeBarcode(string code, BarcodeFormat format = BarcodeFormat.QR_CODE, int width = 400, int height = 400, int margin = 5);

        #endregion Public Methods
    }
}