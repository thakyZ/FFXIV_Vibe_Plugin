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

    // The value to send as a test for vibes.
    private int test_sendVibeValue = 0;

    private Plugin currentPlugin;

    // passing in the image here just for simplicity
    public PluginUI(Configuration configuration, DalamudPluginInterface pluginInterface, Plugin currentPlugin) {
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
        string imagePath = Path.Combine(Path.GetDirectoryName(assemblyLocation)!, $"Data\\Images\\{img}");
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

    }

    public void DrawMainWindow() {
      if(!Visible) {
        return;
      }
      ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.Appearing);
      ImGui.SetNextWindowSize(new Vector2(400, 500), ImGuiCond.Appearing);
      ImGui.SetNextWindowSizeConstraints(new Vector2(400, 500), new Vector2(float.MaxValue, float.MaxValue));
      if(ImGui.Begin("FFXIV_BP Panel", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {

        ImGui.Spacing();

        ImGui.Indent(120);
        ImGuiScene.TextureWrap imgLogo = this.loadedImages["logo.png"];
        ImGui.Image(imgLogo.ImGuiHandle, new Vector2(imgLogo.Width * 0.2f, imgLogo.Height * 0.2f));
        ImGui.Unindent(120);




        // Experimental
        if(ImGui.BeginTabBar("##ConfigTabBar", ImGuiTabBarFlags.None)) {
          if(ImGui.BeginTabItem("Settings")) {
            this.DrawSettingsTab();
            ImGui.EndTabItem();
          }
          if(ImGui.BeginTabItem("Simulator")) {
            this.DrawExperimentalUi();
            ImGui.EndTabItem();
          }
          if(ImGui.BeginTabItem("Help")) {
            this.DrawExperimentalUi();
            ImGui.EndTabItem();
          }
        }

      }

      ImGui.End();
    }

    public void DrawSettingsTab() {
      // Connect/disconnect button
      if(!this.currentPlugin.buttplugIsConnected()) {
        if(ImGui.Button("Connect", new Vector2(100, 24))) {
          this.currentPlugin.Command_ConnectButtplugs("");
        }
      } else {
        if(ImGui.Button("Disconnect", new Vector2(100, 24))) {
          this.currentPlugin.DisconnectButtplugs();
        }
      }

      // Checkbox DEBUG_VERBOSE
      bool config_DEBUG_VERBOSE = this.configuration.DEBUG_VERBOSE;
      if(ImGui.Checkbox("Verbose mode to display debug messages. ", ref config_DEBUG_VERBOSE)) {
        this.configuration.DEBUG_VERBOSE = config_DEBUG_VERBOSE;
        this.configuration.Save();
      }

      // Checkbox DEBUG_VERBOSE
      bool config_AUTO_CONNECT = this.configuration.AUTO_CONNECT;
      if(ImGui.Checkbox("Automatically connects. ", ref config_AUTO_CONNECT)) {
        this.configuration.AUTO_CONNECT = config_AUTO_CONNECT;
        this.configuration.Save();
      }

      // Checkbox VIBE_HP_TOGGLE
      bool config_VIBE_HP_TOGGLE = this.configuration.VIBE_HP_TOGGLE;
      if(ImGui.Checkbox("Vibe on HP change.", ref config_VIBE_HP_TOGGLE)) {
        this.configuration.VIBE_HP_TOGGLE = config_VIBE_HP_TOGGLE;
        this.configuration.Save();
      }

      // Checkbox VIBE_HP_TOGGLE
      int config_VIBE_HP_MODE = this.configuration.VIBE_HP_MODE;
      ImGui.SetNextItemWidth(200);
      string[] VIBE_HP_MODES = new string[] { "normal", "shake", "mountain" };
      if(ImGui.Combo("Vibe mode.", ref config_VIBE_HP_MODE, VIBE_HP_MODES, VIBE_HP_MODES.Length)) {
        this.configuration.VIBE_HP_MODE = config_VIBE_HP_MODE;
        this.configuration.Save();
      }

      // Checkbox MAX_VIBE_THRESHOLD
      int config_MAX_VIBE_THRESHOLD = this.configuration.MAX_VIBE_THRESHOLD;
      ImGui.SetNextItemWidth(200);
      if(ImGui.SliderInt("Maximum vibration threshold", ref config_MAX_VIBE_THRESHOLD, 0, 100)) {
        this.configuration.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
        this.configuration.Save();
      }
    }

    public void DrawExperimentalUi() {


      // Test of the vibe
      ImGui.SetNextItemWidth(200);
      if(ImGui.SliderInt("Intensity", ref this.test_sendVibeValue, 0, 100)) { }
      ImGui.Columns(2, "##SendVibeTest", false);
      ImGui.SetColumnWidth(0, 110);


      if(ImGui.Button("Send vibe", new Vector2(100, 24))) {
        this.currentPlugin.buttplug_sendVibe(this.test_sendVibeValue);
      }
      ImGui.NextColumn();
      if(ImGui.Button("Stop vibe", new Vector2(100, 24))) {
        this.currentPlugin.buttplug_sendVibe(0);
      }
      ImGui.Columns(1);
    }
  }
}
