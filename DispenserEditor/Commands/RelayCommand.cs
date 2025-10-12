using System;
using System.Windows.Input;

namespace DispenserEditor.Commands
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action execute)
            : this(_ => execute(), _ => true)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
            : this(_ => execute(), _ => canExecute())
        {
        }

        public RelayCommand(Action<object> execute)
            : this(execute, _ => true)
        {
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
