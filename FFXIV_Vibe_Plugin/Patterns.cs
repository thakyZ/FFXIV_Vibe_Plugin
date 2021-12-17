using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin {

  public class Patterns {
    public readonly List<Pattern> List = new();

    public Patterns() {
      this.Add(new Pattern("testOfPattern", "10:1000|20:1000|"));
    }

    public List<Pattern> Get() {
      return this.List;
    }

    public void Add(Pattern pattern) {
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
    public string Name = "pattern";
    public string Value = "10:1000";
    public Pattern(string name, string value) {
      this.Name = name;
      this.Value = value;
    }
  }
}
