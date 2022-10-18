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
    private readonly Logger Logger;
    private readonly ActionEffect hook_ActionEffect;
    private readonly PlayerStats PlayerStats;
    private readonly Device.Controller DeviceController;
    private readonly Triggers.Controller TriggersController;
    private readonly Patterns Patterns;
    private readonly Sequencer Sequencer; // TODO: complete me

    // Experiments
    private readonly NetworkCapture experiment_networkCapture;

    // Chat types
    private readonly XivChatType[] allowedChatTypes = {
      XivChatType.Say, XivChatType.Party,
      XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4,
      XivChatType.Ls5, XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8,
      XivChatType.FreeCompany, XivChatType.CrossParty,
      XivChatType.CrossLinkShell1, XivChatType.CrossLinkShell2,
      XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4,
      XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6,
      XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8,
      XivChatType.StandardEmote, XivChatType.CustomEmote
    };

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

      // Initialize player stats monitoring.
      this.PlayerStats = new PlayerStats(this.ClientState);
      PlayerStats.Event_CurrentHpChanged += this.PlayerCurrentHPChanged;
      PlayerStats.Event_MaxHpChanged += this.PlayerCurrentHPChanged;

      // Initialize the devices Controller
      this.DeviceController = new Device.Controller(this.Logger, this.Configuration);
      if(this.Configuration.AUTO_CONNECT) {
        Task.Delay(2000);
        this.Command_DeviceController_Connect();
      }     

      // Initialize Hook ActionEffect
      this.hook_ActionEffect = new(this.DataManager, this.Logger, scanner, clientState, gameObjects);
      this.hook_ActionEffect.ReceivedEvent += SpellWasTriggered;

      // Triggers
      this.TriggersController = new Triggers.Controller(this.Logger, this.PlayerStats);
      this.TriggersController.Set(this.Configuration.TRIGGERS);
      
      // Experimental
      this.experiment_networkCapture = new NetworkCapture(this.Logger, this.GameNetwork);

      // Patterns
      this.Patterns = new Patterns();

      // UI
      this.PluginUi = new PluginUI(this.Logger, this.PluginInterface, this.Configuration, this, this.DeviceController, this.TriggersController, this.Patterns);
      this.PluginInterface.UiBuilder.Draw += DrawUI;
      this.PluginInterface.UiBuilder.OpenConfigUi += DisplayConfigUI;
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

      this.PlayerStats.Update();

      // TODO: this.RunSequencer(this.sequencerTasks);

      // Trigger first updated method
      if(!this._firstUpdated) {
        this.FirstUpdated();
        this._firstUpdated = true;
      }
    }

    private void FirstUpdated() {
      this.Logger.Debug("First updated");
      if(this.Configuration.AUTO_OPEN) {
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
        } else if(args.StartsWith("play_pattern")) {
          this.Play_pattern(args);
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
      string host = this.Configuration.BUTTPLUG_SERVER_HOST;
      int port = this.Configuration.BUTTPLUG_SERVER_PORT;
      this.DeviceController.Connect(host, port);
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
      if(this.Configuration.VERBOSE_SPELL) {
        this.Logger.Debug($"{spell}");
      }
      List<Trigger>? triggers = this.TriggersController.CheckTrigger_Spell(spell);
      foreach(Trigger trigger in triggers) {
        if(trigger.StartAfter > 0 || trigger.StopAfter > 0) {
          this.DeviceController.AddTriggerTask(trigger);
        } else {
          this.DeviceController.SendTrigger(trigger);
        }
      }
    }

    private void ChatWasTriggered(XivChatType type, uint senderId, ref SeString _sender, ref SeString _message, ref bool isHandled) {
      if(allowedChatTypes == null) {
        this.Logger.Warn("ChatWasTriggered: Chat hook not ready, ignoring chat trigger");
        return;
      }
      if(this.TriggersController == null) {
        this.Logger.Warn("ChatWasTriggered: TriggersController not init yet, ignoring chat...");
        return;
      }
      string fromPlayerName = _sender.ToString();

      if(!allowedChatTypes.Any(ct => ct == type)) {
        return;
      }

      List<Trigger> triggers = this.TriggersController.CheckTrigger_Chat(fromPlayerName, _message.TextValue);
      foreach(Trigger trigger in triggers) {
        this.DeviceController.SendTrigger(trigger);
      }
    }


    /**************************/
    /** LEGACY CODE IS BELLOW */
    /**************************/

    private void Play_pattern(string args) {
      this.Logger.Warn("Play_pattern is disabled temporary");
      return;
      try {
        string[] param = args.Split(" ", 2);
        string patternName = param[1];
        this.Logger.Chat($"Play pattern {patternName}");
        if(patternName == "shake") {
          this.DeviceController.Play_PatternShake(100);
        } else if(patternName == "mountain") {
          this.DeviceController.Play_PatternMountain(30);
        }
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for play_pattern [pattern_name] # shake, mountain", e);
        return;
      }
    }

    private void PlayerCurrentHPChanged(object? send, EventArgs e) {
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
          this.DeviceController.SendVibeToAll((int)percentage);
        } else if(mode == 1) { // shake
          this.DeviceController.Play_PatternShake(percentage);
        } else if(mode == 2) { // mountain
          this.DeviceController.Play_PatternMountain(percentage);
        }
      }
    }


    

    
  }
}
