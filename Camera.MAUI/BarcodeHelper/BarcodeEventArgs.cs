namespace Camera.MAUI.ZXingHelper;

public record BarcodeEventArgs
{
    public BarcodeResult[] Result { get; init; }
}
