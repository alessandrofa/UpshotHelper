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
using System.Web.Mvc;

namespace UpshotHelper
{
    public class UpshotConfigBuilder : IHtmlString
    {
        private interface IDataSourceConfig
        {
            /// <summary>
            /// Gets the name of the client.
            /// </summary>
            /// <value>
            /// The name of the client.
            /// </value>
            string ClientName { get; }
            /// <summary>
            /// Gets the type of the controller.
            /// </summary>
            /// <value>
            /// The type of the controller.
            /// </value>
            Type ControllerType { get; }
            /// <summary>
            /// Gets the shared context expression.
            /// </summary>
            /// <value>
            /// The shared context expression.
            /// </value>
            string SharedContextExpression { get; }
            /// <summary>
            /// Sets the context expression.
            /// </summary>
            /// <value>
            /// The context expression.
            /// </value>
            string ContextExpression { set; }
            /// <summary>
            /// Sets the client mapping json.
            /// </summary>
            /// <value>
            /// The client mapping json.
            /// </value>
            string ClientMappingJson { set; }
            /// <summary>
            /// Gets the type of the entity.
            /// </summary>
            /// <value>
            /// The type of the entity.
            /// </value>
            Type EntityType { get; }
            /// <summary>
            /// Gets the initialization script.
            /// </summary>
            /// <returns>Returns the Upshot Data Source initialization JavaScript</returns>
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

            /// <summary>
            /// Gets the name of the client.
            /// </summary>
            /// <value>
            /// The name of the client.
            /// </value>
            public string ClientName { get { return this.clientName; } }

            /// <summary>
            /// Gets the type of the controller.
            /// </summary>
            /// <value>
            /// The type of the controller.
            /// </value>
            public Type ControllerType { get { return typeof(TApiController); } }

            /// <summary>
            /// Gets the shared context expression.
            /// </summary>
            /// <value>
            /// The shared context expression.
            /// </value>
            public string SharedContextExpression { get { return this.ClientExpression + ".getDataContext()"; } }

            /// <summary>
            /// Gets or sets the context expression.
            /// </summary>
            /// <value>
            /// The context expression.
            /// </value>
            public string ContextExpression { private get; set; }

            /// <summary>
            /// Gets or sets the client mapping json.
            /// </summary>
            /// <value>
            /// The client mapping json.
            /// </value>
            public string ClientMappingJson { private get; set; }

            /// <summary>
            /// Gets the client expression.
            /// </summary>
            /// <value>
            /// The client expression.
            /// </value>
            private string ClientExpression { get { return "upshot.dataSources." + this.ClientName; } }

            /// <summary>
            /// Gets the type of the entity.
            /// </summary>
            /// <value>
            /// The type of the entity.
            /// </value>
            /// <exception cref="System.ArgumentException"></exception>
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

            /// <summary>
            /// Gets the service URL.
            /// </summary>
            /// <value>
            /// The service URL.
            /// </value>
            /// <exception cref="System.ArgumentException"></exception>
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

            /// <summary>
            /// Gets the default name of the client.
            /// </summary>
            /// <value>
            /// The default name of the client.
            /// </value>
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

            /// <summary>
            /// Gets the operation method.
            /// </summary>
            /// <value>
            /// The operation method.
            /// </value>
            /// <exception cref="System.ArgumentException"></exception>
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

            /// <summary>
            /// Initializes a new instance of the <see cref="DataSourceConfig" /> class.
            /// </summary>
            /// <param name="htmlHelper">The HTML helper.</param>
            /// <param name="bufferChanges">if set to <c>true</c> [buffer changes].</param>
            /// <param name="queryOperation">The query operation.</param>
            /// <param name="entityType">Type of the entity.</param>
            /// <param name="serviceUrlOverride">The service URL override.</param>
            /// <param name="clientName">Name of the client.</param>
            public DataSourceConfig(HtmlHelper htmlHelper, bool bufferChanges, Expression<Func<TApiController, object>> queryOperation, Type entityType, string serviceUrlOverride, string clientName)
            {
                this.htmlHelper = htmlHelper;
                this.bufferChanges = bufferChanges;
                this.queryOperation = queryOperation;
                this.entityType = entityType;
                this.serviceUrlOverride = serviceUrlOverride;
                this.clientName = (string.IsNullOrEmpty(clientName) ? this.DefaultClientName : clientName);
            }

            /// <summary>
            /// Gets the initialization script.
            /// </summary>
            /// <returns>
            /// Returns the Upshot Data Source initialization JavaScript
            /// </returns>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="UpshotConfigBuilder" /> class.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <param name="bufferChanges">if set to <c>true</c> [buffer changes].</param>
        public UpshotConfigBuilder(HtmlHelper htmlHelper, bool bufferChanges)
        {
            this.htmlHelper = htmlHelper;
            this.bufferChanges = bufferChanges;
            this.dataSources = new Dictionary<string, UpshotConfigBuilder.IDataSourceConfig>();
            this.clientMappings = new Dictionary<Type, string>();
        }

        /// <summary>
        /// Datas the source.
        /// </summary>
        /// <typeparam name="TApiController">The type of the API controller.</typeparam>
        /// <param name="queryOperation">The query operation.</param>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns>Returns an instance of <seealso cref="UpshotConfigBuilder"/> configured for a DataSource.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Following established design pattern for HTML helpers.")]
        public UpshotConfigBuilder DataSource<TApiController>(Expression<Func<TApiController, object>> queryOperation, Type entityType) where TApiController : ApiController
        {
            return this.DataSource<TApiController>(queryOperation, entityType, null, null);
        }

        /// <summary>
        /// Datas the source.
        /// </summary>
        /// <typeparam name="TApiController">The type of the API controller.</typeparam>
        /// <param name="queryOperation">The query operation.</param>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="serviceUrl">The service URL.</param>
        /// <param name="clientName">Name of the client.</param>
        /// <returns>Returns an instance of <seealso cref="UpshotConfigBuilder"/> configured for a DataSource.</returns>
        /// <exception cref="System.ArgumentException"></exception>
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

        /// <summary>
        /// Clients the mapping.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="clientConstructor">The client constructor.</param>
        /// <returns>Returns an instance of <seealso cref="UpshotConfigBuilder"/> configured for ClientMapping.</returns>
        /// <exception cref="System.ArgumentException"></exception>
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

        /// <summary>
        /// Returns an HTML-encoded string.
        /// </summary>
        /// <returns>
        /// An HTML-encoded string.
        /// </returns>
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

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the client mappings object literal.
        /// </summary>
        /// <returns>Returns a formatted string for the Client Mapping object.</returns>
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

        /// <summary>
        /// Encodes the name of the server type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Returns a string that Upshot will recognize for the given <paramref name="type"/>.</returns>
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
