namespace Wof.Application
{
    // Player input is expressed as small interfaces implemented by the states that
    // accept it. A thin GameController forwards a button click to
    // (fsm.Current as IXxxInput)?.OnYyy(), so Unity never decides game rules and an
    // input is simply ignored in any state that doesn't accept it.

    public interface ISpinInput
    {
        void OnSpin();
        void OnLeave();
    }

    public interface ICollectInput
    {
        void OnCollect();          // bank into the run + continue to the next zone
        void OnCollectAndLeave();  // bank into the run + cash out now (safe/super zones only)
    }

    public interface IReviveInput
    {
        void OnReviveGold();
        void OnReviveAd();
        void OnGiveUp();
    }

    public interface ICashOutInput
    {
        void OnConfirm();
    }

    public interface IRestartInput
    {
        void OnRestart();
    }
}
