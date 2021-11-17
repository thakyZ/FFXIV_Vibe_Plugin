using Dalamud.Game.Gui;

namespace FFXIV_BP {
  public class PlayerStats  {
    private ChatGui chatGui;

    public PlayerStats(ChatGui chatGui) {
      this.chatGui = chatGui;
      this.chatGui.Print("PlayerStats");
    }
  }
}
