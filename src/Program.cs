using gspro_r10.UnityBroadcast;
using Microsoft.Extensions.Configuration;

namespace gspro_r10
{
  class Program
  {
    public static void Main()
    {
      
      IConfigurationBuilder builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory());

      if (File.Exists(Path.Join(Directory.GetCurrentDirectory(), "settings.json")))
      {
        builder.AddJsonFile("settings.json");
      }
      else
      {
        BaseLogger.LogMessage($"settings.json file not found or could not be opened in {Directory.GetCurrentDirectory()}", "Main", LogMessageType.Error);
      }

      IConfigurationRoot configuration = builder.Build();
      
      Console.Title = "GSP-R10 Connect";
      BaseLogger.LogMessage("GSP - R10 Bridge starting. Press enter key to close", "Main");
      UnityBroadcastSettings unitySettings = UnityBroadcastSettings.FromConfiguration(configuration.GetSection("unityBroadcast"));
      IShotPublisher? shotPublisher = null;
      UnityShotWebSocketBroadcaster? unityBroadcaster = null;
      if (unitySettings.Enabled)
      {
        shotPublisher = new ShotPublisher();
        unityBroadcaster = new UnityShotWebSocketBroadcaster(shotPublisher, unitySettings);
        BaseLogger.LogMessage($"Unity broadcast enabled at {unitySettings.WebsocketUrl}", "Unity");
      }

      ConnectionManager manager = new ConnectionManager(configuration, shotPublisher);
      Console.ReadLine();
      BaseLogger.LogMessage("Shutting down...", "Main");
      manager.Dispose();
      unityBroadcaster?.Dispose();
      BaseLogger.LogMessage("Exiting...", "Main");
    }
  }
}
