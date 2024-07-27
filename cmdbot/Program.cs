using cmdbot;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
  #region Arguments
  static ConfigSystem config;
  static string exePath;
  #endregion

  static async Task<int> Main(string[] args)
  {
    config = Config.Get();

    bool strBotRunning = Process.GetProcessesByName("streamer.bot").Length > 0;
    exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    if (!File.Exists(Path.Combine(exePath, "config.json")))
    {
      Config.Init();
      Console.WriteLine("Config has been initialized. Run cmdbot.exe --help to see available options.");
      return 0;
    }

    if (!config.noStreamerBot && !strBotRunning)
    {
      Console.WriteLine("Streamer.bot is not running");
      return 1;
    }

    #region Init Command
    var listOpt = new Option<bool>("--list", "List actions");
    listOpt.AddAlias("-l");

    var addrOpt = new Option<string>("--addr", "Set address");
    addrOpt.AddAlias("-a");

    var actionOpt = new Option<string>("--action", "Action name and id") {
      ArgumentHelpName = "name://uuid",
      IsRequired = true,
    };
    actionOpt.AddAlias("-e");

    var groupOpt = new Option<bool>("--group", "Group name");
    groupOpt.AddAlias("-g");

    var addrBool = new Option<bool>("--with-address", "Insert address in action");
    addrBool.AddAlias("-sa");

    var pathOpt = new Option<string>("--path", "Save path");
    pathOpt.AddAlias("-p");

    var varArg = new Argument<List<string>>("key:value", "Action arguments");
    varArg.Arity = ArgumentArity.ZeroOrMore;

    var nameArg = new Argument<List<string>>("name", "many name actions or a name group");
    nameArg.Arity = ArgumentArity.ZeroOrMore;


    var rootCommand = new RootCommand("Streamer.bot command generator") {
    listOpt, addrOpt
    };

    var exeCom = new Command("run", "Execute command") {
    actionOpt,addrOpt, varArg
    };

    var fixCom = new Command("fix", "Fix action") {
   pathOpt,
    };
    var saveCom = new Command("save", "Add action") {
    groupOpt, addrBool, addrOpt, pathOpt, nameArg
    };

    rootCommand.AddCommand(exeCom);
    rootCommand.AddCommand(fixCom);
    rootCommand.AddCommand(saveCom);
    #endregion

    rootCommand.SetHandler(rootHandler, listOpt, addrOpt);
    saveCom.SetHandler(saveHandler, groupOpt, addrBool, addrOpt, pathOpt, nameArg);
    exeCom.SetHandler(exeHandler, actionOpt, addrOpt, varArg);
    fixCom.SetHandler(fixHandler, pathOpt);

    return await rootCommand.InvokeAsync(args);
  }

  #region fix handler
  private static void fixHandler(string path)
  {
    if (!File.Exists(path) || Path.GetExtension(path) != ".bat")
    {
      Console.WriteLine("Invalid path.");
      return;
    }

    string oldBatfile = File.ReadAllText(path);
    string oldVbsfile = File.ReadAllText(Path.ChangeExtension(path, ".vbs"));

    string regexExe = "(?<cmdbot_dir>([A-Za-z]:\\\\)?([^\"]*?[\\\\\\/])?[Cc]mdbot\\.exe)";
    string exeFile = (config.portableMode) ? System.Reflection.Assembly.GetExecutingAssembly().Location : "Cmdbot.exe";

    string newBatfile = Regex.Replace(oldBatfile, regexExe, match =>
    {
      return match.Value.Replace(match.Groups["cmdbot_dir"].Value, exeFile);
    }, RegexOptions.IgnoreCase);

    string newVbsfile = Regex.Replace(oldVbsfile, regexExe, match =>
    {
      return match.Value.Replace(match.Groups["cmdbot_dir"].Value, exeFile);
    }, RegexOptions.IgnoreCase);

    File.WriteAllText(Path.ChangeExtension(path, ".bat"), newBatfile);
    File.WriteAllText(Path.ChangeExtension(path, ".vbs"), newVbsfile);

    Console.WriteLine("File successfully fixed!");
  }
  #endregion

  #region root handler
  private static void rootHandler(bool isList, string addr)
  {
    if (!isList)
    {
      if (Admin.IsRunningAsAdministrator() && !Admin.IsPathInSystemPath(exePath))
      {
        Console.WriteLine("Wait... You're in admin mode!");
        Console.WriteLine("Can you add my app to path windows? (y for add)");

        var yesOrNo = Console.ReadLine();
        if (yesOrNo == "y")
        {
          Admin.AddPathToSystemEnvironmentVariable(exePath);
          Console.WriteLine("Done!");
          Console.WriteLine("Restart your computer for cmdbot.exe run properly!");
        }
      }
      else
        Console.WriteLine("Check cmdbot.exe --help for more options");

      return;
    }

    string baseUri = "http://" + ((!string.IsNullOrEmpty(addr)) ? addr : $"{config.address}:{config.port}");
    Actions.ShowList(baseUri);
  }
  #endregion

  #region execude handler
  private static void exeHandler(string action, string addr, List<string> keyvalue)
  {
    string baseUri = "http://" + ((!string.IsNullOrEmpty(addr)) ? addr : $"{config.address}:{config.port}");

    string argx = "";
    string isVar = @"(?<key>\w+):""?(?<value>[\S ]+[^""\s])""?";

    foreach (var vari in keyvalue)
    {
      var match = Regex.Match(vari, isVar);
      if (match.Success)
      {
        if (IsNotString(match.Groups["value"].Value))
          argx += $"\"{match.Groups["key"].Value}\": {match.Groups["value"].Value}";
        else
          argx += $"\"{match.Groups["key"].Value}\": \"{match.Groups["value"].Value}\"";
      }
    }

    string actionRegex = @"^(?<name>.*?)://(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$";

    var actionValue = Regex.Match(action, actionRegex);

    if (actionValue.Success)
      Actions.ExecuteCommand(actionValue.Groups["name"].Value, actionValue.Groups["id"].Value, baseUri, argx).Wait();

    else
      Console.WriteLine($"Invalid action: {action}");

  }
  #endregion

  #region save handler
  private static void saveHandler(bool addGroup, bool addUri, string addr, string savePath, List<string> name)
  {
    string baseUri = "http://" + ((!string.IsNullOrEmpty(addr)) ? addr : $"{config.address}:{config.port}");
    string path = (!string.IsNullOrEmpty(savePath)) ? savePath : Config.TSPath(config.savePath);

    if (addGroup)
    {
      if (name.Count > 1)
      {
        Console.WriteLine("You can only add one group at a time");
        return;
      }

      Actions.SaveCommand(baseUri, path, group: name.Count > 0 ? name[0] : "(No Group)", withUri: addUri);
    }
    else
      Actions.SaveCommand(baseUri, path, name, withUri: addUri);
  }
  #endregion

  #region unececary code
  static bool IsNotString(string input)
  {
    // Try to parse the string as a boolean
    return bool.TryParse(input, out _) ||
      int.TryParse(input, out _) ||
      double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
  }
  #endregion
}

#region Action Object
class ActionsResponse
{
  public int Count { get; set; }
  public List<ActionItem> Actions { get; set; }
}

class ActionItem
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Group { get; set; }
  public bool Enabled { get; set; }
  public int SubactionsCount { get; set; }
}
#endregion