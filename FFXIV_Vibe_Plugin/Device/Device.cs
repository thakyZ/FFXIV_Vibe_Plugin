using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Buttplug;

namespace FFXIV_Vibe_Plugin.Device {
  public class Device {
    private ButtplugClientDevice ButtplugClientDevice;
    public int Id { get; set; }
    public string Name { get; set; }
    public bool CanVibrate = false;
    public int VibrateMotors = -1;
    public uint[]? VibrateSteps;
    public bool CanRotate = false;
    public int RotateMotors = -1;
    public uint[]? RotateSteps;
    public bool CanLinear = false;
    public int LinearMotors = -1;
    public uint[]? LinearSteps;
    public bool CanBattery = false;
    public bool CanStop = false;    
    public bool IsConnected = false;
    public double BatteryLevel = -1;

    public Device(ButtplugClientDevice buttplugClientDevice) {
      this.ButtplugClientDevice = buttplugClientDevice;
      Id = (int)buttplugClientDevice.Index;
      Name = buttplugClientDevice.Name;
      this.SetCommands();
      this.UpdateBatteryLevel();
    }

    public override string ToString() {
      List<string> commands = this.GetCommands();
      return $"Device: {Id}:{Name} (connected={IsConnected}, commands={String.Join(",", commands)})";
    }

    private void SetCommands() {
      foreach(var cmd in this.ButtplugClientDevice.AllowedMessages) {
        if(cmd.Key == ServerMessage.Types.MessageAttributeType.VibrateCmd) {
          this.CanVibrate = true;
          this.VibrateMotors = (int)cmd.Value.FeatureCount;
          this.VibrateSteps = cmd.Value.StepCount;
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.RotateCmd) {
          this.CanRotate = true;
          this.RotateMotors = (int)cmd.Value.FeatureCount;
          this.RotateSteps = cmd.Value.StepCount;
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.LinearCmd) {
          this.CanLinear = true;
          this.LinearMotors = (int)cmd.Value.FeatureCount;
          this.LinearSteps = cmd.Value.StepCount;
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.BatteryLevelCmd) {
          this.CanBattery = true;
        } else if(cmd.Key == ServerMessage.Types.MessageAttributeType.StopDeviceCmd) {
          this.CanStop = true;
        }
      }
    }

    public List<String> GetCommands() {
      List<string> commands = new();
      if(CanVibrate) {
        commands.Add($"vibrate motors={VibrateMotors} steps={String.Join(",", VibrateSteps)}");
      }
      if(CanRotate) {
        commands.Add($"rotate motors={RotateMotors} steps={String.Join(",", RotateSteps)}");
      }
      if(CanLinear) {
        commands.Add($"rotate motors={LinearMotors} steps={String.Join(",", LinearSteps)}");
      }
      if(CanBattery) commands.Add("battery");
      if(CanStop) commands.Add("stop");
      return commands;
    }


    public double UpdateBatteryLevel() {
      Task<double> batteryLevelTask = this.ButtplugClientDevice.SendBatteryLevelCmd();
      batteryLevelTask.Wait();
      this.BatteryLevel = batteryLevelTask.Result;
      return this.BatteryLevel;
    }

    public double GetBatteryLevel() {
      return this.BatteryLevel;
    }
  }
}
