namespace Camera.MAUI;

public class CameraInfo
{
    public string Name { get; internal set; }
    public string DeviceId { get; internal set; }
    public CameraPosition Position { get; internal set; }
    public override string ToString()
    {
        return Name;
    }
}
