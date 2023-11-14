using ZXing;

#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else

using DecodeDataType = System.Object;

#endif

namespace Camera.MAUI.Plugin.ZXing
{
    public class ZXingDecoder : PluginDecoder<ZXingDecoderOptions, ZXingResult>
    {
        #region Private Fields

        private readonly BarcodeReaderGeneric BarcodeReader = new();

        #endregion Private Fields

        #region Public Properties

        /// <summary>
        /// If true BarcodeDetected event will invoke only if a Results is diferent from preview Results
        /// </summary>
        public bool ControlBarcodeResultDuplicate { get; set; } = false;

        #endregion Public Properties

        #region Public Methods

        public override void ClearResults()
        {
            Results = null;
        }

        public override void Decode(DecodeDataType data)
        {
            System.Diagnostics.Debug.WriteLine("Calculate Luminance " + DateTime.Now.ToString("mm:ss:fff"));

            LuminanceSource lumSource = default;
#if ANDROID
            lumSource = new Platforms.Android.BitmapLuminanceSource(data);
#elif IOS || MACCATALYST
            lumSource = new Platforms.MaciOS.RGBLuminanceSource(data);
#elif WINDOWS
            lumSource = new Platforms.Windows.SoftwareBitmapLuminanceSource(data);
#endif
            System.Diagnostics.Debug.WriteLine("End Calculate Luminance " + DateTime.Now.ToString("mm:ss:fff"));

            try
            {
                Result[] results = null;
                if (Options is ZXingDecoderOptions zxingOptions && zxingOptions.ReadMultipleCodes)
                {
                    results = BarcodeReader.DecodeMultiple(lumSource);
                }
                else
                {
                    var result = BarcodeReader.Decode(lumSource);
                    if (result != null) results = new Result[] { result };
                }
                if (results?.Length > 0)
                {
                    var nativeResults = results.Select(x => x.ToNative()).ToArray();
                    bool refresh = true;
                    if (ControlBarcodeResultDuplicate)
                    {
                        if (Results != null)
                        {
                            foreach (var result in nativeResults)
                            {
                                refresh = Results.FirstOrDefault(b => b.Text == result.Text && b.BarcodeFormat == result.BarcodeFormat) == null;
                                if (refresh) break;
                            }
                        }
                    }
                    if (refresh)
                    {
                        Results = nativeResults;
                        OnDecoded(new PluginDecodedEventArgs { Results = Results });
                    }
                }
            }
            catch { }
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void OnOptionsChanged(object oldValue, object newValue)
        {
            if (newValue is BarcodeDecoderOptions barcodeOptions)
            {
                BarcodeReader.Options.PossibleFormats = barcodeOptions.PossibleFormats?.Select(x => x.ToPlatform()).ToList();
            }
            if (newValue is ZXingDecoderOptions zxingOptions)
            {
                BarcodeReader.AutoRotate = zxingOptions.AutoRotate;
                if (zxingOptions.CharacterSet != string.Empty)
                    BarcodeReader.Options.CharacterSet = zxingOptions.CharacterSet;

                BarcodeReader.Options.TryHarder = zxingOptions.TryHarder;
                BarcodeReader.Options.TryInverted = zxingOptions.TryInverted;
                BarcodeReader.Options.PureBarcode = zxingOptions.PureBarcode;
            }
        }

        #endregion Protected Methods

        /*
        internal void DecodeBarcode(DecodeDataType data)
        {
            System.Diagnostics.Debug.WriteLine("Calculate Luminance " + DateTime.Now.ToString("mm:ss:fff"));

            LuminanceSource lumSource = default;
#if ANDROID
            lumSource = new Platforms.Android.BitmapLuminanceSource(data);
#elif IOS || MACCATALYST
            lumSource = new Platforms.MaciOS.RGBLuminanceSource(data);
#elif WINDOWS
            lumSource = new Platforms.Windows.SoftwareBitmapLuminanceSource(data);
#endif
            System.Diagnostics.Debug.WriteLine("End Calculate Luminance " + DateTime.Now.ToString("mm:ss:fff"));

            try
            {
                Result[] results = null;
                if (Options.ReadMultipleCodes)
                    results = BarcodeReader.DecodeMultiple(lumSource);
                else
                {
                    var result = BarcodeReader.Decode(lumSource);
                    if (result != null) results = new Result[] { result };
                }
                if (results?.Length > 0)
                {
                    bool refresh = true;
                    if (ControlBarcodeResultDuplicate)
                    {
                        if (BarCodeResults != null)
                        {
                            foreach (var result in results)
                            {
                                refresh = BarCodeResults.FirstOrDefault(b => b.Text == result.Text && b.BarcodeFormat == result.BarcodeFormat) == null;
                                if (refresh) break;
                            }
                        }
                    }
                    if (refresh)
                    {
                        int width;
                        int height;
#if ANDROID
                        width = data.Width;
                        height = data.Height;
#else
                        width = 0;
                        height = 0;
#endif

                        var images = new List<byte[]>();
                        foreach (var result in results)
                        {
                            var points = new List<ResultPoint>();

                            //foreach (var p in result.ResultPoints)
                            for (int i = 0; i < result.ResultPoints.Length; i++)
                            {
                                var p = result.ResultPoints[i];

                                float msize;
                                if (p is ZXing.QrCode.Internal.FinderPattern fp)
                                    msize = fp.EstimatedModuleSize * 3;
                                else
                                    msize = 0;

                                float adjX;
                                float adjY;
                                switch (i)
                                {
                                    case 0:
                                        adjX = msize;
                                        adjY = -msize;
                                        break;

                                    case 1:
                                        adjX = msize;
                                        adjY = msize;
                                        break;

                                    case 2:
                                        adjX = -msize;
                                        adjY = msize;
                                        break;

                                    default:
                                        adjX = adjY = 0;
                                        break;
                                }

                                ResultPoint pNew = result.ResultMetadata[ResultMetadataType.ORIENTATION] switch
                                {
                                    0 => new ResultPoint(p.X + adjX, p.Y + adjY),
                                    90 => new ResultPoint(width - p.Y + adjX, p.X + adjY),
                                    180 => new ResultPoint(width - p.X + adjX, height - p.Y + adjY),
                                    270 => new ResultPoint(p.Y + adjX, height - p.X + adjY),
                                    _ => null,
                                };

                                points.Add(pNew);
                            }

                            float? p0_x = null, p0_y = null;
                            float? p1_x = null, p1_y = null;
                            float? p2_x = null, p2_y = null;
                            foreach (var p in points)
                            {
                                if (!p1_x.HasValue || p.X < p1_x) p1_x = p.X;
                                if (!p1_y.HasValue || p.Y < p1_y) p1_y = p.Y;
                                if (!p2_x.HasValue || p.X > p2_x) p2_x = p.X;
                                if (!p2_y.HasValue || p.Y > p2_y) p2_y = p.Y;
                            }
                            p0_x = ((p2_x - p1_x) / 2) + p1_x;
                            p0_y = ((p2_y - p1_y) / 2) + p1_y;

                            var stream = new MemoryStream();
#if ANDROID
#if REMOVED
                            var cropped = DecodeDataType.CreateBitmap(
                                data,
                                (int)p1_x, (int)p1_y,
                                (int)(p2_x - p1_x), (int)(p2_y - p1_y));

                            cropped.Compress(DecodeDataType.CompressFormat.Jpeg, 100, stream);
#endif

                            var paint = new Android.Graphics.Paint
                            {
                                AntiAlias = true,
                                Color = Android.Graphics.Color.Red,
                                StrokeWidth = 4
                            };
                            var paint2 = new Android.Graphics.Paint
                            {
                                AntiAlias = true,
                                Color = Android.Graphics.Color.Yellow,
                                StrokeWidth = 1,
                            };
                            paint2.SetStyle(Android.Graphics.Paint.Style.Stroke);

                            var canvas = new Android.Graphics.Canvas(data);
                            canvas.DrawLine(points[0].X, points[0].Y, points[1].X, points[1].Y, paint);
                            canvas.DrawLine(points[1].X, points[1].Y, points[2].X, points[2].Y, paint);
                            canvas.DrawLine(points[2].X, points[2].Y, points[3].X, points[3].Y, paint);
                            canvas.DrawLine(points[3].X, points[3].Y, points[0].X, points[0].Y, paint);

                            var modsize = (result.ResultPoints[0] as ZXing.QrCode.Internal.FinderPattern).EstimatedModuleSize * 3;
                            canvas.DrawRect(points[0].X - modsize, points[0].Y - modsize, points[0].X + modsize, points[0].Y + modsize, paint2);

                            //data.Compress(DecodeDataType.CompressFormat.Jpeg, 100, stream);

                            var src = new List<float>();
                            var dst = new List<float>();
                            foreach (var p in points)
                            {
#if REMOVED
                                // X and Y values are "flattened" into the array.
                                src.Add(p.X - p1_x.Value);
                                src.Add(p.Y - p1_y.Value);

                                // set up a dest polygon which is just a rectangle
                                if (p.X < p0_x)
                                    dst.Add(0);
                                else
                                    dst.Add(p2_x.Value - p1_x.Value);
                                if (p.Y < p0_y)
                                    dst.Add(0);
                                else
                                    dst.Add(p2_y.Value - p1_y.Value);
#endif

                                // X and Y values are "flattened" into the array.
                                src.Add(p.X);
                                src.Add(p.Y);

                                // set up a dest polygon which is just a rectangle
                                if (p.X < p0_x)
                                    dst.Add(0);
                                else
                                    dst.Add(330);
                                if (p.Y < p0_y)
                                    dst.Add(0);
                                else
                                    dst.Add(330);
                            }

                            var matrix = new Android.Graphics.Matrix();
                            // set the matrix to map the source values to the dest values.
                            matrix.SetPolyToPoly(src.ToArray(), 0, dst.ToArray(), 0, 4);
                            //var cropped = DecodeDataType.CreateBitmap(data, (int)p1_x, (int)p1_y, (int)(p2_x - p1_x), (int)(p2_y - p1_y), matrix, true);
                            //var cropped = DecodeDataType.CreateBitmap(data, 0, 0, width, height, matrix, true);
                            //var cropped = DecodeDataType.CreateBitmap(data, (int)p1_x, (int)p1_y, (int)(p2_x - p1_x), (int)(p2_y - p1_y));

                            var cropped = DecodeDataType.CreateBitmap(300, 300, DecodeDataType.Config.Argb8888);
                            canvas = new Android.Graphics.Canvas(cropped);
                            canvas.DrawBitmap(data, matrix, null);

                            cropped.Compress(DecodeDataType.CompressFormat.Jpeg, 100, stream);
#endif
                                stream.Seek(0, SeekOrigin.Begin);
                            images.Add(stream.ToArray());
                        }

                        BarCodeResults = results;
                        OnPropertyChanged(nameof(BarCodeResults));
                        BarcodeDetected?.Invoke(this, new BarcodeEventArgs { Result = results, Images = images.ToArray() });
                    }
                }
            }
            catch { }
        }

        private enum TransformMode
        {
            None,
            RotateLeft,
            RotateRight,
            FlipVertical,
            FlipHorizontal,
        }
        */
    }
}