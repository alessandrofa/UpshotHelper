using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace UpshotHelper
{
    public static class MetadataExtensions
    {
        /// <summary>
        /// Metadatas the specified HTML helper.
        /// </summary>
        /// <typeparam name="TEntityType">The type of the entity type.</typeparam>
        /// <param name="htmlHelper">The HTML helper.</param>
        /// <returns></returns>
        public static IHtmlString Metadata<TEntityType>(this HtmlHelper htmlHelper)
        {
            MetadataGenerator.TypeMetadata metadata = MetadataGenerator.GetMetadata(typeof(TEntityType));
            JObject value = new JObject();
            value.Add(metadata.EncodedTypeName, metadata.ToJsonValue());
            
            return htmlHelper.Raw(value);
        }
    }
}
