using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin {

  public class Patterns {
    private readonly List<Pattern> BuiltinPatterns = new();
    private List<Pattern> CustomPatterns = new();

    public Patterns() {
      this.AddBuiltinPattern(new Pattern("intensity", "100:0")); // TODO: change 5 to 100 and use threshold
      this.AddBuiltinPattern(new Pattern("ramp", "10:150|20:150|30:150|40:150|50:150|60:150|70:150|80:150|90:150|100:250|0:0"));
      this.AddBuiltinPattern(new Pattern("bump", "10:150|20:150|30:150|40:150|50:150|60:150|70:150|80:150|90:150|100:250|50:250|100:500|0:0"));
      this.AddBuiltinPattern(new Pattern("square", "100:800|50:800|0:200|100:1000|0:0"));
      this.AddBuiltinPattern(new Pattern("shake", "100:500|20:200|100:500|80:500|100:200|90:100|100:200|90:200|100:800|0:0"));
      this.AddBuiltinPattern(new Pattern("sos", "100:500|50:200|100:500|50:200|100:500|50:200|100:1000|30:200|100:1000|30:200|100:1000|30:200|100:500|50:200|100:500|50:200|100:500|0:0"));
      this.AddBuiltinPattern(new Pattern("xenoWave", "10:650|15:500|20:400|30:400|45:350|60:300|75:300|95:250|100:200|90:250|75:300|60:300|45:350|30:400|20:400|15:500|10:650|5:750|0:0"));
      this.AddBuiltinPattern(new Pattern("slowVibe", "10:1000|20:1000|10:1000|50:1000|0:0"));
    }

    public List<Pattern> GetAllPatterns() {
      return this.BuiltinPatterns.Concat(this.CustomPatterns).ToList();
    }

    public List<Pattern> GetBuiltinPatterns() {
      return this.BuiltinPatterns;
    }

    /** Returns a copy of the list to avoid error if any modification happens */
    public List<Pattern> GetCustomPatterns() {
      List<Pattern> newList = new();
      foreach(Pattern pattern in this.CustomPatterns) {
        newList.Add(pattern);
      }
      return newList;
    }

    public void SetCustomPatterns(List<Pattern> customPatterns) {
      this.CustomPatterns = customPatterns;
    }

    public Pattern GetPatternById(int index) {
      return this.GetAllPatterns()[index];
    }

    public void AddBuiltinPattern(Pattern pattern) {
      BuiltinPatterns.Add(pattern);
    }

    public void AddCustomPattern(Pattern pattern) {
      Pattern? foundPattern = CustomPatterns.FirstOrDefault<Pattern>(p => p.Name == pattern.Name );
      if(foundPattern != null) {
        foundPattern.Name = pattern.Name;
        foundPattern.Value = pattern.Value;
      } else {
        CustomPatterns.Add(pattern);
      }
    }

    public bool RemoveCustomPattern(Pattern pattern) {
      int index = CustomPatterns.IndexOf(pattern);
      if(index > -1) {
        this.CustomPatterns.RemoveAt(index);
        return true;
      }
      return false;
    }
  }

  public class Pattern {
    public int Index = -1;
    public string Name = "pattern";
    public string Value = "10:1000";
    public Pattern(string name="pattern", string value="10:1000") {
      this.Name = name;
      this.Value = value;
    }
  }
}
