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
  }
}
