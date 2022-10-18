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
    private static Mutex mut = new Mutex();
    private float _currentIntensity = -1;

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
          this.Logger.Chat($"Connecting to {hostandport}.");
          Task task = this.ButtplugClient.ConnectAsync(connector);
          task.Wait();
        } catch(Exception e) {
          this.Logger.Error($"Could not connect to {hostandport}.", e);
        }

        Thread.Sleep(200);

        if(this.ButtplugClient.Connected) {
          this.Logger.Chat($"Buttplug connected!");
        } else {
          this.Logger.Error("Failed connecting (Intiface server is up?)");
          return;
        }

        this.ScanToys();
      
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
      this.Logger.Chat("Scanning for devices...");
      if(this.IsConnected()) {
        try {
          this.ButtplugClient.StartScanningAsync();
        } catch(Exception e) {
          this.Logger.Error("Scanning issue...", e);
        }
      }
    }

    private void ButtplugClient_DeviceAdded(object? sender, DeviceAddedEventArgs arg) {
      mut.WaitOne();
      ButtplugClientDevice buttplugClientDevice = arg.Device;
      Device device = new(buttplugClientDevice);
      device.IsConnected = true;
      this.Devices.Add(device);
      this.Logger.Chat($"Added {device})");
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
      this.Logger.Log($"Removed {Devices[index]}");
      this.Devices.RemoveAt(index);
      mut.ReleaseMutex();
    }

    public void Disconnect() {
      if(this.ButtplugClient == null || !this.IsConnected()) {
        return;
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
    public void SendVibe(float intensity, int motor=-1) {
      if(this._currentIntensity != intensity && this.IsConnected() && this.ButtplugClient != null) {
        this.Logger.Debug($"Intensity: {intensity} / Threshold: {this.Configuration.MAX_VIBE_THRESHOLD}");

        // Set min and max limits
        if(intensity < 0) { intensity = 0.0f; } else if(intensity > 100) { intensity = 100; }
        var newIntensity = intensity / (100.0f / this.Configuration.MAX_VIBE_THRESHOLD) / 100.0f;
        Dictionary<uint,double> intensityPair = new Dictionary<uint,double>();
        if(motor != -1) {
          intensityPair.Add((uint)motor, newIntensity);
        }
        //this.Devices[deviceId].SendVibrateCmd(intensityPair);
        this._currentIntensity = newIntensity;
      }
    }







    /************ LEGACY ************/


    private void Command_ToysList() {
      if(this.ButtplugClient == null) { return; }
      for(int i = 0; i < this.ButtplugClient.Devices.Length; i++) {
        string name = this.ButtplugClient.Devices[i].Name;
        this.Logger.Chat($"    {i}: {name}");
      }
    }


    private void Command_SendIntensity(string args) {
      string[] blafuckcsharp;
      float intensity;
      try {
        blafuckcsharp = args.Split(" ", 2);
        intensity = float.Parse(blafuckcsharp[1]);
        this.Logger.Chat($"Command Send intensity {intensity}");
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for send [intensity].", e);
        return;
      }
      this.SendVibe(intensity);
    }
    

    private void Play_pattern(string args) {
      try {
        string[] param = args.Split(" ", 2);
        string patternName = param[1];
        this.Logger.Chat($"Play pattern {patternName}");
        if(patternName == "shake") {
          this.Play_PatternShake(100);
        } else if(patternName == "mountain") {
          this.Play_PatternMountain(30);
        }
      } catch(Exception e) when(e is FormatException or IndexOutOfRangeException) {
        this.Logger.Error($"Malformed arguments for play_pattern [pattern_name] # shake, mountain", e);
        return;
      }
    }

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
