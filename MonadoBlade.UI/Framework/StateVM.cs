using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonadoBlade.UI.Framework
{
    /// <summary>
    /// Base class for all ViewModels with reactive property binding support.
    /// Implements INotifyPropertyChanged for WPF/WinUI binding.
    /// </summary>
    public class StateVM : INotifyPropertyChanged
    {
        private Dictionary<string, object> _propertyValues = new();
        
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a property value with automatic change notification.
        /// </summary>
        protected T GetProperty<T>(string propertyName)
        {
            if (_propertyValues.TryGetValue(propertyName, out var value))
            {
                return (T)value;
            }
            return default(T);
        }

        /// <summary>
        /// Sets a property value with automatic change notification.
        /// Only notifies if the value actually changed.
        /// </summary>
        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets a property in the property values dictionary with automatic change notification.
        /// </summary>
        protected bool SetProperty<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var hasValue = _propertyValues.TryGetValue(propertyName, out var oldValue);
            
            if (hasValue && EqualityComparer<T>.Default.Equals((T)oldValue, value))
                return false;

            _propertyValues[propertyName] = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Manually raise the PropertyChanged event for the specified property.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raise PropertyChanged for multiple properties at once.
        /// </summary>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                OnPropertyChanged(name);
            }
        }
    }

    /// <summary>
    /// Represents the loading state of an async operation.
    /// </summary>
    public enum LoadingState
    {
        Idle,
        Loading,
        Success,
        Error
    }

    /// <summary>
    /// Base class for ViewModels that handle async operations with loading states.
    /// </summary>
    public class AsyncStateVM : StateVM
    {
        private LoadingState _loadingState = LoadingState.Idle;
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        public LoadingState LoadingState
        {
            get => _loadingState;
            set => SetProperty(ref _loadingState, value, nameof(LoadingState));
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value, nameof(ErrorMessage));
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, nameof(IsLoading));
        }

        /// <summary>
        /// Mark operation as loading.
        /// </summary>
        public void SetLoading()
        {
            LoadingState = LoadingState.Loading;
            IsLoading = true;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Mark operation as successful.
        /// </summary>
        public void SetSuccess()
        {
            LoadingState = LoadingState.Success;
            IsLoading = false;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Mark operation as failed with an error message.
        /// </summary>
        public void SetError(string message)
        {
            LoadingState = LoadingState.Error;
            IsLoading = false;
            ErrorMessage = message ?? "An error occurred";
        }

        /// <summary>
        /// Reset to idle state.
        /// </summary>
        public void SetIdle()
        {
            LoadingState = LoadingState.Idle;
            IsLoading = false;
            ErrorMessage = string.Empty;
        }
    }

    /// <summary>
    /// Base class for commands implementing the Command pattern.
    /// </summary>
    public abstract class CommandBase : StateVM
    {
        public event EventHandler CanExecuteChanged;

        public virtual bool CanExecute(object parameter) => true;

        public virtual void Execute(object parameter)
        {
            OnCanExecuteChanged();
        }

        protected void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Simple implementation of ICommand for binding to buttons.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);
    }
}
