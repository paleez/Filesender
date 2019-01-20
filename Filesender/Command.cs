using System;
using System.Windows.Input;

namespace Filesender
{
    class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private Action execute;

        public Command(Action execute)
        {
            this.execute = execute;
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }
        public void Execute(object parameter)
        {
            execute();
        }
    }
}
