using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace FFXIV_Vibe_Plugin.Commons {

  internal class Logger {
    // Initialize the Dalamud.Gui system.
    private Dalamud.Game.Gui.ChatGui? DalamudChatGui { get; init; }

    // Name used as prefix.
    private readonly string name;

    // Current log level.
    private readonly LogLevel log_level = LogLevel.INFO;

    // Available log levels.
    public enum LogLevel {
      DEBUG, LOG, INFO, WARN, ERROR, FATAL,
    }

    /** Constructor */
    public Logger(string name, LogLevel log_level) {
      this.name = name;
      this.log_level = log_level;
    }

    /** Printing in the chat gui a message. */
    public void Chat(string msg) {
      DalamudChatGui?.Print($"{this.name}▹{msg}");
    }

    /** Printing in the chat gui an error message. */
    public void ChatError(string msg) {
      DalamudChatGui?.PrintError(msg);
      this.Error(msg);
    }

    /** Printing in the chat gui an error message with an exception. */
    public void ChatError(string msg, Exception e) {
      string m = $"{this.name} ERROR▹{msg}\n{e}";
      DalamudChatGui?.PrintError(m);
      this.Error(m);
    }

    /** Log message as 'debug' to logs. */
    public void Debug(string msg) {
      if(this.log_level > LogLevel.DEBUG) { return; }
      Dalamud.Logging.PluginLog.LogDebug(msg);
    }

    /** Log message as 'log' to logs. */
    public void Log(string msg) {
      if(this.log_level > LogLevel.LOG) { return; }
      Dalamud.Logging.PluginLog.Log(msg);
    }

    /** Log message as 'info' to logs. */
    public void Info(string msg) {
      if(this.log_level > LogLevel.INFO) { return; }
      Dalamud.Logging.PluginLog.Information(msg);
    }

    /** Log message as 'warning' to logs. */
    public void Warn(string msg) {
      if(this.log_level > LogLevel.WARN) { return; }
      Dalamud.Logging.PluginLog.Warning(msg);
    }

    /** Log message as 'error' to logs. */
    public void Error(string msg) {
      if(this.log_level > LogLevel.ERROR) { return; }
      Dalamud.Logging.PluginLog.Error(msg);
    }

    /** Log message as 'error' to logs with an exception. */
    public void Error(Exception e, string msg) {
      if(this.log_level > LogLevel.ERROR) { return; }
      Dalamud.Logging.PluginLog.Error($"{msg}\n{e}");
    }

    /** Log message as 'fatal' to logs. */
    public void Fatal(string msg) {
      if(this.log_level > LogLevel.FATAL) { return; }
      Dalamud.Logging.PluginLog.Fatal(msg);
    }

    /** Log message as 'fatal' to logs with an exception. */
    public void Fatal(Exception e, string msg) {
      if(this.log_level > LogLevel.FATAL) { return; }
      Dalamud.Logging.PluginLog.Fatal($"{msg}\n{e}");
    }
  }
}
