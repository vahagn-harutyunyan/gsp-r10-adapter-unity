using System.Text.Json;
using System.Text.Json.Serialization;
using gspro_r10.OpenConnect;
using gspro_r10.UnityBroadcast;
using Microsoft.Extensions.Configuration;

namespace gspro_r10
{
  public class ConnectionManager: IDisposable
  {
    private R10ConnectionServer? R10Server;
    private OpenConnectClient OpenConnectClient;
    private BluetoothConnection? BluetoothConnection { get; }
    internal HttpPuttingServer? PuttingConnection { get; }
    private readonly IShotPublisher? shotPublisher;
    public event ClubChangedEventHandler? ClubChanged;
    public delegate void ClubChangedEventHandler(object sender, ClubChangedEventArgs e);
    public class ClubChangedEventArgs: EventArgs
    {
      public Club Club { get; set; }
    }

    private JsonSerializerOptions serializerSettings = new JsonSerializerOptions()
    {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private int shotNumber = 0;
    private bool disposedValue;

    public ConnectionManager(IConfigurationRoot configuration, IShotPublisher? shotPublisher = null)
    {
      this.shotPublisher = shotPublisher;
      OpenConnectClient = new OpenConnectClient(this, configuration.GetSection("openConnect"));
      OpenConnectClient.ConnectAsync();

      if (bool.Parse(configuration.GetSection("r10E6Server")["enabled"] ?? "false"))
      {
        R10Server = new R10ConnectionServer(this, configuration.GetSection("r10E6Server"));
        R10Server.Start();
      }

      if (bool.Parse(configuration.GetSection("bluetooth")["enabled"] ?? "false"))
        BluetoothConnection = new BluetoothConnection(this, configuration.GetSection("bluetooth"));

      if (bool.Parse(configuration.GetSection("putting")["enabled"] ?? "false"))
      {
        PuttingConnection = new HttpPuttingServer(this, configuration.GetSection("putting"));
        PuttingConnection.Start();
      }
    }

    internal void SendShot(BallData? ballData, ClubData? clubData)
    {
      string openConnectMessage = JsonSerializer.Serialize(OpenConnectApiMessage.CreateShotData(
        shotNumber++,
        ballData,
        clubData
      ), serializerSettings);

      OpenConnectClient.SendAsync(openConnectMessage);

      if (shotPublisher != null)
      {
        try
        {
          UnityShotMessage unityShot = CreateUnityShotMessage(ballData);
          shotPublisher.Publish(unityShot);
        }
        catch (Exception ex)
        {
          BaseLogger.LogMessage($"Unity broadcast failed: {ex.Message}", "Unity", LogMessageType.Error);
        }
      }
    }

    private static UnityShotMessage CreateUnityShotMessage(BallData? ballData)
    {
      const float MilesPerHourToMetersPerSecond = 0.44704f;
      const float YardsToMeters = 0.9144f;

      float speedMps = 0f;
      float launchVertDeg = 0f;
      float launchHorizDeg = 0f;
      float spinRpm = 0f;
      float spinAxisDeg = 0f;
      float carryMeters = 0f;

      if (ballData != null)
      {
        speedMps = (float)ballData.Speed * MilesPerHourToMetersPerSecond;
        launchVertDeg = (float)ballData.VLA;
        launchHorizDeg = (float)ballData.HLA;
        spinRpm = (float)ballData.TotalSpin;
        spinAxisDeg = (float)ballData.SpinAxis;
        carryMeters = (float)ballData.CarryDistance * YardsToMeters;
      }

      return new UnityShotMessage()
      {
        UtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        BallSpeedMps = speedMps,
        LaunchVertDeg = launchVertDeg,
        LaunchHorizDeg = launchHorizDeg,
        SpinRpm = spinRpm,
        SpinAxisDeg = spinAxisDeg,
        CarryMeters = carryMeters,
        TotalMeters = 0f,
        IsTestShot = false
      };
    }

    public void ClubUpdate(Club club)
    {
      Task.Run(() => {
        ClubChanged?.Invoke(this, new ClubChangedEventArgs()
        {
          Club = club
        });
      });

    }

    internal void SendLaunchMonitorReadyUpdate(bool deviceReady)
    {
      OpenConnectClient.SetDeviceReady(deviceReady);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          R10Server?.Dispose();
          PuttingConnection?.Dispose();
          BluetoothConnection?.Dispose();
          OpenConnectClient?.DisconnectAndStop();
          OpenConnectClient?.Dispose();
        }
        disposedValue = true;
      }
    }

    public void Dispose()
    {
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
