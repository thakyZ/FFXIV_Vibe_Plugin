using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using System.Collections.Generic;

namespace FFXIV_Vibe_Plugin {

  class PluginUI : IDisposable {

    private readonly DalamudPluginInterface PluginInterface;
    private readonly Configuration Configuration;
    private readonly Device.Controller DeviceController;
    private readonly Triggers.Controller TriggerController;
    private readonly Plugin CurrentPlugin;

    // Images
    private readonly Dictionary<string, ImGuiScene.TextureWrap> loadedImages = new();

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible {
      get { return this.visible; }
      set { this.visible = value; }
    }

    private readonly int WIDTH = 650;
    private readonly int HEIGHT = 700;

    // The value to send as a test for vibes.
    private int simulator_currentAllIntensity = 0;

    // Trigger
    private Triggers.Trigger? SelectedTrigger = null;
    private string triggersViewMode = "default"; // default|edit|delete;

    /** Constructor */
    public PluginUI(
      DalamudPluginInterface pluginInterface,
      Configuration configuration,
      Plugin currentPlugin,
      Device.Controller deviceController,
      Triggers.Controller triggersController
    ) {
      this.Configuration = configuration;
      this.PluginInterface = pluginInterface;
      this.CurrentPlugin = currentPlugin;
      this.DeviceController = deviceController;
      this.TriggerController = triggersController;
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



        ImGuiScene.TextureWrap imgLogo = this.loadedImages["logo.png"];
        ImGui.Columns(2, "###main_header", false);
        float logoScale = 0.2f;
        ImGui.SetColumnWidth(0, (int)(imgLogo.Width * logoScale+20));
        ImGui.Image(imgLogo.ImGuiHandle, new Vector2(imgLogo.Width * logoScale, imgLogo.Height * logoScale));
        ImGui.NextColumn();
        if(this.DeviceController.IsConnected()) {
          int nbrDevices = this.DeviceController.GetDevices().Count;
          ImGui.TextColored(ImGuiColors.ParsedGreen, "Your are connected!");
          ImGui.Text($"Number of device(s): {nbrDevices}");
        } else {
          ImGui.TextColored(ImGuiColors.ParsedGrey, "Your are not connected!");
        }
        ImGui.Columns(1);



        // Experimental
        if(ImGui.BeginTabBar("##ConfigTabBar", ImGuiTabBarFlags.None)) {
          if(ImGui.BeginTabItem("Connect")) {
            this.DrawConnectTab();
            ImGui.EndTabItem();
          }

          if(this.DeviceController.IsConnected()) {
            if(ImGui.BeginTabItem("Options")) {
              this.DrawOptionsTab();
              ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Devices")) {
              this.DrawDevicesTab();
              ImGui.EndTabItem();
            }
            if(ImGui.BeginTabItem("Triggers")) {
              this.DrawTriggersTab();
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

    public void DrawConnectTab() {
      ImGui.Spacing();
      ImGui.TextColored(ImGuiColors.DalamudViolet, "Server address & port");
      ImGui.BeginChild("###Server", new Vector2(-1, 40f), true);
      {

        // Connect/disconnect button
        string config_BUTTPLUG_SERVER_HOST = this.Configuration.BUTTPLUG_SERVER_HOST;
        ImGui.SetNextItemWidth(200);
        if(ImGui.InputText("##serverHost", ref config_BUTTPLUG_SERVER_HOST, 99)) {
          this.Configuration.BUTTPLUG_SERVER_HOST = config_BUTTPLUG_SERVER_HOST.Trim().ToLower();
          this.Configuration.Save();
        }

        ImGui.SameLine();
        int config_BUTTPLUG_SERVER_PORT = this.Configuration.BUTTPLUG_SERVER_PORT;
        ImGui.SetNextItemWidth(100);
        if(ImGui.InputInt("##serverPort", ref config_BUTTPLUG_SERVER_PORT, 10)) {
          this.Configuration.BUTTPLUG_SERVER_PORT = config_BUTTPLUG_SERVER_PORT;
          this.Configuration.Save();
        }
      }
      ImGui.EndChild();

      ImGui.Spacing();
      ImGui.BeginChild("###Main_Connection", new Vector2(-1, 40f), true);
      {
        if(!this.DeviceController.IsConnected()) {
          if(ImGui.Button("Connect", new Vector2(100, 24))) {
            this.CurrentPlugin.Command_DeviceController_Connect();
          }
        } else {
          if(ImGui.Button("Disconnect", new Vector2(100, 24))) {
            this.DeviceController.Disconnect();
          }
        }

        // Checkbox AUTO_CONNECT
        ImGui.SameLine();
        bool config_AUTO_CONNECT = this.Configuration.AUTO_CONNECT;
        if(ImGui.Checkbox("Automatically connects. ", ref config_AUTO_CONNECT)) {
          this.Configuration.AUTO_CONNECT = config_AUTO_CONNECT;
          this.Configuration.Save();
        }
      }
      ImGui.EndChild();

      ImGui.TextColored(ImGuiColors.DalamudViolet, "Others");
      ImGui.BeginChild("###Main_Others", new Vector2(-1, 40f), true);
      {
        // Checkbox AUTO_OPEN
        bool config_AUTO_OPEN = this.Configuration.AUTO_OPEN;
        if(ImGui.Checkbox("Automatically open configuration panel. ", ref config_AUTO_OPEN)) {
          this.Configuration.AUTO_OPEN = config_AUTO_OPEN;
          this.Configuration.Save();
        }
      }
      ImGui.EndChild();
    }

    public void DrawOptionsTab() {
      ImGui.Spacing();

      // Checkbox VIBE_HP_TOGGLE
      bool config_VIBE_HP_TOGGLE = this.Configuration.VIBE_HP_TOGGLE;
      if(ImGui.Checkbox("Vibe on HP change.", ref config_VIBE_HP_TOGGLE)) {
        this.Configuration.VIBE_HP_TOGGLE = config_VIBE_HP_TOGGLE;
        this.Configuration.Save();
      }

      // Checkbox VIBE_HP_TOGGLE
      int config_VIBE_HP_MODE = this.Configuration.VIBE_HP_MODE;
      ImGui.SetNextItemWidth(200);
      string[] VIBE_HP_MODES = new string[] { "intensity", "shake", "mountain" };
      if(ImGui.Combo("###HP_Changed_VibeMode", ref config_VIBE_HP_MODE, VIBE_HP_MODES, VIBE_HP_MODES.Length)) {
        this.Configuration.VIBE_HP_MODE = config_VIBE_HP_MODE;
        this.Configuration.Save();
      }
      ImGuiComponents.HelpMarker("Pattern to play when HP Change.");

      // Checkbox MAX_VIBE_THRESHOLD
      int config_MAX_VIBE_THRESHOLD = this.Configuration.MAX_VIBE_THRESHOLD;
      ImGui.SetNextItemWidth(200);
      if(ImGui.SliderInt("###MaximumThreshold", ref config_MAX_VIBE_THRESHOLD, 5, 100)) {
        this.Configuration.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
        this.Configuration.Save();
      }
      ImGuiComponents.HelpMarker("Maximum threshold for vibes.");
    }

    public void DrawDevicesTab() {
      ImGui.Spacing();

      ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
      ImGui.BeginChild("###DevicesTab_General", new Vector2(-1, 40f), true);
      {
        if(ImGui.Button("Scan toys", new Vector2(100, 24))) {
          this.DeviceController.ScanToys();
        }

        ImGui.SameLine();
        if(ImGui.Button("Update Battery", new Vector2(100, 24))) {
          this.DeviceController.UpdateAllBatteryLevel();
        }
        ImGui.SameLine();
        if(ImGui.Button("Stop All", new Vector2(100, 24))) {
          this.DeviceController.StopAll();
          this.simulator_currentAllIntensity = 0;
        }
      }
      ImGui.EndChild();

      if(ImGui.CollapsingHeader($"All devices")) {
        ImGui.Text("Send to all:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if(ImGui.SliderInt("###SendVibeAll_Intensity", ref this.simulator_currentAllIntensity, 0, 100)) {
          this.DeviceController.SendVibeToAll(this.simulator_currentAllIntensity);
        }
      }

      foreach(Device.Device device in this.DeviceController.GetDevices()) {
        if(ImGui.CollapsingHeader($"{device.Id} {device.Name} - Battery: {device.GetBatteryPercentage()}")) {
          ImGui.TextWrapped(device.ToString());
          if(device.CanVibrate) {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "VIBRATE");
            ImGui.Indent(10);
            for(int i = 0; i < device.VibrateMotors; i++) {
              ImGui.Text($"Motor {i + 1}: ");
              ImGui.SameLine();
              ImGui.SetNextItemWidth(200);
              if(ImGui.SliderInt($"###{device.Id} Intensity Vibrate Motor {i}", ref device.CurrentVibrateIntensity[i], 0, 100)) {
                this.DeviceController.SendVibrate(device, device.CurrentVibrateIntensity[i], i);
              }
            }
            ImGui.Unindent(10);
          }

          if(device.CanRotate) {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "ROTATE");
            ImGui.Indent(10);
            for(int i = 0; i < device.RotateMotors; i++) {
              ImGui.Text($"Motor {i + 1}: ");
              ImGui.SameLine();
              ImGui.SetNextItemWidth(200);
              if(ImGui.SliderInt($"###{device.Id} Intensity Rotate Motor {i}", ref device.CurrentRotateIntensity[i], 0, 100)) {
                this.DeviceController.SendRotate(device, device.CurrentRotateIntensity[i], true, i);
              }
            }
            ImGui.Unindent(10);
          }

          if(device.CanLinear) {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "LINEAR VIBES");
            ImGui.Indent(10);
            for(int i = 0; i < device.LinearMotors; i++) {
              ImGui.Text($"Motor {i + 1}: ");
              ImGui.SameLine();
              ImGui.SetNextItemWidth(200);
              if(ImGui.SliderInt($"###{device.Id} Intensity Linear Motor {i}", ref device.CurrentLinearIntensity[i], 0, 100)) {
                this.DeviceController.SendLinear(device, device.CurrentLinearIntensity[i], 500, i);
              }
            }
            ImGui.Unindent(10);
          }
        }

      }
    }

    public void DrawTriggersTab() {
      List<Triggers.Trigger> triggers = this.TriggerController.GetTriggers();
      string selectedId = this.SelectedTrigger != null ? this.SelectedTrigger.Id : "";
      if(ImGui.BeginChild("###TriggersSelector", new Vector2(200, -ImGui.GetFrameHeightWithSpacing()), true)) {
        ImGui.Text($"--- Number of triggers {triggers.Count} ---");
        foreach(Triggers.Trigger trigger in triggers) {
          string enabled = trigger.Enabled ? "" : "[disabled]";
          if(ImGui.Selectable($"{enabled}{trigger.Name}{new String(' ', 100)}{trigger.Id}", selectedId == trigger.Id)) { // We don't want to show the ID
            this.SelectedTrigger = trigger;
            this.triggersViewMode = "edit";
          }
        }
        ImGui.EndChild();
      }

      ImGui.SameLine();
      if(ImGui.BeginChild("###TriggerViewerPanel", new Vector2(0, -ImGui.GetFrameHeightWithSpacing()), true)) {
        if(this.triggersViewMode == "default") {
          ImGui.Text("Please select or add a trigger");
        } else if(this.triggersViewMode == "edit") {
          if(this.SelectedTrigger != null) {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Work in progress");

            ImGui.Columns(2, "#TRIGGER_FORM", false);
            ImGui.SetColumnWidth(0, 100);

            // Displaying the trigger ID
            ImGui.Text($"TriggerID:");
            ImGui.NextColumn();
            ImGui.Text($"{this.SelectedTrigger.GetShortID()}");
            ImGui.NextColumn();

            // TRIGGER ENABLED
            ImGui.Text("Enabled:");
            ImGui.NextColumn();
            if(ImGui.Checkbox("###TRIGGER_ENABLED", ref this.SelectedTrigger.Enabled)){
              this.Configuration.Save();
            };
            ImGui.NextColumn();

            // TRIGGER NAME
            ImGui.Text("Trigger Name:");
            ImGui.NextColumn();
            if(ImGui.InputText("###TRIGGER_NAME", ref this.SelectedTrigger.Name, 99)) {
              if(this.SelectedTrigger.Name == "") {
                this.SelectedTrigger.Name = "no_name";
              }
              this.Configuration.Save();
            };
            ImGui.NextColumn();

            // TRIGGER KIND
            ImGui.Text("Kind:");
            ImGui.NextColumn();
            string[] TRIGGER_KIND = System.Enum.GetNames( typeof(Triggers.KIND));
            int currentKind = (int)this.SelectedTrigger.Kind;
            if(ImGui.Combo("###TRIGGER_FORM_KIND", ref currentKind, TRIGGER_KIND, TRIGGER_KIND.Length)) {
              this.SelectedTrigger.Kind = currentKind;
              this.Configuration.Save();
            }
            ImGui.NextColumn();


            // TRIGGER KIND:CHAT OPTIONS
            if(this.SelectedTrigger.Kind == (int)Triggers.KIND.Chat) {
              ImGui.Text("Chat text:");
              ImGui.NextColumn();
              string currentChatText = this.SelectedTrigger.ChatText;
              if(ImGui.InputText("###TRIGGER_CHAT_TEXT", ref currentChatText, 250)) {
                this.SelectedTrigger.ChatText = currentChatText.ToLower(); // ChatMsg is always lower
                this.Configuration.Save();
              };
              ImGui.NextColumn();

              ImGui.Text("Intensity:");
              ImGui.NextColumn();
              if(ImGui.SliderInt("###TRIGGER_CHAT_INTENSITY", ref this.SelectedTrigger.Intensity, 0, 100)) {
                this.Configuration.Save();
              };
              ImGui.NextColumn();
            }

            // TRIGGER KIND:SPELL OPTIONS
            if(this.SelectedTrigger.Kind == (int)Triggers.KIND.Spell) {

              // TRIGGER EVENT
              ImGui.Text("Trigger:");
              ImGui.NextColumn();
              string[] TRIGGER = System.Enum.GetNames(typeof(Triggers.TRIGGER));
              int currentEvent = (int)this.SelectedTrigger.Event;
              if(ImGui.Combo("###TRIGGER_FORM_TRIGGER", ref currentEvent, TRIGGER, TRIGGER.Length)) {
                this.SelectedTrigger.Event = currentEvent;
                this.Configuration.Save();
              }
              ImGui.NextColumn();

              //TRIGGER DIRECTION
              ImGui.Text("Direction:");
              ImGui.NextColumn();
              string[] DIRECTIONS = System.Enum.GetNames(typeof(Triggers.DIRECTION));
              int currentDirection = (int)this.SelectedTrigger.Direction;
              if(ImGui.Combo("###TRIGGER_FORM_DIRECTION", ref currentDirection, DIRECTIONS, DIRECTIONS.Length)) {
                this.SelectedTrigger.Direction = currentDirection;
                this.Configuration.Save();
              }
              ImGui.NextColumn();

              //TRIGGER DIRECTION
              ImGui.Text("Spell Text:");
              ImGui.NextColumn();
              string SPELL_TEXT = this.SelectedTrigger.SpellText;
              if(ImGui.InputText("###TRIGGER_FORM_SPELLNAME", ref SPELL_TEXT, 100)){
                this.SelectedTrigger.SpellText = SPELL_TEXT;
                this.Configuration.Save();
              }
              ImGui.NextColumn();

                            ImGui.Text("Intensity:");
              ImGui.NextColumn();
              if(ImGui.SliderInt("###TRIGGER_CHAT_INTENSITY", ref this.SelectedTrigger.Intensity, 0, 100)) {
                this.Configuration.Save();
              };
              ImGui.NextColumn();
            }
          }
        } else if(this.triggersViewMode == "delete") {
          ImGui.TextColored(ImGuiColors.DalamudRed, $"Are you sure you want to delete trigger ID: {this.SelectedTrigger.Id}");
          if(ImGui.Button("Yes")) {
            if(this.SelectedTrigger != null) {
              this.TriggerController.RemoveTrigger(this.SelectedTrigger);
              this.SelectedTrigger = null;
              this.Configuration.Save();
            }
            this.triggersViewMode = "default";
          };
          ImGui.SameLine();
          if(ImGui.Button("No")) {
            
            this.SelectedTrigger = null;
            this.triggersViewMode = "default";
          };

        }
        ImGui.EndChild();
      }

      if(ImGui.Button("Add")) {
        Triggers.Trigger trigger = new("New Trigger");
        this.TriggerController.AddTrigger(trigger);
        this.SelectedTrigger = trigger;
        this.triggersViewMode = "edit";
      };
      ImGui.SameLine();
      if(ImGui.Button("Delete")) {
        this.triggersViewMode = "delete";
      }

    }

    public void DrawHelpTab() {
      string help = Plugin.GetHelp(this.CurrentPlugin.commandName);
      ImGui.TextWrapped(help);

    }

    private void SaveTriggers() {
      this.Configuration.TRIGGERS = this.TriggerController.GetTriggers();
      this.Configuration.Save();
    }
  }
}
