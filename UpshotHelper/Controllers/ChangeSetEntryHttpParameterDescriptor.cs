using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using UpshotHelper.Models;

namespace UpshotHelper.Controllers
{
    public class ChangeSetEntryHttpParameterDescriptor : HttpParameterDescriptor
    {
        public ChangeSetEntryHttpParameterDescriptor(HttpActionDescriptor actionDescriptor)
            : base(actionDescriptor)
        {
        }

        public override string ParameterName
        {
            get { return "changeSet"; }
        }

        public override Type ParameterType
        {
            get { return typeof(ChangeSetEntry); }
        }
    }
}
