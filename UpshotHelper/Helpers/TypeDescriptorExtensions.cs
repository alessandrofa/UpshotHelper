using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UpshotHelper.Helpers
{
    internal static class TypeDescriptorExtensions
    {
        /// <summary>
        /// Explicits the attributes.
        /// </summary>
        /// <param name="propertyDescriptor">The property descriptor.</param>
        /// <returns></returns>
        public static AttributeCollection ExplicitAttributes(this PropertyDescriptor propertyDescriptor)
        {
            List<Attribute> list = new List<Attribute>(propertyDescriptor.Attributes.Cast<Attribute>());
            AttributeCollection attributes = TypeDescriptor.GetAttributes(propertyDescriptor.PropertyType);
            bool flag = false;
            foreach (Attribute objA in attributes)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (object.ReferenceEquals(objA, list[i]))
                    {
                        list.RemoveAt(i);
                        flag = true;
                    }
                }
            }
            if (!flag)
            {
                return propertyDescriptor.Attributes;
            }
            return new AttributeCollection(list.ToArray());
        }
        /// <summary>
        /// Attributeses the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static AttributeCollection Attributes(this Type type)
        {
            AttributeCollection attributes = TypeDescriptor.GetAttributes(type.BaseType);
            List<Attribute> list = new List<Attribute>(TypeDescriptor.GetAttributes(type).Cast<Attribute>());
            foreach (Attribute attribute in attributes)
            {
                AttributeUsageAttribute attributeUsageAttribute = (AttributeUsageAttribute)TypeDescriptor.GetAttributes(attribute)[typeof(AttributeUsageAttribute)];
                if (attributeUsageAttribute != null && !attributeUsageAttribute.Inherited)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (object.ReferenceEquals(attribute, list[i]))
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            return new AttributeCollection(list.ToArray());
        }
        /// <summary>
        /// Determines whether [contains attribute type] [the specified attributes].
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="attributes">The attributes.</param>
        /// <returns>
        ///   <c>true</c> if [contains attribute type] [the specified attributes]; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsAttributeType<TAttribute>(this AttributeCollection attributes) where TAttribute : Attribute
        {
            return attributes.Cast<Attribute>().Any((Attribute a) => a.GetType() == typeof(TAttribute));
        }
    }
}
