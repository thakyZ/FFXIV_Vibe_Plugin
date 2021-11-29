using System;

namespace FFXIV_BP {
  [Serializable]
  public class ChatTrigger : IComparable {
    
    public ChatTrigger(int intensity, string text) {
        Intensity = intensity;
        ToMatch = text;
        Text = text;
      }

      public int Intensity { get; }
      public string ToMatch { get; }
      public string Text { get; }

      public override string ToString() {
        return $"Trigger(intensity: {Intensity}, text: '{ToMatch}')";
      }
      public string ToConfigString() {
        return $"{Intensity} {ToMatch}";
      }
      public int CompareTo(object? obj) {
        ChatTrigger? that = obj as ChatTrigger;
        int thatintensity = that != null ? that.Intensity : 0;
        return this.Intensity.CompareTo(thatintensity);
      }
    }
  
}
