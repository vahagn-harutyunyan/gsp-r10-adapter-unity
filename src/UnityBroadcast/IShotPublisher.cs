namespace gspro_r10.UnityBroadcast
{
  public interface IShotPublisher
  {
    event Action<UnityBallDataMessage>? ShotPublished;
    void Publish(UnityBallDataMessage shot);
  }
}
