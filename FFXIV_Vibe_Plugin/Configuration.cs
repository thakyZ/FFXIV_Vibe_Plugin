using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin {
  [Serializable]
  public class Configuration : IPluginConfiguration {

    public int Version { get; set; } = 0; // TODO: remove me ?

    public bool VERBOSE_SPELL = false;
    public bool VERBOSE_CHAT = false;

    public bool VIBE_HP_TOGGLE { get; set; } = false;
    public int VIBE_HP_MODE { get; set; } = 0;
    public int MAX_VIBE_THRESHOLD { get; set; } = 100;
    public bool AUTO_CONNECT { get; set; } = true;
    public bool AUTO_OPEN { get; set; } = false;
    public List<Pattern> PatternList = new();

    public string BUTTPLUG_SERVER_HOST { get; set; } = "127.0.0.1";
    public int BUTTPLUG_SERVER_PORT { get; set; } = 12345;

    public List<Triggers.Trigger> TRIGGERS { get; set; } = new();

    public Dictionary<string, FFXIV_Vibe_Plugin.Device.Device> VISITED_DEVICES = new();


    // the below exist just to make saving less cumbersome

    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface) {
      this.pluginInterface = pluginInterface;
    }

    public void Save() {
      this.pluginInterface!.SavePluginConfig(this);
    }
  }
}
