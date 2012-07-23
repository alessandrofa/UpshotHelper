using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace UpshotHelper
{
    public class UpshotConfigBuilder : IHtmlString
    {
        private interface IDataSourceConfig
        {
            string ClientName { get; }
            Type ControllerType { get; }
            string SharedContextExpression { get; }
            string ContextExpression { set; }
            string ClientMappingJson { set; }
            Type EntityType { get; }
            string GetInitializationScript();
        }

        private class DataSourceConfig<TApiController> : UpshotConfigBuilder.IDataSourceConfig where TApiController : ApiController
        {
            private readonly HtmlHelper htmlHelper;
            private readonly bool bufferChanges;
            private readonly Expression<Func<TApiController, object>> queryOperation;
            private readonly string serviceUrlOverride;
            private readonly string clientName;
            private readonly Type entityType;
            public string ClientName { get { return this.clientName; } }

            public Type ControllerType { get { return typeof(TApiController); } }

            public string SharedContextExpression { get { return this.ClientExpression + ".getDataContext()"; } }

            public string ContextExpression { private get; set; }

            public string ClientMappingJson { private get; set; }

            private string ClientExpression { get { return "upshot.dataSources." + this.ClientName; } }

            public Type EntityType
            {
                get
                {
                    Type returnType = this.OperationMethod.ReturnType;
                    Type left = returnType.IsGenericType ? returnType.GetGenericTypeDefinition() : null;
                    Type entityType;
                    if (left != null && (left == typeof(IQueryable<>) || left == typeof(IEnumerable<>)))
                    {
                        entityType = returnType.GetGenericArguments().Single<Type>();
                    }
                    else
                    {
                        entityType = returnType;
                    }
                    if (entityType != this.entityType)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "queryOperation '{0}' must return the entity type of '{1}' or an IEnumerable/IQueryable of the entity type '{1}'", new object[]
                        {
                            this.OperationMethod.Name,
                            this.entityType.Name
                        }));
                    }
                    return entityType;
                }
            }

            private string ServiceUrl
            {
                get
                {
                    if (!string.IsNullOrEmpty(this.serviceUrlOverride))
                    {
                        return this.serviceUrlOverride;
                    }
                    UrlHelper urlHelper = new UrlHelper(this.htmlHelper.ViewContext.RequestContext);
                    string name = typeof(TApiController).Name;
                    if (!name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("DataController type name must end with 'Controller'");
                    }
                    string controller = name.Substring(0, name.Length - "Controller".Length);
                    return urlHelper.RouteUrl(new
                    {
                        controller = controller,
                        action = UrlParameter.Optional,
                        httproute = true
                    });
                }
            }

            private string DefaultClientName
            {
                get
                {
                    string name = this.OperationMethod.Name;
                    if (!name.StartsWith("Get", StringComparison.OrdinalIgnoreCase) || name.Length <= 3 || !char.IsLetter(name[3]))
                    {
                        return name;
                    }
                    return name.Substring(3);
                }
            }

            private MethodInfo OperationMethod
            {
                get
                {
                    Expression expression = this.queryOperation.Body;
                    if (expression.NodeType == ExpressionType.Convert)
                    {
                        UnaryExpression unaryExpression = (UnaryExpression)expression;
                        if (unaryExpression.Type == typeof(object))
                        {
                            expression = unaryExpression.Operand;
                        }
                    }
                    MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                    if (methodCallExpression == null)
                    {
                        throw new ArgumentException("queryOperation must be a method call");
                    }
                    if (!methodCallExpression.Method.DeclaringType.IsAssignableFrom(typeof(TApiController)))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "queryOperation must be a method on '{0}' or a base type", new object[]
                        {
                            typeof(TApiController).Name
                        }));
                    }
                    return methodCallExpression.Method;
                }
            }

            public DataSourceConfig(HtmlHelper htmlHelper, bool bufferChanges, Expression<Func<TApiController, object>> queryOperation, Type entityType, string serviceUrlOverride, string clientName)
            {
                this.htmlHelper = htmlHelper;
                this.bufferChanges = bufferChanges;
                this.queryOperation = queryOperation;
                this.entityType = entityType;
                this.serviceUrlOverride = serviceUrlOverride;
                this.clientName = (string.IsNullOrEmpty(clientName) ? this.DefaultClientName : clientName);
            }

            public string GetInitializationScript()
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} = upshot.RemoteDataSource({{\r\n    providerParameters: {{ url: \"{1}\", operationName: \"{2}\" }},\r\n    entityType: \"{3}\",\r\n    bufferChanges: {4},\r\n    dataContext: {5},\r\n    mapping: {6}\r\n}});", new object[] 
                {
                    this.ClientExpression,
                    this.ServiceUrl,
                    this.OperationMethod.Name,
                    UpshotConfigBuilder.EncodeServerTypeName(this.EntityType),
                    this.bufferChanges ? "true" : "false",
                    this.ContextExpression ?? "undefined",
                    this.ClientMappingJson ?? "undefined"
                });
            }
        }

        private readonly HtmlHelper htmlHelper;
        private readonly bool bufferChanges;
        private readonly IDictionary<string, UpshotConfigBuilder.IDataSourceConfig> dataSources;
        private readonly IDictionary<Type, string> clientMappings;

        public UpshotConfigBuilder(HtmlHelper htmlHelper, bool bufferChanges)
        {
            this.htmlHelper = htmlHelper;
            this.bufferChanges = bufferChanges;
            this.dataSources = new Dictionary<string, UpshotConfigBuilder.IDataSourceConfig>();
            this.clientMappings = new Dictionary<Type, string>();
        }

        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Following established design pattern for HTML helpers.")]
        public UpshotConfigBuilder DataSource<TApiController>(Expression<Func<TApiController, object>> queryOperation, Type entityType) where TApiController : ApiController
        {
            return this.DataSource<TApiController>(queryOperation, entityType, null, null);
        }

        [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "Following established design pattern for HTML helpers."), SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Following established design pattern for HTML helpers.")]
        public UpshotConfigBuilder DataSource<TApiController>(Expression<Func<TApiController, object>> queryOperation, Type entityType, string serviceUrl, string clientName) where TApiController : ApiController
        {
            UpshotConfigBuilder.IDataSourceConfig dataSourceConfig = new UpshotConfigBuilder.DataSourceConfig<TApiController>(this.htmlHelper, this.bufferChanges, queryOperation, entityType, serviceUrl, clientName);

            if (this.dataSources.ContainsKey(dataSourceConfig.ClientName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Cannot have multiple data sources with the same clientName.  Found multip data sources with the name '{0}'", new object[]
                {
                    dataSourceConfig.ClientName
                }));
            }
            this.dataSources.Add(dataSourceConfig.ClientName, dataSourceConfig);
            return this;
        }

        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Following established design pattern for HTML helpers.")]
        public UpshotConfigBuilder ClientMapping<TEntity>(string clientConstructor)
        {
            if (string.IsNullOrEmpty(clientConstructor))
            {
                throw new ArgumentException("clientConstructor cannot be null or empty", "clientConstructor");
            }
            if (this.clientMappings.ContainsKey(typeof(TEntity)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Cannot have multiple client mappings for the same entity type. Found multiple client mappings for '{0}'", new object[]
				{
					typeof(TEntity).FullName
				}));
            }
            this.clientMappings.Add(typeof(TEntity), clientConstructor);
            return this;
        }

        public string ToHtmlString()
        {
            StringBuilder stringBuilder = new StringBuilder("upshot.dataSources = upshot.dataSources || {};\n");
            IEnumerable<Type> enumerable = (
                from x in this.dataSources
                select x.Value.EntityType).Distinct<Type>();
            foreach (Type current in enumerable)
            {
                stringBuilder.AppendFormat("upshot.metadata({0});\n", this.GetMetadata(current));
            }

            IEnumerable<UpshotConfigBuilder.IDataSourceConfig> values = this.dataSources.Values;
            UpshotConfigBuilder.IDataSourceConfig dataSourceConfig = values.FirstOrDefault<UpshotConfigBuilder.IDataSourceConfig>();
            if (dataSourceConfig != null)
            {
                foreach (UpshotConfigBuilder.IDataSourceConfig current2 in values.Skip(1))
                {
                    current2.ContextExpression = dataSourceConfig.SharedContextExpression;
                }
                dataSourceConfig.ClientMappingJson = this.GetClientMappingsObjectLiteral();
            }
            foreach (UpshotConfigBuilder.IDataSourceConfig current3 in values)
            {
                stringBuilder.AppendLine("\n" + current3.GetInitializationScript());
            }
            foreach (KeyValuePair<Type, string> current4 in this.clientMappings)
            {
                stringBuilder.AppendFormat("upshot.registerType(\"{0}\", function() {{ return {1} }});\n", UpshotConfigBuilder.EncodeServerTypeName(current4.Key), current4.Value);
            }
            return string.Format(CultureInfo.InvariantCulture, "<script type='text/javascript'>\n{0}</script>", new object[]
			{
				stringBuilder
			});
        }

        private string GetMetadata(Type entityType)
        {
            MethodInfo method = typeof(MetadataExtensions).GetMethod("Metadata");
            IHtmlString htmlString = (IHtmlString)method.MakeGenericMethod(new Type[]
            {
                entityType
            }).Invoke(null, new HtmlHelper[]
            {
                this.htmlHelper
            });
            return htmlString.ToHtmlString(); 
        }

        private string GetClientMappingsObjectLiteral()
        {
            IEnumerable<string> values =
                from clientMapping in this.clientMappings
                select string.Format(CultureInfo.InvariantCulture, "\"{0}\": function(data) {{ return new {1}(data) }}", new object[]
				{
					UpshotConfigBuilder.EncodeServerTypeName(clientMapping.Key),
					clientMapping.Value
				});
            return string.Format(CultureInfo.InvariantCulture, "{{{0}}}", new object[]
			{
				string.Join(",", values)
			});
        }

        private static string EncodeServerTypeName(Type type)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", new object[]
			{
				type.Name,
				":#",
				type.Namespace
			});
        }
    }
}
