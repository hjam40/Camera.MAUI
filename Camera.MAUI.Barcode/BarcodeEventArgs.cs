namespace Camera.MAUI.Barcode;

public record BarcodeEventArgs
{
    public BarcodeResult[] Result { get; init; }
    public byte[][] Images { get; init; }
}