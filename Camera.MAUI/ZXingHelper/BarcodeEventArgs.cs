using ZXing;

namespace Camera.MAUI.ZXingHelper;

public record BarcodeEventArgs
{
    public Result[] Result { get; init; }
}
