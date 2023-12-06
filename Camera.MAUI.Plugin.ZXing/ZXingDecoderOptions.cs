namespace Camera.MAUI.Plugin.ZXing
{
    public record ZXingDecoderOptions : BarcodeDecoderOptions
    {
        public bool AutoRotate { get; init; } = true;
        public string CharacterSet { get; init; } = string.Empty;
        public bool PureBarcode { get; init; } = false;
        public bool ReadMultipleCodes { get; init; } = false;
        public bool TryHarder { get; init; } = true;
        public bool TryInverted { get; init; } = true;
    }
}