using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Buttplug;

using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace FFXIV_Vibe_Plugin {

  public sealed class Plugin : IDalamudPlugin {

    /** Experimental */
    private class SequencerTask {
      public string command { get; init; }
      public int duration { get; init; }
      public int _startedTime = 0;

      public SequencerTask(string cmd, int dur) {
        command = cmd;
        duration = dur;
      }

      public void play() {
        this._startedTime = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
      }
    }

    private List<SequencerTask> sequencerTasks = new List<SequencerTask>();
    private bool playSequence = true;



    [PluginService]
    [RequiredVersion("1.0")]

    // Initialize the ChatGui.
    private ChatGui Chat { get; init; }

    private Buttplug.ButtplugClient buttplugClient;
    private SortedSet<ChatTrigger> Triggers = new SortedSet<ChatTrigger>();

    public string Name => "FFXIV Vibe Plugin";
    public readonly string commandName = "/fvp";

    // Custom variables from Kacie
    private bool _buttplugIsConnected = false;
    private float currentIntensity = 0;
    private bool _firstUpdated = false;
    private PlayerStats playerStats;

    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private Configuration Configuration { get; init; }
    private PluginUI PluginUi { get; init; }
    private ClientState clientState;
    private string AuthorizedUser { get; set; }

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ClientState clientState) {

      this.PluginInterface = pluginInterface;
      this.CommandManager = commandManager;
      this.clientState = clientState;

      this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      this.Configuration.Initialize(this.PluginInterface);
      this.PluginUi = new PluginUI(this.Configuration, this.PluginInterface, this);
      this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
        HelpMessage = "A buttplug plugin for fun..."
      });

      this.PluginInterface.UiBuilder.Draw += DrawUI;
      this.PluginInterface.UiBuilder.OpenConfigUi += DisplayConfigUI;
      if (Chat != null && CheckForTriggers != null) {
        Chat.ChatMessage += CheckForTriggers; // XXX: o.o
      }

      // Default values
      this.AuthorizedUser = "";

      /** Experimental */
      this.playerStats = new PlayerStats(this.clientState);
      playerStats.event_CurrentHpChanged += this._player_currentHPChanged;
      playerStats.event_MaxHpChanged += this._player_currentHPChanged;

      /** Experimental auto connect */
      if (this.Configuration.AUTO_CONNECT) {
        this.sequencerTasks.Add(new SequencerTask("nothing", 1000));
        this.sequencerTasks.Add(new SequencerTask("connect", 500));
      }
    }

    private static bool IsValidRegex(string pattern) {
      try {
        Regex.Match("", pattern);
      } catch (ArgumentException) {
        return false;
      }
      return true;
    }

    private readonly XivChatType[ ] allowedChatTypes = {
      XivChatType.Say, XivChatType.Party,
      XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4,
      XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8,
      XivChatType.FreeCompany, XivChatType.CrossParty,
      XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2,
      XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4,
      XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6,
      XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8
    };

    private void CheckForTriggers(XivChatType type, uint senderId, ref SeString _sender, ref SeString _message, ref bool isHandled) {
      string sender = _sender.ToString();
      var code = (ushort)type;
      var channel = code & 0x7F;
      var target = 1 << ((code >> 7) & 0xF);
      if (channel != 41 && target != 2 && !allowedChatTypes.Any(ct => ct == type) || (AuthorizedUser.Length > 0 && !sender.ToString().Contains(AuthorizedUser))) {
        return;
      }
      string message = _message.ToString().ToLower();
      var matchingintensities = this.Triggers.Where(t => IsValidRegex(t.ToMatch) ? Regex.Match(message, t.ToMatch).Success : message.Contains(t.Text.ToLower()));
      if (matchingintensities.Any() && buttplugClient != null) {
        int intensity = matchingintensities.Select(t => t.Intensity).Max();
        //this.PrintDebug($"Sending vibe from chat {message}, {intensity}");
        this.buttplug_sendVibe(intensity);
      }
    }

    private void DrawUI() {

      this.PluginUi.Draw();

      this.playerStats.update();

      this.RunSequencer(this.sequencerTasks);

      this.draw_firstUpdated();

    }

    private void draw_firstUpdated() {
      if (!this._firstUpdated) {
        this.firstUpdated();
        this.PrintDebug("First updated");
      }
      this._firstUpdated = true;


    }
    private void firstUpdated() {
      this.loadTriggersConfig();
    }



    private void DisplayUI() {
      this.PluginUi.Visible = true;
    }

    private void DisplayConfigUI() {
      this.PluginUi.Visible = true;
    }

    private void RunSequencer(List<SequencerTask> sequencerTasks) {
      if (sequencerTasks != null) {

        this.sequencerTasks = sequencerTasks;
      }

      if (this.playSequence && this.sequencerTasks.Count > 0) {

        SequencerTask st = this.sequencerTasks[0];

        if (st._startedTime == 0) {
          st.play();
          string[ ] commandSplit = st.command.Split(':', 2);
          string task = commandSplit[0];
          string param1 = commandSplit.Count() > 1 ? commandSplit[1] : "";
          PrintDebug($"Playing sequence: {task} {param1}");
          if (task == "connect") {
            this.Command_ConnectButtplugs("connect");
          } else if (task == "buttplug_sendVibe") {
            float intensity = float.Parse(param1);
            this.buttplug_sendVibe(intensity);
          } else if (task == "print") {
            this.Print(param1);
          } else if (task == "print_debug") {
            this.PrintDebug(param1);
          } else if (task == "nothing") {
            // do nothing
          } else {
            PrintDebug($"Sequencer task unknown: {task} {param1}");
          }
        }

        if (st._startedTime + st.duration < this.getUnix()) {
          this.sequencerTasks[0]._startedTime = 0;
          this.sequencerTasks.RemoveAt(0);
        } else {
          // Double check that we turn off the device,
          // as not every device turns off on it's own.
          //
          // This also prevents the device from running too long in a row
          // resulting in potential injuries.
          this.buttplug_sendVibe(0);
        }
      }
    }

    public void Dispose() {
      this.CommandManager.RemoveHandler(commandName);
      Chat.ChatMessage -= CheckForTriggers; // XXX: ???
      this.PluginUi.Dispose();
      Print("Plugin dispose...");

      // Check for buttplugClient
      if (this.buttplugClient != null) {
        Print("Buttplug disconnecting...");
        try {
          this.buttplugClient.DisconnectAsync();

        } catch (Exception e) {
          PrintError("Could not disconnect from buttplug. Was connected?");
          PrintError(e.ToString());
          return;
        }
      }
    }

    public void Print(string message) {
      Chat.Print($"FFXIV_BP> {message}");
    }

    private void PrintDebug(string message) {
      if (this.Configuration.DEBUG_VERBOSE) {
        Chat.Print($"FFXIV_BP Debug> {message}");
      }
    }

    private void PrintError(string error) {
      Chat.PrintError($"FFXIV_BP error> {error}");
    }

    public string getHelp(string command) {
      string helpMessage = $@"Usage:
      
      {command} connect [ip[:port]]
      {command} disconnect
      {command} toys_list
      {command} save [file path]
      {command} load [file path]
      {command} config

Chat features
      {command} chat_list_triggers
      {command} chat_add <intensity 0-100> <trigger text>
      {command} chat_remove <id>
      {command} chat_user <authorized user>

Player features
      {command} hp_toggle 

New features
      {command} send <0-100>
      {command} threshold <0-100>
      {command} stop

Example:
       {command} connect
       {command} chat_add 0 shh
       {command} chat_add 20 slowly 
       {command} chat_add 75 getting there
       {command} chat_add 100 hey ;)
       {command} user Alice
       {command} hp_toggle
       {command} threshold 90

These commands let anyone whose name contains 'Alice'
control all your connected toys with the appropriate 
phrases, as long as those are uttered in a tell, a 
party, a (cross) linkshell, or a free company chat.
";
      return helpMessage;
    }

    private void PrintHelp(string command) {
      Chat.Print("Please go to the configuration menu under 'help'");
    }

    private void OnCommand(string command, string args) {
      if (args.Length == 0) {
        PrintHelp(command);
        this.DisplayUI();
      } else {
        switch (args) {
          case string s when s.StartsWith("config"):
            this.DisplayConfigUI();
            break;
          case string s when s.StartsWith("connect"):
            Command_ConnectButtplugs(args);
            break;
          case string s when s.StartsWith("disconnect"):
            this.DisconnectButtplugs();
            break;
          case string s when s.StartsWith("toys_list"):
            this.Command_ToysList();
            break;
          case string s when s.StartsWith("chat_list_triggers"):
            this.Command_ListTriggers();
            break;
          case string s when s.StartsWith("chat_add"):
            this.Command_AddTrigger(args);
            break;
          case string s when s.StartsWith("chat_remove"):
            this.Command_RemoveTrigger(args);
            break;
          case string s when s.StartsWith("chat_user"):
            this.Command_SetAuthorizedUser(args);
            break;
          case string s when s.StartsWith("save"):
            Command_SaveConfig(args);
            break;
          case string s when s.StartsWith("load"):
            Command_LoadConfig(args);
            break;
          case string s when s.StartsWith("hp_toggle"):
            this.Command_ToggleHP();
            break;
          case string s when s.StartsWith("threshold"):
            this.Command_SetThreshold(args);
            break;
          case string s when s.StartsWith("send"):
            this.Command_SendIntensity(args);
            break;
          case string s when s.StartsWith("stop"):
            this.buttplug_sendVibe(0);
            break;
          case string s when s.StartsWith("play_pattern"):
            this.play_pattern(args);
            break;
          case string s when s.StartsWith("verbose"):
            this.Configuration.DEBUG_VERBOSE = !this.Configuration.DEBUG_VERBOSE;
            Print($"Verbose: {this.Configuration.DEBUG_VERBOSE}");
            break;
          default:
            Print($"Unknown subcommand: {args}");
            break;
        }
      }
    }

    private void Command_LoadConfig(string args) {
      string config = "";
      string path = "";
      try {
        path = args.Split(" ")[1];
        config = File.ReadAllText(path);
      } catch (Exception) {
        PrintError($"Malformed or invalid arguments for [load]: {args}");
        return;
      }
      foreach (string line in config.Split("\n")) {
        string[ ] trigargs = line.Split(" ");
        int intensity;
        string toMatch = trigargs[1];
        if (int.TryParse(trigargs[0], out intensity)) {
          ChatTrigger trigger = new(intensity, toMatch);
          if (!Triggers.Add(trigger)) {
            Print($"Note: duplicate trigger: {trigger}");
          };
        }
      }
      updateTriggersConfig();
    }

    private void updateTriggersConfig() {
      this.Configuration.TRIGGERS = this.Triggers;
      this.Configuration.Save();
    }

    private void loadTriggersConfig() {
      SortedSet<ChatTrigger> triggers = this.Configuration.TRIGGERS;
      this.PrintDebug($"Loading {triggers.Count.ToString()} triggers");
      this.Triggers = new SortedSet<ChatTrigger>();
      foreach (ChatTrigger trigger in triggers) {
        this.Triggers.Add(new ChatTrigger(trigger.Intensity, trigger.Text));
      }
    }

    private void Command_SaveConfig(string args) {
      string path;
      var config = string.Join("\n", Triggers.Select(t => t.ToString()));
      try {
        path = args.Split(" ")[1];
        File.WriteAllText(path, config);
      } catch (Exception) {
        PrintError($"Malformed or invalid arguments for [save]: {args}");
        return;
      }
      Print($"Wrote current config to {path}");
    }


    public void Command_ConnectButtplugs(string args) {
      if (this._buttplugIsConnected) {
        PrintDebug("Disconnecting previous instance! Waiting 2sec...");
        this.DisconnectButtplugs();
        Thread.Sleep(200);
      }

      try {
        this.buttplugClient = new("buttplugtriggers-dalamud");
      } catch (Exception e) {
        PrintError($"Can't load buttplug.io: {e.Message}");
        return;
      }
      buttplugClient.ServerDisconnect += ButtplugClient_ServerDisconnected;
      buttplugClient.DeviceAdded += ButtplugClient_DeviceAdded;
      buttplugClient.DeviceRemoved += ButtplugClient_DeviceRemoved;
      string host = this.Configuration.BUTTPLUG_SERVER_HOST;
      int port = this.Configuration.BUTTPLUG_SERVER_PORT;
      string hostandport = host + ":" + port.ToString();
      if (args.Contains(" ")) {
        hostandport = args.Split(" ", 2)[1];
        if (!hostandport.Contains(":")) {
          hostandport += port;
        }
      }

      try {
        var uri = new Uri($"ws://{hostandport}/buttplug");
        var connector = new ButtplugWebsocketConnectorOptions(uri);
        Print($"Connecting to {hostandport}.");
        Task task = buttplugClient.ConnectAsync(connector);
        task.Wait();
      } catch (Exception e) {
        PrintError($"Could not connect to {hostandport}");
      }

      Thread.Sleep(200);

      if (buttplugClient.Connected) {
        Print($"Buttplug connected!");
        this._buttplugIsConnected = true;
      } else {
        PrintError("Failed connecting (Intiface server is up?)");
        return;
      }

      this.ScanToys();
    }

    private void ButtplugClient_ServerDisconnected(object? sender, EventArgs e) {
      this.Print("Server disconnected");
      this.DisconnectButtplugs();
      this._buttplugIsConnected = false;
    }

    private void ScanToys() {
      Print("Scanning for devices...");

      try {
        buttplugClient.StartScanningAsync();
      } catch (Exception e) {
        PrintError("Scanning issue...");
      }
    }

    private void ButtplugClient_DeviceAdded(object? sender, DeviceAddedEventArgs e) {
      Thread.Sleep(500); // Make sure we are connected by waiting a bit
      string name = e.Device.Name;
      int index = (int)e.Device.Index;
      Print($"Added device: {index}:{name}");
      /**
       * Sending some vibes at the intial stats make sure that some toys re-sync to Intiface. 
       * Therefore, it is important to trigger a zero and some vibes before continuing further.
       * Don't remove this part unless you want to debug for hours.
       */
      this.sequencerTasks.Add(new SequencerTask("nothing", 1000));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:0", 0));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:1", 500));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:0", 0));
    }

    private void ButtplugClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs e) {
      Print("Removed device: " + e.Device.Name);
    }

    public void DisconnectButtplugs() {
      try {
        for (int i = 0; i < buttplugClient.Devices.Length; i++) {
          buttplugClient.Devices[i].Dispose();
        }
        Task task = this.buttplugClient.DisconnectAsync();
        task.Wait();
        Print("Disconnecting! Bye... Waiting 2sec...");
        Thread.Sleep(2000); // Wait a bit before reloading the plugin.
      } catch (Exception e) {
        // ignore exception, we are trying to do our best
      }

      this._buttplugIsConnected = false;
    }

    private void Command_ToysList() {
      Print("Listing toys");
      for (int i = 0; i < buttplugClient.Devices.Length; i++) {
        string name = buttplugClient.Devices[i].Name;
        Print($"    {i}: {name}");
      }
    }

    private void Command_SetAuthorizedUser(string args) {
      try {
        AuthorizedUser = args.Split(" ", 2)[1];
      } catch (IndexOutOfRangeException) {
        Print("Cleared authorized user.");
        return;
      }
      Print($"Authorized user set to '{AuthorizedUser}'");
    }

    private void Command_ToggleHP() {
      bool hp_toggle = !this.Configuration.VIBE_HP_TOGGLE;
      this.Configuration.VIBE_HP_TOGGLE = hp_toggle;
      if (!hp_toggle && this._buttplugIsConnected) {
        this.buttplug_sendVibe(0); // Don't be cruel
      }
      Print($"HP Toggle set to {hp_toggle}");
      this.Configuration.Save();
    }

    private void Command_SetThreshold(string args) {
      string[ ] blafuckcsharp = args.Split(" ", 2);
      int threshold = 0;
      try {
        threshold = int.Parse(blafuckcsharp[1]);
      } catch (Exception e) when (e is FormatException or IndexOutOfRangeException) {
        PrintError($"Malformed arguments for [threshold].");
        return;
      }
      this.Configuration.MAX_VIBE_THRESHOLD = threshold;
      this.Configuration.Save();
      Print($"Threshold set to {threshold}");
    }

    private void Command_AddTrigger(string args) {
      string[ ] blafuckcsharp;
      int intensity;
      string text;
      try {
        blafuckcsharp = args.Split(" ", 3);
        intensity = int.Parse(blafuckcsharp[1]);
        text = blafuckcsharp[2].ToLower();
        ;
      } catch (Exception e) when (e is FormatException or IndexOutOfRangeException) {
        PrintError($"Malformed arguments for [chat_add].");
        return; // XXX: exceptional control flow
      }
      ChatTrigger newTrigger = new(intensity, text);

      if (Triggers.Add(newTrigger)) {
        Print($"Trigger added successfully: {newTrigger}...");
        this.updateTriggersConfig();
      } else {
        PrintError($"Failed. Possible duplicate for intensity {intensity}");
      }
    }
    private void Command_RemoveTrigger(string args) {
      int id = -1;
      try {
        id = int.Parse(args.Split(" ")[1]);
        if (id < 0) {
          throw new FormatException(); // XXX: exceptionally exceptional control flow please somnenoee hehhehjel;;  ,.-
        }
      } catch (FormatException) {
        PrintError("Malformed argument for [chat_remove]");
        return; // XXX: exceptional control flow
      }
      ChatTrigger removed = Triggers.ElementAt(id);
      Triggers.Remove(removed);
      Print($"Removed Trigger: {removed}");
      this.updateTriggersConfig();
    }

    private void Command_ListTriggers() {
      string message =
          @"Configured triggers:
ID   Intensity   Text Match
";
      for (int i = 0; i < Triggers.Count; ++i) {
        message += $"[{i}] | {Triggers.ElementAt(i).Intensity} | {Triggers.ElementAt(i).Text}\n";
      }
      Chat.Print(message);
    }


    /**
     * Sends an itensity vibe to all of the devices 
     * @param {float} intensity
     */
    public void buttplug_sendVibe(float intensity) {

      if (this.currentIntensity != intensity && this._buttplugIsConnected && this.buttplugClient != null) {

        PrintDebug($"Intensity: {intensity.ToString()} / Threshold: {this.Configuration.MAX_VIBE_THRESHOLD}");

        // Set min and max limits
        if (intensity < 0) { intensity = 0.0f; } else if (intensity > 100) { intensity = 100; }
        var newIntensity = intensity / (100.0f / this.Configuration.MAX_VIBE_THRESHOLD) / 100.0f;
        for (int i = 0; i < buttplugClient.Devices.Length; i++) {
          buttplugClient.Devices[i].SendVibrateCmd(newIntensity);
        }
        this.currentIntensity = newIntensity;
      }
    }

    private void Command_SendIntensity(string args) {
      string[ ] blafuckcsharp;
      float intensity;
      try {
        blafuckcsharp = args.Split(" ", 2);
        intensity = float.Parse(blafuckcsharp[1]);
        Print($"Command Send intensity {intensity}");
      } catch (Exception e) when (e is FormatException or IndexOutOfRangeException) {
        PrintError($"Malformed arguments for send [intensity].");
        return;
      }
      this.buttplug_sendVibe(intensity);
    }
    private void _player_currentHPChanged(object send, EventArgs e) {
      float currentHP = this.playerStats.getCurrentHP();
      float maxHP = this.playerStats.getMaxHP();
      this.PrintDebug($"CurrentHP: {currentHP} / {maxHP}");
      if (this.Configuration.VIBE_HP_TOGGLE) {
        float percentageHP = currentHP / maxHP * 100f;
        float percentage = 100 - percentageHP;
        if (percentage == 0) {
          percentage = 0;
        }
        this.PrintDebug($"CurrentPercentage: {percentage}");

        int mode = this.Configuration.VIBE_HP_MODE;
        if (mode == 0) { // normal
          this.buttplug_sendVibe(percentage);
        } else if (mode == 1) { // shake
          this.play_patternShake(percentage);
        } else if (mode == 2) { // shake
          this.play_patternMountain(percentage);
        }
      }
    }

    private void play_pattern(string args) {
      try {
        string[ ] param = args.Split(" ", 2);
        string patternName = param[1];
        Print($"Play pattern {patternName}");
        if (patternName == "shake") {
          this.play_patternShake(100);
        } else if (patternName == "mountain") {
          this.play_patternMountain(30);
        }
      } catch (Exception e) when (e is FormatException or IndexOutOfRangeException) {
        PrintError($"Malformed arguments for play_pattern [pattern_name] # shake, mountain");
        return;
      }
    }

    private void play_patternShake(float from) {
      this.sequencerTasks = new List<SequencerTask>();
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 50));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 1.5}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 2}", 700));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 1.5}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 200));
    }

    private void play_patternMountain(float from) {
      this.sequencerTasks = new List<SequencerTask>();
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 50));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 1.5}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 2.5}", 600));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 2}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 200));
    }

    private int getUnix() {
      return (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public bool buttplugIsConnected() {
      return this._buttplugIsConnected;
    }
  }
}
