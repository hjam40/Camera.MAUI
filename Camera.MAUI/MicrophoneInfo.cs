namespace Camera.MAUI;

public class MicrophoneInfo
{
    public string Name { get; internal set; }
    public string DeviceId { get; internal set; }
    public override string ToString()
    {
        return Name;
    }
}
