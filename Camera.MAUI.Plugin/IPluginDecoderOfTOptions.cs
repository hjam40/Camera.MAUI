using System.Windows.Input;

namespace Camera.MAUI.Plugin
{
    public interface IPluginDecoder<TOptions, TResult> : IPluginDecoder
        where TOptions : IPluginDecoderOptions
        where TResult : IPluginResult
    {
        #region Public Properties

        TOptions Options { get; set; }
        TResult[] Results { get; set; }

        #endregion Public Properties
    }
}