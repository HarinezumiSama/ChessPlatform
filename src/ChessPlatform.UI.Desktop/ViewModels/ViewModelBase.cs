using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Threading;
using Omnifactotum.Annotations;
using Omnifactotum.Validation;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal abstract class ViewModelBase : INotifyPropertyChanged
    {
        #region Constants and Fields

        private const string InvalidExpressionMessageAutoFormat =
            "Invalid expression (must be a getter of a property of the type '{0}'): {{ {1} }}.";

        private readonly Dispatcher _dispatcher;
        private readonly Dictionary<string, List<EventHandler>> _propertyChangedSubscriptionsDictionary;

        #endregion

        #region Constructors

        protected ViewModelBase()
        {
            _dispatcher = Dispatcher.CurrentDispatcher.EnsureNotNull();
            _propertyChangedSubscriptionsDictionary = new Dictionary<string, List<EventHandler>>();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Public Methods

        public void SubscribeToChangeOf<TProperty>(
            Expression<Func<TProperty>> propertyGetterExpression,
            EventHandler handler)
        {
            #region Argument Check

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            #endregion

            var propertyName = GetPropertyName(propertyGetterExpression);

            var handlers = _propertyChangedSubscriptionsDictionary.GetOrCreateValue(propertyName);
            handlers.Add(handler);
        }

        [NotNull]
        public ObjectValidationResult Validate()
        {
            return ObjectValidator.Validate(this);
        }

        public bool IsValid()
        {
            return Validate().IsObjectValid;
        }

        #endregion

        #region Protected Methods

        [NotifyPropertyChangedInvocator]
        protected void RaisePropertyChanged<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            var propertyName = GetPropertyName(propertyGetterExpression);

            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                var eventArgs = new PropertyChangedEventArgs(propertyName);
                propertyChanged(this, eventArgs);
            }

            var handlers = _propertyChangedSubscriptionsDictionary.GetValueOrDefault(propertyName);
            if (handlers != null && handlers.Count != 0)
            {
                handlers.ForEach(handler => handler(this, EventArgs.Empty));
            }
        }

        protected void ExecuteOnDispatcher(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                _dispatcher.Invoke(action, priority);
            }
        }

        #endregion

        #region Private Methods

        private string GetPropertyName<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            #region Argument Check

            if (propertyGetterExpression == null)
            {
                throw new ArgumentNullException(nameof(propertyGetterExpression));
            }

            #endregion

            var thisType = GetType();

            var memberExpression = propertyGetterExpression.Body as MemberExpression;
            if ((memberExpression == null) || (memberExpression.NodeType != ExpressionType.MemberAccess))
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, thisType, propertyGetterExpression),
                    nameof(propertyGetterExpression));
            }

            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, thisType, propertyGetterExpression),
                    nameof(propertyGetterExpression));
            }

            if (propertyInfo.DeclaringType != thisType)
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, thisType, propertyGetterExpression),
                    nameof(propertyGetterExpression));
            }

            if (memberExpression.Expression == null)
            {
                var accessor = propertyInfo.GetGetMethod(true) ?? propertyInfo.GetSetMethod(true);
                if ((accessor == null) || !accessor.IsStatic)
                {
                    throw new ArgumentException(
                        string.Format(InvalidExpressionMessageAutoFormat, thisType, propertyGetterExpression),
                        nameof(propertyGetterExpression));
                }
            }

            return propertyInfo.EnsureNotNull().Name;
        }

        #endregion
    }
}