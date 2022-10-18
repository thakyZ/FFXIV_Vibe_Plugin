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

  enum TRIGGER {
    Damage,
    Heal,
    DamageAmount,
    HealAmount,
    Miss,
    SelfMount
  }

  enum DIRECTION {
    Incoming,
    Outgoing,
    Self
  }

  public class Trigger : IComparable<Trigger> {
    // First idea

    // General
    public bool Enabled = true;
    public int SortOder = -1;
    public readonly string Id = "";
    public string Name = "";
    public int Kind = (int)KIND.Chat;
    public int Event = (int)TRIGGER.Damage;
    public int Direction = (int)DIRECTION.Outgoing;
    public string ChatText = "";
    public string SpellText = "";
    public int Intensity = 0;
  

    // Device
    public Device.Device? Device = null;
    private int motorId = -1;
    
    private int minValue = 0;
    private int maxValue = 0;
    private string fromName = "me";
    private string toName = "any";
    private string action = "vibe|vibrate|rotate|linear|stop";
    private int duration = 2000;
    private string pattern = "default";
    /**
     * If it's damage, then check average overtime.
     * If it's heal, then check average overtime.
     */

    
    

    public Trigger(string name) {
      this.Id = Guid.NewGuid().ToString();
      this.Name = name;
    }

    public int CompareTo(Trigger other) {
      if(this.SortOder < other.SortOder) {
        return 1;
      } else if(this.SortOder > other.SortOder){
        return -1;
      } else {
        return 0;
      }
    }

    public string GetShortID() {
      return this.Id[..13];
    }
  }
}
