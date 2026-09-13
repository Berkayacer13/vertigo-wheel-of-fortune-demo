using UnityEngine;
using Wof.Application;

namespace Wof.Presentation
{
    /// <summary>
    /// Key bindings: SPACE spins, ENTER confirms whatever screen is up, back closes the stash.
    /// Keys go through the same <see cref="StateInput"/> the buttons use, so the keyboard can
    /// never reach an action the on-screen UI would not offer right now.
    /// </summary>
    public sealed class KeyboardInput
    {
        private readonly StateInput _input;
        private readonly InventoryPresenter _inventory;

        public KeyboardInput(StateInput input, InventoryPresenter inventory)
        {
            _input = input;
            _inventory = inventory;
        }

        /// <summary>Read this frame's key presses. Call once per frame.</summary>
        public void Tick()
        {
            // Android maps the hardware/gesture back button to Escape. It only ever closes
            // the stash here — a back press that quietly quit the app mid-run would throw
            // away everything the player had staked.
            if (Input.GetKeyDown(KeyCode.Escape) && _inventory.CloseIfOpen()) return;

            if (Input.GetKeyDown(KeyCode.Space))
                _input.TryPress<ISpinInput>(s => s.OnSpin());

            if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter)) return;

            // Exactly one state is active, so at most one of these fires. The bomb screen is
            // deliberately missing: reviving spends gold and giving up ends the run, and
            // neither belongs on a key the player is already mashing to dismiss popups.
            if (_input.TryPress<ICollectInput>(s => s.OnCollect())) return;
            if (_input.TryPress<ICashOutInput>(s => s.OnConfirm())) return;
            _input.TryPress<IRestartInput>(s => s.OnRestart());
        }
    }
}
