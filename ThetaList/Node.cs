using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ThetaList
{
    public partial class OperationTree<T>
    {
        /// <summary>
        /// a single operation object that is held by a node
        /// </summary>
        public class NodeOperation
        {
            public OperationType Operation { get; set; }
            /// <summary>
            /// the amount of time the operation was performed. This should 1 for all operation except remove
            /// </summary>
            public int Count { get; set; }
            /// <summary>
            /// the value of the operations (for insert and set ops)
            /// </summary>
            public T Value { get; set; }

            public NodeOperation()
            {
                Count = 1;
            }
            public NodeOperation(OperationType op,T val)
            {
                Operation = op;
                Value = val;
                Count = 1;
            }
        }

        [Serializable]
        public class Node
        {
            public int Key { get; set; }
            /// <summary>
            /// the key modifier is a value that is added to the key to determine the real key of the current node and the nodes below it.
            /// this is useful if we want to increase or deacrease the key value of an entire subtree in constant time
            /// </summary>
            public int KeyModifier { get; set; }
            /// <summary>
            /// the amount of insert operations minus the amount of remove operations in this node and the ones below it. This is useful for calculting the original index from an index in the theta list 
            /// </summary>
            public int IndexBalance { get; set; }
            public virtual NodeColor Color { get { return mColor; } set { mColor = value; } }
            public virtual Node Left
            {
                get { return mLeft; }
                set { mLeft = value; }
            }
            public virtual Node Right
            {
                get { return mRight; }
                set { mRight = value; }
            }
            public virtual Node Parent
            {
                get { return mParent; }
                set { mParent = value; }
            }

            protected NodeColor mColor;
            protected Node mLeft, mRight, mParent;
            /// <summary>
            /// the first operation of this node. This can be either set inset or remove
            /// </summary>
            public NodeOperation OperationA { get { return mOperationA; } }
            /// <summary>
            /// second operation. By rule if this is not null it means that OperationA is remove and OperationB is set. if not the state of this instance is invalid
            /// </summary>
            public NodeOperation OperationB { get { return mOperationB; } }
            private NodeOperation mOperationA;
            /// <summary>
            /// second operation. By rule if this is not null it means that OperationA is remove and OperationB is set. if not the state of this instance is invalid
            /// </summary>
            private NodeOperation mOperationB;

            public void DeleteWith(Node n)
            {
                Key = n.Key;
                if (n.KeyModifier != 0 || KeyModifier != 0)
                    throw new InvalidOperationException();
                mOperationA = n.mOperationA;
                mOperationB = n.mOperationB;
            }
            /// <summary>
            /// This method will try to assimilate a new operation into the node. This method maintains the rules of the operation node
            /// (only allowed combinations are insert,set,remove and remove set)
            /// returns true if the operation was assimilated into the node.
            /// 
            /// </summary>
            /// <param name="currentA"></param>
            /// <param name="currentB"></param>
            /// <param name="newOp"></param>
            /// <returns></returns>
            private bool Append(ref NodeOperation currentA, ref NodeOperation currentB, NodeOperation newOp)
            {
                switch (newOp.Operation)
                {
                    case OperationType.Insert:
                        return InsertHandler(ref currentA, ref currentB, newOp);
                    case OperationType.Set:
                        return SetHandler(ref currentA, ref currentB, newOp);
                    case OperationType.Remove:
                        return RemoveHandler(ref currentA, ref currentB, newOp);
                }
                throw new InvalidOperationException();
            }
            /// <summary>
            /// handles appending of an insert operation to the node
            /// </summary>
            /// <param name="current"></param>
            /// <param name="currentB"></param>
            /// <param name="newOp"></param>
            /// <returns></returns>
            private bool InsertHandler(ref NodeOperation current, ref NodeOperation currentB, NodeOperation newOp)
            {
                if (current == null)    // this is no operation node , push it right and continue
                    return false;
                if (current.Operation == OperationType.Remove)
                    return InsertOnRemoveHandler(ref current, ref currentB, newOp);
                return false; // both insert and set require pushing this node right and continuing with insertion
            }

            /// <summary>
            /// it is maintaind the current operation type is remove and newOp operation type is insert
            /// </summary>
            /// <param name="current"></param>
            /// <param name="currentB"></param>
            /// <param name="newOp"></param>
            /// <returns></returns>
            private bool InsertOnRemoveHandler(ref NodeOperation current, ref NodeOperation currentB, NodeOperation newOp)
            {
                if (currentB == null)    // insert done on remove, change it to set
                {
                    if (current.Count == 1)   // if there is only one remove 
                    {
                        current.Operation = OperationType.Set;
                        current.Value = newOp.Value;
                    }
                    else
                    {
                        current.Count--;        //decrease the remove count
                        currentB = new NodeOperation();             // and append a new set opertaion
                        currentB.Operation = OperationType.Set;
                        currentB.Value = newOp.Value;
                    }
                    return true;    // the insert operation is assimilated into the node
                }
                //the only options here is that currentB operation type is set  (other operationB are not allowed, it always remove and set
                if (currentB.Operation != OperationType.Set)
                    throw new Exception();
                return false; // the insert should push this right and continue
            }
            /// <summary>
            /// handles appending of a set operation to the node
            /// </summary>
            /// <param name="current"></param>
            /// <param name="currentB"></param>
            /// <param name="newOp"></param>
            /// <returns></returns>
            private bool SetHandler(ref NodeOperation current, ref NodeOperation currentB, NodeOperation newOp)
            {
                if (current == null)    // this is no operation node , apply the set operation
                {
                    current = newOp;
                    return true;  // the set operation is assimilated into the node
                }
                if (current.Operation == OperationType.Insert || current.Operation == OperationType.Set) // this means currentB is false (because it is only allowed when current is remove and currentb is set
                {
                    current.Value = newOp.Value; // just use the new value instead
                    return true;
                }
                //current operation is remove
                if (currentB == null)    // this should be removeSet node then , if there is no set it is created
                    currentB = newOp;
                else
                {
                    if (currentB.Operation != OperationType.Set)
                        throw new InvalidOperationException();
                    currentB.Value = newOp.Value;
                }
                return true; // the set operation is assimilated into the node

            }
            /// <summary>
            /// handles appending of a remove operation to the node
            /// </summary>
            /// <param name="current"></param>
            /// <param name="currentB"></param>
            /// <param name="newOp"></param>
            /// <returns></returns>
            private bool RemoveHandler(ref NodeOperation current, ref NodeOperation currentB, NodeOperation newOp)
            {
                if (current == null)    // this is no operation node , push it right and continue
                {
                    current = newOp;
                    return true;  // the remove operation is assimilated into the node
                }
                if (current.Operation == OperationType.Insert)
                {
                    if (newOp.Count != 1)
                        throw new InvalidOperationException();
                    current = null; // this is a noop , insert and then remove at the same index
                    currentB = null;
                    return true; // the remove operation is assimilated into the node
                }
                if (current.Operation == OperationType.Set)
                {
                    if (newOp.Count != 1)
                        throw new InvalidOperationException();
                    // setting a value and the removeing that index , it should be overriden
                    current = newOp;
                    return true; // the remove operation is assimilated into the node
                }
                //current operation is remove.
                currentB = null; // regardles of the value of currentB , the set operation that it might contain is removed
                                 //no addiational operations just add one to the remove count
                current.Count += newOp.Count;
                return true; // the remove operation is assimilated into the node
            }

            /// <summary>
            /// copies the operations of another node to this node
            /// </summary>
            /// <param name="n"></param>
            public void SetNodeOps(Node n)
            {
                mOperationA = n.OperationA;
                mOperationB = n.OperationB;
            }
            /// <summary>
            /// this is called if two nodes of similar index are identified. They are joined
            /// </summary>
            /// <param name="n"></param>
            public void AppendNode(Node n)
            {
                if (n.mOperationA != null) // same as have the operations done in order
                {
                    AppendOperation(n.mOperationA);
                    if(n.mOperationB != null)
                    {
                        AppendOperation(n.mOperationB);
                    }
                }
                
            }

            /// <summary>
            /// try to append an operation to the node. returns true if the node has been assimilated , false otherwise
            /// </summary>
            /// <param name="newOp"></param>
            /// <returns></returns>
            public bool AppendOperation(NodeOperation newOp)
            {
                return Append(ref mOperationA, ref mOperationB, newOp);
            }

            public Node()
            {
                Key = 0;
                KeyModifier = 0;
                mColor = NodeColor.Black;
            }


            /// <summary>
            /// returns true if this node has no operations attached
            /// </summary>
            /// <returns></returns>
            public bool IsNOOP()
            {
                if (mOperationA == null)
                    return true;
                return false;
            }
            public Node(int key, T value, Node left, Node right,Node parent, NodeColor color,OperationType op)
            {
                Key = key;
                Parent = parent;
                Left = left;
                Right = right;
                Color = color;
                mOperationA = new NodeOperation();
                mOperationA.Count = 1;
                mOperationA.Operation = op;
                mOperationA.Value = value;
            }

        }
    }
}