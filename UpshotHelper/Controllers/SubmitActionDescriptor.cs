using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace UpshotHelper.Controllers
{
    public class SubmitActionDescriptor : HttpActionDescriptor
    {
        public SubmitActionDescriptor(HttpControllerDescriptor controllerDescriptor)
            : base(controllerDescriptor)
        {
        }

        public override string ActionName
        {
            get { return "Submit"; }
        }

        public override Task<object> ExecuteAsync(HttpControllerContext controllerContext, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException("Need to implement the SubmitActionDescriptor.ExecuteAsync");
        }

        public override Collection<HttpParameterDescriptor> GetParameters()
        {
            Collection<HttpParameterDescriptor> parameterDesc = new Collection<HttpParameterDescriptor>();
            parameterDesc.Add(new ChangeSetEntryHttpParameterDescriptor(this));
            return parameterDesc;
        }

        public override Type ReturnType
        {
            get { return typeof(HttpResponseMessage); }
        }

        public override Collection<FilterInfo> GetFilterPipeline()
        {
            return base.GetFilterPipeline();
        }
    }
}
