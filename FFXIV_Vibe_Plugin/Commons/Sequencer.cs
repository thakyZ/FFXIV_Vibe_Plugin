using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIV_Vibe_Plugin.Commons {
  internal class Sequencer {
    private readonly Logger Logger;
    private List<SequencerTask> SequencerTasks = new();

    private readonly bool playSequence = true;

    public Sequencer(Logger logger) {
      this.Logger = logger;
    }

    public void RunSequencer(List<SequencerTask> sequencerTasks) {
      if(sequencerTasks != null) {
        this.SequencerTasks = sequencerTasks;
      }

      if(this.playSequence && this.SequencerTasks.Count > 0) {

        SequencerTask st = this.SequencerTasks[0];

        if(st._startedTime == 0) {
          st.Play();
          string[] commandSplit = st.Command.Split(':', 2);
          string task = commandSplit[0];
          string param1 = commandSplit.Length > 1 ? commandSplit[1] : "";
          this.Logger.Debug($"Playing sequence: {task} {param1}");
          if(task == "connect") {
            // TODO: this.Command_DeviceController_Connect();
          } else if(task == "buttplug_sendVibe") {
            float intensity = float.Parse(param1);
            // TODO: this.Buttplug_sendVibe(intensity);
          } else if(task == "print") {
            this.Logger.Chat(param1);
          } else if(task == "print_debug") {
            this.Logger.Debug(param1);
          } else if(task == "nothing") {
            // do nothing
          } else {
            this.Logger.Debug($"Sequencer task unknown: {task} {param1}");
          }
        }

        if(st._startedTime + st.Duration < Helpers.GetUnix()) {
          this.SequencerTasks[0]._startedTime = 0;
          this.SequencerTasks.RemoveAt(0);
        }
      }
    }
  }

  /** A task that can be executed by the Sequencer */
  public class SequencerTask {
    public string Command { get; init; }
    public int Duration { get; init; }
    public int _startedTime = 0;

    public SequencerTask(string cmd, int dur) {
      Command = cmd;
      Duration = dur;
    }

    public void Play() {
      this._startedTime = (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
  }
}
