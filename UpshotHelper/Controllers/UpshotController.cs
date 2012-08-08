using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using UpshotHelper.Models;
using System.Linq;
using System.Web.Http.Controllers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UpshotHelper.Controllers
{
    public abstract class UpshotController : ApiController
    {
        private UpshotControllerDescription _description;
        private ChangeSet _changeSet;

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            _description = new UpshotControllerDescription(controllerContext.ControllerDescriptor);
            base.Initialize(controllerContext);
        }

        public virtual bool Submit(IEnumerable<ChangeSetEntry> changeSet)
        {
            if (changeSet == null)
            {
                throw new ArgumentNullException("changeSetEntries");
            }

            _changeSet = new ChangeSet(changeSet, _description.EntityTypes);

            return ProcessSubmit(_changeSet);
        }

        protected abstract bool ProcessSubmit(ChangeSet changeSet);
    }
}
