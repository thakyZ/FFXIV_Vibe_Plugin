using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Triggers;
using FFXIV_Vibe_Plugin.Commons;

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

    public Trigger? CheckTrigger_Chat(string ChatMsg) {
      Trigger? triggerFound = null;
      foreach(Trigger trigger in this.Triggers) {
        if(trigger.Kind == (int)KIND.Chat) {
          string triggerChatText = trigger.ChatText;
          if(!trigger.caseInsensitive) {
            ChatMsg = ChatMsg.ToLower();
            triggerChatText = triggerChatText.ToLower();
          }
          if(ChatMsg.Contains(triggerChatText)){
            Logger.Log($"{trigger.Name}:{ChatMsg} {triggerChatText}");
            triggerFound = trigger;
          }
        }
      }
      return triggerFound;
    }

    public Trigger? CheckTrigger_Spell(Structures.Spell spell) {
      Trigger? triggerFound = null;
      string spellName = spell.name;
      foreach(Trigger trigger in this.Triggers) {
        if(trigger.Kind == (int)KIND.Spell) {
          string triggerSpellText = trigger.SpellText;
          if(!trigger.caseInsensitive) {
            spellName = spellName.ToLower();
            triggerSpellText = triggerSpellText.ToLower();
          }
          if(spellName.Contains(triggerSpellText)) {
            Logger.Log($"{trigger.Name}:{spellName} {triggerSpellText}");
            triggerFound = trigger;
          }
        }
      }
      return triggerFound;
    }
  }
  

}
