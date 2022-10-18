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
  internal class Controller {
    private readonly Logger Logger;
    private readonly Configuration Configuration;
    
    // Buttplug related
    private ButtplugClient? ButtplugClient;
    private readonly List<Device> Devices = new();

    // Internal variables
    private readonly static Mutex mut = new();

    public Controller(Logger logger, Configuration configuration) {
      this.Logger = logger;
      this.Configuration = configuration;
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
        string hostandport = host + ":" + port.ToString();
        

        try {
          var uri = new Uri($"ws://{hostandport}/buttplug");
          var connector = new ButtplugWebsocketConnectorOptions(uri);
          this.Logger.Log($"Connecting to {hostandport}.");
          Task task = this.ButtplugClient.ConnectAsync(connector);
          task.Wait();
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

    public void ScanToys() {
      if(this.ButtplugClient == null) { return;  }
      this.Logger.Debug("Scanning for devices...");
      if(this.IsConnected()) {
        try {
          var task = this.ButtplugClient.StartScanningAsync();
          task.Wait();
        } catch(Exception e) {
          this.Logger.Error("Scanning issue. No 'Device Comm Managers' enabled on Intiface?");
          this.Logger.Error(e.Message);
        }
      }
    }

    private void ButtplugClient_DeviceAdded(object? sender, DeviceAddedEventArgs arg) {
      mut.WaitOne();
      ButtplugClientDevice buttplugClientDevice = arg.Device;
      Device device = new(buttplugClientDevice);
      device.IsConnected = true;
      this.Devices.Add(device);
      this.Logger.Debug($"Added {device})");
      mut.ReleaseMutex();

      /**
       * Sending some vibes at the intial stats make sure that some toys re-sync to Intiface. 
       * Therefore, it is important to trigger a zero and some vibes before continuing further.
       * Don't remove this part unless you want to debug for hours.
       */
      /* TODO:
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:0", 0));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:1", 500));
      this.sequencerTasks.Add(new SequencerTask("buttplug_sendVibe:0", 0));
      */
    }

    private void ButtplugClient_DeviceRemoved(object? sender, DeviceRemovedEventArgs e) {
      mut.WaitOne();
      int index = this.Devices.FindIndex(device => device.Id == e.Device.Index);
      Device device = Devices[index];
      device.IsConnected = false;
      this.Logger.Debug($"Removed {Devices[index]}");
      this.Devices.RemoveAt(index);
      mut.ReleaseMutex();
    }

    public void Disconnect() {
      if(this.ButtplugClient == null || !this.IsConnected()) {
        return;
      }
      try {
        var task = this.ButtplugClient.StopScanningAsync();
        task.Wait();
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

    /**
     * Sends an itensity vibe to all of the devices 
     * @param {float} intensity
     */
    public void SendVibeToAll(int intensity) {
      if(this.IsConnected() && this.ButtplugClient != null) {
        // DEBUG: this.Logger.Debug($"Intensity: {intensity} / Threshold: {this.Configuration.MAX_VIBE_THRESHOLD}");
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

    public void SendRotate(Device device, int intensity, bool clockwise=true, int motorId = -1) {
      device.SendRotate(intensity, clockwise, motorId, this.Configuration.MAX_VIBE_THRESHOLD);
    }

    public void SendLinear(Device device, int intensity, int duration = 500, int motorId = -1) {
      device.SendLinear(intensity, duration, motorId, this.Configuration.MAX_VIBE_THRESHOLD);
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
