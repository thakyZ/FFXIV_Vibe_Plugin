using System;
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
        this.Logger.Info($"Encountered an error loading DamageInfoPlugin: {e.Message}");
        this.Logger.Info("Plugin will not be loaded.");

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
      this.Logger.Info("ReciveActionEffect");
      try {
        uint id = *((uint*)effectHeader.ToPointer() + 0x2);
        uint animId = *((ushort*)effectHeader.ToPointer() + 0xE);
        ushort op = *((ushort*)effectHeader.ToPointer() - 0x7);
        byte targetCount = *(byte*)(effectHeader + 0x21);
        string charName = GetCharacterNameFromSourceId(sourceId);
        this.Logger.Log($"--- {charName}: action id {id}, anim id {animId}, opcode: {op:X} numTargets: {targetCount} ---");
        this.EffectArray(targetCount, effectArray);
        this.EffectTrail(targetCount, effectTrail);
        this.LuminaGet(id);
      } catch(Exception e) {
        this.Logger.Log($"{e.Message} {e.StackTrace}");
      }      
    }

    private void RestoreOriginalHook(int sourceId, IntPtr sourceCharacter, IntPtr pos, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
      receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTrail);
    }

    unsafe private void EffectArray(byte count, IntPtr effectArray) {
      var effect = *(EffectEntry*)(effectArray);
      this.Logger.Log($"{count} {effect.type}");
    }

    unsafe private void EffectTrail(byte count, IntPtr effectTrail) {
      if((int)count >= 1) {
        ulong[] targets = new ulong[(int)count];
        for(int i=0; i < count; i++) {
          targets[i] = *(ulong*)(effectTrail + i * 8);
          var charName = this.GetCharacterNameFromSourceId((int)targets[i]);
          this.Logger.Log($"Targetting: {charName}");
        }
      }
      
    }
 
    private void LuminaGet(uint actionId) {
      var row = this.LuminaActionSheet.GetRow(actionId);
      if(row.Name != null) {
        this.Logger.Log($"--- Spell triggered: {row.Name} (action id: {row.RowId})");
      }
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
