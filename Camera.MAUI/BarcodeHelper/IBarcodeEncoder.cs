namespace Camera.MAUI;

public interface IBarcodeEncoder
{
    MemoryStream EncodeBarcode(string code, BarcodeFormat format, int width, int height, int margin, Color Foreground, Color Background);
}
