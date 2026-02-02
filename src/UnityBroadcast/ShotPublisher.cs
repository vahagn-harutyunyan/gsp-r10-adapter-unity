namespace gspro_r10.UnityBroadcast
{
  public sealed class ShotPublisher : IShotPublisher
  {
    public event Action<UnityBallDataMessage>? ShotPublished;

    public void Publish(UnityBallDataMessage shot)
    {
      ShotPublished?.Invoke(shot);
    }
  }
}
