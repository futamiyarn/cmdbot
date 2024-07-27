using Newtonsoft.Json;
using System;
using System.IO;

namespace cmdbot
{
  internal class Config
  {
    public static void Init()
    {
      ConfigSystem configSystem = new ConfigSystem
      {
        portableMode = false,
        noStreamerBot = false,
        address = "127.0.0.1",
        port = "7474",
        savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "cmdbot")
      };

      // save aside exe file
      string exeFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string exePath = Path.GetDirectoryName(exeFile);
      string configPath = Path.Combine(exePath, "config.json");

      string json = JsonConvert.SerializeObject(configSystem, Formatting.Indented);
      File.WriteAllText(configPath, json);
    }

    public static ConfigSystem Get()
    {
      string exeFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string exePath = Path.GetDirectoryName(exeFile);
      string configPath = Path.Combine(exePath, "config.json");

      if (!File.Exists(configPath)) return null;

      string json = File.ReadAllText(configPath);
      return JsonConvert.DeserializeObject<ConfigSystem>(json);
    }

    public static string TSPath(string path)
    {
      string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      if (path.StartsWith("(exe)\\"))
        return Path.GetFullPath(Path.Combine(exePath, "..", path.Replace("(exe)", "")));
      if (path.StartsWith("~\\"))
        return Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.Replace("~", "")));
      else
        return path;
    }
  }

  public class ConfigSystem
  {
    [JsonProperty("portable-mode")]
    public bool portableMode { get; set; }
    [JsonProperty("no-streamer-bot")]
    public bool noStreamerBot { get; set; }
    public string address { get; set; }
    public string port { get; set; }
    public string savePath { get; set; }
  }
}
