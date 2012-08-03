using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace UpshotHelper.Controllers
{
    public class UpshotControllerActionSelector : ApiControllerActionSelector
    {
        public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            object actionText;
            if (controllerContext.RouteData.Values.TryGetValue("action", out actionText))
            {
                if (((string)actionText).Equals("Submit", StringComparison.Ordinal))
                {
                    return new SubmitActionDescriptor(controllerContext.ControllerDescriptor);
                }
            }
            return base.SelectAction(controllerContext);
        }
    }
}
