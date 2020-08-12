using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheaterControl.Interface.Helper
{
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Markup;
    using System.Windows.Threading;

    class EventBindingExtension: MarkupExtension
    {
        #region Fields

        private static readonly object BindingLockObject = new object();

        private static readonly DependencyProperty HelperCommandParameterProperty = DependencyProperty.RegisterAttached(
            nameof(EventBindingExtension.HelperCommandParameterProperty).Replace("Property", string.Empty),
            typeof(object),
            typeof(EventBindingExtension));

        private static readonly DependencyProperty HelperCommandProperty = DependencyProperty.RegisterAttached(
            nameof(EventBindingExtension.HelperCommandProperty).Replace("Property", string.Empty),
            typeof(ICommand),
            typeof(EventBindingExtension));

        private static readonly object QueueLockObject = new object();

        #endregion

        #region Properties

        private CancellationTokenSource CancellationTokenSource { get; set; }

        public Binding Command { get; set; }

        public Binding CommandParameter { get; set; }

        private ICommand CommandReference { get; set; }

        private object DataContext { get; set; }

        /// <summary>
        /// Gets or sets the time (in milliseconds) to wait after firing of events.
        /// </summary>
        public int Delay { get; set; }

        private Action<Task> DelayedExecution { get; set; }

        private Dispatcher Dispatcher { get; set; }

        private DependencyObject Element { get; set; }

        public bool IncludeSenderAndArgsInCommandParameter { get; set; } = true;

        private ConcurrentQueue<object[]> Queue { get; set; }

        #endregion

        #region Methods

        internal virtual void DispatchInvocation(Action action)
        {
            this.Dispatcher.Invoke(action);
        }

        private void ExecuteCommand(ICommand command, object[] args, int delay)
        {
            if (delay <= 0)
            {
                if (this.IncludeSenderAndArgsInCommandParameter)
                {
                    command.Execute(args);
                }
                else
                {
                    command.Execute(args[2]);
                }
            }
            else
            {
                if (this.Queue == null)
                {
                    this.Queue = new ConcurrentQueue<object[]>();
                    this.Dispatcher = Dispatcher.CurrentDispatcher;

                    this.DelayedExecution = t =>
                    {
                        if (!t.IsCanceled)
                        {
                            var items = new List<object[]>();

                            lock (EventBindingExtension.QueueLockObject)
                            {
                                while (this.Queue.TryDequeue(out var item))
                                {
                                    items.Add(item);
                                }
                            }

                            if (items.Count > 0)
                            {
                                if (this.IncludeSenderAndArgsInCommandParameter)
                                {
                                    this.DispatchInvocation(() => command.Execute(items));
                                }
                                else
                                {
                                    var parameters = new List<object>(items.Count);
                                    items.ForEach(x => parameters.Add(x[2]));

                                    this.DispatchInvocation(() => command.Execute(parameters));
                                }
                            }
                        }
                    };
                }

                this.CancellationTokenSource?.Cancel();

                lock (EventBindingExtension.QueueLockObject)
                {
                    this.Queue.Enqueue(args);
                }

                this.CancellationTokenSource = new CancellationTokenSource();
                Task.Delay(this.Delay, this.CancellationTokenSource.Token).ContinueWith(this.DelayedExecution);
            }
        }

        private static ICommand GetHelperCommand(DependencyObject element)
        {
            return (ICommand)element.GetValue(EventBindingExtension.HelperCommandProperty);
        }

        private static object GetHelperCommandParameter(DependencyObject element)
        {
            return element.GetValue(EventBindingExtension.HelperCommandParameterProperty);
        }

        private void Invoked(object sender, EventArgs e)
        {
            var currentDataContext = (this.Element as FrameworkElement)?.DataContext;
            if (this.CommandReference == null || this.DataContext != currentDataContext)
            {
                lock (EventBindingExtension.BindingLockObject)
                {
                    BindingOperations.SetBinding(this.Element, EventBindingExtension.HelperCommandProperty, this.Command);
                    this.CommandReference = EventBindingExtension.GetHelperCommand(this.Element);
                    BindingOperations.ClearBinding(this.Element, EventBindingExtension.HelperCommandProperty);
                    this.DataContext = currentDataContext;
                }
            }

            if (this.CommandReference == null)
            {
                ////throw new ArgumentException($"{nameof(this.Command)} must be bound to a property of {nameof(ICommand)} type.");
                return;
            }

            object[] args = { sender, e };
            if (this.CommandParameter != null)
            {
                object parameter;

                lock (EventBindingExtension.BindingLockObject)
                {
                    BindingOperations.SetBinding(this.Element, EventBindingExtension.HelperCommandParameterProperty, this.CommandParameter);
                    parameter = EventBindingExtension.GetHelperCommandParameter(this.Element);
                    BindingOperations.ClearBinding(this.Element, EventBindingExtension.HelperCommandParameterProperty);
                }

                if (parameter != null)
                {
                    args = new[] { sender, e, parameter };
                }
            }

            if (this.CommandReference.CanExecute(args))
            {
                this.ExecuteCommand(this.CommandReference, args, this.Delay);
            }
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var target = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));

            Delegate handler = null;

            if (target.TargetProperty is EventInfo eventInfo)
            {
                handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, new EventHandler(this.Invoked).Method);
            }
            else if (target.TargetProperty is MethodInfo methodInfo)
            {
                var parameter = methodInfo.GetParameters()[1]; // 0 - dependency object, 1 = event handler
                handler = Delegate.CreateDelegate(parameter.ParameterType, this, new EventHandler(this.Invoked).Method);
            }

            this.Element = (DependencyObject)target.TargetObject;
            if (this.Command == null)
            {
                throw new ArgumentException($"{nameof(this.Command)} must not be null.");
            }

            return handler;
        }

        private static void SetHelperCommand(DependencyObject element, ICommand value)
        {
            element.SetValue(EventBindingExtension.HelperCommandProperty, value);
        }

        private static void SetHelperCommandParameter(DependencyObject element, object value)
        {
            element.SetValue(EventBindingExtension.HelperCommandParameterProperty, value);
        }

        #endregion
    }
}
