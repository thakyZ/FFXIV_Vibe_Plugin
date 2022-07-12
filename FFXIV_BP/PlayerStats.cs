using System;

using Dalamud.Game.ClientState;

namespace FFXIV_Vibe_Plugin {

  internal class PlayerStats {
    Dalamud.Game.ClientState.Objects.SubKinds.PlayerCharacter localPlayer;

    // EVENTS
    public event EventHandler event_CurrentHpChanged;
    public event EventHandler event_MaxHpChanged;

    // Stats of the player
    private float _CurrentHp, _prevCurrentHp = 0;
    private float _MaxHp, _prevMaxHp = 0;

    public PlayerStats(ClientState clientState) {
      if (clientState != null && clientState.LocalPlayer != null) {
        this.localPlayer = clientState.LocalPlayer;

        // Init variables
        this._CurrentHp = this._prevCurrentHp = this.localPlayer.CurrentHp;
        this._MaxHp = this._prevMaxHp = this.localPlayer.MaxHp;
      }
    }

    public void update() {
      if (this.localPlayer == null) { return; }
      this._updateCurrentHp();
    }

    private void _updateCurrentHp() {

      // Updating current values
      this._CurrentHp = this.localPlayer.CurrentHp;
      this._MaxHp = this.localPlayer.MaxHp;

      // Send events after all value updated
      if (this._CurrentHp != this._prevCurrentHp) {
        event_CurrentHpChanged?.Invoke(this, EventArgs.Empty);
      }
      if (this._MaxHp != this._prevMaxHp) {
        event_MaxHpChanged?.Invoke(this, EventArgs.Empty);
      }

      // Save previous values
      this._prevCurrentHp = this._CurrentHp;
      this._prevMaxHp = this._MaxHp;

    }

    /***** PUBLIC API ******/
    public float getCurrentHP() {
      return this._CurrentHp;
    }

    public float getMaxHP() {
      return this._MaxHp;
    }
  }
}
