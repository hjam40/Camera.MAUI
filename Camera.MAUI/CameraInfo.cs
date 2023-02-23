namespace Camera.MAUI;

public class CameraInfo
{
    public string Name { get; set; }
    public string DeviceId { get; set; }
    public override string ToString()
    {
        return Name;
    }
}
