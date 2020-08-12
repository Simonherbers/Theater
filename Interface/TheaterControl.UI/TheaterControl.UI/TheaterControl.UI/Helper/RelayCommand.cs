using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.UI.Helper
{
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq.Expressions;
    using System.Windows.Input;
    using System.Windows.Threading;

    public class RelayCommand : ICommand
        {
            private Predicate<object> CanExecutePredicate { get; set; }

            private IDictionary<EventHandler, Dispatcher> EventHandlers { get; set; }

            private Action<object> ExecuteAction { get; set; }

            public event EventHandler CanExecuteChanged
            {
                add
                {
                    CommandManager.RequerySuggested += value;
                    this.EventHandlers.Add(value, Dispatcher.CurrentDispatcher);
                }
                remove
                {
                    CommandManager.RequerySuggested -= value;
                    this.EventHandlers.Remove(value);
                }
            }

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                this.EventHandlers = (IDictionary<EventHandler, Dispatcher>)new Dictionary<EventHandler, Dispatcher>();
                this.ExecuteAction = execute;
                this.CanExecutePredicate = canExecute;
            }

            public RelayCommand(
              Action<object> execute,
              Predicate<object> canExecute,
              INotifyPropertyChanged canExecuteObject,
              Expression<Func<object>> canExecuteProperty)
              : this(execute, canExecute)
            {
                Expression<Func<object, object>> expression = (Expression<Func<object, object>>)(x => string.Empty);
                string[] strArray = RelayCommand.GetName<object>(Expression.Lambda<Func<object, object>>(canExecuteProperty.Body, canExecuteProperty.TailCall, (IEnumerable<ParameterExpression>)expression.Parameters)).Split('.');
                string propertyName = strArray[strArray.Length - 1];
                canExecuteObject.PropertyChanged += (PropertyChangedEventHandler)((s, a) =>
                {
                    if (!(a.PropertyName == propertyName))
                        return;
                    this.Refresh();
                });
            }

            [DebuggerStepThrough]
            public bool CanExecute(object parameter)
            {
                return this.CanExecutePredicate == null || this.CanExecutePredicate(parameter);
            }

            public void Execute(object parameter)
            {
                this.ExecuteAction(parameter);
            }

            public void Refresh()
            {
                foreach (KeyValuePair<EventHandler, Dispatcher> eventHandler in (IEnumerable<KeyValuePair<EventHandler, Dispatcher>>)this.EventHandlers)
                {
                    KeyValuePair<EventHandler, Dispatcher> pair = eventHandler;
                    pair.Value.Invoke((Action)(() => pair.Key((object)this, EventArgs.Empty)));
                }
            }
            private static string GetName<TObject>(Expression<Func<TObject, object>> property)
            {
                return RelayCommand.GetName<TObject, object>(property);
            }
            private static string GetName<TObject, TProperty>(Expression<Func<TObject, TProperty>> property)
            {
                return (property.Body is UnaryExpression ? (property.Body as UnaryExpression).Operand : property.Body) is MemberExpression expression ? RelayCommand.GetName(expression) : (string)null;
            }
            public static string GetName(MemberExpression expression)
            {
                string str = expression.Member.Name;
                if (expression.Expression is MemberExpression expression1)
                    str = string.Format("{0}.{1}", (object)RelayCommand.GetName(expression1), (object)str);
                return str;
            }
        }
    }



