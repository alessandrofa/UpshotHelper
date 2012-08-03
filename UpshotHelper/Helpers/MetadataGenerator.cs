using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UpshotHelper.Helpers
{
    internal static class MetadataGenerator
    {
        public class TypeMetadata
        {
            private List<string> key = new List<string>();
            private List<MetadataGenerator.TypePropertyMetadata> properties = new List<MetadataGenerator.TypePropertyMetadata>();
            /// <summary>
            /// Gets the name of the type.
            /// </summary>
            /// <value>
            /// The name of the type.
            /// </value>
            public string TypeName
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the type namespace.
            /// </summary>
            /// <value>
            /// The type namespace.
            /// </value>
            public string TypeNamespace
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the name of the encoded type.
            /// </summary>
            /// <value>
            /// The name of the encoded type.
            /// </value>
            public string EncodedTypeName
            {
                get
                {
                    return MetadataGenerator.EncodeTypeName(this.TypeName, this.TypeNamespace);
                }
            }
            /// <summary>
            /// Gets the key.
            /// </summary>
            /// <value>
            /// The key.
            /// </value>
            public IEnumerable<string> Key
            {
                get
                {
                    return this.key;
                }
            }
            /// <summary>
            /// Gets the properties.
            /// </summary>
            /// <value>
            /// The properties.
            /// </value>
            public IEnumerable<MetadataGenerator.TypePropertyMetadata> Properties
            {
                get
                {
                    return this.properties;
                }
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypeMetadata" /> class.
            /// </summary>
            /// <param name="entityType">Type of the entity.</param>
            public TypeMetadata(Type entityType)
            {
                Type elementType = TypeUtility.GetElementType(entityType);
                this.TypeName = elementType.Name;
                this.TypeNamespace = elementType.Namespace;
                IEnumerable<PropertyDescriptor> enumerable =
                    from PropertyDescriptor p in TypeDescriptor.GetProperties(entityType)
                    orderby p.Name
                    where TypeUtility.IsDataMember(p)
                    select p;
                foreach (PropertyDescriptor current in enumerable)
                {
                    this.properties.Add(new MetadataGenerator.TypePropertyMetadata(current));
                    if (current.ExplicitAttributes()[typeof(KeyAttribute)] != null)
                    {
                        this.key.Add(current.Name);
                    }
                }
            }
            /// <summary>
            /// To the json value.
            /// </summary>
            /// <returns></returns>
            public JToken ToJsonValue()
            {
                JObject jsonObject = new JObject();
                jsonObject.Add("key", new JArray(
                    from k in this.Key
                    select k));
                jsonObject.Add("fields", new JObject(
                    from p in this.Properties
                    select new JProperty(p.Name, p.ToJsonValue())));
                jsonObject.Add("rules", new JObject(this.Properties.SelectMany(delegate(MetadataGenerator.TypePropertyMetadata p)
                {
                    if (p.ValidationRules.Count != 0)
                    {
                        JArray array = new JArray();
                        array[0] = new JProperty(p.Name, new JObject(
                            from r in p.ValidationRules
                            select new JProperty(r.Name, r.ToJsonValue())));
                        return array;
                    }
                    return MetadataGenerator.emptyKeyValuePairEnumerable;
                })));
                jsonObject.Add("messages", new JObject(this.Properties.SelectMany(delegate(MetadataGenerator.TypePropertyMetadata p)
                {
                    if (p.ValidationRules.Any((MetadataGenerator.TypePropertyValidationRuleMetadata r) => r.ErrorMessageString != null))
                    {
                        JArray array = new JArray();
                        array[0] = new JProperty(p.Name, new JObject(p.ValidationRules.SelectMany(delegate(MetadataGenerator.TypePropertyValidationRuleMetadata r)
                        {
                            if (r.ErrorMessageString != null)
                            {
                                return new JArray()
								{
									new JProperty(r.Name, r.ErrorMessageString)
								};
                            }
                            return MetadataGenerator.emptyKeyValuePairEnumerable;
                        })));
                        return array;
                    }
                    return MetadataGenerator.emptyKeyValuePairEnumerable;
                })));
                return jsonObject;
            }
        }
        public class TypePropertyMetadata
        {
            private List<MetadataGenerator.TypePropertyValidationRuleMetadata> validationRules = new List<MetadataGenerator.TypePropertyValidationRuleMetadata>();
            /// <summary>
            /// Gets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the name of the type.
            /// </summary>
            /// <value>
            /// The name of the type.
            /// </value>
            public string TypeName
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the type namespace.
            /// </summary>
            /// <value>
            /// The type namespace.
            /// </value>
            public string TypeNamespace
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets a value indicating whether this instance is read only.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is read only; otherwise, <c>false</c>.
            /// </value>
            public bool IsReadOnly
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets a value indicating whether this instance is array.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is array; otherwise, <c>false</c>.
            /// </value>
            public bool IsArray
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the association.
            /// </summary>
            /// <value>
            /// The association.
            /// </value>
            public MetadataGenerator.TypePropertyAssociationMetadata Association
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the validation rules.
            /// </summary>
            /// <value>
            /// The validation rules.
            /// </value>
            public IList<MetadataGenerator.TypePropertyValidationRuleMetadata> ValidationRules
            {
                get
                {
                    return this.validationRules;
                }
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyMetadata" /> class.
            /// </summary>
            /// <param name="descriptor">The descriptor.</param>
            public TypePropertyMetadata(PropertyDescriptor descriptor)
            {
                this.Name = descriptor.Name;
                Type elementType = TypeUtility.GetElementType(descriptor.PropertyType);
                this.IsArray = !elementType.Equals(descriptor.PropertyType);
                this.TypeName = elementType.Name;
                this.TypeNamespace = elementType.Namespace;
                AttributeCollection attributeCollection = descriptor.ExplicitAttributes();
                ReadOnlyAttribute readOnlyAttribute = (ReadOnlyAttribute)attributeCollection[typeof(ReadOnlyAttribute)];
                this.IsReadOnly = (readOnlyAttribute != null && readOnlyAttribute.IsReadOnly);
                AssociationAttribute associationAttribute = (AssociationAttribute)attributeCollection[typeof(AssociationAttribute)];
                if (associationAttribute != null)
                {
                    this.Association = new MetadataGenerator.TypePropertyAssociationMetadata(associationAttribute);
                }
                RequiredAttribute requiredAttribute = (RequiredAttribute)attributeCollection[typeof(RequiredAttribute)];
                if (requiredAttribute != null)
                {
                    this.validationRules.Add(new MetadataGenerator.TypePropertyValidationRuleMetadata(requiredAttribute));
                }
                RangeAttribute rangeAttribute = (RangeAttribute)attributeCollection[typeof(RangeAttribute)];
                if (rangeAttribute != null)
                {
                    Type type = rangeAttribute.OperandType;
                    type = (Nullable.GetUnderlyingType(type) ?? type);
                    if (type.Equals(typeof(double)) || type.Equals(typeof(short)) || type.Equals(typeof(int)) || type.Equals(typeof(long)) || type.Equals(typeof(float)))
                    {
                        this.validationRules.Add(new MetadataGenerator.TypePropertyValidationRuleMetadata(rangeAttribute));
                    }
                }
                StringLengthAttribute stringLengthAttribute = (StringLengthAttribute)attributeCollection[typeof(StringLengthAttribute)];
                if (stringLengthAttribute != null)
                {
                    this.validationRules.Add(new MetadataGenerator.TypePropertyValidationRuleMetadata(stringLengthAttribute));
                }
                DataTypeAttribute dataTypeAttribute = (DataTypeAttribute)attributeCollection[typeof(DataTypeAttribute)];
                if (dataTypeAttribute != null && (dataTypeAttribute.DataType.Equals(DataType.EmailAddress) || dataTypeAttribute.DataType.Equals(DataType.Url)))
                {
                    this.validationRules.Add(new MetadataGenerator.TypePropertyValidationRuleMetadata(dataTypeAttribute));
                }
            }
            /// <summary>
            /// To the json value.
            /// </summary>
            /// <returns></returns>
            public JToken ToJsonValue()
            {
                JObject jsonObject = new JObject();
                jsonObject.Add("type", MetadataGenerator.EncodeTypeName(this.TypeName, this.TypeNamespace));
                if (this.IsReadOnly)
                {
                    jsonObject.Add("readonly", true);
                }
                if (this.IsArray)
                {
                    jsonObject.Add("array", true);
                }
                if (this.Association != null)
                {
                    jsonObject.Add("association", this.Association.ToJsonValue());
                }
                return jsonObject;
            }
        }
        public class TypePropertyAssociationMetadata
        {
            private List<string> thisKeyMembers = new List<string>();
            private List<string> otherKeyMembers = new List<string>();
            /// <summary>
            /// Gets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets a value indicating whether this instance is foreign key.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is foreign key; otherwise, <c>false</c>.
            /// </value>
            public bool IsForeignKey
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the this key members.
            /// </summary>
            /// <value>
            /// The this key members.
            /// </value>
            public IEnumerable<string> ThisKeyMembers
            {
                get
                {
                    return this.thisKeyMembers;
                }
            }
            /// <summary>
            /// Gets the other key members.
            /// </summary>
            /// <value>
            /// The other key members.
            /// </value>
            public IEnumerable<string> OtherKeyMembers
            {
                get
                {
                    return this.otherKeyMembers;
                }
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyAssociationMetadata" /> class.
            /// </summary>
            /// <param name="associationAttr">The association attr.</param>
            public TypePropertyAssociationMetadata(AssociationAttribute associationAttr)
            {
                this.Name = associationAttr.Name;
                this.IsForeignKey = associationAttr.IsForeignKey;
                this.otherKeyMembers = associationAttr.OtherKeyMembers.ToList<string>();
                this.thisKeyMembers = associationAttr.ThisKeyMembers.ToList<string>();
            }
            /// <summary>
            /// To the json value.
            /// </summary>
            /// <returns></returns>
            public JToken ToJsonValue()
            {
                JObject jsonObject = new JObject();
                jsonObject.Add("name", this.Name);
                jsonObject.Add("thisKey", new JArray(
                    from k in this.ThisKeyMembers
                    select k));
                jsonObject.Add("otherKey", new JArray(
                    from k in this.OtherKeyMembers
                    select k));
                jsonObject.Add("isForeignKey", this.IsForeignKey);
                return jsonObject;
            }
        }
        public class TypePropertyValidationRuleMetadata
        {
            private string type;
            /// <summary>
            /// Gets the name.
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the value1.
            /// </summary>
            /// <value>
            /// The value1.
            /// </value>
            public object Value1
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the value2.
            /// </summary>
            /// <value>
            /// The value2.
            /// </value>
            public object Value2
            {
                get;
                private set;
            }
            /// <summary>
            /// Gets the error message string.
            /// </summary>
            /// <value>
            /// The error message string.
            /// </value>
            public string ErrorMessageString
            {
                get;
                private set;
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyValidationRuleMetadata" /> class.
            /// </summary>
            /// <param name="attribute">The attribute.</param>
            public TypePropertyValidationRuleMetadata(RequiredAttribute attribute)
                //: this(attribute)
            {
                this.Name = "required";
                this.Value1 = true;
                this.type = "boolean";
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyValidationRuleMetadata" /> class.
            /// </summary>
            /// <param name="attribute">The attribute.</param>
            public TypePropertyValidationRuleMetadata(RangeAttribute attribute)
                //: this(attribute)
            {
                this.Name = "range";
                this.Value1 = attribute.Minimum;
                this.Value2 = attribute.Maximum;
                this.type = "array";
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyValidationRuleMetadata" /> class.
            /// </summary>
            /// <param name="attribute">The attribute.</param>
            public TypePropertyValidationRuleMetadata(StringLengthAttribute attribute)
                //: this(attribute)
            {
                if (attribute.MinimumLength != 0)
                {
                    this.Name = "rangelength";
                    this.Value1 = attribute.MinimumLength;
                    this.Value2 = attribute.MaximumLength;
                    this.type = "array";
                    return;
                }
                this.Name = "maxlength";
                this.Value1 = attribute.MaximumLength;
                this.type = "number";
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyValidationRuleMetadata" /> class.
            /// </summary>
            /// <param name="attribute">The attribute.</param>
            public TypePropertyValidationRuleMetadata(DataTypeAttribute attribute)
                //: this(attribute)
            {
                switch (attribute.DataType)
                {
                    case DataType.EmailAddress:
                        this.Name = "email";
                        break;
                    case DataType.Url:
                        this.Name = "url";
                        break;
                }
                this.Value1 = true;
                this.type = "boolean";
            }
            /// <summary>
            /// Initializes a new instance of the <see cref="TypePropertyValidationRuleMetadata" /> class.
            /// </summary>
            /// <param name="attribute">The attribute.</param>
            public TypePropertyValidationRuleMetadata(ValidationAttribute attribute)
            {
                if (attribute.ErrorMessage != null)
                {
                    this.ErrorMessageString = attribute.ErrorMessage;
                }
            }
            /// <summary>
            /// To the json value.
            /// </summary>
            /// <returns></returns>
            /// <exception cref="System.InvalidOperationException"></exception>
            public JToken ToJsonValue()
            {
                if (this.type == "array")
                {
                    JValue jsonPrimitive = new JValue(this.Value1);
                    JValue jsonPrimitive2 = new JValue(this.Value2);
                    return new JArray(new object[]
					{
						jsonPrimitive,
						jsonPrimitive2
					});
                }
                if (this.type == "boolean")
                {
                    return (bool)this.Value1;
                }
                if (this.type == "number")
                {
                    return (int)this.Value1;
                }
                throw new InvalidOperationException("Unexpected validation rule type.");
            }
        }
        private static class MetadataStrings
        {
            public const string NamespaceMarker = ":#";
            public const string TypeString = "type";
            public const string ArrayString = "array";
            public const string AssociationString = "association";
            public const string FieldsString = "fields";
            public const string ThisKeyString = "thisKey";
            public const string IsForeignKey = "isForeignKey";
            public const string OtherKeyString = "otherKey";
            public const string NameString = "name";
            public const string ReadOnlyString = "readonly";
            public const string KeyString = "key";
            public const string RulesString = "rules";
            public const string MessagesString = "messages";
        }
        //private static readonly ConcurrentDictionary<DataControllerDescription, IEnumerable<MetadataGenerator.TypeMetadata>> _metadataMap = new ConcurrentDictionary<DataControllerDescription, IEnumerable<MetadataGenerator.TypeMetadata>>();
        //private static IEnumerable<KeyValuePair<string, JToken>> emptyKeyValuePairEnumerable = Enumerable.Empty<KeyValuePair<string, JToken>>();
        /// <summary>
        /// Gets the empty key value pair enumerable.
        /// </summary>
        /// <value>
        /// The empty key value pair enumerable.
        /// </value>
        private static JToken emptyKeyValuePairEnumerable { get { return new JValue(string.Empty); } }
        //public static IEnumerable<MetadataGenerator.TypeMetadata> GetMetadata(DataControllerDescription description)
        //{
        //    return MetadataGenerator._metadataMap.GetOrAdd(description, (DataControllerDescription desc) => MetadataGenerator.GenerateMetadata(desc));
        //}
        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
        public static MetadataGenerator.TypeMetadata GetMetadata(Type entityType)
        {
            return new MetadataGenerator.TypeMetadata(entityType);
        }
        //private static IEnumerable<MetadataGenerator.TypeMetadata> GenerateMetadata(DataControllerDescription description)
        //{
        //    List<MetadataGenerator.TypeMetadata> list = new List<MetadataGenerator.TypeMetadata>();
        //    foreach (Type current in description.EntityTypes)
        //    {
        //        list.Add(new MetadataGenerator.TypeMetadata(current));
        //    }
        //    return list;
        //}
        /// <summary>
        /// Encodes the name of the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="typeNamespace">The type namespace.</param>
        /// <returns></returns>
        private static string EncodeTypeName(string typeName, string typeNamespace)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", new object[]
			{
				typeName,
				":#",
				typeNamespace
			});
        }
    }
}
