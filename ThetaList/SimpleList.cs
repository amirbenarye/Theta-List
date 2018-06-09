using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThetaList
{
    /// <summary>
    /// a simple implementation of an array list 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class SimpleList<T> : IList<T>
    {
        /// <summary>
        /// the work queue is used when applying an operation tree to the list
        /// </summary>
        [ThreadStatic]
        static Queue<T> mWorkQueue = new Queue<T>();  
        int mCount = 0;
        T[] mData;


        /// <summary>
        /// best not to access this. the underlying array of the list.
        /// </summary>
        public T[] RawArray
        {
            get { return mData; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity">the inital capacity of the list</param>
        public SimpleList(int capacity)
        {
            mData = new T[capacity];
            GrowFactor = 2;
        }

        public SimpleList()
            : this(4)
        {

        }
        /// <summary>
        /// applies an operation tree to the list. this is done wihout reallicating the underlying array (unless it's capcity is exceeded)
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="operations">the operation tree</param>
        public void ApplyOperations(OperationTree<T> operations)
        {
            ApplyOperations(operations, x => x);

        }
        /// <summary>
        /// applies an operation tree to the list. this is done wihout reallicating the underlying array (unless it's capcity is exceeded)
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <param name="operations">the operation tree</param>
        /// <param name="convert">converts an item in the operation tree to an item of the list</param>
        public void ApplyOperations<S>(OperationTree<S> operations,Func<S,T> convert)
        {
            int newCount = mCount + operations.IndexBalance;
            EnsureCapacity(newCount);
            mWorkQueue.Clear();
            int writeIndex = 0;
            int readIndex =0;
            bool hasDequeValue = false;
            T dequeValue = default(T);
            OperationTree<S>.OperationNode lastOp = new OperationTree<S>.OperationNode(newCount,default(S),OperationTree<S>.OperationType.EndOp,1);
            foreach (OperationTree<S>.OperationNode op in operations.OperationData(lastOp))
            {

                for (; writeIndex < op.Index; writeIndex++, readIndex++)
                {
                    if (readIndex < mCount)
                        mWorkQueue.Enqueue(mData[readIndex]);
                    if (hasDequeValue)
                    {
                        hasDequeValue = false;
                        mData[writeIndex] = dequeValue;
                        if (mWorkQueue.Count > 0)
                            mWorkQueue.Dequeue();
                    }
                    else
                    {

                        if (mWorkQueue.Count > 0)
                            mData[writeIndex] = mWorkQueue.Dequeue();
                    }
                }

                switch(op.Operation)
                {
                        case OperationTree<S>.OperationType.Insert:
                            if (readIndex < mCount)
                                mWorkQueue.Enqueue(mData[readIndex]);
                            mData[writeIndex] = convert(op.Value);
                            writeIndex++;
                            readIndex++;
                        break;
                        case OperationTree<S>.OperationType.Set:
                            hasDequeValue = true;
                            dequeValue = convert(op.Value);
                        break;
                        case OperationTree<S>.OperationType.Remove:
                            int i = op.Count;
                            for(; mWorkQueue.Count > 0 && i>0 ; --i)
                                mWorkQueue.Dequeue();
                            readIndex+= i;
                        break;
                        default:
                        break;
                }
            }
            mCount = newCount;
        }

        void ValidateIndex(int index)
        {
            if (index < 0 || index >= mCount)
                throw new IndexOutOfRangeException();
        }

        public T this[int index]
        {

            get
            {
                ValidateIndex(index);
                return mData[index];
            }
            set
            {
                ValidateIndex(index);
                mData[index] = value;
            }
        }

        public int Count { get { return mCount; } }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public double GrowFactor
        {
            get;
            private set;
        }
        void EnsureCapacity(int capacity)
        {
            if (capacity <= mData.Length)
                return;
            int newSize = Math.Max(capacity, (int)(mData.Length * GrowFactor));
            T[] arr = new T[newSize];
            Array.Copy(mData, arr, mCount);
            mData = arr;
        }

        T[] AddEmpty()
        {
            EnsureCapacity(mCount + 1);
            ++mCount;
            return mData;
        }

        public void Append(SimpleList<T> list)
        {
            EnsureCapacity(mCount + list.mCount);
            Array.Copy(list.mData, 0, mData, mCount, list.mCount);
            mCount += list.mCount;
        }

        public void Add(T item)
        {
            EnsureCapacity(mCount + 1);
            mData[mCount] = item;
            ++mCount;
        }
        public T[] AddEmpty(int count)
        {
            EnsureCapacity(mCount + count);
            mCount += count;
            return mData;
        }
        /// <summary>
        /// If T is a value type , sometimes it is not necessary to set all value the default value. So this clear method works at O(1)
        /// </summary>
        public void ClearWithoutRelease()
        {
            mCount = 0;
        }
        public void Clear()
        {
            Array.Clear(mData, 0, mCount);
            mCount = 0;
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < mCount; i++)
                if (mData[i].Equals(item))
                    return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(mData, 0, array, arrayIndex, mCount);
        }
        public void AddRange(IEnumerable<T> range)
        {
            foreach (T t in range)
                Add(t);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return mData.Take(mCount).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < mCount; i++)
                if (mData[i].Equals(item))
                    return i;
            return -1;
        }

        public void Insert(int index, T item)
        {
            EnsureCapacity(mCount + 1);
            if (index < mCount)
            {
                Array.Copy(mData, index, mData, index + 1, mCount - index);
            }
            mData[index] = item;
            mCount++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (mCount == 0)
                throw new IndexOutOfRangeException();
            mCount--;
            if (index < mCount)
                Array.Copy(mData, index + 1, mData, index, mCount - index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
