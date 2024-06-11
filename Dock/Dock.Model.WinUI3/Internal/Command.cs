using System;
using System.Windows.Input;

namespace Dock.Model.WinUI3.Internal
{
    public class Command : ICommand
    {
        public static ICommand Create(Delegate execute) => new Command(execute);

        public static ICommand Create(Delegate execute, Func<bool> canExecute) => new Command(execute, canExecute);

        private readonly Delegate _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public Command(Delegate execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (() => true);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
            {
                _execute.DynamicInvoke();
            }
            else
            {
                _execute.DynamicInvoke(parameter);
            }
        }

        /// <summary>
        /// Method used to raise the <see cref="CanExecuteChanged"/> event
        /// to indicate that the return value of the <see cref="CanExecute"/>
        /// method has changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
