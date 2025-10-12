using DispenserEditor.Services;

namespace DispenserEditor.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        public MainViewModel()
            : this(new DialogService())
        {
        }

        public MainViewModel(IDialogService dialogService)
        {
            DispensePath = new DispensePathViewModel(dialogService);
        }

        public DispensePathViewModel DispensePath { get; }
    }
}
