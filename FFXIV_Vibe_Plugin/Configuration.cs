using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

using FFXIV_Vibe_Plugin.Triggers;

namespace FFXIV_Vibe_Plugin {
  [Serializable]
  public class Configuration : IPluginConfiguration {

    public int Version { get; set; } = 0; // TODO: remove me ?

    public bool VIBE_HP_TOGGLE { get; set; } = false;
    public int VIBE_HP_MODE { get; set; } = 0;
    public int MAX_VIBE_THRESHOLD { get; set; } = 100;
    public bool AUTO_CONNECT { get; set; } = true;
    public bool AUTO_OPEN { get; set; } = false;

    public string BUTTPLUG_SERVER_HOST { get; set; } = "localhost";
    public int BUTTPLUG_SERVER_PORT { get; set; } = 12345;

    //public List<Triggers.Trigger> TRIGGERS { get; set; } = new();
    public SortedSet<ChatTrigger> CHAT_TRIGGERS { get; set; } = new SortedSet<ChatTrigger>();
    

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
