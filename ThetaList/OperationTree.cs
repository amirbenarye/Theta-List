using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;


namespace ThetaList
{
    /// <summary>
    /// The main purpose of an operation tree is to efficently cache operations applied to an array list. The operations can later be applied back to the list at an O(n) runtime complexity.
    /// The Operation tree is essentialy a red black tree with some additional properties.
    /// Each node contains a key modifier that applies to all keys below it. When an item is inserted at index x , every tree node that is larger then or equal to x is increased by 1 (this is done in logarithmic time using key modifiers)
    /// Similarly , if a remove node is inserted , all indices larger then x are deacreased.
    /// Each Node also maintains an index balance field. This field is decreased by one when a remove operation is inserted under it and increase by one when an insert operation is inserted under it.
    /// the index balance allows to retrace the index of an item in the original non modified array 
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>ss
    [Serializable]
    public partial class OperationTree<T>
    {
        public struct OperationNode
        {
            public int Index { get; private set; }
            public T Value { get; private set; }
            public OperationType Operation { get; private set; }
            public int Count { get; private set; }
            public OperationNode(int index, T value, OperationType op, int count)
            {
                Index = index;
                Value = value;
                Operation = op;
                Count = count;
            }
        }

        /// <summary>
        /// represents a nil node in the redblack tree. this simplifies null checking when righting the code
        /// </summary>
        private readonly NilNode NIL = new NilNode();
        /// <summary>
        /// the amount of nodes contained by this tree
        /// </summary>
        private int mNodeCount = 0;
        /// <summary>
        /// a bounding number on the tree height
        /// </summary>
        private int mHeightBound = 0;
        /// <summary>
        /// stack used for in order tree walks
        /// </summary>
        private Stack<Node> mTreeWalkStack;
        /// <summary>
        /// the root node of the tree. This can be either a Node , NIL or null
        /// </summary>
        private Node mRoot;
        private Node mDummyNode = new Node();
        
        public OperationTree()
        {
        }

        /// <summary>
        /// returns a bound on the height of the search tree.
        /// </summary>
        public int HeightBound
        {
            get { return mHeightBound; }
        }

        public int NodeCount
        {
            get { return mNodeCount; }
            private set
            { 
                mNodeCount = value;
                CalcHeightBound();
            }
        }
        /// <summary>
        /// calculates a bound on the tree height. it follows the rbtree formula h <= 2 log (n+1)
        /// </summary>
        void CalcHeightBound()
        {
            mHeightBound = (int)(2 * Math.Log(NodeCount + 1, 2));
        }
        public int IndexBalance
        {
            get
            {
                if (mRoot == null || mRoot == NIL)
                    return 0;
                return mRoot.IndexBalance;
            }
        }

        /// <summary>
        /// performs an in order tree walk, going through all operations and returning lastOp as the last one
        /// </summary>
        /// <param name="lastOp"></param>
        /// <returns></returns>
        public IEnumerable<OperationNode> OperationData(OperationNode lastOp)
        {
            if (mRoot == null || mRoot == NIL)
            {
                yield return lastOp;
                yield break;
            }
            if (mTreeWalkStack == null)
                mTreeWalkStack = new Stack<Node>();
            Node current = mRoot;
            bool hasNodes = true;
            while (hasNodes)
            {
                if (current != NIL)
                {
                    ApplyModifier(current); // all modifier must be appied so that the indices are valid
                    mTreeWalkStack.Push(current);
                    current = current.Left;
                }
                else
                {
                    if (mTreeWalkStack.Count > 0)
                    {
                        current = mTreeWalkStack.Pop();
                        var op = current.OperationA;
                        if (op != null)
                            yield return new OperationNode(current.Key, op.Value, op.Operation, op.Count);
                        op = current.OperationB;
                        if (op != null)
                            yield return new OperationNode(current.Key, op.Value, op.Operation, op.Count);
                        current = current.Right;
                    }
                    else
                        hasNodes = false;
                }
            }
            yield return lastOp;
        }


        /// <summary>
        /// return true if the element is found in the tree. In this cas Val is filled with it's value.
        /// returns false if the element is not found , and then currentArrayIndex returns the current index at which the element should be found
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="currentArrayIndex"></param>
        /// <returns></returns>
        public bool Find(int key, out T val, out int currentArrayIndex)
        {
            currentArrayIndex = -1;
            val = default(T);

            if (mRoot == null)  // no tree
            {
                currentArrayIndex = key;
                return false;
            }
            Node current = mRoot;   // the current node
            int indexModifier = 0;  // the index modifier for the case the node is node found
            while (current != NIL)
            {
                //apply key modifier to this node
                ApplyModifier(current);
                //if true the next turn on the search tree is right
                if (current.Key <= key)
                {
                    indexModifier += current.Left.IndexBalance; // index should be decreased by all item on the left of it
                    indexModifier += Weight(current); // increase the index modifier by the weight of the current opertation
                    if (current.Key == key)
                        break;
                    
                    current = current.Right;    // turn right
                }
                else
                {

                    current = current.Left; // turn left
                }
            }
            currentArrayIndex = key - indexModifier;    // apply the indexmodifier to the key.
            if (current.OperationB != null) // if this a removeset node , then return the set value
            {
                val = current.OperationB.Value;
                return true;
            }
            if (current == NIL || current.OperationA.Operation == OperationType.Remove) // null or remove ops are ignored
                return false;
            val = current.OperationA.Value; // if this is an insert or set node return it's value
            return true;
        }

        /// <summary>
        /// simple method to delete a one child node from the tree.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        Node DeleteOneChildNode(Node n)
        {
            Node child = n.Left;
            if (child == NIL)
                child = n.Right;        // find the only child of n (or NIL if no child)

            if (child != NIL)       // if it has a child set the childs new parent
                child.Parent = n.Parent;
            if(n.Parent == NIL)     // make sure the root is properly set
                mRoot = child;
            else
            {
                if (n == n.Parent.Left)     // set the child as the child of it's new parent
                    n.Parent.Left = child;
                else
                    n.Parent.Right = child;
            }
            return child;   // the child is returned for later use
        }
        /// <summary>
        /// used for testing purpose. This will recursivly calculate the actual height of the tree and will compare it with the tree height bound.
        /// This is used to make sure the height is of log(NodeCount) magnitude and that rb tree properties are maintained
        /// </summary>
        public void ValidateBoundingHeight()
        {
            if (TreeHeight(mRoot) > mHeightBound +1)
                throw new InvalidOperationException();

        }
        int TreeHeight(Node n)
        {
            if (n == NIL)
                return 0;
            int left = TreeHeight(n.Left);
            int right = TreeHeight(n.Right);
            return 1 + Math.Max(left, right);
        }
        /// <summary>
        /// deletes a node from the operation tree.  Note that in the end of this call the content of n may have been replaced
        /// </summary>
        /// <param name="n"></param>
        void DeleteNode(Node n)
        {
            NodeCount--;
            ApplyModifier(n);       // apply modifiers prior to deletion
            int weight = Weight(n);
            SetParentsWeight(n, -weight);       //remove the weight of this node from parent nodes
            Node deleteNode = n;
            Node child = null;
            if (n.Left == NIL || n.Right == NIL)
                child = DeleteOneChildNode(n);      // if it has one child then delete it
            else
            {
                deleteNode = MinNode(n.Right);      // otherwise find the next node (that has only one child)
                int replacmentWeight = Weight(deleteNode);  
                SetParentsWeight(deleteNode, -replacmentWeight); // remove it's weight from the parents
                child = DeleteOneChildNode(deleteNode); // delete it
                n.DeleteWith(deleteNode);       // copy the data from the deleted node to the current node .
                SetParentsWeight(n, replacmentWeight);      //  reapply the parents weight on the replaced node
            }

            if (deleteNode.Color == NodeColor.Black)
                FixTreeAfterDelete(child);
        }
        int Weight(Node n)
        {
            if (n.IsNOOP())
                return 0;
            int w = Weight(n.OperationA.Operation);
            if(n.OperationA.Operation == OperationType.Remove)
                return w * n.OperationA.Count;
            return w;
        }
        int Weight(OperationType op)    // returns the weight of an operation. This is the value at which larger indices are pushed
        {
            switch (op)
            {
                case OperationType.Insert:
                    return 1;
                case OperationType.Remove:
                    return -1;
            }
            return 0;
        }
        /// <summary>
        /// Append an insert operation to the tree
        /// </summary>
        /// <param name="index"></param>
        /// <param name="val"></param>
        public void InsertOp(int index, T val)
        {
            ApplyOperation(index, val, OperationType.Insert);
        }
        /// <summary>
        /// Append a remove operation to the tree
        /// </summary>
        /// <param name="index"></param>
        public void RemoveOp(int index)
        {
            ApplyOperation(index, default(T), OperationType.Remove);
        }
        /// <summary>
        /// append a set opertation to the tree
        /// </summary>
        /// <param name="index"></param>
        /// <param name="val"></param>
        public void SetOp(int index, T val)
        {
            ApplyOperation(index, val, OperationType.Set);
        }

        /// <summary>
        /// Applies an operation to the tree
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        void ApplyOperation(int key, T val, OperationType op)
        {
            if (mRoot == null || mRoot == NIL)  // if the tree is empty set the operation to the roots
            {
                mRoot = new Node(key, val, NIL, NIL, NIL, NodeColor.Black, op);
                mRoot.IndexBalance = Weight(op);
                NodeCount = 1;
                return;
            }
            int weight = Weight(op);
            Node parent = NIL;
            Node current = mRoot;
            NodeOperation newOp = new NodeOperation(op, val);
            bool isLeft = false;
            while (current != NIL)
            {
                //apply key modifier to this node
                ApplyModifier(current);
                //if true the next turn on the search tree is right
                if (current.Key < key)
                {
                    isLeft = false;
                    parent = current;
                    current = current.Right;
                }
                else
                {
                    isLeft = true;
                    if (current.Right != NIL)
                        current.Right.KeyModifier += weight; // all keys that are larger then the current keys are increased by it's weight
                    if (current.Key == key) // if the indices are equal collision should be handeled
                    {
                        if (current.AppendOperation(newOp)) // try to assimilate the new operation in the node
                            break;
                    }
                    current.Key += weight;
                    parent = current;
                    current = current.Left;
                }
            }

            if (current == NIL) // the node was not assimilated
            {
                current = new Node(key, val, NIL, NIL, NIL, NodeColor.Red, op);
                ApplyOperationInsert(parent, current, isLeft, weight);
            }
            else
                SetParentsWeight(current, weight);
            //it is maintained here that current is not NIL
            if (current.IsNOOP())   // if the current node is NOOP remove it (this happens when the operation was assimilated in a nother node
            {
                DeleteNode(current);
            }
            else
            {
                if (op == OperationType.Remove)
                {
                    // if an item was removed , check that there is no node with a similar index in the tree
                    Node next = NextNode(current);
                    if (next.KeyModifier != 0)
                        throw new InvalidOperationException(); // should not happen
                    if (next != NIL && next.Key == key) // there is a duplicate key
                    {
                        mDummyNode.SetNodeOps(next);
                        DeleteNode(next);
                        current.AppendNode(mDummyNode); // append the node to the current node.
                        SetParentsWeight(current, Weight(mDummyNode));
                    }
                }
            }
        }

        /// <summary>
        /// finds the minimum node in a sub tree
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        Node MinNode(Node n)
        {
            if (n == NIL)
                throw new Exception();
            ApplyModifier(n);
            while (n.Left != NIL)
            {
                n = n.Left;
                ApplyModifier(n);
            }
            return n;
        }

        /// <summary>
        /// find the successor node in the tree
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        Node NextNode(Node n)
        {
            ApplyModifier(n);
            if (n.Right != NIL)
                return MinNode(n.Right);

            Node parent = n.Parent;
            while (parent != NIL && n == parent.Right)
            {
                if (n.KeyModifier != 0)
                    throw new InvalidOperationException(); // this means that we should make sure that all nodes have their value applied on them , this should work because of rotations appling key modifiers
                n = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        /// <summary>
        /// applies the index balane of the opertation to it's parents
        /// </summary>
        /// <param name="newNode"></param>
        /// <param name="weight"></param>
        void SetParentsWeight(Node newNode, int weight)
        {
            while (newNode != NIL)
            {
                newNode.IndexBalance += weight;
                newNode = newNode.Parent;
            }
        }

        /// <summary>
        /// Inserts the a new operation to the tree after ApplyOperation is called.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="isLeft"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="op"></param>
        /// <param name="weight"></param>
        void ApplyOperationInsert(Node parent, Node newNode, bool isLeft, int weight)
        {
            NodeCount++;
            newNode.Parent = parent;
            if (isLeft)
                parent.Left = newNode;
            else
                parent.Right = newNode;
            SetParentsWeight(newNode, weight);
            FixTreeAfterInsert(newNode);
        }

        /// <summary>
        /// Clear current tree
        /// </summary>
        public void Clear()
        {
            mRoot = NIL;
        }

        /// <summary>
        /// Check if the tree is empty
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return (mRoot == null || mRoot == NIL);
        }

        /// <summary>
        /// returns the Uncle of node n
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private Node Uncle(Node n)
        {
            Node parent = n.Parent;
            if (parent == NIL)
                return NIL;
            Node grandParent = parent.Parent;
            if (grandParent == NIL)
                return NIL;
            if (grandParent.Right == parent)
                return grandParent.Left;
            return grandParent.Right;
        }

        Node Sibling(Node n)
        {
            Node p = n.Parent;
            if (p == NIL)
                return NIL;
            if (n == p.Left)
                return p.Right;
            return p.Left;
        }

        void DeleteCase1And2(Node n)
        {
            if (n.Parent == NIL)
                return;
            Node s = Sibling(n);
            if (s.Color == NodeColor.Red)
            {
                n.Parent.Color = NodeColor.Red;
                s.Color = NodeColor.Black;
                if (n == n.Parent.Left)
                    RotateLeft(n.Parent);
                else
                    RotateRight(n.Parent);
            }
            DeleteCase3(n);
        }

        void DeleteCase3(Node n)
        {
            Node s = Sibling(n);
            if ((n.Parent.Color == NodeColor.Black) &&
                (s.Color == NodeColor.Black) &&
                (s.Left.Color == NodeColor.Black) &&
                (s.Right.Color == NodeColor.Black))
            {
                s.Color = NodeColor.Red;
                DeleteCase1And2(n.Parent);
            }
            else
                DeleteCase4(n);
        }

        void DeleteCase4(Node n)
        {
            Node s = Sibling(n);
            if ((n.Parent.Color == NodeColor.Red) &&
                (s.Color == NodeColor.Black) &&
                (s.Left.Color == NodeColor.Black) &&
                (s.Right.Color == NodeColor.Black))
            {
                s.Color = NodeColor.Red;
                n.Parent.Color = NodeColor.Black;
            }
            else
                DeleteCase5(n);
        }

        void DeleteCase5(Node n)
        {
            Node s = Sibling(n);
            if (s.Color == NodeColor.Black)
            {
                if ((n == n.Parent.Left) &&
                    (s.Right.Color == NodeColor.Black) &&
                    (s.Left.Color == NodeColor.Red))
                {
                    s.Color = NodeColor.Red;
                    s.Left.Color = NodeColor.Black;
                    RotateRight(s);
                }
                else if ((n == n.Parent.Right) &&
                          (s.Left.Color == NodeColor.Black) &&
                          (s.Right.Color == NodeColor.Red))
                {
                    s.Color = NodeColor.Red;
                    s.Right.Color = NodeColor.Black;
                    RotateLeft(s);
                }
            }
            DeleteCase6(n);
        }
        void DeleteCase6(Node n)
        {
            Node s = Sibling(n);

            s.Color = n.Parent.Color;
            n.Parent.Color = NodeColor.Black;

            if (n == n.Parent.Left)
            {
                s.Right.Color = NodeColor.Black;
                RotateLeft(n.Parent);
            }
            else
            {
                s.Left.Color = NodeColor.Black;
                RotateRight(n.Parent);
            }
        }


        private void FixTreeAfterDelete(Node child)
        {
            if (child.Color == NodeColor.Red)
                child.Color = NodeColor.Black;
            else
                DeleteCase1And2(child);
        }

        private void FindNewRoot(Node n)
        {
            if (n == NIL || n == null)
                throw new Exception();
            while (n.Parent != NIL)
                n = n.Parent;
            mRoot = n;
        }

        /// <summary>
        /// Fix after insert routine for rb tree implemented as described on wiki : https://en.wikipedia.org/wiki/Red%E2%80%93black_tree#Insertion
        /// </summary>
        /// <param name="n"></param>
        private void FixTreeAfterInsert(Node n)
        {
            if (n == NIL)
                return;
            Node parent = n.Parent;
            if (parent == NIL)
            {
                n.Color = NodeColor.Black;
                return;
            }

            if (parent.Color == NodeColor.Black)
                return;

            Node uncle = Uncle(n);
            Node gradParent = parent.Parent;
            if (uncle.Color == NodeColor.Red) // NIL is never red, this condition ensures that uncle and grandparent exist and non NIL
            {
                parent.Color = NodeColor.Black;
                uncle.Color = NodeColor.Black;
                gradParent.Color = NodeColor.Red;
                FixTreeAfterInsert(gradParent);
            }
            else
            {
                if (n == gradParent.Left.Right)
                {
                    RotateLeft(parent);
                    n = n.Left;

                }
                else if (n == gradParent.Right.Left)
                {
                    RotateRight(parent);
                    n = n.Right;
                }
                parent = n.Parent;
                gradParent = parent.Parent;
                if (n == parent.Left)
                    RotateRight(gradParent);
                else
                    RotateLeft(gradParent);                
                parent.Color = NodeColor.Black;
                gradParent.Color = NodeColor.Red;
            }
        }

        /// <summary>
        /// applies the key modifier of the node to it's key and children.
        /// </summary>
        /// <param name="n"></param>
        void ApplyModifier(Node n)
        {
            if (n == NIL)
                return;
            n.Key += n.KeyModifier; // apply to the current node
            if (n.Left != NIL)  // apply to child nodes if they exist
                n.Left.KeyModifier += n.KeyModifier;
            if (n.Right != NIL)
                n.Right.KeyModifier += n.KeyModifier;
            n.KeyModifier = 0;  // clear the key modifier
        }

        bool IsLeftChild(Node n)
        {
            if (n.Parent.Left == n)
                return true;
            return false;
        }
        /// <summary>
        /// tree rotate left opertation that also maintains indexbalances
        /// </summary>
        /// <param name="root"></param>
        void RotateLeft(Node root)
        {
            ApplyModifier(root);
            ApplyModifier(root.Right);
            Node newRoot = root.Right;
            if (newRoot == NIL)
                throw new Exception();
            int rootBalance = root.IndexBalance;
            root.IndexBalance += newRoot.Left.IndexBalance - newRoot.IndexBalance;
            newRoot.IndexBalance = rootBalance;


            root.Right = newRoot.Left;
            if(newRoot.Left != NIL)
                newRoot.Left.Parent = root;

            newRoot.Left = root;

            newRoot.Parent = root.Parent;
            if (root.Parent != NIL)
            {
                if (IsLeftChild(root))
                    root.Parent.Left = newRoot;
                else
                    root.Parent.Right = newRoot;
            }

            root.Parent = newRoot;

            if (mRoot == root)
                mRoot = newRoot;
        }
        /// <summary>
        /// tree rotate right opertation that also maintains indexbalances
        /// </summary>
        /// <param name="root"></param>
        void RotateRight(Node root)
        {
            ApplyModifier(root);
            ApplyModifier(root.Left);
            Node newRoot = root.Left;
            if (newRoot == NIL)
                throw new Exception();

            int rootBalance = root.IndexBalance;
            root.IndexBalance += newRoot.Right.IndexBalance - newRoot.IndexBalance;
            newRoot.IndexBalance = rootBalance;

            root.Left = newRoot.Right;
            if (newRoot.Right != NIL)
                newRoot.Right.Parent = root;

            newRoot.Right = root;
            newRoot.Parent = root.Parent;
            if (root.Parent != NIL)
            {
                if (IsLeftChild(root))
                    root.Parent.Left = newRoot;
                else
                    root.Parent.Right = newRoot;
            }

            root.Parent = newRoot;
            if (mRoot == root)
                mRoot = newRoot;
        }

    }
}