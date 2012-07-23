using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UpshotHelper
{
    public static class MetadataExtensions
    {
        public static IHtmlString Metadata<TEntityType>(this HtmlHelper htmlHelper)
        {
            MetadataGenerator.TypeMetadata metadata = MetadataGenerator.GetMetadata(typeof(TEntityType));
            JObject value = new JObject();
            value.Add(metadata.EncodedTypeName, metadata.ToJsonValue());
            
            return htmlHelper.Raw(value);
        }
    }
}
