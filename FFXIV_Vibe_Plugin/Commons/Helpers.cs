using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin.Commons {
  internal class Helpers {

    /** Get number of milliseconds (unix timestamp) */
    public static int GetUnix() {
      return (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }


    public static int ClampIntensity(int intensity, int threshold) {
      if(intensity < 0) { intensity = 0; } else if(intensity > 100) { intensity = 100; }
      return (int)(intensity / (100.0f / threshold));
    }
  }
}
