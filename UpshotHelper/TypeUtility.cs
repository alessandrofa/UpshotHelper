using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace UpshotHelper
{
    internal static class TypeUtility
    {
        public static Type GetElementType(Type type)
        {
            if (type.HasElementType)
            {
                return type.GetElementType();
            }
            Type type2 = TypeUtility.FindIEnumerable(type);
            if (type2 != null)
            {
                return type2.GetGenericArguments()[0];
            }
            return type;
        }
        internal static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
            {
                return null;
            }
            if (seqType.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(new Type[]
				{
					seqType.GetElementType()
				});
            }
            if (seqType.IsGenericType)
            {
                Type[] genericArguments = seqType.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Type type = genericArguments[i];
                    Type type2 = typeof(IEnumerable<>).MakeGenericType(new Type[]
					{
						type
					});
                    if (type2.IsAssignableFrom(seqType))
                    {
                        Type result = type2;
                        return result;
                    }
                }
            }
            Type[] interfaces = seqType.GetInterfaces();
            if (interfaces != null && interfaces.Length > 0)
            {
                Type[] array = interfaces;
                for (int j = 0; j < array.Length; j++)
                {
                    Type seqType2 = array[j];
                    Type type3 = TypeUtility.FindIEnumerable(seqType2);
                    if (type3 != null)
                    {
                        Type result = type3;
                        return result;
                    }
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return TypeUtility.FindIEnumerable(seqType.BaseType);
            }
            return null;
        }
        internal static bool IsDataMember(PropertyDescriptor pd)
        {
            AttributeCollection attributeCollection = pd.ComponentType.Attributes();
            if (attributeCollection[typeof(DataContractAttribute)] != null)
            {
                if (pd.Attributes[typeof(DataMemberAttribute)] == null)
                {
                    return false;
                }
            }
            else
            {
                if (pd.Attributes[typeof(IgnoreDataMemberAttribute)] != null)
                {
                    return false;
                }
            }
            return true;
        }
        internal static IEnumerable<Type> GetKnownTypes(Type type, bool inherit)
        {
            IDictionary<Type, Type> dictionary = new Dictionary<Type, Type>();
            IEnumerable<KnownTypeAttribute> enumerable = type.GetCustomAttributes(typeof(KnownTypeAttribute), inherit).Cast<KnownTypeAttribute>();
            foreach (KnownTypeAttribute current in enumerable)
            {
                Type type2 = current.Type;
                if (type2 != null)
                {
                    dictionary[type2] = type2;
                }
                string methodName = current.MethodName;
                if (!string.IsNullOrEmpty(methodName))
                {
                    Type typeFromHandle = typeof(IEnumerable<Type>);
                    MethodInfo method = type.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null && typeFromHandle.IsAssignableFrom(method.ReturnType))
                    {
                        IEnumerable<Type> enumerable2 = method.Invoke(null, null) as IEnumerable<Type>;
                        if (enumerable2 != null)
                        {
                            foreach (Type current2 in enumerable2)
                            {
                                dictionary[current2] = current2;
                            }
                        }
                    }
                }
            }
            return dictionary.Keys;
        }
        internal static Type UnwrapTaskInnerType(Type t)
        {
            if (typeof(Task).IsAssignableFrom(t) && t.IsGenericType)
            {
                return t.GetGenericArguments()[0];
            }
            return t;
        }
    }
}
