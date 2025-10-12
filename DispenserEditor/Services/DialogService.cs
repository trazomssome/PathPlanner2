using System.Windows;

namespace DispenserEditor.Services
{
    public sealed class DialogService : IDialogService
    {
        public bool ShowConfirmation(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;
        }

        public void ShowInformation(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
