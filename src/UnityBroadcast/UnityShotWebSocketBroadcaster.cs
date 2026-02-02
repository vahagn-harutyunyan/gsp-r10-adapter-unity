using System.Text.Json;
using Fleck;

namespace gspro_r10.UnityBroadcast
{
  public sealed class UnityShotWebSocketBroadcaster : IDisposable
  {
    private readonly object sync = new object();
    private readonly HashSet<IWebSocketConnection> clients = new HashSet<IWebSocketConnection>();
    private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly IShotPublisher publisher;
    private readonly WebSocketServer server;

    public UnityShotWebSocketBroadcaster(IShotPublisher publisher, UnityBroadcastSettings settings)
    {
      this.publisher = publisher;
      publisher.ShotPublished += OnShotPublished;

      server = new WebSocketServer(settings.WebsocketUrl);
      server.Start(socket =>
      {
        socket.OnOpen = () =>
        {
          lock (sync)
          {
            clients.Add(socket);
          }
          // Send one synthetic shot per connection for integration testing.
          SendTestShot(socket);
        };
        socket.OnClose = () =>
        {
          lock (sync)
          {
            clients.Remove(socket);
          }
        };
        socket.OnError = _ =>
        {
          lock (sync)
          {
            clients.Remove(socket);
          }
        };
      });
    }

    private void OnShotPublished(UnityShotMessage shot)
    {
      string payload = JsonSerializer.Serialize(shot, serializerOptions);
      List<IWebSocketConnection> snapshot;
      lock (sync)
      {
        snapshot = clients.ToList();
      }

      int sentCount = 0;
      foreach (IWebSocketConnection client in snapshot)
      {
        if (!client.IsAvailable)
        {
          lock (sync)
          {
            clients.Remove(client);
          }
          continue;
        }

        try
        {
          client.Send(payload);
          sentCount += 1;
        }
        catch
        {
          lock (sync)
          {
            clients.Remove(client);
          }
        }
      }

      BaseLogger.LogMessage($"Unity broadcasted shot to {sentCount} client(s)", "Unity");
    }

    public void Dispose()
    {
      publisher.ShotPublished -= OnShotPublished;
      lock (sync)
      {
        clients.Clear();
      }
      server.Dispose();
    }

    private void SendTestShot(IWebSocketConnection socket)
    {
      UnityShotMessage testShot = new UnityShotMessage()
      {
        UtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        BallSpeedMps = 70.0f,
        LaunchVertDeg = 14.5f,
        LaunchHorizDeg = 1.2f,
        SpinRpm = 2600.0f,
        SpinAxisDeg = -5.0f,
        CarryMeters = 215.0f,
        TotalMeters = 230.0f,
        IsTestShot = true
      };

      string payload = JsonSerializer.Serialize(testShot, serializerOptions);
      if (!socket.IsAvailable)
        return;

      try
      {
        socket.Send(payload);
      }
      catch
      {
        lock (sync)
        {
          clients.Remove(socket);
        }
      }
    }
  }
}
