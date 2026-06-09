namespace Wof.Application
{
    /// <summary>A single screen/phase of the game. Each state owns what the player can do.</summary>
    public interface IGameState
    {
        void Enter();
        void Tick(float dt);
        void Exit();
    }
}
