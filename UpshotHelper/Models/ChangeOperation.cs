using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpshotHelper.Models
{
    /// <summary> Enumeration of the types of operations a <see cref="T:Upshot.ChangeSetEntry" /> can perform. </summary>
    public enum ChangeOperation
    {
        /// <summary> Indicates that no operation is to be performed </summary>
        None,
        /// <summary> Indicates an operation that inserts new data </summary>
        Insert,
        /// <summary> Indicates an operation that updates existing data </summary>
        Update,
        /// <summary> Indicates an operation that deletes existing data </summary>
        Delete,
        /// <summary> Indicates a custom update operation </summary>
        Custom
    }
}
