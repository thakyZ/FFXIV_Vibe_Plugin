using System;
using Dalamud.Game.ClientState;

namespace FFXIV_Vibe_Plugin {
  
  internal class PlayerStats {
    readonly Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter? localPlayer;

    // EVENTS
    public event EventHandler? Event_CurrentHpChanged;
    public event EventHandler? Event_MaxHpChanged;

    // Stats of the player
    private float _CurrentHp, _prevCurrentHp = 0;
    private float _MaxHp, _prevMaxHp = 0;

    public PlayerStats( ClientState clientState) {
      if(clientState != null && clientState.LocalPlayer != null) {
        this.localPlayer = clientState.LocalPlayer;

        // Init variables
        this._CurrentHp = this._prevCurrentHp = this.localPlayer.CurrentHp;
        this._MaxHp = this._prevMaxHp = this.localPlayer.MaxHp;
      }
    }

    public void Update() {
      if(this.localPlayer == null) { return;  }
      this.UpdateCurrentHp();
    }

    public string GetPlayerName() {
      string playerName = "*undefined*";
      if(this.localPlayer != null) {
        playerName = this.localPlayer.Name.TextValue;
      }
      return playerName;
    }

    private void UpdateCurrentHp() {

      // Updating current values
      if(this.localPlayer != null) {
        this._CurrentHp = this.localPlayer.CurrentHp;
        this._MaxHp = this.localPlayer.MaxHp;
      }

      // Send events after all value updated
      if(this._CurrentHp != this._prevCurrentHp) {
        Event_CurrentHpChanged?.Invoke(this, EventArgs.Empty);
      }
      if(this._MaxHp != this._prevMaxHp) {
        Event_MaxHpChanged?.Invoke(this, EventArgs.Empty);
      }

      // Save previous values
      this._prevCurrentHp = this._CurrentHp;
      this._prevMaxHp = this._MaxHp;

    }

    /***** PUBLIC API ******/
    public float GetCurrentHP() {
      return this._CurrentHp;
    }

    public float GetMaxHP() {
      return this._MaxHp;
    }
  }
}
