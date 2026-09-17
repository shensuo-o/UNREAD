namespace MelenitasDev.SoundsGood.Demo
{
    public interface IInteractive
    {
        void Interact ();
    }

    public interface IHoldInteractive : IInteractive
    {
        void BeginInteract ();
        void HoldInteract ();
        void EndInteract ();
    }
}
