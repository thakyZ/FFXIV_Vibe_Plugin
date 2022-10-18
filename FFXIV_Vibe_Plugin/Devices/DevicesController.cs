using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#region FFXIV_Vibe_Plugin deps
using FFXIV_Vibe_Plugin.Commons;
#endregion

#region Other deps
using Buttplug;
#endregion

namespace FFXIV_Vibe_Plugin.Device{
  internal class DevicesController {
    private readonly Logger Logger;
    private readonly Configuration Configuration;
    private readonly Patterns Patterns;
    
    // TODO:
    private readonly Sequencer Sequencer;
    
    // Buttplug related
    private ButtplugClient? ButtplugClient;
    private readonly List<Device> Devices = new();
    private readonly Dictionary<String, Device> VisitedDevices = new();
    private bool isScanning = false;

    // Internal variables
    private readonly static Mutex mut = new();

    public DevicesController(Logger logger, Configuration configuration, Sequencer sequencer, Patterns patterns) {
      this.Logger = logger;
      this.Configuration = configuration;
      this.VisitedDevices = configuration.VISITED_DEVICES;
      this.Sequencer = sequencer;
      this.Patterns = patterns;
    }

    public void Dispose() {
      this.Disconnect();
    }

    public void Connect(String host, int port) {
        if(this.IsConnected()) {
          this.Logger.Debug("Disconnecting previous instance! Waiting 2sec...");
          this.Disconnect();
          Thread.Sleep(200);
        }

        try {
          this.ButtplugClient = new("buttplugtriggers-dalamud");
        } catch(Exception e) {
          this.Logger.Error($"Can't load buttplug.io.", e);
          return;
        }
        this.ButtplugClient.ServerDisconnect += ButtplugClient_ServerDisconnected;
        this.ButtplugClient.DeviceAdded += ButtplugClient_DeviceAdded;
        this.ButtplugClient.DeviceRemoved += ButtplugClient_DeviceRemoved;
        this.ButtplugClient.ScanningFinished += ButtplugClient_OnScanComplete;
        string hostandport = host + ":" + port.ToString();
        

        try {
          var uri = new Uri($"ws://{hostandport}/buttplug");
          var connector = new ButtplugWebsocketConnectorOptions(uri);
          this.Logger.Log($"Connecting to {hostandport}.");
          Task task = this.ButtplugClient.ConnectAsync(connector);
          task.Wait();
          this.ScanDevice();
        } catch(Exception e) {
          this.Logger.Error($"Could not connect to {hostandport}.", e);
        }

        Thread.Sleep(200);

        if(this.ButtplugClient.Connected) {
          this.Logger.Log($"FVP connected to Intiface!");
        } else {
          this.Logger.Error("Failed connecting (Intiface server is up?)");
          return;
        }
    }

    private void ButtplugClient_ServerDisconnected(object? sender, EventArgs e) {
      this.Logger.Debug("Server disconnected");
      this.Disconnect();
    }

    public bool IsConnected() {
      bool isConnected = false;
      if(this.ButtplugClient != null) {
        isConnected = this.ButtplugClient.Connected;
      }
      return isConnected;
    }

    public void ScanDevice() {
      if(this.ButtplugClient == null) { return;  }
      this.Logger.Debug("Scanning for devices...");
      if(this.IsConnected()) {
        try {
          this.isScanning = true;
          var task = this.ButtplugClient.StartScanningAsync();
          task.Wait();
        } catch(Exception e) {
          this.isScanning = false;
          this.Logger.Error("Scanning issue. No 'Device Comm Managers' enabled on Intiface?");
          this.Logger.Error(e.Message);
        }
      }
      
    }
    public bool IsScanning() {
      return this.isScanning;
    }

    public void StopScanningDevice() {
      if(this.ButtplugClient != null && this.IsConnected()) {
        try {
          Task task = this.ButtplugClient.StopScanningAsync();
          task.Wait();
        } catch(Exception) {
          this.Logger.Debug("StopScanningDevice ignored: already stopped");
        }
      }
      this.isScanning = false;
    }

    private void ButtplugClient_OnScanComplete(object? sender, EventArgs e) {
      // FIXME: this is not working, buttplug client emit the trigger instantly. Let's ignore for the moment.
      // this.isScanning = false;
    }

    private void ButtplugClient_DeviceAdded(object? sender, DeviceAddedEventArgs arg) {
      try {
        mut.WaitOne();
        ButtplugClientDevice buttplugClientDevice = arg.Device;
        Device device = new(buttplugClientDevice);
        device.IsConnected = true;
        this.Logger.Log($"{arg.Device.Name}, {buttplugClientDevice.Name}");
        this.Devices.Add(device);
        if(!this.VisitedDevices.ContainsKey(device.Name)) {
          this.VisitedDevices[device.Name] = device;
          this.Configuration.VISITED_DEVICES = this.VisitedDevices;
          this.Configuration.Save();
          this.Logger.Debug($"Adding device to visited list {device})");
        }
        this.Logger.Debug($"Added {device})");
      } finally {
        mut.ReleaseMutex();
      }
    }

    private void ButtplugClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs e) {
      try {
        mut.WaitOne();
        int index = this.Devices.FindIndex(device => device.Id == e.Device.Index);
        if(index > -1) {
          this.Logger.Debug($"Removed {Devices[index]}");
          Device device = Devices[index];
          this.Devices.RemoveAt(index);
          device.IsConnected = false;
        }

      } finally {
        mut.ReleaseMutex();
      }
    }

    public void Disconnect() {
      this.Devices.Clear();
      if(this.ButtplugClient == null || !this.IsConnected()) {
        return;
      }
      try {
        if(this.ButtplugClient.IsScanning) {
          var task = this.ButtplugClient.StopScanningAsync();
          task.Wait();
        }
      } catch(Exception e) {
        this.Logger.Error("Couldn't stop scanning device... Unknown reason.");
        this.Logger.Error(e.Message);
      }
      try {
        for(int i = 0; i < this.ButtplugClient.Devices.Length; i++) {
          this.Logger.Log($"Disconnecting device {i} {this.ButtplugClient.Devices[i].Name}");
          this.ButtplugClient.Devices[i].Dispose();
        }
      } catch(Exception e) {
        this.Logger.Error("Error while disconnecting device", e);
      }
      try {
        Thread.Sleep(1000);
        if(this.ButtplugClient != null) {
          this.ButtplugClient.DisconnectAsync();
          this.Logger.Log("Disconnecting! Bye... Waiting 2sec...");
        }
      } catch(Exception e) {
        // ignore exception, we are trying to do our best
        this.Logger.Error("Error while disconnecting client", e);
      }
      this.ButtplugClient = null;
 
    }

    public List<Device> GetDevices() {
      return this.Devices;
    }

    public Dictionary<String, Device> GetVisitedDevices() {
      return this.VisitedDevices;
    }

    public void UpdateAllBatteryLevel() {
      foreach(Device device in this.GetDevices()) {
        device.UpdateBatteryLevel();
      }
    }

    public void StopAll() {
      foreach(Device device in this.GetDevices()) {
        device.Stop();
      }
    }

    public void SendTrigger(Triggers.Trigger trigger) {
      this.Logger.Log($"Sending trigger {trigger}");
      foreach(Triggers.TriggerDevice triggerDevice in trigger.Devices) {
        Device? device = this.FindDevice(triggerDevice.Name);
        if(device != null && triggerDevice != null) {
          
          if(triggerDevice.ShouldVibrate) {
            for(int motorId = 0; motorId < triggerDevice.VibrateSelectedMotors?.Length; motorId++) {
              if(triggerDevice.VibrateSelectedMotors != null && triggerDevice.VibrateMotorsThreshold != null) {
                bool motorEnabled = triggerDevice.VibrateSelectedMotors[motorId];
                int motorIntensity = triggerDevice.VibrateMotorsThreshold[motorId];
                if(motorEnabled) {
                  if(triggerDevice.VibrateMotorsPattern[motorId] == 0) {
                    this.Logger.Debug($"Sending {device.Name} vibration to motor: {motorId} with intensity: {motorIntensity}!");
                    this.SendVibrate(device, motorIntensity, motorId);
                  } else {
                    // WIP
                    int patternId = triggerDevice.VibrateMotorsPattern[motorId];
                    Pattern pattern = Patterns.Get(patternId);
                    // TODO: this.SendVibratePattern(device, "vibrate", pattern, motorId);
                    this.Logger.Debug($"Sending {device.Name} vibration pattern {patternId}:{pattern.Name}:{pattern.Value} to motor {motorId}");
                  }
                }
              }
            }
          }
          if(triggerDevice.ShouldRotate) {
            for(int motorId = 0; motorId < triggerDevice.RotateSelectedMotors?.Length; motorId++) {
              if(triggerDevice.RotateSelectedMotors != null && triggerDevice.RotateMotorsThreshold != null) {
                bool motorEnabled = triggerDevice.RotateSelectedMotors[motorId];
                int motorIntensitiy = triggerDevice.RotateMotorsThreshold[motorId];
                if(motorEnabled) {
                  if(triggerDevice.RotateMotorsPattern[motorId] == 0) {
                    this.Logger.Debug($"Sending {device.Name} rotation to motor: {motorId} with intensity: {motorIntensitiy}!");
                    this.SendRotate(device, motorIntensitiy, motorId);
                  } else {
                    // TODO: use pattern !!!
                  }
                }
              }
            }
          }
          if(triggerDevice.ShouldLinear) {
            for(int motorId = 0; motorId < triggerDevice.LinearSelectedMotors?.Length; motorId++) {
              if(triggerDevice.LinearSelectedMotors != null && triggerDevice.LinearMotorsThreshold != null) {
                bool motorEnabled = triggerDevice.LinearSelectedMotors[motorId];
                int motorIntensitiy = triggerDevice.LinearMotorsThreshold[motorId];
                int motorDuration = triggerDevice.LinearMotorsDuration[motorId];
                if(motorEnabled) {
                  if(triggerDevice.RotateMotorsPattern[motorId] == 0) {
                    this.Logger.Debug($"Sending {device.Name} linear to motor: {motorId} with intensity: {motorIntensitiy}, duration: {motorDuration}!");
                    this.SendLinear(device, motorIntensitiy, motorDuration, motorId);
                  } else {
                    // TODO: use pattern !!!
                  }
                }
              }
            }
          }
          if(triggerDevice.ShouldStop) {
            this.Logger.Debug($"Sending stop to {device.Name}!");
            DevicesController.SendStop(device);
          }
        }
      }
    }

    /** Search for a device with the corresponding text */
    public Device? FindDevice(string text) {
      Device? foundDevice = null;
      foreach(Device device in this.Devices) {
        if(device.Name.Contains(text) && device != null) {
          foundDevice = device;
        }
      }
      return foundDevice;
    }

    /**
     * Sends an itensity vibe to all of the devices 
     * @param {float} intensity
     */
    public void SendVibeToAll(int intensity) {
      if(this.IsConnected() && this.ButtplugClient != null) {
        foreach(Device device in this.Devices) {
          device.SendVibrate(intensity, -1, this.Configuration.MAX_VIBE_THRESHOLD);
          device.SendRotate(intensity, true, -1 , this.Configuration.MAX_VIBE_THRESHOLD);
          device.SendLinear(intensity, 500, -1, this.Configuration.MAX_VIBE_THRESHOLD);
        }
      }
    }

    public void SendVibrate(Device device, int intensity, int motorId=-1) {
      device.SendVibrate(intensity, motorId, this.Configuration.MAX_VIBE_THRESHOLD);
    }

    public void SendRotate(Device device, int intensity, int motorId = -1, bool clockwise = true) {
      device.SendRotate(intensity, clockwise, motorId, this.Configuration.MAX_VIBE_THRESHOLD);
    }

    public void SendLinear(Device device, int intensity, int duration = 500, int motorId = -1) {
      device.SendLinear(intensity, duration, motorId, this.Configuration.MAX_VIBE_THRESHOLD);
    }

    public static void SendStop(Device device) {
      device.Stop();
    }

    public void SendVibratePattern(Triggers.Trigger trigger) {
      this.Logger.Log($"Adding trigger task: {trigger}");
    }





    /************ LEGACY ************/

    public void Play_PatternShake(float from) {
      /* TODO:PatternShake
      this.sequencerTasks = new();
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 50));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 1.5}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 2}", 700));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from / 1.5}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 200));
      */
    }

    public void Play_PatternMountain(float from) {
      /* TODO: PatternMountain
      this.sequencerTasks = new();
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 50));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 1.5}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 200));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 2.5}", 600));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from * 2}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{from}", 500));
      this.sequencerTasks.Add(new SequencerTask($"buttplug_sendVibe:{0}", 200));*/
    }
  }




}
