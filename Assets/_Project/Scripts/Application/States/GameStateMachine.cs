namespace Wof.Application
{
    /// <summary>Drives the active <see cref="IGameState"/>. The only place transitions happen.</summary>
    public sealed class GameStateMachine
    {
        public IGameState Current { get; private set; }

        public void Change(IGameState next)
        {
            Current?.Exit();
            Current = next;
            Current?.Enter();
        }

        public void Tick(float dt) => Current?.Tick(dt);
    }
}
