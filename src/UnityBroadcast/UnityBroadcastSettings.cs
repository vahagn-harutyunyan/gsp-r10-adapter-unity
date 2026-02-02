using Microsoft.Extensions.Configuration;

namespace gspro_r10.UnityBroadcast
{
  public class UnityBroadcastSettings
  {
    public bool Enabled { get; set; } = true;
    public string WebsocketUrl { get; set; } = "ws://0.0.0.0:8765";

    public static UnityBroadcastSettings FromConfiguration(IConfigurationSection section)
    {
      UnityBroadcastSettings settings = new UnityBroadcastSettings();

      string? enabledValue = section["enabled"];
      if (!string.IsNullOrWhiteSpace(enabledValue))
        settings.Enabled = bool.Parse(enabledValue);

      string? urlValue = section["websocketUrl"];
      if (!string.IsNullOrWhiteSpace(urlValue))
        settings.WebsocketUrl = urlValue;

      return settings;
    }
  }
}
