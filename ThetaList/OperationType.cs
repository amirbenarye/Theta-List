using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThetaList
{
    public partial class OperationTree<T>
    {
        /// <summary>
        /// Operation types allows on the operation tree
        /// </summary>
        public enum OperationType
        {
            Insert,
            Remove,
            Set,
            /// <summary>
            /// this is the last operation returned when traversing the tree
            /// </summary>
            EndOp,
            
        }
    }
}
