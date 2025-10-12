using DispenserEditor.Services;

namespace DispenserEditor.ViewModels
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            var fileDialogService = new FileDialogService();
            var messageService = new MessageService();

            DispensePath = new DispensePathViewModel(fileDialogService, messageService);
        }

        public DispensePathViewModel DispensePath { get; }
    }
}
