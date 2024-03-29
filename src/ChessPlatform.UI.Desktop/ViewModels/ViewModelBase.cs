﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Threading;
using Omnifactotum.Annotations;
using Omnifactotum.Validation;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    internal abstract class ViewModelBase : INotifyPropertyChanged
    {
        private const string InvalidExpressionMessageAutoFormat =
            "Invalid expression (must be a getter of a property of the type '{0}'): {{ {1} }}.";

        private readonly Dispatcher _dispatcher;
        private readonly Dictionary<string, List<EventHandler>> _propertyChangedSubscriptionsDictionary;

        protected ViewModelBase()
        {
            _dispatcher = Dispatcher.CurrentDispatcher.EnsureNotNull();
            _propertyChangedSubscriptionsDictionary = new Dictionary<string, List<EventHandler>>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SubscribeToChangeOf<TProperty>(
            Expression<Func<TProperty>> propertyGetterExpression,
            EventHandler handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

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
            if (action is null)
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

        private string GetPropertyName<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            if (propertyGetterExpression is null)
            {
                throw new ArgumentNullException(nameof(propertyGetterExpression));
            }

            var thisType = GetType();

            if (!(propertyGetterExpression.Body is MemberExpression { NodeType: ExpressionType.MemberAccess } memberExpression))
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, thisType, propertyGetterExpression),
                    nameof(propertyGetterExpression));
            }

            if (!(memberExpression.Member is PropertyInfo propertyInfo))
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

            //// ReSharper disable once InvertIf
            if (memberExpression.Expression is null)
            {
                var accessor = propertyInfo.GetGetMethod(true) ?? propertyInfo.GetSetMethod(true);
                if (accessor is null || !accessor.IsStatic)
                {
                    throw new ArgumentException(
                        string.Format(InvalidExpressionMessageAutoFormat, thisType, propertyGetterExpression),
                        nameof(propertyGetterExpression));
                }
            }

            return propertyInfo.EnsureNotNull().Name;
        }
    }
}