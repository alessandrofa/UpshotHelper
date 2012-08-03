using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using UpshotHelper.Models;
using System.Linq;
using System.Web.Http.Controllers;

namespace UpshotHelper.Controllers
{
    [HttpControllerConfiguration(HttpActionInvoker = typeof(UpshotControllerActionInvoker), HttpActionSelector = typeof(UpshotControllerActionSelector))]
    public abstract class UpshotController : ApiController
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
        }

        public virtual bool Submit(IEnumerable<ChangeSetEntry> changeSet)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSetEntries");
            }

            return ProcessSubmit(changeSet);
        }

        protected abstract bool ProcessSubmit(IEnumerable<ChangeSetEntry> changeSet);
    }
}
