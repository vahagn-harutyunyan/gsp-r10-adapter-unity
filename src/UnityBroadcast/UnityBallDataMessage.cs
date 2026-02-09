namespace gspro_r10.UnityBroadcast
{
  public class UnityBallDataMessage
  {
    public float SpeedMph { get; set; }
    public float HlaDeg { get; set; }
    public float VlaDeg { get; set; }
    public float TotalSpinRpm { get; set; }
    public float SpinAxisDeg { get; set; }
    public float CarryYards { get; set; }
    public float BackSpinRpm { get; set; }
    public float SideSpinRpm { get; set; }
    public bool IsTestShot { get; set; }
  }
}
