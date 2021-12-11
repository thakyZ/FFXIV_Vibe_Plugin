using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

#region Dalamud deps
using Dalamud.IoC;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Network;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
#endregion

#region Other deps
using Buttplug;
#endregion

#region FFXIV_Vibe_Plugin deps
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Hooks;
using FFXIV_Vibe_Plugin.Experimental;
#endregion

namespace FFXIV_Vibe_Plugin {

  public sealed class Plugin : IDalamudPlugin {
    // Plugin definition
    public string Name => "FFXIV Vibe Plugin";
    public static readonly string ShortName = "FVP";
    public readonly string commandName = "/fvp";

    // Custom variables from Kacie
    private readonly Logger Logger;
    private readonly PlayerStats PlayerStats;
    private bool _buttplugIsConnected = false;
    private float _currentIntensity = -1;
    private bool _firstUpdated = false;
    private readonly FFXIV_Vibe_Plugin.Hooks.ActionEffect hook_ActionEffect;

    // Experiments
    private readonly FFXIV_Vibe_Plugin.Experimental.NetworkCapture experiment_networkCapture;

    // Buttplug
    public class ButtplugDevice {
      public string Name { get; set; }
      public int Id { get; set; }
      public ButtplugDevice(int id, string name) {
        Name = name;
        Id= id;
      }
    }
    public List<ButtplugDevice> ButtplugDevices = new();
    
    // Initialize buttplug
    private Buttplug.ButtplugClient? buttplugClient;
    private SortedSet<ChatTrigger> Triggers = new();

    [PluginService]
    [RequiredVersion("1.0")]
    private Dalamud.Game.Gui.ChatGui? DalamudChat { get; init; }
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private Configuration Configuration { get; init; }
    private PluginUI PluginUi { get; init; }
    private GameNetwork GameNetwork { get; init; }
    private DataManager DataManager { get; init; }

    // Others
    private readonly ClientState ClientState;
    private string AuthorizedUser = "";

    // SequencerTask
    private List<SequencerTask> sequencerTasks = new();
    private readonly bool playSequence = true;

    // Chat types
    private readonly XivChatType[] allowedChatTypes = {
      XivChatType.Say, XivChatType.Party,
      XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4,
      XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8,
      XivChatType.FreeCompany, XivChatType.CrossParty,
      XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2,
      XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4,
      XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6,
      XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8
    };

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] GameNetwork gameNetwork,
        [RequiredVersion("1.0")] SigScanner scanner,
        [RequiredVersion("1.0")] ObjectTable gameObjects,
        [RequiredVersion("1.0")] DataManager dataManager
        )
    {

      this.PluginInterface = pluginInterface;
      this.CommandManager = commandManager;
      this.GameNetwork = gameNetwork;
      this.ClientState = clientState;
      this.DataManager = dataManager;

      this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      this.Configuration.Initialize(this.PluginInterface);
      this.PluginUi = new PluginUI(this.Configuration, this.PluginInterface, this);
      this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
        HelpMessage = "A vibe plugin for fun..."
      });

      this.PluginInterface.UiBuilder.Draw += DrawUI;
      this.PluginInterface.UiBuilder.OpenConfigUi += DisplayConfigUI;
      if(DalamudChat != null && CheckForTriggers != null) {
        DalamudChat.ChatMessage += CheckForTriggers; // XXX: o.o
      }


      // Default values
      this.AuthorizedUser = "";

      /** Experimental */
      this.PlayerStats = new PlayerStats(this.ClientState);
      
      PlayerStats.Event_CurrentHpChanged += this.Player_currentHPChanged;
      PlayerStats.Event_MaxHpChanged += this.Player_currentHPChanged;

      /** Experimental auto connect */
      if(this.Configuration.AUTO_CONNECT) {
        this.sequencerTasks.Add(new SequencerTask("nothing", 1000));
        this.sequencerTasks.Add(new SequencerTask("connect", 500));
      }

      // Initialize the logger
      this.Logger = new Logger(this.DalamudChat, ShortName, Logger.LogLevel.VERBOSE);

      // Initialize Hook ActionEffect
      this.hook_ActionEffect = new(this.DataManager, this.Logger, scanner, clientState, gameObjects);
      this.hook_ActionEffect.ReceivedEvent += SpellWasTriggered;

      // Experimental
      this.experiment_networkCapture = new NetworkCapture(this.Logger, this.GameNetwork);
      
      
      
    }

    private void SpellWasTriggered(object? sender, HookActionEffects_ReceivedEventArgs args) {
      this.Logger.Info(args.Spell.ToString());
    }


    public void Dispose() {
      this.Logger.Debug("Plugin dispose...");

      this.CommandManager.RemoveHandler(commandName);
      if(DalamudChat != null) {
        DalamudChat.ChatMessage -= CheckForTriggers;
      }

      // Cleaning hooks
      this.hook_ActionEffect.Dispose();

      // Cleaning experimentations
      this.experiment_networkCapture.Dispose();

      this.PluginUi.Dispose();

      // Check for buttplugClient
      if(this.buttplugClient != null && this.buttplugClient.Connected) {
        this.Logger.Debug("Buttplug disconnecting...");
        try {
          this.buttplugClient.DisconnectAsync();
        } catch(Exception e) {
          this.Logger.Error("Could not disconnect from buttplug. Was connected?", e);
          return;
        }
      }
    }



    private void DrawUI() {

      this.PluginUi.Draw();

      this.PlayerStats.Update();

      this.RunSequencer(this.sequencerTasks);

      this.Draw_firstUpdated();

    }

    private void Draw_firstUpdated() {
      if(!this._firstUpdated) {
        this.FirstUpdated();
        this.Logger.Debug("First updated");
      }
      this._firstUpdated = true;


    }

    private void FirstUpdated() {
      this.LoadTriggersConfig();
    }

    private void DisplayUI() {
      this.PluginUi.Visible = true;
    }

    private void DisplayConfigUI() {
      this.PluginUi.Visible = true;
    }

    private void RunSequencer(List<SequencerTask> sequencerTasks) {
      if(sequencerTasks != null) {
        this.sequencerTasks = sequencerTasks;
      }

      if(this.playSequence && this.sequencerTasks.Count > 0) {

        SequencerTask st = this.sequencerTasks[0];

        if(st._startedTime == 0) {
          st.Play();
          string[] commandSplit = st.Command.Split(':', 2);
          string task = commandSplit[0];
          string param1 = commandSplit.Length > 1 ? commandSplit[1] : "";
          this.Logger.Debug($"Playing sequence: {task} {param1}");
          if(task == "connect") {
            this.Command_ConnectButtplugs("connect");
          } else if(task == "buttplug_sendVibe") {
            float intensity = float.Parse(param1);
            this.Buttplug_sendVibe(intensity);
          } else if(task == "print") {
            this.Logger.Chat(param1);
          } else if(task == "print_debug") {
            this.Logger.Debug(param1);
          } else if(task == "nothing") {
            // do nothing
          } else {
            this.Logger.Debug($"Sequencer task unknown: {task} {param1}");
          }
        }

        if(st._startedTime + st.Duration < GetUnix()) {
          this.sequencerTasks[0]._startedTime = 0;
          this.sequencerTasks.RemoveAt(0);
        }
      }
    }

   


    private void CheckForTriggers(XivChatType type, uint senderId, ref SeString _sender, ref SeString _message, ref bool isHandled) {
      string sender = _sender.ToString();
      if(!allowedChatTypes.Any(ct => ct == type) || (AuthorizedUser.Length > 0 && !sender.ToString().Contains(AuthorizedUser))) {
        return;
      }
      string message = _message.ToString().ToLower();
      var matchingintensities = this.Triggers.Where(t => message.Contains(t.Text.ToLower()));
      if(matchingintensities.Any() && buttplugClient != null) {
        int intensity = matchingintensities.Select(t => t.Intensity).Max();
        this.Logger.Debug($"Sending vibe from chat {message}, {intensity}");
        this.Buttplug_sendVibe(intensity);
      }
    }


    public static string GetHelp(string command) {
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

    private void PrintHelp() {
      this.Logger.Chat("Please go to the configuration menu under 'help'");
    }

    private void OnCommand(string command, string args) {
      if(args.Length == 0) {
        PrintHelp();
        this.DisplayUI();
      } else {
        if(args.StartsWith("help")) {
          this.Logger.Chat(GetHelp($"/{ShortName}"));
        } else if(args.StartsWith("config")) {
          this.DisplayConfigUI();
        } else if(args.StartsWith("connect")) {
          Command_ConnectButtplugs(args);
        } else if(args.StartsWith("disconnect")) {
          this.DisconnectButtplugs();
        } else if(args.StartsWith("toys_list")) {
          this.Command_ToysList();
        } else if(args.StartsWith("chat_list_triggers")) {
          this.Command_ListTriggers();
        } else if(args.StartsWith("chat_add")) {
          this.Command_AddTrigger(args);
        } else if(args.StartsWith("chat_remove")) {
          this.Command_RemoveTrigger(args);
        } else if(args.StartsWith("chat_user")) {
          this.Command_SetAuthorizedUser(args);
        } else if(args.StartsWith("save")) {
          Command_SaveConfig(args);
        } else if(args.StartsWith("load")) {
          Command_LoadConfig(args);
        } else if(args.StartsWith("hp_toggle")) {
          this.Command_ToggleHP();
        } else if(args.StartsWith("threshold")) {
          this.Command_SetThreshold(args);
        } else if(args.StartsWith("send")) {
          this.Command_SendIntensity(args);
        } else if(args.StartsWith("stop")) {
          this.Buttplug_sendVibe(0);
        } else if(args.StartsWith("play_pattern")) {
          this.Play_pattern(args);
        } else if(args.StartsWith("exp_network_start")) {
          this.experiment_networkCapture.StartNetworkCapture();
        } else if(args.StartsWith("exp_network_stop")) {
          this.experiment_networkCapture.StopNetworkCapture();
        } else {
          this.Logger.Chat($"Unknown subcommand: {args}");
        }
      }
    }

    private void Command_LoadConfig(string args) {
      string config; 
      try {
        string path = args.Split(" ")[1];
        config = File.ReadAllText(path);
      } catch(Exception e) {
        this.Logger.Error($"Malformed or invalid arguments for [load]: {args}", e);
        return;
      }
      foreach(string line in config.Split("\n")) {
        string[] trigargs = line.Split(" ");
        string toMatch = trigargs[1];
        if(int.TryParse(trigargs[0], out int intensity)) {
          ChatTrigger trigger = new(intensity, toMatch);
          if(!Triggers.Add(trigger)) {
            this.Logger.Chat($"Note: duplicate trigger: {trigger}");
          };
        }
      }
      UpdateTriggersConfig();
    }

    private void UpdateTriggersConfig() {
      this.Configuration.TRIGGERS = this.Triggers;
      this.Configuration.Save();
    }

    private void LoadTriggersConfig() {
      SortedSet<ChatTrigger> triggers = this.Configuration.TRIGGERS;
      this.Logger.Debug($"Loading {triggers.Count} triggers");
      this.Triggers = new SortedSet<ChatTrigger>();
      foreach(ChatTrigger trigger in triggers) {
        this.Triggers.Add(new ChatTrigger(trigger.Intensity, trigger.Text));
      }
    }

    private void Command_SaveConfig(string args) {
      string path;
      var config = string.Join("\n", Triggers.Select(t => t.ToString()));
      try {
        path = args.Split(" ")[1];
        File.WriteAllText(path, config);
      } catch(Exception e) {
        this.Logger.Error($"Malformed or invalid arguments for [save]: {args}", e);
        return;
      }
      this.Logger.Chat($"Wrote current config to {path}");
    }


    public void Command_ConnectButtplugs(string args) {
      if(this._buttplugIsConnected) {
        this.Logger.Debug("Disconnecting previous instance! Waiting 2sec...");
        this.DisconnectButtplugs();
        Thread.Sleep(200);
      }

      try {
        this.buttplugClient = new("buttplugtriggers-dalamud");
      } catch(Exception e) {
        this.Logger.Error($"Can't load buttplug.io.", e);
        return;
      }
      buttplugClient.ServerDisconnect += ButtplugClient_ServerDisconnected;
      buttplugClient.DeviceAdded += ButtplugClient_DeviceAdded;
      buttplugClient.DeviceRemoved += ButtplugClient_DeviceRemoved;
      string host = this.Configuration.BUTTPLUG_SERVER_HOST;
      int port = this.Configuration.BUTTPLUG_SERVER_PORT;
      string hostandport = host + ":" + port.ToString();
      if(args.Contains(" ")) {
        hostandport = args.Split(" ", 2)[1];
        if(!hostandport.Contains(":")) {
          hostandport += port;
        }
      }

      try {
        var uri = new Uri($"ws://{hostandport}/buttplug");
        var connector = new ButtplugWebsocketConnectorOptions(uri);
        this.Logger.Chat($"Connecting to {hostandport}.");
        Task task = buttplugClient.ConnectAsync(connector);
        task.Wait();
      } catch(Exception e) {
        this.Logger.Error($"Could not connect to {hostandport}.", e);
      }

      Thread.Sleep(200);

      if(buttplugClient.Connected) {
        this.Logger.Chat($"Buttplug connected!");
        this._buttplugIsConnected = true;
      } else {
        this.Logger.Error("Failed connecting (Intiface server is up?)");
        return;
      }

      this.ScanToys();
    }

    private void ButtplugClient_ServerDisconnected(object? sender, EventArgs e) {
      this.Logger.Debug("Server disconnected");
      this.DisconnectButtplugs();
      
    }

    public void ScanToys() {
      this.Logger.Chat("Scanning for devices...");
      if(buttplugClient != null) {
        try {
          buttplugClient.StartScanningAsync();
        } catch(Exception e) {
          this.Logger.Error("Scanning issue...", e);
        }
      }
    }

    private void ButtplugClient_DeviceAdded(object? sender, DeviceAddedEventArgs e) {
      Thread.Sleep(500); // Make sure we are connected by waiting a bit
      string name = e.Device.Name;
      int index = (int)e.Device.Index;
      this.Logger.Chat($"Added device: {index}:{name}");
      this.ButtplugDevices.Add(new ButtplugDevice(index, name));

      /**
       * Sending some vibes at the intial stats make sure that some toys re-sync to Intiface. 
       * Therefore, it is important to trigger a zero and some vibes before continuing further.
       * Don't remove this part unless you want to debug for hours.
       */
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:0", 0));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:1", 500));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:0", 0));
    }

    private void ButtplugClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs e) {
      this.Logger.Log($"Removed device: {e.Device.Name}:{e.Device.Index}");
      int index = this.ButtplugDevices.FindIndex(device => device.Id == e.Device.Index);
      this.ButtplugDevices.RemoveAt(index);
    }

    public void DisconnectButtplugs() {
      if(buttplugClient == null || !buttplugClient.Connected) {
        this._buttplugIsConnected = false;
        return;  }
      try {
        for(int i = 0; i < buttplugClient.Devices.Length; i++) {
          this.Logger.Log($"Disconnecting device {i} {buttplugClient.Devices[i].Name}");
          buttplugClient.Devices[i].Dispose();
        }
      } catch(Exception e) {
        this.Logger.Error("Error while disconnecting device", e);
      }
      try {
        Thread.Sleep(1000);
        this.buttplugClient.DisconnectAsync();
        this.Logger.Log("Disconnecting! Bye... Waiting 2sec...");
      } catch(Exception e) {
        // ignore exception, we are trying to do our best
        this.Logger.Error("Error while disconnecting client", e);
      }

      this._buttplugIsConnected = false;
      this.buttplugClient = null;
    }

    private void Command_ToysList() {
      if(buttplugClient == null) { return; }
      for(int i = 0; i < buttplugClient.Devices.Length; i++) {
        string name = buttplugClient.Devices[i].Name;
        this.Logger.Chat($"    {i}: {name}");
      }
    }

    private void Command_SetAuthorizedUser(string args) {
      try {
        AuthorizedUser = args.Split(" ", 2)[1];
      } catch(IndexOutOfRangeException) {
        this.Logger.Chat("Cleared authorized user.");
        return;
      }
      this.Logger.Chat($"Authorized user set to '{AuthorizedUser}'");
    }

    private void Command_ToggleHP() {
      bool hp_toggle = !this.Configuration.VIBE_HP_TOGGLE;
      this.Configuration.VIBE_HP_TOGGLE = hp_toggle;
      if(!hp_toggle && this._buttplugIsConnected) {
        this.Buttplug_sendVibe(0); // Don't be cruel
      }
      this.Logger.Chat($"HP Toggle set to {hp_toggle}");
      this.Configuration.Save();
    }

    private void Command_SetThreshold(string args) {
      string[] blafuckcsharp = args.Split(" ", 2);
      int threshold;
      try {
        threshold = int.Parse(blafuckcsharp[1]);
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for [threshold].", e);
        return;
      }
      this.Configuration.MAX_VIBE_THRESHOLD = threshold;
      this.Configuration.Save();
      this.Logger.Chat($"Threshold set to {threshold}");
    }

    private void Command_AddTrigger(string args) {
      string[] blafuckcsharp;
      int intensity;
      string text;
      try {
        blafuckcsharp = args.Split(" ", 3);
        intensity = int.Parse(blafuckcsharp[1]);
        text = blafuckcsharp[2].ToLower(); ;
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for [chat_add].", e);
        return; // XXX: exceptional control flow
      }
      ChatTrigger newTrigger = new(intensity, text);

      if(Triggers.Add(newTrigger)) {
        this.Logger.Chat($"Trigger added successfully: {newTrigger}...");
        this.UpdateTriggersConfig();
      } else {
        this.Logger.Error($"Failed. Possible duplicate for intensity {intensity}");
      }
    }
    private void Command_RemoveTrigger(string args) {
      int id;
      try {
        id = int.Parse(args.Split(" ")[1]);
        if(id < 0) {
          throw new FormatException(); // XXX: exceptionally exceptional control flow please somnenoee hehhehjel;;  ,.-
        }
      } catch(FormatException e) {
        this.Logger.Error("Malformed argument for [chat_remove]", e);
        return; // XXX: exceptional control flow
      }
      ChatTrigger removed = Triggers.ElementAt(id);
      Triggers.Remove(removed);
      this.Logger.Chat($"Removed Trigger: {removed}");
      this.UpdateTriggersConfig();
    }

    private void Command_ListTriggers() {
      string message =
          @"Configured triggers:
ID   Intensity   Text Match
";
      for(int i = 0; i < Triggers.Count; ++i) {
        message += $"[{i}] | {Triggers.ElementAt(i).Intensity} | {Triggers.ElementAt(i).Text}\n";
      }
      this.Logger.Chat(message);
    }


    /**
     * Sends an itensity vibe to all of the devices 
     * @param {float} intensity
     */
    public void Buttplug_sendVibe(float intensity) {
      if(this._currentIntensity != intensity && this._buttplugIsConnected && this.buttplugClient != null) {
        this.Logger.Debug($"Intensity: {intensity} / Threshold: {this.Configuration.MAX_VIBE_THRESHOLD}");

        // Set min and max limits
        if(intensity < 0) { intensity = 0.0f; } else if(intensity > 100) { intensity = 100; }
        var newIntensity = intensity / (100.0f / this.Configuration.MAX_VIBE_THRESHOLD) / 100.0f;
        for(int i = 0; i < buttplugClient.Devices.Length; i++) {
          buttplugClient.Devices[i].SendVibrateCmd(newIntensity);
        }
        this._currentIntensity = newIntensity;
      }
    }

    private void Command_SendIntensity(string args) {
      string[] blafuckcsharp;
      float intensity;
      try {
        blafuckcsharp = args.Split(" ", 2);
        intensity = float.Parse(blafuckcsharp[1]);
        this.Logger.Chat($"Command Send intensity {intensity}");
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for send [intensity].", e);
        return;
      }
      this.Buttplug_sendVibe(intensity);
    }
    private void Player_currentHPChanged(object? send, EventArgs e) {
      float currentHP = this.PlayerStats.GetCurrentHP();
      float maxHP = this.PlayerStats.GetMaxHP();
      
      if(this.Configuration.VIBE_HP_TOGGLE) {
        float percentageHP = currentHP / maxHP * 100f;
        float percentage = 100 - percentageHP;
        if(percentage == 0) {
          percentage = 0;
        }
        this.Logger.Debug($"Current: HP={currentHP} MaxHP={maxHP} Percentage={percentage}");

        int mode = this.Configuration.VIBE_HP_MODE;
        if(mode == 0) { // normal
          this.Buttplug_sendVibe(percentage);
        } else if(mode == 1) { // shake
          this.Play_patternShake(percentage);
        } else if(mode == 2) { // shake
          this.Play_patternMountain(percentage);
        }
      }
    }

    private void Play_pattern(string args) {
      try {
        string[] param = args.Split(" ", 2);
        string patternName = param[1];
        this.Logger.Chat($"Play pattern {patternName}");
        if(patternName == "shake") {
          this.Play_patternShake(100);
        } else if(patternName == "mountain") {
          this.Play_patternMountain(30);
        }
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for play_pattern [pattern_name] # shake, mountain", e);
        return;
      }
    }

    private void Play_patternShake(float from) {
      this.sequencerTasks = new();
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 50));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 1.5}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 2}", 700));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 1.5}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 200));
    }

    private void Play_patternMountain(float from) {
      this.sequencerTasks = new();
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 50));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 1.5}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 2.5}", 600));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 2}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 200));
    }

    private static int GetUnix() {
      return (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    public bool ButtplugIsConnected() {
      return this._buttplugIsConnected;
    }

   

  }
}
