using System;

namespace FFXIV_Vibe_Plugin {

  public class SequencerTask {
    public string Command { get; init; }
    public int Duration { get; init; }
    public int _startedTime = 0;

    public SequencerTask(string cmd, int dur) {
      Command = cmd;
      Duration = dur;
    }

    public void play() {
      this._startedTime = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
  }
}
