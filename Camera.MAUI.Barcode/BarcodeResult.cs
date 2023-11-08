using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Camera.MAUI.Barcode
{
    public class BarcodeResult
    {
        public BarcodeResult(string text, byte[] rawBytes, Point[] resultPoints, BarcodeFormat barcodeFormat, IDictionary<string, object> resultMetadata, int numBits, long timestamp)
        {
            Text = text;
            RawBytes = rawBytes;
            ResultPoints = resultPoints;
            BarcodeFormat = barcodeFormat;
            ResultMetadata = resultMetadata;
            NumBits = numBits;
            Timestamp = timestamp;
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