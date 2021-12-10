using System;
using System.Collections.Generic;

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Lumina;
using Lumina.Excel;

using FFXIV_Vibe_Plugin.Commons;

namespace FFXIV_Vibe_Plugin.Experimental {
  internal class HookActionEffect {
    private readonly Logger Logger;
    private readonly SigScanner scanner;
    private delegate void ReceiveActionEffectDelegate(int sourceId, IntPtr sourceCharacter, IntPtr pos,
            IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
    private Hook<ReceiveActionEffectDelegate> receiveActionEffectHook;
    private readonly Lumina.GameData Lumina;
    private readonly Lumina.Excel.ExcelSheet<Lumina.Excel.GeneratedSheets.Action> LuminaActionSheet;
    private readonly ClientState clientState;
    private readonly ObjectTable gameObjects;

    public HookActionEffect(Logger logger, SigScanner scanner, ClientState clientState, ObjectTable gameObjects) {
      this.Logger = logger;
      this.scanner = scanner;
      this.clientState = clientState;
      this.gameObjects = gameObjects;
      this.InitHook();
      this.Lumina = new Lumina.GameData("F:/Games/FFXIV Online/SquareEnix/FINAL FANTASY XIV - A Realm Reborn/game/sqpack"); // TODO: use dalamud to get the path ?
      this.LuminaActionSheet = this.Lumina.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>();
    }

    public void Dispose() {
      receiveActionEffectHook?.Disable();
      receiveActionEffectHook?.Dispose();
    }
    
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
      StartActionCombo = 27,
      ComboSucceed = 28,
      Knockback = 33,
      Mount = 40,
      VFX = 59,
    };

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
      public ActionEffectType type;
      public byte param0;
      public byte param1;
      public byte param2;
      public byte mult;
      public byte flags;
      public ushort value;

      public override string ToString() {
        return
            $"Type: {type}, p0: {param0}, p1: {param1}, p2: {param2}, mult: {mult}, flags: {flags} | {Convert.ToString(flags, 2)}, value: {value}";
      }
    }

    private void InitHook() {
      try {
        this.Logger.Log("Hooking ActionEffect");
        IntPtr receiveActionEffectFuncPtr = this.scanner.ScanText("4C 89 44 24 18 53 56 57 41 54 41 57 48 81 EC ?? 00 00 00 8B F9");
        receiveActionEffectHook = new Hook<ReceiveActionEffectDelegate>(receiveActionEffectFuncPtr, (ReceiveActionEffectDelegate)ReceiveActionEffect);
        this.Logger.Log("Hooking ActionEffect => ok");
        /**
        IntPtr setCastBarFuncPtr = this.scanner.ScanText(
            "48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 80 7C 24 ?? ?? 49 8B D9 49 8B E8 48 8B F2 74 22 49 8B 09 66 41 C7 41 ?? ?? ?? E8 ?? ?? ?? ?? 66 83 F8 69 75 0D 48 8B 0B BA ?? ?? ?? ?? E8 ?? ?? ?? ??");
        setCastBarHook = new Hook<SetCastBarDelegate>(setCastBarFuncPtr, (SetCastBarDelegate)SetCastBarDetour);

        IntPtr setFocusTargetCastBarFuncPtr = scanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 41 0F B6 F9 49 8B E8 48 8B F2 48 8B D9");
        setFocusTargetCastBarHook = new Hook<SetCastBarDelegate>(setFocusTargetCastBarFuncPtr, (SetCastBarDelegate)SetFocusTargetCastBarDetour);

        ftGui.FlyTextCreated += OnFlyTextCreated;*/
      } catch(Exception e) {
        this.Logger.Warn($"Encountered an error loading HookActionEffect: {e.Message}. Disabling it...");

        receiveActionEffectHook?.Disable();
        receiveActionEffectHook?.Dispose();
        /*setCastBarHook?.Disable();
        setCastBarHook?.Dispose();
        setFocusTargetCastBarHook?.Disable();
        setFocusTargetCastBarHook?.Dispose();
        cmdMgr.RemoveHandler(CommandName);*/

        throw;
      }
      
      receiveActionEffectHook.Enable();
    }


    unsafe private void ReceiveActionEffect(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
      try {
        uint id = *((uint*)effectHeader.ToPointer() + 0x2);
        uint animId = *((ushort*)effectHeader.ToPointer() + 0xE);
        ushort op = *((ushort*)effectHeader.ToPointer() - 0x7);
        byte targetCount = *(byte*)(effectHeader + 0x21);
        string charName = GetCharacterNameFromSourceId(sourceId);
        String spellName = this.GetSpellName(id, true);
        String allTargets = this.GetAllTarget(targetCount, effectTrail);
        String type = this.GetSpellType(targetCount, effectArray);
        int amount = this.GetAmount(targetCount, effectArray);
        this.Logger.Log($"{charName} cast '{spellName}' (type:{type}) targetting '{allTargets}' (Number of targets: {targetCount})");
        // DEBUG: this.Logger.Log($"--- {charName}: action id {id}, anim id {animId}, opcode: {op:X} numTargets: {targetCount} ---");

      } catch(Exception e) {
        this.Logger.Log($"{e.Message} {e.StackTrace}");
      }
      receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
    }

    private void RestoreOriginalHook(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
      receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
    }

    unsafe private int GetAmount(byte count, IntPtr effectArray) {
      int targetCount = (int)count;
      this.Logger.Info($"TARGET_COUNT: {targetCount}");
      int effectsEntries = 0;
      int targetEntries = 1;
      if(targetCount == 0) {
        effectsEntries = 0;
        targetEntries = 1;
      } else if(targetCount == 1) {
        effectsEntries = 8;
        targetEntries = 1;
      } else if(targetCount <= 8) {
        effectsEntries = 64;
        targetEntries = 8;
      } else if(targetCount <= 16) {
        effectsEntries = 128;
        targetEntries = 16;
      } else if(targetCount <= 24) {
        effectsEntries = 192;
        targetEntries = 24;
      } else if(targetCount <= 32) {
        effectsEntries = 256;
        targetEntries = 32;
      }

      List<EffectEntry> entries = new List<EffectEntry>(effectsEntries);

      for(int i = 0; i < effectsEntries; i++) {
        entries.Add(*(EffectEntry*)(effectArray + i * 8));
      }


      // Experimental way to sum up all the damange values when doing AOE spells
      for(int i = 0; i < entries.Count; i++) {
        if(i % 8 == 0) { // Value of dmg is located every 8
          uint tDmg = entries[i].value;
          if(entries[i].mult != 0) {
            tDmg += ((uint)ushort.MaxValue + 1) * entries[i].mult;
          }
          this.Logger.Log($"Testing effectentry: {tDmg}");
        }
      }


      
      return 0;
    }

    unsafe private string GetSpellType(byte count, IntPtr effectArray) {
      var effect = *(EffectEntry*)(effectArray);
      return effect.type.ToString();
    }

    unsafe private string GetAllTarget(byte count, IntPtr effectTrail) {
      List<String> names = new List<String>();
      if((int)count >= 1) {
        ulong[] targets = new ulong[(int)count];
        for(int i=0; i < count; i++) {
          targets[i] = *(ulong*)(effectTrail + i * 8);
          var charName = this.GetCharacterNameFromSourceId((int)targets[i]);
          names.Add(charName);
        }
      }
      return String.Join(",", names);
      
    }
 
    private string GetSpellName(uint actionId, bool withId) {
      var row = this.LuminaActionSheet.GetRow(actionId);
      var spellName = "";
      if(row != null) { 
        if(withId) {
          spellName = $"{row.RowId}:";
        }
        if(row.Name != null) {
          spellName += $"{row.Name}";
        }
      } else {
        spellName = "!Unknown Spell Name!";
      }
      return spellName;
    }

    private string GetCharacterNameFromSourceId(int sourceId) {
      var character = this.gameObjects.SearchById((uint)sourceId);
      var characterName = "";
      if(character != null) {
        characterName = character.Name.TextValue;
      }
      return characterName;
    }
  }
}
