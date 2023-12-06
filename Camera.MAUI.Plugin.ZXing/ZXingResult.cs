namespace Camera.MAUI.Plugin.ZXing
{
    public class ZXingResult : BarcodeResult
    {
        public ZXingResult(string text, byte[] rawBytes, Point[] resultPoints, BarcodeFormat barcodeFormat, IDictionary<string, object> resultMetadata, int numBits, long timestamp) : base(text, rawBytes, resultPoints, barcodeFormat)
        {
            ResultMetadata = resultMetadata;
            NumBits = numBits;
            Timestamp = timestamp;
        }

        //
        // Returns:
        //     {@link Hashtable} mapping {@link ResultMetadataType} keys to values. May be
        //     null
        //     . This contains optional metadata about what was detected about the barcode,
        //     like orientation.
        public IDictionary<string, object> ResultMetadata { get; private set; }

        //
        // Summary:
        //     Gets the timestamp.
        public long Timestamp { get; private set; }

        //
        // Summary:
        //     how many bits of ZXing.Result.RawBytes are valid; typically 8 times its length
        public int NumBits { get; private set; }
    }
}