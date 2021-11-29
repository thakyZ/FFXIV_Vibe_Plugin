using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace FFXIV_BP {
  [Serializable]
  public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public bool VIBE_HP_TOGGLE { get; set; } = false;
    public int VIBE_HP_MODE { get; set; } = 0;
    public int MAX_VIBE_THRESHOLD { get; set; } = 100;
    public bool DEBUG_VERBOSE { get; set; } = true;
    public bool AUTO_CONNECT { get; set; } = true;

    public SortedSet<ChatTrigger> TRIGGERS { get; set; } = new SortedSet<ChatTrigger>();

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
