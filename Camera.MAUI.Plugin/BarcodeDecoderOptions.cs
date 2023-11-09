namespace Camera.MAUI.Plugin
{
    public record BarcodeDecoderOptions : IPluginDecoderOptions
    {
        public IList<BarcodeFormat> PossibleFormats { get; init; } = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };
    }
}