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

    public Trigger? CheckTrigger_Spell(Structures.Spell spell) {
      Trigger? triggerFound = null;
      string spellName = "";
      if(spell.name != null) {
        spellName = spell.name.ToLower();
      }

      foreach(Trigger trigger in this.Triggers) {
        // Check if the KIND of the trigger is a spell
        if(trigger.Kind == (int)KIND.Spell ) {
          triggerFound = trigger;
          string triggerSpellText = trigger.SpellText.ToLower();
          if(!spellName.Contains(triggerSpellText) && spellName != "") {
            triggerFound = null;
          }
          
        }
      }
      return triggerFound;
    }

    public void ExecuteTrigger(Trigger trigger) {
      if(trigger != null) {
        this.Logger.Log($"CHAT_TRIGGER:{trigger.Name}");
        //this.DeviceController.SendVibeToAll(0);
      }
    }
  }
  

}
