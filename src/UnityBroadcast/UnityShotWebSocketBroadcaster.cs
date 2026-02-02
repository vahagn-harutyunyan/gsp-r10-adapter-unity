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
    private readonly string websocketUrl;

    public UnityShotWebSocketBroadcaster(IShotPublisher publisher, UnityBroadcastSettings settings)
    {
      this.publisher = publisher;
      publisher.ShotPublished += OnShotPublished;

      websocketUrl = settings.WebsocketUrl;
      server = new WebSocketServer(websocketUrl);
      server.Start(socket =>
      {
        socket.OnOpen = () =>
        {
          lock (sync)
          {
            clients.Add(socket);
          }
          BaseLogger.LogMessage($"Unity WebSocket client connected/reconnected ({GetEndpointDescription(socket)}) url={websocketUrl} clients={GetClientCount()}", "Unity");
          // Send one synthetic shot per connection for integration testing.
          SendTestShot(socket);
        };
        socket.OnClose = () =>
        {
          lock (sync)
          {
            clients.Remove(socket);
          }
          BaseLogger.LogMessage($"Unity WebSocket client disconnected ({GetEndpointDescription(socket)}) url={websocketUrl} clients={GetClientCount()}", "Unity");
        };
        socket.OnError = _ =>
        {
          lock (sync)
          {
            clients.Remove(socket);
          }
          BaseLogger.LogMessage($"Unity WebSocket client error/disconnected ({GetEndpointDescription(socket)}) url={websocketUrl} clients={GetClientCount()}", "Unity", LogMessageType.Error);
        };
      });
      BaseLogger.LogMessage($"Unity WebSocket server listening at {websocketUrl}", "Unity");
    }

    private void OnShotPublished(UnityBallDataMessage shot)
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

      LogShotPublish(shot, sentCount);
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

    private int GetClientCount()
    {
      lock (sync)
      {
        return clients.Count;
      }
    }

    private static string GetEndpointDescription(IWebSocketConnection socket)
    {
      try
      {
        string ip = socket.ConnectionInfo?.ClientIpAddress ?? "unknown-ip";
        int port = socket.ConnectionInfo?.ClientPort ?? 0;
        return port > 0 ? $"{ip}:{port}" : ip;
      }
      catch
      {
        return "unknown-endpoint";
      }
    }

    private void SendTestShot(IWebSocketConnection socket)
    {
      UnityBallDataMessage testShot = new UnityBallDataMessage()
      {
        UtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        SpeedMph = 155.0f,
        HlaDeg = 1.5f,
        VlaDeg = 12.8f,
        TotalSpinRpm = 2600.0f,
        SpinAxisDeg = -5.0f,
        CarryYards = 245.0f,
        BackSpinRpm = 2450.0f,
        SideSpinRpm = -220.0f,
        IsTestShot = true
      };

      string payload = JsonSerializer.Serialize(testShot, serializerOptions);
      LogShotPublish(testShot, GetClientCount());
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

    private static void LogShotPublish(UnityBallDataMessage shot, int clientCount)
    {
      try
      {
        string shotType = "n/a";
        string strength01 = "n/a";
        string curveBias = "n/a";
        string seed = "n/a";
        long timestamp = shot.UtcUnixMs != 0 ? shot.UtcUnixMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        BaseLogger.LogMessage(
          $"Unity shot publish ts={timestamp} shotType={shotType} strength01={strength01} speedMph={shot.SpeedMph:F1} hlaDeg={shot.HlaDeg:F1} vlaDeg={shot.VlaDeg:F1} totalSpinRpm={shot.TotalSpinRpm:F0} spinAxisDeg={shot.SpinAxisDeg:F1} carryYards={shot.CarryYards:F1} backSpinRpm={shot.BackSpinRpm:F0} sideSpinRpm={shot.SideSpinRpm:F0} curveBias={curveBias} seed={seed} isTestShot={shot.IsTestShot} clients={clientCount}",
          "Unity");
      }
      catch (Exception ex)
      {
        BaseLogger.LogMessage($"Unity shot publish log failed: {ex.Message}", "Unity", LogMessageType.Error);
      }
    }
  }
}
