using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThetaList
{
    /// <summary>
    /// Theta list is an implementaion of a list that takes the best properties out of rb trees and array list.
    /// It works as follows :
    /// When inserting and removing object from the list , these operations are saved in an rb tree. The size of the rb tree is corralted with the amount of operations performed
    /// calling get and set work as expected and will either use values from the tree or from the list.
    /// when you call Commit , all the operations from the tree are applied to back to the arraylist in o(n) time. there is no reallocation of the underlying array unless it's capacity is exceeded , just like a regular array list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThetaList<T> : IList<T>
    {
        /// <summary>
        /// the operation tree caches operations done on the list
        /// </summary>
        OperationTree<T> mTree = new OperationTree<T>();
        /// <summary>
        /// the main list object
        /// </summary>
        SimpleList<T> mList;
        /// <summary>
        /// the item count in the list
        /// </summary>
        int mCount = 0;

        public ThetaList()
        {
            MaximumPerformanceIndicator = int.MaxValue; // this means you have to call commit mannualy
            mList = new SimpleList<T>();
        }

        public ThetaList(int initalCapcity)
        {
            MaximumPerformanceIndicator = int.MaxValue; // this means you have to call commit mannualy
            mList = new SimpleList<T>(initalCapcity);
        }
        public T this[int index]
        {
            get
            {
                T res;
                Get(index, out res);
                return res;
            }
            set
            {
                Set(index, value);
            }
        }

        /// <summary>
        /// The higher this value , the more time it takes to get,set operations to complete
        /// returns an indicator of the data structures performance.This indicator states the maxium amount of tree nodes it takes to get a value from the array or to apply a new operation
        /// For simplity you can treat it as rough indicator of the difference betweem a regular list lookup to the current theta list lookup.
        /// Once you call Commit on the list this indicator will turn to 0, thus stating a constant lookup time 
        /// </summary>
        public int PerformanceIndicator
        {
            get { return mTree.HeightBound; }
        }

        /// <summary>
        /// The maximum value allowed for PerformanceIndicator. if the PerformanceIndicator is over MaximumPerformanceIndicator then Commit will be called.
        /// if this value is set to int.MaxValue this means that you should call commit mannualy when you want to perform a large amount of lookup and set opertations
        /// </summary>
        public int MaximumPerformanceIndicator
        {
            get;
            set;
        }
        /// <summary>
        /// validates that index is within range that includes mCount
        /// </summary>
        /// <param name="index"></param>
        void ValidateIndexInclusive(int index)
        {
            if (index < 0 || index > Count)
                throw new IndexOutOfRangeException();
        }
        /// <summary>
        /// validates the index is within range excluding mCount
        /// </summary>
        /// <param name="index"></param>
        void ValidateIndex(int index)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// gets an item from the theta list.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="val"></param>
        void Get(int index,out T val)
        {
            ValidateIndex(index);       
            if (mTree.NodeCount == 0)
                val = mList[index];         // if the tree is empty we just return the item in the list
            int currentArrayIndex;          // this contains the index of the value if it is not found on the tree
            if (mTree.Find(index, out val, out currentArrayIndex) == false) // try finding the value in the tree
                val = mList[currentArrayIndex]; // if it is not present , take it from the list
        }

        /// <summary>
        /// sets an item to the theta list
        /// </summary>
        /// <param name="index"></param>
        /// <param name="val"></param>
        void Set(int index, T val)
        {
            ValidateIndex(index);
            if (mTree.NodeCount == 0)   // if the tree is empty we just set on the list
                mList[index] = val;
            else
            {
                mTree.SetOp(index, val);    // other wise we append the set operation to the tree
                MaintainPerformance();
            }
        }

        /// <summary>
        /// this is called after an operation is performed to re assign mCount with the correct value
        /// </summary>
        void MaintainCount()
        {
            mCount= mTree.IndexBalance + mList.Count; 
        }

        /// <summary>
        /// makes sure that PerformanceIndicator is smaller then or equal to MaximumPerformanceIndicator
        /// </summary>
        void MaintainPerformance()
        {
            if (PerformanceIndicator > MaximumPerformanceIndicator)
                Commit();
        }

        /// <summary>
        /// returns the total amount of elements in the list. this takes into account all operations in the operation tree.
        /// </summary>
        public int Count
        {
            get
            {
                return mCount;
            }
        }

        public bool IsReadOnly { get { return false; } }

        public void Add(T item)
        {
            if (mTree.NodeCount == 0)
            {
                mList.Add(item);    // normal add if there is no tree (this will take constant time unless the list has to be recreated)
                MaintainCount();
            }
            else
                Insert(Count, item);    // otherwise perform insert at the end of the list
        }

        public void Clear()
        {
            mList.Clear();
            mTree.Clear();
            MaintainCount();
        }

        /// <summary>
        /// When commit is called, all the operations in the rb tree are applied to the array list , the underlying array is not reallocated unless it's capacity is exceeded.
        /// this operation takes O(n) time
        /// </summary>
        public void Commit()
        {
            if (mTree.NodeCount == 0)
                return; // nothing to comit
            mList.ApplyOperations(mTree);
            mTree.Clear();
        }
        public bool Contains(T item)
        {
            Commit();    // this method required iteration of all objects , so it is best to commit first
            return mList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Commit();    // this method required iteration of all objects , so it is best to commit first   
            mList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Commit();    // this method required iteration of all objects , so it is best to commit first
            return mList.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            Commit();    // this method required iteration of all objects , so it is best to commit first
            return mList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ValidateIndexInclusive(index);
            mTree.InsertOp(index, item);    // apply the insert operation to the tree
            MaintainPerformance();  // maintain the constants of the theta list
            MaintainCount();
        }

        public bool Remove(T item)
        {
            Commit();   // this method required iteration of all objects , so it is best to commit first
            return mList.Remove(item);
        }

        public void RemoveAt(int index)
        {
            mTree.RemoveOp(index);  // apply the remove operation to the tree
            MaintainPerformance(); // maintain the constants of the theta list
            MaintainCount();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Commit();  // this method required iteration of all objects , so it is best to commit first
            return mList.GetEnumerator();
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (mTree.NodeCount == 0)
            {
                mList.AddRange(items);
                MaintainCount();
            }
            else
            {
                foreach (T it in items)
                    Add(it);
            }
           
        }
    }
}
