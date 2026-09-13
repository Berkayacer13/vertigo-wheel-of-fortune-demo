using System;
using Wof.Application;
using Wof.Domain;

namespace Wof.Presentation
{
    /// <summary>
    /// The wheel's half of the game: draws each built wheel, headlines the zone, gates SPIN and
    /// LEAVE by phase, marks the chamber the spin landed on, and turns the two buttons into
    /// state inputs.
    /// </summary>
    public sealed class WheelPresenter : IDisposable
    {
        private readonly GameEvents _events;
        private readonly WheelView _view;
        private bool _canLeave;

        public WheelPresenter(GameEvents events, StateInput input, WheelView view)
        {
            _events = events;
            _view = view;

            _view.BindInput(onSpin: () => input.Press<ISpinInput>(s => s.OnSpin()),
                            onLeave: () => input.Press<ISpinInput>(s => s.OnLeave()));

            _events.WheelBuilt += _view.Render;
            _events.ZoneChanged += OnZoneChanged;
            _events.SpinLandedOnIndex += _view.HighlightSlice;
            _events.PhaseChanged += OnPhaseChanged;
        }

        public void Dispose()
        {
            _events.WheelBuilt -= _view.Render;
            _events.ZoneChanged -= OnZoneChanged;
            _events.SpinLandedOnIndex -= _view.HighlightSlice;
            _events.PhaseChanged -= OnPhaseChanged;
        }

        private void OnZoneChanged(ZoneInfo zone)
        {
            _canLeave = zone.CanLeave;
            _view.SetZone(zone.Type);
            _view.SetLeavePrompt(zone.ZonesUntilLeave);
            _view.SetLeaveEnabled(false); // re-enabled once we settle into Idle
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            bool idle = phase == GamePhase.Idle;
            _view.SetSpinEnabled(idle);
            _view.SetLeaveEnabled(idle && _canLeave);
        }
    }
}
