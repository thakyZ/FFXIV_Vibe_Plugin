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
    public uint[] VibrateSteps = { };
    public bool CanRotate = false;
    public int RotateMotors = -1;
    public uint[] RotateSteps = { };
    public bool CanLinear = false;
    public int LinearMotors = -1;
    public uint[] LinearSteps = { };
    public bool CanBattery = false;
    public bool CanStop = false;    
    public bool IsConnected = false;
    public double BatteryLevel = -1;

    public int[] CurrentVibrateIntensity;
    public int[] CurrentRotateIntensity;
    public int[] CurrentLinearIntensity;

    public Device(ButtplugClientDevice buttplugClientDevice) {
      this.ButtplugClientDevice = buttplugClientDevice;
      Id = (int)buttplugClientDevice.Index;
      Name = buttplugClientDevice.Name;
      this.SetCommands();
      this.ResetMotors();
      this.UpdateBatteryLevel();
    }

    public override string ToString() {
      List<string> commands = this.GetCommands();
      return $"Device: {Id}:{Name} (connected={IsConnected}, battery={GetBatteryPercentage()}, commands={String.Join(",", commands)})";
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

    /** Init all current motors intensity and default to zero */
    private void ResetMotors() {
      if(this.CanVibrate) {
        this.CurrentVibrateIntensity = new int[this.VibrateMotors];
        for(int i=0; i<this.VibrateMotors; i++) { this.CurrentVibrateIntensity[i] = 0; };
      }
      if(this.CanRotate) {
        this.CurrentRotateIntensity = new int[this.RotateMotors];
        for(int i = 0; i < this.RotateMotors; i++) { this.CurrentRotateIntensity[i] = 0; };
      }
      if(this.CanLinear) {
        this.CurrentLinearIntensity = new int[this.LinearMotors];
        for(int i = 0; i < this.LinearMotors; i++) { this.CurrentLinearIntensity[i] = 0; };
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

    public string GetBatteryPercentage() {
      return $"{this.BatteryLevel*100}%";
    }

    public void Stop() {
      if(CanStop) {
        this.ButtplugClientDevice.SendStopDeviceCmd();
      }
      if(CanVibrate) {
        this.ButtplugClientDevice.SendVibrateCmd(0);
      }
      if(CanRotate) {
        this.ButtplugClientDevice.SendRotateCmd(0f, true);
      }
      ResetMotors();
    }

    public void SendVibrate(int intensity, int motorId=-1) {
      Dictionary<uint, double> motorIntensity = new();
      for(int i=0; i < this.VibrateMotors; i++) {
        if(motorId == -1 || motorId == i) {
          this.CurrentVibrateIntensity[i] = intensity;
          motorIntensity.Add((uint)i, intensity / 100.0);
        }
      }
      this.ButtplugClientDevice.SendVibrateCmd(motorIntensity);
    }

    public void SendRotate(int intensity, bool clockWise, int motorId=-1) {
      Dictionary<uint, (double, bool)> motorIntensity = new();
      for(int i = 0; i < this.RotateMotors; i++) {
        if(motorId == -1 || motorId == i) {
          this.CurrentRotateIntensity[i] = intensity;
          (double, bool) values = (intensity/100.0, clockWise);
          motorIntensity.Add((uint)i, values);
        }
      }
      this.ButtplugClientDevice.SendRotateCmd(motorIntensity);
    }

    public void SendLinear(int intensity, int duration, int motorId = -1) {
      Dictionary<uint, (uint, double)> motorIntensity = new();
      for(int i = 0; i < this.LinearMotors; i++) {
        if(motorId == -1 || motorId == i) {
          this.CurrentLinearIntensity[i] = intensity;
          (uint, double) values = ((uint)duration, intensity / 100.0);
          motorIntensity.Add((uint)i, values);
        }
      }
      this.ButtplugClientDevice.SendLinearCmd(motorIntensity);
    }
  }
}
