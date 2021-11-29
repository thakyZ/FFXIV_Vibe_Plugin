using System;

namespace FFXIV_Vibe_Plugin {

  public class SequencerTask {
    public string command { get; init; }
    public int duration { get; init; }
    public int _startedTime = 0;

    public SequencerTask(string cmd, int dur) {
      command = cmd;
      duration = dur;
    }

    public void play() {
      this._startedTime = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
  }
}
