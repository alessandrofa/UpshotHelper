using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace UpshotHelper.Controllers
{
    public class UpshotControllerDescription
    {
        protected Type _upshotControllerType;
        protected ReadOnlyCollection<Type> _entityTypes;

        public ReadOnlyCollection<Type> EntityTypes
        {
            get { return _entityTypes; }
        }

        public UpshotControllerDescription(HttpControllerDescriptor controllerDescriptor)
        {
            HashSet<Type> entityTypes = new HashSet<Type>();

            _upshotControllerType = controllerDescriptor.ControllerType;

            IEnumerable<MethodInfo> enumerable =
            from p in _upshotControllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where p.DeclaringType != typeof(UpshotController) && p.DeclaringType != typeof(object) && !p.IsSpecialName
            select p;
            foreach (MethodInfo current in enumerable)
            {
                if (current.GetCustomAttributes(typeof(NonActionAttribute), false).Length <= 0 && (!current.IsVirtual || !(current.GetBaseDefinition().DeclaringType == typeof(UpshotController))))
                {
                    if (current.ReturnType != typeof(void))
                    {
                        Type type = TypeUtility.UnwrapTaskInnerType(current.ReturnType);
                        Type elementType = TypeUtility.GetElementType(type);
                        if (LookUpIsEntityType(elementType))
                        {
                            if (!entityTypes.Contains(elementType))
                            {
                                entityTypes.Add(elementType);
                            }
                        }
                    }
                }
            }
            _entityTypes = new ReadOnlyCollection<Type>(entityTypes.ToList());
        }

        private bool LookUpIsEntityType(Type type)
        {
            return TypeDescriptor.GetProperties(type)
                .Cast<PropertyDescriptor>()
                .Any((PropertyDescriptor p) => p.Attributes[typeof(KeyAttribute)] != null);
        }
    }
}
