using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using System.Collections.Generic;
using System.Linq;

using FFXIV_Vibe_Plugin.Commons;
using FFXIV_Vibe_Plugin.UIComponents;


namespace FFXIV_Vibe_Plugin {

  class PluginUI : IDisposable {

    private readonly DalamudPluginInterface PluginInterface;
    private readonly Configuration Configuration;
    private readonly Device.Controller DeviceController;
    private readonly Triggers.Controller TriggerController;
    private readonly Plugin CurrentPlugin;
    private readonly Logger Logger;

    // Images
    private readonly Dictionary<string, ImGuiScene.TextureWrap> loadedImages = new();

    // Patterns
    private readonly Patterns Patterns = new();

    private readonly string DonationLink = "http://paypal.me/kaciedev";

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible {
      get { return this.visible; }
      set { this.visible = value; }
    }
    private bool _expandedOnce = false;
    private readonly int WIDTH = 650;
    private readonly int HEIGHT = 700;

    // The value to send as a test for vibes.
    private int simulator_currentAllIntensity = 0;

    // Temporary UI values
    private int TRIGGER_CURRENT_SELECTED_DEVICE = -1;

    // Trigger
    private Triggers.Trigger? SelectedTrigger = null;
    private string triggersViewMode = "default"; // default|edit|delete;

    /** Constructor */
    public PluginUI(
      Logger logger,
      DalamudPluginInterface pluginInterface,
      Configuration configuration,
      Plugin currentPlugin,
      Device.Controller deviceController,
      Triggers.Controller triggersController,
      Patterns Patterns
    ) {
      this.Logger = logger;
      this.Configuration = configuration;
      this.PluginInterface = pluginInterface;
      this.CurrentPlugin = currentPlugin;
      this.DeviceController = deviceController;
      this.TriggerController = triggersController;
      this.LoadImages();
    }

    public void Display() {
      this.Visible = true;
      this._expandedOnce = false;
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
      if(!this._expandedOnce) {
        ImGui.SetNextWindowCollapsed(false);
        this._expandedOnce = true;
      }
      
      ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.Appearing);
      ImGui.SetNextWindowSize(new Vector2(this.WIDTH, this.HEIGHT), ImGuiCond.Appearing);
      ImGui.SetNextWindowSizeConstraints(new Vector2(this.WIDTH, this.HEIGHT), new Vector2(float.MaxValue, float.MaxValue));
      if(ImGui.Begin("FFXIV Vibe Plugin", ref this.visible, ImGuiWindowFlags.None)) {
        ImGui.Spacing();

        ImGuiScene.TextureWrap imgLogo = this.loadedImages["logo.png"];
        ImGui.Columns(2, "###main_header", false);
        float logoScale = 0.2f;
        ImGui.SetColumnWidth(0, (int)(imgLogo.Width * logoScale + 20));
        ImGui.Image(imgLogo.ImGuiHandle, new Vector2(imgLogo.Width * logoScale, imgLogo.Height * logoScale));
        ImGui.NextColumn();
        if(this.DeviceController.IsConnected()) {
          int nbrDevices = this.DeviceController.GetDevices().Count;
          ImGui.TextColored(ImGuiColors.ParsedGreen, "Your are connected!");
          ImGui.Text($"Number of device(s): {nbrDevices}");
        } else {
          ImGui.TextColored(ImGuiColors.ParsedGrey, "Your are not connected!");
        }
        
        ImGui.Text($"Donations: {this.DonationLink}");
        ImGui.SameLine();
        UIComponents.ButtonLink.Draw("Thanks for the donation ;)", this.DonationLink, Dalamud.Interface.FontAwesomeIcon.Pray, this.Logger);
        
        // Back to on column
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
      // Checkbox MAX_VIBE_THRESHOLD
      ImGui.Text("Global threshold: ");
      ImGui.SameLine();
      int config_MAX_VIBE_THRESHOLD = this.Configuration.MAX_VIBE_THRESHOLD;
      ImGui.SetNextItemWidth(200);
      if(ImGui.SliderInt("###OPTION_MaximumThreshold", ref config_MAX_VIBE_THRESHOLD, 2, 100)) {
        this.Configuration.MAX_VIBE_THRESHOLD = config_MAX_VIBE_THRESHOLD;
        this.Configuration.Save();
      }
      ImGuiComponents.HelpMarker("Maximum threshold for vibes (will override every devices).");

      // Checkbox VIBE_HP_TOGGLE
      bool config_VIBE_HP_TOGGLE = this.Configuration.VIBE_HP_TOGGLE;
      ImGui.Text("Vibe on HP Change: ");
      ImGui.SameLine();
      if(ImGui.Checkbox("###Vibe on HP change.", ref config_VIBE_HP_TOGGLE)) {
        this.Configuration.VIBE_HP_TOGGLE = config_VIBE_HP_TOGGLE;
        this.Configuration.Save();
      }

      // Checkbox VIBE_HP_MODE
      ImGui.SameLine();
      int config_VIBE_HP_MODE = this.Configuration.VIBE_HP_MODE;
      ImGui.SetNextItemWidth(200);
      string[] VIBE_HP_MODES = new string[] { "intensity", "shake", "mountain" };
      if(ImGui.Combo("###OPTION_VIBE_HP_MODES", ref config_VIBE_HP_MODE, VIBE_HP_MODES, VIBE_HP_MODES.Length)) {
        this.Configuration.VIBE_HP_MODE = config_VIBE_HP_MODE;
        this.Configuration.Save();
      }
      ImGui.SameLine();
      ImGuiComponents.HelpMarker("The more you loose HP, the more it will vibe all toys");


      // Checkbox OPTION_VERBOSE_SPELL
      ImGui.Text("Log casted spells:");
      ImGui.SameLine();
      if(ImGui.Checkbox("###OPTION_VERBOSE_SPELL.", ref this.Configuration.VERBOSE_SPELL)) {
        this.Configuration.Save();
      }
      ImGui.SameLine();
      ImGuiComponents.HelpMarker("Use the /xllog to see all casted spells. Disable this to have better ingame performance.");
    }

    public void DrawDevicesTab() {
      ImGui.Spacing();

      ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions");
      ImGui.BeginChild("###DevicesTab_General", new Vector2(-1, 40f), true);
      {
        if(this.DeviceController.IsScanning()) {
          if(ImGui.Button("Stop scanning", new Vector2(100, 24))) {
            this.DeviceController.StopScanningDevice();
          }
        } else {
          if(ImGui.Button("Scan device", new Vector2(100, 24))) {
            this.DeviceController.ScanDevice();
          }
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
        if(ImGui.CollapsingHeader($"[{device.Id}] {device.Name} - Battery: {device.GetBatteryPercentage()}")) {
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
                this.DeviceController.SendRotate(device, device.CurrentRotateIntensity[i], i, true);
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
          if(trigger != null) {
            string enabled = trigger.Enabled ? "" : "[disabled]";
            string kindStr = $"{Enum.GetName(typeof(Triggers.KIND), trigger.Kind)}";
            if(kindStr != null) {
              kindStr = kindStr.ToUpper();
            }
            if(ImGui.Selectable($"{enabled}[{kindStr}] {trigger.Name}{new String(' ', 100)}{trigger.Id}", selectedId == trigger.Id)) { // We don't want to show the ID
              this.SelectedTrigger = trigger;
              this.triggersViewMode = "edit";
            }
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

            // Init table
            int COLUMN0_WIDTH = 120;
            ImGui.BeginTable("###TRIGGER_FORM_TABLE_GENERAL", 2);
            ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
            ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_COL2", ImGuiTableColumnFlags.WidthStretch);


            // Displaying the trigger ID
            ImGui.TableNextColumn();
            ImGui.Text($"TriggerID:");
            ImGui.TableNextColumn();
            ImGui.Text($"{this.SelectedTrigger.GetShortID()}");
            ImGui.TableNextRow();

            // TRIGGER ENABLED
            ImGui.TableNextColumn();
            ImGui.Text("Enabled:");
            ImGui.TableNextColumn();
            if(ImGui.Checkbox("###TRIGGER_ENABLED", ref this.SelectedTrigger.Enabled)) {
              this.Configuration.Save();
            };
            ImGui.TableNextRow();

            // TRIGGER NAME
            ImGui.TableNextColumn();
            ImGui.Text("Trigger Name:");
            ImGui.TableNextColumn();
            if(ImGui.InputText("###TRIGGER_NAME", ref this.SelectedTrigger.Name, 99)) {
              if(this.SelectedTrigger.Name == "") {
                this.SelectedTrigger.Name = "no_name";
              }
              this.Configuration.Save();
            };
            ImGui.TableNextRow();


            // TRIGGER KIND
            ImGui.TableNextColumn();
            ImGui.Text("Kind:");
            ImGui.TableNextColumn();
            string[] TRIGGER_KIND = System.Enum.GetNames(typeof(Triggers.KIND));
            int currentKind = (int)this.SelectedTrigger.Kind;
            if(ImGui.Combo("###TRIGGER_FORM_KIND", ref currentKind, TRIGGER_KIND, TRIGGER_KIND.Length)) {
              this.SelectedTrigger.Kind = currentKind;
              this.Configuration.Save();
            }
            ImGui.TableNextRow();

            // TRIGGER FROM_PLAYER_NAME
            ImGui.TableNextColumn();
            ImGui.Text("Player name:");
            ImGui.TableNextColumn();
            if(ImGui.InputText("###TRIGGER_CHAT_FROM_PLAYER_NAME", ref this.SelectedTrigger.FromPlayerName, 100)) {
              this.SelectedTrigger.FromPlayerName = this.SelectedTrigger.FromPlayerName.Trim();
              this.Configuration.Save();
            };
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("You can use RegExp. Leave empty for any.");
            ImGui.TableNextRow();

            // TRIGGER START_AFTER
            ImGui.TableNextColumn();
            ImGui.Text("Start after");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(230);
            if(ImGui.SliderFloat("###TRIGGER_FORM_START_AFTER", ref this.SelectedTrigger.StartAfter, 0, 120)) {
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("In seconds");
            ImGui.TableNextRow();

            // TRIGGER STOP_AFTER
            ImGui.TableNextColumn();
            ImGui.Text("Stop after");
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(230);
            if(ImGui.SliderFloat("###TRIGGER_FORM_STOP_AFTER", ref this.SelectedTrigger.StopAfter, 0, 120)) {
              this.Configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("In seconds. Use zero to avoid stopping.");
            ImGui.TableNextRow();

            ImGui.EndTable();

            ImGui.Separator();

            // TRIGGER KIND:CHAT OPTIONS
            if(this.SelectedTrigger.Kind == (int)Triggers.KIND.Chat) {
              // TRIGGER FORM_TABLE_KIND_CHAT
              ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_CHAT", 2);
              ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
              ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_CHAT_COL2", ImGuiTableColumnFlags.WidthStretch);

              // TRIGGER CHAT_TEXT
              ImGui.TableNextColumn();
              ImGui.Text("Chat text:");
              ImGui.TableNextColumn();
              string currentChatText = this.SelectedTrigger.ChatText;
              if(ImGui.InputText("###TRIGGER_CHAT_TEXT", ref currentChatText, 250)) {
                this.SelectedTrigger.ChatText = currentChatText.ToLower(); // ChatMsg is always lower
                this.Configuration.Save();
              };
              ImGui.SameLine();
              ImGuiComponents.HelpMarker("You can use RegExp.");
              ImGui.TableNextRow();

              // END OF TABLE
              ImGui.EndTable();
            }



            // TRIGGER KIND:SPELL OPTIONS
            if(this.SelectedTrigger.Kind == (int)Triggers.KIND.Spell) {
              // TRIGGER FORM_TABLE_KIND_CHAT
              ImGui.BeginTable("###TRIGGER_FORM_TABLE_KIND_SPELL", 2);
              ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL1", ImGuiTableColumnFlags.WidthFixed, COLUMN0_WIDTH);
              ImGui.TableSetupColumn("###TRIGGER_FORM_TABLE_KIND_SPELL_COL2", ImGuiTableColumnFlags.WidthStretch);

              // TRIGGER TYPE
              ImGui.TableNextColumn();
              ImGui.Text("Type:");
              ImGui.TableNextColumn();
              string[] TRIGGER = System.Enum.GetNames(typeof(FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType));
              int currentEffectType = (int)this.SelectedTrigger.ActionEffectType;
              if(ImGui.Combo("###TRIGGER_FORM_EVENT", ref currentEffectType, TRIGGER, TRIGGER.Length)) {
                this.SelectedTrigger.ActionEffectType = currentEffectType;
                this.SelectedTrigger.Reset();
                this.Configuration.Save();
              }
              ImGui.TableNextRow();

              //TRIGGER SPELL TEXT
              ImGui.TableNextColumn();
              ImGui.Text("Spell Text:");
              ImGui.TableNextColumn();
              if(ImGui.InputText("###TRIGGER_FORM_SPELLNAME", ref this.SelectedTrigger.SpellText, 100)) {
                this.Configuration.Save();
              }
              ImGui.SameLine();
              ImGuiComponents.HelpMarker("You can use RegExp.");
              ImGui.TableNextRow();

              //TRIGGER DIRECTION
              ImGui.TableNextColumn();
              ImGui.Text("Direction:");
              ImGui.TableNextColumn();
              string[] DIRECTIONS = System.Enum.GetNames(typeof(Triggers.DIRECTION));
              int currentDirection = (int)this.SelectedTrigger.Direction;
              if(ImGui.Combo("###TRIGGER_FORM_DIRECTION", ref currentDirection, DIRECTIONS, DIRECTIONS.Length)) {
                this.SelectedTrigger.Direction = currentDirection;
                this.Configuration.Save();
              }
              ImGui.SameLine();
              ImGuiComponents.HelpMarker("Warning: Hitting no target will result to self as if you cast on yourself");
              ImGui.TableNextRow();

              if(this.SelectedTrigger.ActionEffectType != (int)FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType.Nothing) { 
                if(currentEffectType != (int)FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType.Mount &&
                  currentEffectType != (int)FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType.Miss &&
                  currentEffectType != (int)Structures.ActionEffectType.Transport &&
                  currentEffectType != (int)Structures.ActionEffectType.Unknown_0
                ) {



                  // Min/Max amount values
                  if(this.SelectedTrigger.ActionEffectType == (int)Structures.ActionEffectType.Damage || this.SelectedTrigger.ActionEffectType == (int)Structures.ActionEffectType.Heal) {
                    // TRIGGER MIN_VALUE
                    ImGui.TableNextColumn();
                    ImGui.Text("Minimum value:");
                    ImGui.TableNextColumn();
                    if(ImGui.InputInt("###TRIGGER_FORM_MIN_AMOUNT", ref this.SelectedTrigger.AmountMinValue, 100)) {
                      this.Configuration.Save();
                    }
                    ImGui.TableNextRow();

                    // TRIGGER MAX_VALUE
                    ImGui.TableNextColumn();
                    ImGui.Text("Maximum value:");
                    ImGui.TableNextColumn();
                    if(ImGui.InputInt("###TRIGGER_FORM_MAX_AMOUNT", ref this.SelectedTrigger.AmountMaxValue, 100)) {
                      this.Configuration.Save();
                    }
                    ImGui.TableNextRow();
                  }
                }
              }


              ImGui.EndTable();
            }

            ImGui.TextColored(ImGuiColors.DalamudViolet, "Actions & Devices");
            ImGui.Separator();

            // TRIGGER COMBO_DEVICES
            Dictionary<String, Device.Device> visitedDevice = DeviceController.GetVisitedDevices();
            string[] devicesStrings = visitedDevice.Keys.ToArray();
            ImGui.Combo("###TRIGGER_FORM_COMBO_DEVICES", ref this.TRIGGER_CURRENT_SELECTED_DEVICE, devicesStrings, devicesStrings.Length);
            ImGui.SameLine();
            List<Triggers.TriggerDevice> triggerDevices = this.SelectedTrigger.Devices;
            if(ImGuiComponents.IconButton(Dalamud.Interface.FontAwesomeIcon.Plus)) {
              if(this.TRIGGER_CURRENT_SELECTED_DEVICE >= 0) {
                Device.Device device = visitedDevice[devicesStrings[this.TRIGGER_CURRENT_SELECTED_DEVICE]];
                Triggers.TriggerDevice newTriggerDevice = new(device);
                triggerDevices.Add(newTriggerDevice);
                this.Configuration.Save();
              }
            };

            if(triggerDevices.Count == 0) {
              ImGui.TextColored(ImGuiColors.DalamudGrey, "Please add device(s)...");
            }

            for(int indexDevice = 0; indexDevice < triggerDevices.Count; indexDevice++) {
              string prefixLabel = $"###TRIGGER_FORM_COMBO_DEVICE_${indexDevice}";
              Triggers.TriggerDevice triggerDevice = triggerDevices[indexDevice];
              string deviceName = triggerDevice.Device != null ? triggerDevice.Device.Name : "UnknownDevice";
              if(ImGui.CollapsingHeader($"{deviceName}")) {
                ImGui.Indent(10);
               
                if(triggerDevice != null && triggerDevice.Device != null) {
                  if(triggerDevice.Device.CanVibrate) {
                    if(ImGui.Checkbox($"{prefixLabel}_SHOULD_VIBRATE", ref triggerDevice.ShouldVibrate)) {
                      triggerDevice.ShouldStop = false;
                      this.Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("Should Vibrate");
                    if(triggerDevice.ShouldVibrate) {
                      ImGui.Indent(20);
                      for(int motorId = 0; motorId < triggerDevice.Device.VibrateMotors; motorId++) {
                        ImGui.Text($"Motor {motorId+1}");
                        ImGui.SameLine();
                        // Display Vibrate Motor checkbox
                        if(ImGui.Checkbox($"{prefixLabel}_SHOULD_VIBRATE_MOTOR_{motorId}", ref triggerDevice.VibrateSelectedMotors[motorId])) {
                          this.Configuration.Save();
                        }

                        if(triggerDevice.VibrateSelectedMotors[motorId]) {
                          string[] patternNames = this.Patterns.Get().Select(p => p.Name).ToArray();
                          
                          // WIP
                          ImGui.SameLine();
                          if(ImGui.Combo($"###{prefixLabel}_VIBRATE_PATTERNS_{motorId}", ref triggerDevice.VibrateMotorsPattern[motorId], patternNames, patternNames.Length)) {
                            this.Configuration.Save();
                          }

                          // Special intensity pattern asks for intensity param.
                          int currentPatternIndex = triggerDevice.VibrateMotorsPattern[motorId];
                          if(currentPatternIndex == 0) {
                            ImGui.Text($"Motor {motorId + 1} intensity:");
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(250);
                            if(ImGui.SliderInt($"{prefixLabel}_SHOULD_VIBRATE_MOTOR_{motorId}_INTENSITY", ref triggerDevice.VibrateMotorsIntensity[motorId], 0, 100)) {
                              if(triggerDevice.VibrateMotorsIntensity[motorId] > 0) {
                                triggerDevice.VibrateSelectedMotors[motorId] = true;
                              }
                              this.Configuration.Save();
                            }
                          }
                        }
                      }
                      ImGui.Indent(-20);
                    }
                  }
                  if(triggerDevice.Device.CanRotate) {
                    if(ImGui.Checkbox($"{prefixLabel}_SHOULD_ROTATE", ref triggerDevice.ShouldRotate)) {
                      triggerDevice.ShouldStop = false;
                      this.Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("Should Rotate");
                    if(triggerDevice.ShouldRotate) {
                      ImGui.Indent(20);
                      for(int motorId = 0; motorId < triggerDevice.Device.RotateMotors; motorId++) {
                        ImGui.Text($"Motor {motorId+1}");
                        ImGui.SameLine();
                        if(ImGui.Checkbox($"{prefixLabel}_SHOULD_ROTATE_MOTOR_{motorId}", ref triggerDevice.RotateSelectedMotors[motorId])) {
                          this.Configuration.Save();
                        }
                        if(triggerDevice.RotateSelectedMotors[motorId]) {
                          ImGui.SameLine();
                          if(ImGui.SliderInt($"{prefixLabel}_SHOULD_ROTATE_MOTOR_{motorId}_INTENSITY", ref triggerDevice.RotateMotorsIntensity[motorId], 0, 100)) {
                            if(triggerDevice.RotateMotorsIntensity[motorId] > 0) {
                              triggerDevice.RotateSelectedMotors[motorId] = true;
                            }
                            this.Configuration.Save();
                          }
                        }
                      }
                      ImGui.Indent(-20);
                    }
                  }
                  if(triggerDevice.Device.CanLinear) {
                    if(ImGui.Checkbox($"{prefixLabel}_SHOULD_LINEAR", ref triggerDevice.ShouldLinear)) {
                      triggerDevice.ShouldStop = false;
                      this.Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("Should Linear");
                    if(triggerDevice.ShouldLinear) {
                      ImGui.Indent(20);
                      for(int motorId = 0; motorId < triggerDevice.Device.LinearMotors; motorId++) {
                        ImGui.Text($"Motor {motorId+1}");
                        ImGui.SameLine();
                        if(ImGui.Checkbox($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}", ref triggerDevice.LinearSelectedMotors[motorId])) {
                          this.Configuration.Save();
                        }
                        if(triggerDevice.LinearSelectedMotors[motorId]) {
                          ImGui.SameLine();
                          if(ImGui.SliderInt($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}_INTENSITY", ref triggerDevice.LinearMotorsIntensity[motorId], 0, 100)) {
                            if(triggerDevice.LinearMotorsIntensity[motorId] > 0) {
                              triggerDevice.LinearSelectedMotors[motorId] = true;
                            }
                            this.Configuration.Save();
                          }
                          if(ImGui.InputInt($"{prefixLabel}_SHOULD_LINEAR_MOTOR_{motorId}_DURATION", ref triggerDevice.LinearMotorsDuration[motorId])) {
                            if(!triggerDevice.LinearSelectedMotors[motorId]) {
                              triggerDevice.LinearMotorsDuration[motorId] = 0;
                              this.Configuration.Save();
                            }
                          }
                        }
                      }
                      ImGui.Indent(-20);
                    }
                  }
                  if(triggerDevice.Device.CanStop) {
                    if(ImGui.Checkbox($"{prefixLabel}_SHOULD_STOP", ref triggerDevice.ShouldStop)) {
                      triggerDevice.ShouldVibrate = false;
                      triggerDevice.ShouldRotate = false;
                      triggerDevice.ShouldLinear = false;
                      this.Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("Should stop all motors");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Instantly stop all motors for this device.");
                  }
                  if(ImGui.Button($"Remove###{prefixLabel}_REMOVE")) {
                    triggerDevices.RemoveAt(indexDevice);
                    this.Logger.Log($"DEBUG: removing {indexDevice}");
                    this.Configuration.Save();
                  }
                }
                ImGui.Indent(-10);
              }
            }
            

          } else {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Current selected trigger is null");
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
        this.Configuration.Save();
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


  }
}
