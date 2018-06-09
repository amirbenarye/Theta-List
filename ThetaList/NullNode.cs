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
        /// an implemention of a null node that makes things easy when implementing rbtree
        /// </summary>
        [Serializable]
        public sealed class NilNode : Node
        {
            public NilNode()
                : base()
            {
                mLeft = this;
                mRight = this;
                mParent = this;
            }
            public override Node Left { get { return base.Left; } set {  } }
            public override Node Right { get { return base.Right; } set { } }
            public override Node Parent { get { return base.Parent; } set { } }
            public override NodeColor Color { get { return base.Color; } set { } }
        }
    }
}