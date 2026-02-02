namespace gspro_r10.UnityBroadcast
{
  public interface IShotPublisher
  {
    event Action<UnityShotMessage>? ShotPublished;
    void Publish(UnityShotMessage shot);
  }
}
