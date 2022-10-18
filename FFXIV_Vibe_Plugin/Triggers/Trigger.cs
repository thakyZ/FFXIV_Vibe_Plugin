using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Device;

namespace FFXIV_Vibe_Plugin.Triggers {
  enum KIND {
    Chat,
    Spell
  }

  enum DIRECTION {
    Any,
    Outgoing,
    Incoming,
    Self
  }

  public class Trigger : IComparable<Trigger> {
    private static readonly int _initAmountMinValue = -1;
    private static readonly int _initAmountMaxValue = 10000000;

    // General
    public bool Enabled = true;
    public int SortOder = -1;
    public readonly string Id = "";
    public string Name = "";
    public int Kind = (int)KIND.Chat;
    public int ActionEffectType = (int)FFXIV_Vibe_Plugin.Commons.Structures.ActionEffectType.Nothing;
    public int Direction = (int)DIRECTION.Outgoing;
    public string ChatText = "hello world";
    public string SpellText = "Spell to monitor";
    public int AmountMinValue = Trigger._initAmountMinValue;
    public int AmountMaxValue = Trigger._initAmountMaxValue;
    public string FromPlayerName = "";
    public string ToPlayerName = "";
    public float StartAfter = 0;
    public float StopAfter = 120;

    // Device
    public List<TriggerDevice> Devices = new();

    // TODO: implement patter
    private string pattern = "default";

    public Trigger(string name) {
      this.Id = Guid.NewGuid().ToString();
      this.Name = name;
    }

    public override string ToString() {
      return $"TRIGGER: ${this.GetShortID()} {this.Name}";
    }

    public int CompareTo(Trigger? other) {
      if(other == null) { return 1; }
      if(this.SortOder < other.SortOder) {
        return 1;
      } else if(this.SortOder > other.SortOder) {
        return -1;
      } else {
        return 0;
      }
    }

    public string GetShortID() {
      return this.Id[..13];
    }

    public void Reset() {
      this.AmountMaxValue = Trigger._initAmountMaxValue;
      this.AmountMinValue = Trigger._initAmountMinValue;
    }
  }

  public class TriggerDevice {
    public string Name = "";
    public bool IsEnabled = false;
    
    public bool ShouldVibrate = false;
    public bool ShouldRotate = false;
    public bool ShouldLinear = false;
    public bool ShouldStop = false;

    public Device.Device? Device;
    
    public bool[]? SelectedVibrateMotors;
    public bool[]? SelectedRotateMotors;
    public bool[]? SelectedLinearMotors;

    public int[]? VibrateMotorsIntensity;
    public int[]? RotateMotorsIntensity;
    public int[]? LinearMotorsIntensity;
    public int[]? LinearMotorsDuration;


    public TriggerDevice() {

    }

    public override string ToString() {
      return $"TRIGGER_DEVICE {this.Name}";
    }

    public void Set(Device.Device device) {
      this.Name = device.Name;
      this.Device = device;

      // Init vibration array
      this.SelectedVibrateMotors = new bool[device.CanVibrate ? device.VibrateMotors : 0];
      this.VibrateMotorsIntensity = new int[device.CanVibrate ? device.VibrateMotors : 0];

      // Init rotate array
      this.SelectedRotateMotors = new bool[device.CanRotate ? device.RotateMotors : 0];
      this.RotateMotorsIntensity = new int[device.CanRotate ? device.RotateMotors : 0];

      // Init linear array

      this.SelectedLinearMotors = new bool[device.CanLinear ? device.LinearMotors : 0];
      this.LinearMotorsIntensity = new int[device.CanLinear ? device.LinearMotors : 0];
      this.LinearMotorsDuration = new int[device.CanLinear ? device.LinearMotors : 0];

    }
  }
}
