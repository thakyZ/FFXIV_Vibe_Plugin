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

#region FFXIV_Vibe_Plugin deps
using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.Triggers;
using FFXIV_Vibe_Plugin.Hooks;
using FFXIV_Vibe_Plugin.Experimental;
using FFXIV_Vibe_Plugin.Migrations;
#endregion

namespace FFXIV_Vibe_Plugin {

  public sealed class Plugin : IDalamudPlugin {
    [PluginService]
    [RequiredVersion("1.0")]
    private Dalamud.Game.Gui.ChatGui? DalamudChat { get; init; }
    private DalamudPluginInterface PluginInterface { get; init; }
    private CommandManager CommandManager { get; init; }
    private Configuration Configuration { get; init; }
    private PluginUI PluginUi { get; init; }
    private GameNetwork GameNetwork { get; init; }
    private DataManager DataManager { get; init; }
    private ClientState ClientState { get; init; }

    // Plugin definition
    public string Name => "FFXIV Vibe Plugin";
    public static readonly string ShortName = "FVP";
    public readonly string commandName = "/fvp";

    // Custom variables from Kacie
    private bool _firstUpdated = false;
    private readonly PlayerStats PlayerStats;
    private ConfigurationProfile ConfigurationProfile;
    private readonly Logger Logger;
    private readonly ActionEffect hook_ActionEffect;
    private readonly Device.DevicesController DeviceController;
    private readonly TriggersController TriggersController;
    private readonly Patterns Patterns;

    // Experiments
    private readonly NetworkCapture experiment_networkCapture;

    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ClientState clientState,
        [RequiredVersion("1.0")] GameNetwork gameNetwork,
        [RequiredVersion("1.0")] SigScanner scanner,
        [RequiredVersion("1.0")] ObjectTable gameObjects,
        [RequiredVersion("1.0")] DataManager dataManager
        ) {

      // Init Plugin
      this.PluginInterface = pluginInterface;
      this.CommandManager = commandManager;
      this.GameNetwork = gameNetwork;
      this.ClientState = clientState;
      this.DataManager = dataManager;
      this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      this.Configuration.Initialize(this.PluginInterface);

      this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {
        HelpMessage = "A vibe plugin for fun..."
      });
      if(DalamudChat != null) {
        DalamudChat.ChatMessage += ChatWasTriggered;
      }

      // Initialize the logger
      this.Logger = new Logger(this.DalamudChat, ShortName, Logger.LogLevel.VERBOSE);

      // Migrations
      Migration migration = new(Configuration, Logger);
      migration.Patch_0_2_0_to_1_0_0_config_profile();

      // Configuration Profile
      this.ConfigurationProfile = this.Configuration.GetDefaultProfile();

      // Patterns
      this.Patterns = new Patterns();
      this.Patterns.SetCustomPatterns(this.ConfigurationProfile.PatternList);

      // Initialize the devices Controller
      this.DeviceController = new Device.DevicesController(this.Logger, this.Configuration, this.ConfigurationProfile, this.Patterns);
      if(this.ConfigurationProfile.AUTO_CONNECT) {
        Thread t = new(delegate () {
          Thread.Sleep(2000);
          this.Command_DeviceController_Connect();
        });
        t.Start();
      }

      // Initialize Hook ActionEffect
      this.hook_ActionEffect = new(this.DataManager, this.Logger, scanner, clientState, gameObjects);
      this.hook_ActionEffect.ReceivedEvent += SpellWasTriggered;

      // Init the login event.
      this.ClientState.Login += this.ClientState_LoginEvent;

      // Initialize player stats monitoring.
      this.PlayerStats = new PlayerStats(this.Logger, this.ClientState);
      PlayerStats.Event_CurrentHpChanged += this.PlayerCurrentHPChanged;
      PlayerStats.Event_MaxHpChanged += this.PlayerCurrentHPChanged;

      // Triggers
      this.TriggersController = new Triggers.TriggersController(this.Logger, this.PlayerStats, this.ConfigurationProfile);


      // Experimental
      this.experiment_networkCapture = new NetworkCapture(this.Logger, this.GameNetwork);

      // UI
      this.PluginUi = new PluginUI(this.Logger, this.PluginInterface, this.Configuration, this.ConfigurationProfile, this, this.DeviceController, this.TriggersController, this.Patterns);
      this.PluginInterface.UiBuilder.Draw += DrawUI;
      this.PluginInterface.UiBuilder.OpenConfigUi += DisplayConfigUI;

      // Make sure we set the current profile everywhere.
      this.SetProfile(this.Configuration.CurrentProfileName);
    }


    public void Dispose() {
      this.Logger.Debug("Disposing plugin...");

      // Cleaning device controller.
      if(this.DeviceController != null) {
        this.DeviceController.Dispose();
      }

      // Cleaning chat triggers.
      this.CommandManager.RemoveHandler(commandName);
      if(DalamudChat != null) {
        DalamudChat.ChatMessage -= ChatWasTriggered;
      }

      // Cleaning hooks
      this.hook_ActionEffect.Dispose();

      // Cleaning experimentations
      this.experiment_networkCapture.Dispose();

      this.PluginUi.Dispose();
      this.Logger.Debug("Plugin disposed!");
    }

    private void DrawUI() {

      this.PluginUi.Draw();

      if(this.ClientState.IsLoggedIn) {
        this.PlayerStats.Update(this.ClientState);
      }

      // Trigger first updated method
      if(!this._firstUpdated) {
        this.FirstUpdated();
        this._firstUpdated = true;
      }
    }

    private void FirstUpdated() {
      this.Logger.Debug("First updated");
      if(this.ConfigurationProfile != null && this.ConfigurationProfile.AUTO_OPEN) {
        this.DisplayUI();
      }
    }

    private void DisplayUI() {
      this.PluginUi.Display();
    }

    private void DisplayConfigUI() {
      this.PluginUi.Display();
    }


    public static string GetHelp(string command) {
      string helpMessage = $@"Usage:
      {command} config
      {command} connect
      {command} disconnect
      {command} send <0-100> # Send vibe intensity to all toys
      {command} stop
";
      return helpMessage;
    }

    private void OnCommand(string command, string args) {
      if(args.Length == 0) {
        this.DisplayUI();
      } else {
        if(args.StartsWith("help")) {
          this.Logger.Chat(GetHelp($"/{ShortName}"));
        } else if(args.StartsWith("config")) {
          this.DisplayConfigUI();
        } else if(args.StartsWith("connect")) {
          this.Command_DeviceController_Connect();
        } else if(args.StartsWith("disconnect")) {
          this.Command_DeviceController_Disconnect();
        } else if(args.StartsWith("send")) {
          this.Command_SendIntensity(args);
        } else if(args.StartsWith("stop")) {
          this.DeviceController.SendVibeToAll(0);
        }
        // Experimental
        else if(args.StartsWith("exp_network_start")) {
          this.experiment_networkCapture.StartNetworkCapture();
        } else if(args.StartsWith("exp_network_stop")) {
          this.experiment_networkCapture.StopNetworkCapture();
        } else {
          this.Logger.Chat($"Unknown subcommand: {args}");
        }
      }
    }

    public void Command_DeviceController_Connect() {
      if(this.ConfigurationProfile != null) {
        string host = this.ConfigurationProfile.BUTTPLUG_SERVER_HOST;
        int port = this.ConfigurationProfile.BUTTPLUG_SERVER_PORT;
        this.DeviceController.Connect(host, port);
      }
    }

    private void Command_DeviceController_Disconnect() {
      this.DeviceController.Disconnect();
    }


    private void Command_SendIntensity(string args) {
      string[] blafuckcsharp;
      int intensity;
      try {
        blafuckcsharp = args.Split(" ", 2);
        intensity = int.Parse(blafuckcsharp[1]);
        this.Logger.Chat($"Command Send intensity {intensity}");
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for send [intensity].", e);
        return;
      }
      this.DeviceController.SendVibeToAll(intensity);
    }

    /************************************
    *         LISTEN TO EVENTS          *
    ************************************/

    private void SpellWasTriggered(object? sender, HookActionEffects_ReceivedEventArgs args) {
      if(this.TriggersController == null) {
        this.Logger.Warn("SpellWasTriggered: TriggersController not init yet, ignoring spell...");
        return;
      }

      Structures.Spell spell = args.Spell;
      if(this.ConfigurationProfile != null && this.ConfigurationProfile.VERBOSE_SPELL) {
        this.Logger.Debug($"VERBOSE_SPELL: {spell}");
      }
      List<Trigger>? triggers = this.TriggersController.CheckTrigger_Spell(spell);
      foreach(Trigger trigger in triggers) {
        this.DeviceController.SendTrigger(trigger);
      }
    }

    private void ChatWasTriggered(XivChatType chatType, uint senderId, ref SeString _sender, ref SeString _message, ref bool isHandled) {
      if(this.TriggersController == null) {
        this.Logger.Warn("ChatWasTriggered: TriggersController not init yet, ignoring chat...");
        return;
      }
      string fromPlayerName = _sender.ToString();
      if(this.ConfigurationProfile != null && this.ConfigurationProfile.VERBOSE_CHAT) {
        string XivChatTypeName = ((XivChatType)chatType).ToString();
        this.Logger.Debug($"VERBOSE_CHAT: {fromPlayerName} type={XivChatTypeName}: {_message}");
      }
      List<Trigger> triggers = this.TriggersController.CheckTrigger_Chat(chatType, fromPlayerName, _message.TextValue);
      foreach(Trigger trigger in triggers) {
        this.DeviceController.SendTrigger(trigger);
      }
    }

    public bool SetProfile(string profileName) {
      bool result = this.Configuration.SetCurrentProfile(profileName);
      if(!result) {
        this.Logger.Warn($"You are trying to use profile {profileName} which can't be found");
        return false;
      }
      ConfigurationProfile? configProfileToCheck = this.Configuration.GetProfile(profileName);
      if(configProfileToCheck != null) {
        this.ConfigurationProfile = configProfileToCheck;
        this.PluginUi.SetProfile(this.ConfigurationProfile);
        this.DeviceController.SetProfile(this.ConfigurationProfile);
        this.TriggersController.SetProfile(this.ConfigurationProfile);
      }
      return true;

    }

    private void ClientState_LoginEvent(object? send, EventArgs e) {
      this.PlayerStats.Update(this.ClientState);
    }

      private void PlayerCurrentHPChanged(object? send, EventArgs e) {
      float currentHP = this.PlayerStats.GetCurrentHP();
      float maxHP = this.PlayerStats.GetMaxHP();

      if(this.TriggersController == null) {
        this.Logger.Warn("PlayerCurrentHPChanged: TriggersController not init yet, ignoring HP change...");
        return;
      }

      float percentageHP = currentHP / maxHP * 100f;
      List<Trigger> triggers = TriggersController.CheckTrigger_HPChanged((int)currentHP, (float)percentageHP);
      this.Logger.Debug($"Player HPChanged {currentHP}/{maxHP} {percentageHP}%");
      // Overwrites the threshold for every motors
      foreach(Trigger trigger in triggers) {
        this.DeviceController.SendTrigger(trigger);
      }
    }
  }
}
