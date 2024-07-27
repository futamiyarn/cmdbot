using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace cmdbot
{
  static class Actions
  {

    public static void SaveCommand(string baseUri, string savePath, List<string> action = null, string group = null, bool withUri = false)
    {
      GetList(baseUri).ContinueWith(task =>
      {
        var actions = task.Result;
        List<ActionItem> selectedAction;
        if (action != null)
          selectedAction = actions.FindAll(a => action.Contains(a.Name));
        else if (group == "(No Group)")
          selectedAction = actions.FindAll(a => a.Group == "");
        else if (group != null)
          selectedAction = actions.FindAll(a => a.Group == group);
        else
          selectedAction = actions;

        if (selectedAction.Count == 0)
        {
          Console.WriteLine("No actions found, please check your action in Streamer.bot");
          return;
        }

        foreach (var a in selectedAction)
        {
          string pathGroup = (group != null || (group == null && action == null)) ?
            Path.Combine(savePath, (string.IsNullOrEmpty(a.Group) ? "No Group" : a.Group)) : savePath;
          if (group != null && !Directory.Exists(pathGroup))
            Directory.CreateDirectory(pathGroup);

          ConfigSystem config = Config.Get();
          string exepath = (config.portableMode) ? System.Reflection.Assembly.GetEntryAssembly().Location : "Cmdbot.exe";

          string defaultCmd = $"\"{exepath}\" run --action \"{a.Name}://{a.Id}\"";
          if (withUri) defaultCmd += $" --addr {baseUri.Replace("http://", "")}";

          string vbsFile = $"CreateObject(\"WScript.Shell\").Run \"{defaultCmd.Replace("\"", "\"\"")}\", 0";

          File.WriteAllText(Path.Combine(pathGroup, $"{a.Name}.bat"), defaultCmd);
          File.WriteAllText(Path.Combine(pathGroup, $"{a.Name}.vbs"), vbsFile);

          Console.WriteLine($"Command '{a.Name}' saved to {pathGroup}");
        }

      }).Wait();
    }

    public static async Task<List<ActionItem>> GetList(string baseUri)
    {
      using (HttpClient client = new HttpClient())
      {
        try
        {
          string url = $"{baseUri}/GetActions"; // Assuming this is the endpoint
          var response = await client.GetStringAsync(url);
          var actionsData = JsonConvert.DeserializeObject<ActionsResponse>(response);

          if (actionsData != null && actionsData.Actions != null)
            return actionsData.Actions;

        }
        catch (Exception ex)
        {
          Console.WriteLine("Error: " + ex.Message);
        }

        return new List<ActionItem>();
      }
    }

    public static async Task ExecuteCommand(string name, string id, string baseUri, string arg)
    {
      using (HttpClient client = new HttpClient())
      {
        try
        {

          // send data post to /DoAction
          string url = $"{baseUri}/DoAction";
          string param = $"{{ \"action\": {{\"id\": \"{id}\", \"name\": \"{name}\"}}, \"args\": {{ {arg} }} }}";

          var content = new StringContent(param, Encoding.UTF8, "application/json");

          var response = await client.PostAsync(url, content);
          Console.WriteLine($"Command '{name}' executed successfully.");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Failed to execute command '{name}'. Error: {ex}");
        }
      }
    }

    public static void ShowList(string baseUri)
    {
      GetList(baseUri).ContinueWith(task =>
      {
        var groupedActions = task.Result
        .GroupBy(a => string.IsNullOrEmpty(a.Group) ? "(No Group)" : a.Group)
        .OrderBy(g => g.Key == "(No Group)" ? int.MaxValue : 0) // Place "No Group" last
        .ThenBy(g => g.Key); // Sort other groups alphabetically

        Console.WriteLine("\nActions:");

        foreach (var group in groupedActions)
        {

          Console.WriteLine(group.Key);
          foreach (var action in group)
          {
            string status = action.Enabled ? "" : " (disabled)";


            Console.WriteLine($"- {action.Name}{status}");
          }


          Console.WriteLine();
        }
      }).Wait();
    }
  }
}
