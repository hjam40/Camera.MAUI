namespace Camera.MAUI.Plugin
{
    public interface IPluginDecoder<TOptions, TResult> : IPluginDecoder
        where TOptions : IPluginDecoderOptions
        where TResult : IPluginResult
    {
        #region Public Properties

        /// <summary>
        /// Options for the plugin.
        /// </summary>
        TOptions Options { get; set; }

        /// <summary>
        /// It refresh each time a successful decode occurs, if "Camera.MAUI.CameraView.BarCodeDetectionEnabled" property is true.
        /// </summary>
        TResult[] Results { get; set; }

        #endregion Public Properties
    }
}