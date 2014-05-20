﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Omnifactotum.Annotations;

namespace ChessPlatform.UI.Desktop.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        #region Constants and Fields

        private const string InvalidExpressionMessageAutoFormat =
            "Invalid expression (must be a getter of a property of some type): {{ {0} }}.";

        private readonly Dictionary<string, List<EventHandler>> _propertyChangedSubscriptionsDictionary;

        #endregion

        #region Constructors

        protected ViewModelBase()
        {
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
                throw new ArgumentNullException("handler");
            }

            #endregion

            var propertyName = GetPropertyName(propertyGetterExpression);

            var handlers = _propertyChangedSubscriptionsDictionary.GetValueOrCreate(propertyName);
            handlers.Add(handler);
        }

        #endregion

        #region Protected Methods

        [NotifyPropertyChangedInvocator]
        protected void RaisePropertyChanged<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            var propertyName = GetPropertyName(propertyGetterExpression);

            var propertyChanged = this.PropertyChanged;
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

        #endregion

        #region Private Methods

        private string GetPropertyName<TProperty>(Expression<Func<TProperty>> propertyGetterExpression)
        {
            #region Argument Check

            if (propertyGetterExpression == null)
            {
                throw new ArgumentNullException("propertyGetterExpression");
            }

            #endregion

            var memberExpression = propertyGetterExpression.Body as MemberExpression;
            if ((memberExpression == null) || (memberExpression.NodeType != ExpressionType.MemberAccess))
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            if (propertyInfo.DeclaringType != GetType())
            {
                throw new ArgumentException(
                    string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                    "propertyGetterExpression");
            }

            if (memberExpression.Expression == null)
            {
                var accessor = propertyInfo.GetGetMethod(true) ?? propertyInfo.GetSetMethod(true);
                if ((accessor == null) || !accessor.IsStatic)
                {
                    throw new ArgumentException(
                        string.Format(InvalidExpressionMessageAutoFormat, propertyGetterExpression),
                        "propertyGetterExpression");
                }
            }

            return propertyInfo.EnsureNotNull().Name;
        }

        #endregion
    }
}