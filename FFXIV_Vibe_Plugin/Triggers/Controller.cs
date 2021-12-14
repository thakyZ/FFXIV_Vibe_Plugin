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
          // WARNING: ChatMessage is always lowercase !
          triggerChatText = triggerChatText.ToLower();
          if(ChatMsg.Contains(triggerChatText)){
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
          spellName = spellName.ToLower();
          string triggerSpellText = trigger.SpellText.ToLower();
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
