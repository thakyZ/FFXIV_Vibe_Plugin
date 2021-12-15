using System;
using System.Collections.Generic;

namespace FFXIV_Vibe_Plugin.Commons {
  internal class Structures {

    public enum ActionEffectType : byte {
      Nothing = 0,
      Miss = 1,
      FullResist = 2,
      Damage = 3,
      Heal = 4,
      BlockedDamage = 5,
      ParriedDamage = 6,
      Invulnerable = 7,
      NoEffectText = 8,
      Unknown_0 = 9,
      MpLoss = 10,
      MpGain = 11,
      TpLoss = 12,
      TpGain = 13,
      GpGain = 14,
      ApplyStatusEffectTarget = 15,
      ApplyStatusEffectSource = 16,
      StatusNoEffect = 20,
      Taunt = 24,
      StartActionCombo = 27,
      ComboSucceed = 28,
      Knockback = 33,
      Mount = 40,
      VFX = 59,
      Transport = 60,
    };

    // Unused, should be usefull for HooActionEffects but don't know where this field is.
    public enum DamageType {
      Unknown = 0,
      Slashing = 1,
      Piercing = 2,
      Blunt = 3,
      Magic = 5,
      Darkness = 6,
      Physical = 7,
      LimitBreak = 8,
    }


    public struct EffectEntry {
      public ActionEffectType type = ActionEffectType.Nothing;
      public byte param0 = 0;
      public byte param1 = 0;
      public byte param2 = 0;
      public byte mult = 0;
      public byte flags = 0;
      public ushort value = 0;
      public override string ToString() {
        return $"Type: {this.type}, p0: {param0}, p1: {param1}, p2: {param2}, mult: {mult}, flags: {flags} | {Convert.ToString(flags, 2)}, value: {value}";
      }
    }

    public struct Player {
      public int Id;
      public string Name;
      public string? Info;
      public Player(int id, string name, string? info=null) {
        this.Id = id;
        this.Name = name;
        this.Info = info;
      }

      public override string ToString() {
        if(this.Info != null) {
          return $"{Name}({Id}) [info:{this.Info}]";
        }
        return $"{Name}({Id})";
      }
    }

    public struct Spell {
      public int Id;
      public string Name = "Undefined_Spell_Name";
      public Player Player;
      public int[]? Amounts;
      public float AmountAverage;
      public List<Player>? Targets;
      public DamageType DamageType = 0;
      public ActionEffectType Type;
      public override string ToString() {
        string targetsString = "";
        if(Targets != null) {
          if(Targets.Count > 0) {
            targetsString = String.Join(",", this.Targets);
          } else {
            targetsString = "*no target*";
          }
        }
        return $"{Player} casts {Name}#{Type} on: {targetsString}. Avg: {AmountAverage}";
      }
    }
  }

}
