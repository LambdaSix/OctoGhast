using System;
using System.Linq.Expressions;
using System.Reflection;

namespace OctoGhast.UserInterface.Controls {
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Decompose an Expression Tree into parts and return a PropertyInfo if the expression
        /// resolved to a property on a class.
        /// </summary>
        /// <typeparam name="T">Type of the property</typeparam>
        /// <param name="propertyExpression">Lambda pointing to the property</param>
        /// <returns>A PropertyInfo object describing the target</returns>
        public static PropertyInfo GetProperty<T>(this Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            MemberExpression body;
            switch (propertyExpression.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    body = ((propertyExpression.Body is UnaryExpression ue) ? ue.Operand : null) as MemberExpression;
                    break;
                default:
                    body = propertyExpression.Body as MemberExpression;
                    break;
            }

            if (body == null)
                throw new ArgumentException("Invalid Expression Body", nameof(propertyExpression));

            var property = body.Member as PropertyInfo;

            if (property == null)
                throw new ArgumentException("Argument body is not a property", nameof(propertyExpression));

            return property;
        }

        /// <summary>
        /// Decompose an Expression Tree into parts and return the root object of the expression.
        /// That is, for an expression <code>() => MyFoo.Property.SubValue.Value</code> return a reference
        /// to <code>MyFoo</code>
        /// </summary>
        /// <param name="propertyExpression">Lambda pointing to the property to retrieve the root object from</param>
        /// <returns></returns>
        public static object GetRootObject<T>(this Expression<Func<T>> propertyExpression) {
            MemberExpression body;
            switch (propertyExpression.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    body = ((propertyExpression.Body is UnaryExpression ue) ? ue.Operand : null) as MemberExpression;
                    break;
                default:
                    body = propertyExpression.Body as MemberExpression;
                    break;
            }

            while (body.Expression is MemberExpression)
                body = (MemberExpression)body.Expression;

            if (!(body.Expression is ConstantExpression rootObject))
                return null;

            if (body.Member.MemberType == MemberTypes.Property) {
                var propInfo = body.Member as PropertyInfo;
                return propInfo != null
                    ? propInfo.GetValue(rootObject.Value)
                    : null;
            }

            if (body.Member.MemberType == MemberTypes.Field) {
                var fieldInfo = body.Member as FieldInfo;
                return fieldInfo != null
                    ? fieldInfo.GetValue(rootObject.Value)
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Get the value from an expression, allowing for types that could be null.
        /// </summary>
        /// <typeparam name="T">Type of the object to retrieve</typeparam>
        /// <param name="accessor">Lambda to retrieve the object</param>
        /// <param name="defaultValue">A default value to return instead of null</param>
        /// <returns>The objects value or the default value</returns>
        public static T GetValue<T>(this Func<T> accessor, T defaultValue = default(T))
        {
            var type = typeof(T);
            bool isNullable = !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
            T value;
            if (isNullable)
            {
                var val = accessor();
                value = val != null ? val : defaultValue;
            }
            else
            {
                value = accessor();
            }
            return value;
        }
    }
}