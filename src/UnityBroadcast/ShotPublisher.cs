namespace gspro_r10.UnityBroadcast
{
  public sealed class ShotPublisher : IShotPublisher
  {
    public event Action<UnityShotMessage>? ShotPublished;

    public void Publish(UnityShotMessage shot)
    {
      ShotPublished?.Invoke(shot);
    }
  }
}
