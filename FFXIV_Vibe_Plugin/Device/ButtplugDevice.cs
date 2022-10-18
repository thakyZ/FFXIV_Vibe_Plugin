using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin.Device {
  public class ButtplugDevice {
    public string Name { get; set; }
    public int Id { get; set; }
    public ButtplugDevice(int id, string name) {
      Name = name;
      Id = id;
    }
  }
}
