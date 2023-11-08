using Camera.MAUI.Barcode;

namespace Camera.MAUI;

public partial class BarcodeImage : ContentView
{
    public static readonly BindableProperty BarcodeForegroundProperty = BindableProperty.Create(nameof(BarcodeForeground), typeof(Color), typeof(BarcodeImage), Colors.Black, propertyChanged: RefreshRender);
    public static readonly BindableProperty BarcodeBackgroundProperty = BindableProperty.Create(nameof(BarcodeBackground), typeof(Color), typeof(BarcodeImage), Colors.White, propertyChanged: RefreshRender);
    public static readonly BindableProperty BarcodeWidthProperty = BindableProperty.Create(nameof(BarcodeWidth), typeof(int), typeof(BarcodeImage), 200, propertyChanged: RefreshRender);
    public static readonly BindableProperty BarcodeHeightProperty = BindableProperty.Create(nameof(BarcodeHeight), typeof(int), typeof(BarcodeImage), 200, propertyChanged: RefreshRender);
    public static readonly BindableProperty BarcodeMarginProperty = BindableProperty.Create(nameof(BarcodeMargin), typeof(int), typeof(BarcodeImage), 200, propertyChanged: RefreshRender);
    public static readonly BindableProperty BarcodeFormatProperty = BindableProperty.Create(nameof(BarcodeFormat), typeof(BarcodeFormat), typeof(BarcodeImage), BarcodeFormat.QR_CODE, propertyChanged: RefreshRender);
    public static readonly BindableProperty BarcodeProperty = BindableProperty.Create(nameof(Barcode), typeof(string), typeof(BarcodeImage), string.Empty, propertyChanged: RefreshRender);
    public static readonly BindableProperty AspectProperty = BindableProperty.Create(nameof(Aspect), typeof(Aspect), typeof(BarcodeImage), Aspect.AspectFit);
    public static readonly BindableProperty BarcodeRendererProperty = BindableProperty.Create(nameof(BarcodeRenderer), typeof(IBarcodeRenderer), typeof(BarcodeImage), null);

    /// <summary>
    /// Foreground color for Codebar generation. This is a bindable property.
    /// </summary>
    public Color BarcodeForeground
    {
        get { return (Color)GetValue(BarcodeForegroundProperty); }
        set { SetValue(BarcodeForegroundProperty, value); }
    }

    /// <summary>
    /// Background color for Codebar generation. This is a bindable property.
    /// </summary>
    public Color BarcodeBackground
    {
        get { return (Color)GetValue(BarcodeBackgroundProperty); }
        set { SetValue(BarcodeBackgroundProperty, value); }
    }

    /// <summary>
    /// Width of generated Barcode image. This is a bindable property.
    /// </summary>
    public int BarcodeWidth
    {
        get { return (int)GetValue(BarcodeWidthProperty); }
        set { SetValue(BarcodeWidthProperty, value); }
    }

    /// <summary>
    /// Height of generated Barcode image. This is a bindable property.
    /// </summary>
    public int BarcodeHeight
    {
        get { return (int)GetValue(BarcodeHeightProperty); }
        set { SetValue(BarcodeHeightProperty, value); }
    }

    /// <summary>
    /// Margin of generated Barcode image. This is a bindable property.
    /// </summary>
    public int BarcodeMargin
    {
        get { return (int)GetValue(BarcodeMarginProperty); }
        set { SetValue(BarcodeMarginProperty, value); }
    }

    /// <summary>
    /// Barcode Format for the generated image. This is a bindable property.
    /// </summary>
    public BarcodeFormat BarcodeFormat
    {
        get { return (BarcodeFormat)GetValue(BarcodeFormatProperty); }
        set { SetValue(BarcodeFormatProperty, value); }
    }

    /// <summary>
    /// Barcode string to Encode. This is a bindable property.
    /// </summary>
    public string Barcode
    {
        get { return (string)GetValue(BarcodeProperty); }
        set { SetValue(BarcodeProperty, value); }
    }

    /// <summary>
    /// Scale mode for the image. This is a bindable property.
    /// </summary>
    public Aspect Aspect
    {
        get { return (Aspect)GetValue(AspectProperty); }
        set { SetValue(AspectProperty, value); }
    }

    /// <summary>
    /// Barcode renderer. This is a bindable property.
    /// </summary>
    public IBarcodeRenderer BarcodeRenderer
    {
        get { return (IBarcodeRenderer)GetValue(BarcodeRendererProperty); }
        set { SetValue(BarcodeRendererProperty, value); }
    }

    public BarcodeImage()
    {
        InitializeComponent();
    }

    private static void RefreshRender(BindableObject bindable, object oldValue, object newValue)
    {
        if (oldValue != newValue && bindable is BarcodeImage barcodeImage)
        {
            barcodeImage.BarcodeRenderer.Foreground = barcodeImage.BarcodeForeground;
            barcodeImage.BarcodeRenderer.Background = barcodeImage.BarcodeBackground;
            if (!string.IsNullOrEmpty(barcodeImage.Barcode) && barcodeImage.BarcodeWidth > 0 && barcodeImage.BarcodeHeight > 0)
            {
                var imageSource = barcodeImage.BarcodeRenderer.EncodeBarcode(barcodeImage.Barcode, barcodeImage.BarcodeFormat, barcodeImage.BarcodeWidth, barcodeImage.BarcodeHeight, barcodeImage.BarcodeMargin);
                if (imageSource != null) barcodeImage.image.Source = imageSource;
            }
        }
    }
}