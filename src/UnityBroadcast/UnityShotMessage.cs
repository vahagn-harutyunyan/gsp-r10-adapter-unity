namespace gspro_r10.UnityBroadcast
{
  public class UnityShotMessage
  {
    public long UtcUnixMs { get; set; }
    public float BallSpeedMps { get; set; }
    public float LaunchVertDeg { get; set; }
    public float LaunchHorizDeg { get; set; }
    public float SpinRpm { get; set; }
    public float SpinAxisDeg { get; set; }
    public float CarryMeters { get; set; }
    public float TotalMeters { get; set; }
    public bool IsTestShot { get; set; }
  }
}
