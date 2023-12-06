namespace Camera.MAUI.Plugin
{
    public interface IPluginRenderer
    {
        #region Public Properties

        Color Background { get; set; }
        Color Foreground { get; set; }

        #endregion Public Properties

        #region Public Methods

        ImageSource EncodeBarcode(string code, IPluginRendererOptions options);

        #endregion Public Methods
    }
}