using System;
using Wof.Application;

namespace Wof.Presentation
{
    /// <summary>
    /// The one door from the UI into the game rules: hands a player input to the active
    /// state if, and only if, that state implements the matching input interface. Buttons
    /// and keys both come through here, so no screen can reach an action the current state
    /// would not offer, and the click sound is decided in exactly one place.
    /// </summary>
    public sealed class StateInput
    {
        private readonly GameStateMachine _fsm;
        private readonly Action _click;

        public StateInput(GameStateMachine fsm, Action click)
        {
            _fsm = fsm;
            _click = click ?? (() => { });
        }

        /// <summary>Click feedback for a press that is not a game input, like opening the stash.</summary>
        public void Click() => _click();

        /// <summary>Plays the click, then routes the input to the active state if it accepts it.</summary>
        public void Press<T>(Action<T> action) where T : class
        {
            _click();
            if (_fsm.Current is T input) action(input);
        }

        /// <summary>
        /// Like <see cref="Press{T}"/>, but silent when the active state does not accept the
        /// input. A button can only be clicked while it is interactable; a key cannot, so
        /// without this SPACE would click-click-click its way through a spin.
        /// </summary>
        public bool TryPress<T>(Action<T> action) where T : class
        {
            if (!(_fsm.Current is T input)) return false;
            _click();
            action(input);
            return true;
        }
    }
}
