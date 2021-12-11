using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Plugin;
using System.Collections.Generic;
using static FFXIV_Vibe_Plugin.Plugin;
using System.Threading;

namespace FFXIV_Vibe_Plugin {

  class PluginUI : IDisposable {

    private readonly DalamudPluginInterface PluginInterface;
    private readonly Configuration Configuration;
    private readonly Device.Controller DeviceController;
    private readonly Plugin CurrentPlugin;

    // Images
    private readonly Dictionary<string, ImGuiScene.TextureWrap> loadedImages = new();

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible {
      get { return this.visible; }
      set { this.visible = value; }
    }

    private readonly int WIDTH = 400;
    private readonly int HEIGHT = 500;

    // The value to send as a test for vibes.
    private int test_sendVibeValue = 0;

    

    /** Constructor */
    public PluginUI(
      DalamudPluginInterface pluginInterface,
      Configuration configuration,       
      Plugin currentPlugin,
      Device.Controller deviceController
    ) {
      this.Configuration = configuration;
      this.PluginInterface = pluginInterface;
      this.CurrentPlugin = currentPlugin;
      this.DeviceController = deviceController;
      this.LoadImages();
    }

    /**
     * Function that will load all the images so that they are usable.
     * Don't forget to add the image into the project file.
     */
    private void LoadImages() {
      List<string> images = new();
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
      ImGui.SetNextWindowSize(new Vector2(this.WIDTH, this.HEIGHT), ImGuiCond.Appearing);
      ImGui.SetNextWindowSizeConstraints(new Vector2(this.WIDTH, this.HEIGHT), new Vector2(float.MaxValue, float.MaxValue));
      if(ImGui.Begin("FFXIV Vibe Plugin", ref this.visible, ImGuiWindowFlags.None)) {

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
          if(this.DeviceController.IsConnected()) {
            if(ImGui.BeginTabItem("Simulator")) {
              this.DrawSimulatorTab();
              ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Devices")) {
              this.DrawDevicesTab();
              ImGui.EndTabItem();
            }
          }
        
          if(ImGui.BeginTabItem("Help")) {
            this.DrawHelpTab();
            ImGui.EndTabItem();
          }
        }
      }

      ImGui.End();
    }

    public void DrawSettingsTab() {
      
      ImGui.Spacing();
      
      // Connect/disconnect button
      ImGui.Columns(3);
      string config_BUTTPLUG_SERVER_HOST = this.Configuration.BUTTPLUG_SERVER_HOST;
      ImGui.SetNextItemWidth(120);
      if(ImGui.InputText("##serverHost", ref config_BUTTPLUG_SERVER_HOST, 99)) {
        this.Configuration.BUTTPLUG_SERVER_HOST = config_BUTTPLUG_SERVER_HOST.Trim().ToLower();
        this.Configuration.Save();
      }

      ImGui.NextColumn();
      int config_BUTTPLUG_SERVER_PORT = this.Configuration.BUTTPLUG_SERVER_PORT;
      ImGui.SetNextItemWidth(120);
      if(ImGui.InputInt("##serverPort", ref config_BUTTPLUG_SERVER_PORT, 10)) {
        this.Configuration.BUTTPLUG_SERVER_PORT = config_BUTTPLUG_SERVER_PORT;
        this.Configuration.Save();
      }

      ImGui.NextColumn();
      if(!this.DeviceController.IsConnected()) {
        if(ImGui.Button("Connect", new Vector2(100, 24))) {
          this.CurrentPlugin.Command_DeviceController_Connect();
        }
      } else {
        if(ImGui.Button("Disconnect", new Vector2(100, 24))) {
          this.DeviceController.Disconnect();
        }
      }

      ImGui.Columns(1);
      ImGui.Spacing();

      // Checkbox AUTO_CONNECT
      bool config_AUTO_CONNECT = this.Configuration.AUTO_CONNECT;
      if(ImGui.Checkbox("Automatically connects. ", ref config_AUTO_CONNECT)) {
        this.Configuration.AUTO_CONNECT = config_AUTO_CONNECT;
        this.Configuration.Save();
      }

      // Shortcut and hide next options
      if(!this.DeviceController.IsConnected()) { return; }

      // Checkbox VIBE_HP_TOGGLE
      bool config_VIBE_HP_TOGGLE = this.Configuration.VIBE_HP_TOGGLE;
      if(ImGui.Checkbox("Vibe on HP change.", ref config_VIBE_HP_TOGGLE)) {
        this.Configuration.VIBE_HP_TOGGLE = config_VIBE_HP_TOGGLE;
        this.Configuration.Save();
      }

      // Checkbox VIBE_HP_TOGGLE
      int config_VIBE_HP_MODE = this.Configuration.VIBE_HP_MODE;
      ImGui.SetNextItemWidth(200);
      string[] VIBE_HP_MODES = new string[] { "normal", "shake", "mountain" };
      if(ImGui.Combo("Vibe mode.", ref config_VIBE_HP_MODE, VIBE_HP_MODES, VIBE_HP_MODES.Length)) {
        this.Configuration.VIBE_HP_MODE = config_VIBE_HP_MODE;
        this.Configuration.Save();
      }

      // Checkbox MAX_VIBE_THRESHOLD
      int config_MAX_VIBE_THRESHOLD = this.Configuration.MAX_VIBE_THRESHOLD;
      ImGui.SetNextItemWidth(200);
      if(ImGui.SliderInt("Maximum vibration threshold", ref config_MAX_VIBE_THRESHOLD, 0, 100)) {
        this.Configuration.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
        this.Configuration.Save();
      }
    }

    public void DrawSimulatorTab() {
      if(!this.DeviceController.IsConnected()) { return;  }

      if(ImGui.Button("Scan toys", new Vector2(100, 24))) {
        this.DeviceController.ScanToys();
      }

      ImGui.Text("Send to all:");

      // Test of the vibe
      ImGui.SetNextItemWidth(200);
      if(ImGui.SliderInt("Intensity", ref this.test_sendVibeValue, 0, 100)) {
        this.DeviceController.SendVibe(this.test_sendVibeValue);
      }
      if(ImGui.Button("Stop vibe", new Vector2(100, 24))) {
        this.test_sendVibeValue = 0;
      }
      ImGui.Columns(1);
    }

    public void DrawDevicesTab() {
      if(!this.DeviceController.IsConnected()) { return; }
      /* TODO: 
      foreach(ButtplugDevice device in this.DeviceController.ButtplugDevices) {
        string deviceEntry = $"{device.Id}:{device.Name}";
        ImGui.Text(deviceEntry);
      }*/
    }

    public void DrawHelpTab() {
      string help = Plugin.GetHelp(this.CurrentPlugin.commandName);
      ImGui.Text(help);
      
    }
  }
}
