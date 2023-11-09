namespace Camera.MAUI.Plugin
{
    public record PluginDecodedEventArgs
    {
        public IPluginResult[] Results { get; init; }
    }
}