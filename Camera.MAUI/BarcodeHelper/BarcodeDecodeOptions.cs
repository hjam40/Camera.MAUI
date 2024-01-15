namespace Camera.MAUI;

public record BarcodeDecodeOptions
{
    public bool AutoRotate { get; init; } = true;
    public string CharacterSet { get; init; } = string.Empty;
    public IList<BarcodeFormat> PossibleFormats { get; init; } = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
    public bool PureBarcode { get; init; } = false;
    public bool ReadMultipleCodes { get; init; } = false;
    public bool TryHarder { get; init; } = true;
    public bool TryInverted { get; init; } = true;
}
