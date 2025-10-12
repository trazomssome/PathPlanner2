namespace DispenserEditor.Services
{
    public interface IDialogService
    {
        bool ShowConfirmation(string title, string message);
        void ShowInformation(string title, string message);
    }
}
