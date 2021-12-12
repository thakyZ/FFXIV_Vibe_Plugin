using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Device;

namespace FFXIV_Vibe_Plugin.Triggers {
  internal class Trigger {
    // First idea
    private string kind = "Spell|Chat";
    private string trigger = "SpellDamage|SpellHeal|DamageRecieved|Miss|Mount";
    private string name = "any";
    private bool incoming = false;
    private bool outgoing = true;
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

    private Device.Device device = null;
    private int motorId = -1;

    public Trigger(Device.Device device) {
      this.device = device;
    }
  }
}
