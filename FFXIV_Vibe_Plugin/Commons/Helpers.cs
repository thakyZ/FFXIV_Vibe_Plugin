using System;
using System.Text.RegularExpressions;


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

    /** Check if a regexp matches the given text */
    public static bool RegExpMatch(Logger Logger, string text, string regexp) {
      bool found = false;

      if(regexp.Trim() == "") {
        found = true;
      } else {
        string patternCheck = String.Concat(@"", regexp);
        try {
          System.Text.RegularExpressions.Match m = Regex.Match(text, patternCheck, RegexOptions.IgnoreCase);
          if(m.Success) {
            found = true;
          }
        } catch(Exception) {
          Logger.Error($"Probably a wrong REGEXP for {regexp}");
        }
      }

      return found;
    }
  }
}
