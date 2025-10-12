namespace DispenserEditor.Services
{
    public interface IMessageService
    {
        bool Confirm(string title, string message);
        void ShowInfo(string title, string message);
        void ShowError(string title, string message);
    }
}
