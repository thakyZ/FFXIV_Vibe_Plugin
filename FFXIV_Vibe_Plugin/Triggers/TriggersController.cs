using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FFXIV_Vibe_Plugin.Triggers;
using FFXIV_Vibe_Plugin.Commons;
using System.Text.RegularExpressions;

namespace FFXIV_Vibe_Plugin.Triggers {
  internal class TriggersController {
    private readonly Logger Logger;
    private readonly PlayerStats PlayerStats;
    private List<Triggers.Trigger> Triggers = new();

    public TriggersController(Logger logger, PlayerStats playerStats) {
      this.Logger = logger;
      this.PlayerStats = playerStats;
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
    
    public List<Trigger> CheckTrigger_Chat(string ChatFromPlayerName, string ChatMsg) {
      List<Trigger> triggers = new();
      ChatFromPlayerName = ChatFromPlayerName.Trim().ToLower();
      for(int triggerIndex = 0; triggerIndex < this.Triggers.Count; triggerIndex++) {
        Trigger trigger = this.Triggers[triggerIndex];
        
        // Ignore if not enabled
        if(!trigger.Enabled) { continue; }

        // Ignore if the player name is not authorized
        if(!Helpers.RegExpMatch(this.Logger, ChatFromPlayerName, trigger.FromPlayerName)) { continue; }

        // Check if the KIND of the trigger is a chat and if it matches
        if(trigger.Kind == (int)KIND.Chat) {
          if(Helpers.RegExpMatch(this.Logger, ChatMsg, trigger.ChatText)){
            triggers.Add(trigger);
          }
        }
      }
      return triggers;
    }

    public List<Trigger> CheckTrigger_Spell(Structures.Spell spell) {
      List<Trigger> triggers = new();
      string spellName = spell.Name != null ? spell.Name.Trim() : "";
      for(int triggerIndex = 0; triggerIndex < this.Triggers.Count; triggerIndex++) { 
        Trigger trigger = this.Triggers[triggerIndex];

        // Ignore if not enabled
        if(!trigger.Enabled) { continue; }

        // Ignore if the player name is not authorized
        if(!Helpers.RegExpMatch(this.Logger, spell.Player.Name, trigger.FromPlayerName)) { continue; }

        if(trigger.Kind == (int)KIND.Spell) {
          
          if(!Helpers.RegExpMatch(this.Logger, spellName, trigger.SpellText)) { continue; }

          if(trigger.ActionEffectType != (int)Structures.ActionEffectType.Nothing && trigger.ActionEffectType != (int)spell.ActionEffectType) {
            continue;
          }

          if(trigger.ActionEffectType == (int)Structures.ActionEffectType.Damage || trigger.ActionEffectType == (int)Structures.ActionEffectType.Heal) {
            if(trigger.AmountMinValue >= spell.AmountAverage) { continue; }
            if(trigger.AmountMaxValue <= spell.AmountAverage) { continue; }
          }

          FFXIV_Vibe_Plugin.Triggers.DIRECTION direction = this.GetSpellDirection(spell);

          if(trigger.Direction != (int)FFXIV_Vibe_Plugin.Triggers.DIRECTION.Any && (int)direction != trigger.Direction) { continue;}

          //this.Logger.Debug($"Sending trigger \"{trigger.Name}\"");
          triggers.Add(trigger);
        }
      }
      return triggers;
    }




    public FFXIV_Vibe_Plugin.Triggers.DIRECTION GetSpellDirection(Structures.Spell spell) {
      string myName = this.PlayerStats.GetPlayerName();
     
      List<Structures.Player> targets = new();
      if(spell.Targets != null) {
        targets = spell.Targets;
      }

      if(targets.Count >= 1 && targets[0].Name != myName) {
        return FFXIV_Vibe_Plugin.Triggers.DIRECTION.Outgoing;
      }
      if(spell.Player.Name != myName) {
        return FFXIV_Vibe_Plugin.Triggers.DIRECTION.Incoming;
      }
      return FFXIV_Vibe_Plugin.Triggers.DIRECTION.Self;
    }
  }

  

}
