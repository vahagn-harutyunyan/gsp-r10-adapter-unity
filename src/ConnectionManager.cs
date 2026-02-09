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
          UnityBallDataMessage unityShot = CreateUnityBallDataMessage(ballData);
          shotPublisher.Publish(unityShot);
        }
        catch (Exception ex)
        {
          BaseLogger.LogMessage($"Unity broadcast failed: {ex.Message}", "Unity", LogMessageType.Error);
        }
      }
    }

    private static UnityBallDataMessage CreateUnityBallDataMessage(BallData? ballData)
    {
      float speedMph = 0f;
      float hlaDeg = 0f;
      float vlaDeg = 0f;
      float totalSpinRpm = 0f;
      float spinAxisDeg = 0f;
      float carryYards = 0f;
      float backSpinRpm = 0f;
      float sideSpinRpm = 0f;

      if (ballData != null)
      {
        speedMph = (float)ballData.Speed;
        hlaDeg = (float)ballData.HLA;
        vlaDeg = (float)ballData.VLA;
        totalSpinRpm = (float)ballData.TotalSpin;
        spinAxisDeg = (float)ballData.SpinAxis;
        carryYards = (float)ballData.CarryDistance;
        backSpinRpm = (float)ballData.BackSpin;
        sideSpinRpm = (float)ballData.SideSpin;
      }

      return new UnityBallDataMessage()
      {
        SpeedMph = speedMph,
        HlaDeg = hlaDeg,
        VlaDeg = vlaDeg,
        TotalSpinRpm = totalSpinRpm,
        SpinAxisDeg = spinAxisDeg,
        CarryYards = carryYards,
        BackSpinRpm = backSpinRpm,
        SideSpinRpm = sideSpinRpm,
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
