using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Plugin;
using System.Collections.Generic;

namespace FFXIV_BP {
  
  class PluginUI : IDisposable {
    
    private DalamudPluginInterface PluginInterface;
    private Configuration configuration;

    // Images
    private Dictionary<string, ImGuiScene.TextureWrap> loadedImages = new Dictionary<string, ImGuiScene.TextureWrap>();

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

    private Plugin currentPlugin;

    // passing in the image here just for simplicity
    public PluginUI(Configuration configuration, DalamudPluginInterface pluginInterface, Plugin currentPlugin ) {
      this.configuration = configuration;
      this.PluginInterface = pluginInterface;
      this.currentPlugin = currentPlugin;
     
      this.loadImages();
    }

    /**
     * Function that will load all the images so that they are usable.
     * Don't forget to add the image into the project file.
     */
    private void loadImages() {      
      List<string> images = new List<string>();
      images.Add("logo.png");

      string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
      foreach(string img in images) {
        string imagePath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, img);
        this.loadedImages.Add(img, this.PluginInterface.UiBuilder.LoadImage(imagePath));
      }
    }

    public void Dispose() {
      // Dispose all loaded images.
      foreach(KeyValuePair<string, ImGuiScene.TextureWrap> img in this.loadedImages) {
        if(img.Value != null) img.Value.Dispose();
      }
      
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
      ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.Appearing);
      ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.Appearing);
      ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
      if(ImGui.Begin("FFXIV_BP Panel", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {

        ImGui.Spacing();

        ImGui.Indent(120);
        ImGuiScene.TextureWrap imgLogo = this.loadedImages["logo.png"];
        ImGui.Image(imgLogo.ImGuiHandle, new Vector2(imgLogo.Width*0.2f, imgLogo.Height*0.2f));
        ImGui.Unindent(120);

        if(ImGui.Button("Connect")) {
          this.currentPlugin.Print("TODO: connect");
        }

        if(ImGui.Button("Edit configuration")) {
          SettingsVisible = true;
        }
      }

      ImGui.End();
    }

    public void DrawSettingsWindow() {
      if(!SettingsVisible) {
        return;
      }

      ImGui.SetNextWindowPos(new Vector2(500, 100), ImGuiCond.Appearing);
      ImGui.SetNextWindowSize(new Vector2(500, 300), ImGuiCond.Always);
      if(ImGui.Begin("FFXIV_BP Configuration", ref this.settingsVisible,
          ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {

        // Checkbox DEBUG_VERBOSE
        bool config_DEBUG_VERBOSE = this.configuration.DEBUG_VERBOSE;
        if(ImGui.Checkbox("Verbose mode to display debug messages. ", ref config_DEBUG_VERBOSE)) {
          this.configuration.DEBUG_VERBOSE = config_DEBUG_VERBOSE;
          this.configuration.Save();
        }

        // Checkbox VIBE_HP_TOGGLE
        bool config_VIBE_HP_TOGGLE = this.configuration.VIBE_HP_TOGGLE;
        if(ImGui.Checkbox("The less HP you have, the more vibes you take.", ref config_VIBE_HP_TOGGLE)) {
          this.configuration.VIBE_HP_TOGGLE = config_VIBE_HP_TOGGLE;
          this.configuration.Save();
        }

        // Checkbox MAX_VIBE_THRESHOLD
        int config_MAX_VIBE_THRESHOLD = this.configuration.MAX_VIBE_THRESHOLD ;
        ImGui.SetNextItemWidth(100);
        if(ImGui.InputInt("Maximum vibration threshold", ref config_MAX_VIBE_THRESHOLD)) {
          this.configuration.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
          this.configuration.Save();
        }
      }
      ImGui.End();
    }
  }
}
