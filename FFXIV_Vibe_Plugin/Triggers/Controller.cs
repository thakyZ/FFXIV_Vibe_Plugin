using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Triggers;
using FFXIV_Vibe_Plugin.Commons;
using System.Text.RegularExpressions;

namespace FFXIV_Vibe_Plugin.Triggers {
  internal class Controller {
    private readonly Logger Logger;
    private List<Triggers.Trigger> Triggers = new();

    public Controller(Logger logger) {
      this.Logger = logger;
    }

    public void Set(List<Triggers.Trigger> triggers) {
      this.Triggers = triggers;
    }

    public List<Triggers.Trigger> GetTriggers() {
      return this.Triggers;
    }

    public void AddTrigger(Trigger trigger) {
      this.Triggers.Add(trigger);
    }

    public void RemoveTrigger(Trigger trigger) {
      this.Triggers.Remove(trigger);
    }

    public List<Trigger> CheckTrigger_Chat(string fromPlayerName, string ChatMsg) {
      List<Trigger> triggers = new();
      fromPlayerName = fromPlayerName.Trim().ToLower();
      foreach(Trigger trigger in this.Triggers) {
        string triggerFromPlayerName = trigger.FromPlayerName.Trim().ToLower();
        bool isAuthorized = triggerFromPlayerName == "" || fromPlayerName.Contains(trigger.FromPlayerName);
        // Check if the KIND of the trigger is a chat and if the author is authorized
        if(trigger.Kind == (int)KIND.Chat && isAuthorized) {
          // WARNING: ChatMessage received from hook is always lowercase !
          string pattern = String.Concat(@"", trigger.ChatText);
          try {
            Match m = Regex.Match(ChatMsg, pattern, RegexOptions.IgnoreCase);
            if(m.Success) {
              triggers.Add(trigger);
            }
          } catch(Exception) {
            this.Logger.Error($"Probably a wrong REGEXP for {trigger.ChatText}");
          }
        }
      }
      return triggers;
    }

    public List<Trigger> CheckTrigger_Spell(Structures.Spell spell) {
      List<Trigger> triggers = new();
      Structures.Player fromPlayerName = spell.Player;
      string spellName = "";
      if(spell.Name != null) {
        spellName = spell.Name.Trim().ToLower();
      }

      foreach(Trigger trigger in this.Triggers) {
        string triggerFromPlayerName = trigger.FromPlayerName.Trim().ToLower();
        bool isAuthorized = triggerFromPlayerName == "" || fromPlayerName.Name.Contains(trigger.FromPlayerName);
        // Check if the KIND of the trigger is a spell and if author is authorized
        if(trigger.Kind == (int)KIND.Spell && isAuthorized) {
          string pattern = String.Concat(@"", trigger.SpellText);
          try {
            Match m = Regex.Match(spellName, pattern, RegexOptions.IgnoreCase);
            if(m.Success) {
              triggers.Add(trigger);
            }
          } catch(Exception) {
            this.Logger.Error($"Probably a wrong REGEXP for {trigger.SpellText}");
          }

        }
      }
      return triggers;
    }

    public void ExecuteTrigger(Trigger trigger) {
      if(trigger != null) {
        this.Logger.Log($"CHAT_TRIGGER:{trigger.Name}");
        //this.DeviceController.SendVibeToAll(0);
      }
    }
  }
  

}
