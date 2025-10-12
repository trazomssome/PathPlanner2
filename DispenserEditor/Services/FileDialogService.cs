using Microsoft.Win32;

namespace DispenserEditor.Services
{
    public class FileDialogService : IFileDialogService
    {
        public string ShowOpenFileDialog(string filter, string title)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                CheckFileExists = true
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
