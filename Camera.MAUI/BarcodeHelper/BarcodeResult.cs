namespace Camera.MAUI;

public class BarcodeResult
{
    public BarcodeResult(string text, byte[] rawBytes, Point[] resultPoints, BarcodeFormat barcodeFormat)
    {
        Text = text;
        RawBytes = rawBytes;
        ResultPoints = resultPoints;
        BarcodeFormat = barcodeFormat;
    }

    //
    // Returns:
    //     raw text encoded by the barcode, if applicable, otherwise
    //     null
    public string Text { get; private set; }

    //
    // Returns:
    //     raw bytes encoded by the barcode, if applicable, otherwise
    //     null
    public byte[] RawBytes { get; private set; }

    //
    // Returns:
    //     points related to the barcode in the image. These are typically points identifying
    //     finder patterns or the corners of the barcode. The exact meaning is specific
    //     to the type of barcode that was decoded.
    public Point[] ResultPoints { get; private set; }

    //
    // Returns:
    //     {@link BarcodeFormat} representing the format of the barcode that was decoded
    public BarcodeFormat BarcodeFormat { get; private set; }
}