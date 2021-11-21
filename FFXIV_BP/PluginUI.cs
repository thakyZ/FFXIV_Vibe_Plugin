using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Plugin;

namespace FFXIV_BP {
  // It is good to have this be disposable in general, in case you ever need it
  // to do any cleanup
  class PluginUI : IDisposable {
    private Configuration configuration;

    private ImGuiScene.TextureWrap goatImage;

    private DalamudPluginInterface PluginInterface;

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible {
      get { return this.visible; }
      set { this.visible = value; }
    }

    private bool settingsVisible = false;
    public bool SettingsVisible {
      get { return this.settingsVisible; }
      set { this.settingsVisible = value; }
    }

    // passing in the image here just for simplicity
    public PluginUI(Configuration configuration, DalamudPluginInterface pluginInterface ) {
      this.configuration = configuration;
      this.PluginInterface = pluginInterface;
      
      
      var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
      var imagePath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "icon.png") ;
      ImGuiScene.TextureWrap goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
      this.goatImage = goatImage;
    }

    public void Dispose() {
      if(this.goatImage != null) this.goatImage.Dispose();
    }

    public void Draw() {
      // This is our only draw handler attached to UIBuilder, so it needs to be
      // able to draw any windows we might have open.
      // Each method checks its own visibility/state to ensure it only draws when
      // it actually makes sense.
      // There are other ways to do this, but it is generally best to keep the number of
      // draw delegates as low as possible.

      DrawMainWindow();
      DrawSettingsWindow();
    }

    public void DrawMainWindow() {
      if(!Visible) {
        return;
      }

      ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
      ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
      if(ImGui.Begin("FFXIV_BP Panel", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {

        ImGui.Spacing();

        ImGui.Indent(120);
        ImGui.Image(this.goatImage.ImGuiHandle, new Vector2(this.goatImage.Width*0.2f, this.goatImage.Height*0.2f));
        ImGui.Unindent(120);

        ImGui.Text("Edit configuration");
        if(ImGui.Button("Show")) {
          SettingsVisible = true;
        }
      }

      ImGui.End();
    }

    public void DrawSettingsWindow() {
      if(!SettingsVisible) {
        return;
      }

      ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
      if(ImGui.Begin("FFXIV_BP Configuration", ref this.settingsVisible,
          ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {

        // Checkbox
        bool config_HP_TOGGLE = this.configuration.HP_TOGGLE;
        if(ImGui.Checkbox("Trigger vibes based on HP: ", ref config_HP_TOGGLE)) {
          this.configuration.HP_TOGGLE = config_HP_TOGGLE;
          this.configuration.Save();
        }
      }
      ImGui.End();
    }
  }
}
