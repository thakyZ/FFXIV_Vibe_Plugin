using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin {

  public class Patterns {
    private int __count = 0;
    private readonly List<Pattern> List = new();

    public Patterns() {
      
      this.Add(new Pattern("intensity", "100:0")); // TODO: change 5 to 100 and use threshold
      this.Add(new Pattern("bump", "10:100|20:100|30:100|40:100|50:100|60:100|70:100|80:100|90:100|100:100|0:0"));
      this.Add(new Pattern("xenoWave", "10:650|15:500|20:400|30:400|45:350|60:300|75:300|95:250|100:200|90:250|75:300|60:300|45:350|30:400|20:400|15:500|10:650|5:750|0:0"));
      this.Add(new Pattern("test of pattern for debugging", "10:1000|20:1000|0:0"));
    }

    public List<Pattern> GetAll() {
      return this.List;
    }

    public Pattern Get(int index) {
      return this.List[index];
    }

    public void Add(Pattern pattern) {
      pattern.Index = this.__count++;
      List.Add(pattern);
    }

    public void Update(Pattern pattern, string name, string value) {
      /* TODO: Get the correct pattern based on pattern name
      pattern.SetName(pattern.Name);
      pattern.SetValue(pattern.Value);
      */
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
