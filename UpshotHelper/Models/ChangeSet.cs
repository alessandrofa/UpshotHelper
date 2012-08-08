using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UpshotHelper.Models
{
    public sealed class ChangeSet
    {
        private IEnumerable<ChangeSetEntry> _changeSetEntries;
        /// <summary> Gets the set of <see cref="T:UpshotHelper.Models.ChangeSetEntry" /> items this <see cref="T:System.Web.Http.Data.ChangeSet" /> represents. </summary>
        public ReadOnlyCollection<ChangeSetEntry> ChangeSetEntries
        {
            get
            {
                return this._changeSetEntries.ToList<ChangeSetEntry>().AsReadOnly();
            }
        }
        /// <summary> Initializes a new instance of the ChangeSet class </summary>
        /// <param name="changeSetEntries">The set of <see cref="T:UpshotHelper.Models.ChangeSetEntry" /> items this <see cref="T:UpshotHelper.Models.ChangeSet" /> represents.</param>
        /// <param name="entityTypes"></param>
        public ChangeSet(IEnumerable<ChangeSetEntry> changeSetEntries, ReadOnlyCollection<Type> entityTypes)
        {
            if (changeSetEntries == null)
            {
                throw new ArgumentNullException("changeSetEntries");
            }

            foreach(ChangeSetEntry entry in changeSetEntries)
            {
                entry.Entity = SetEntity(entry.Entity, entityTypes);
                entry.OriginalEntity = SetEntity(entry.OriginalEntity, entityTypes);
            }

            this._changeSetEntries = changeSetEntries;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityTypes"></param>
        /// <returns></returns>
        private object SetEntity(object entity, ReadOnlyCollection<Type> entityTypes)
        {
            if (entity.GetType() == typeof(JObject))
            {
                JObject entityJObject = entity as JObject;
                if (entity != null)
                {
                    JToken typename = entityJObject["__type"];
                    string str = typename.ToString();
                    string[] splitstr = str.Split(new string[] { ":#" }, StringSplitOptions.RemoveEmptyEntries);

                    Type typeToGet = entityTypes.SingleOrDefault(x => x.FullName == string.Format("{0}.{1}", splitstr[1], splitstr[0]));
                    return JsonConvert.DeserializeObject(entityJObject.ToString(), typeToGet);
                }
            }
            return null;
        }
    }
}
