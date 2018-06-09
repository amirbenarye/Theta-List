using System;
using System.Collections.Generic;
using ThetaList;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace IntegrityTest
{
    class Program
    {


        /// <summary>
        /// compares the contents of two IList objects
        /// </summary>
        /// <param name="listA"></param>
        /// <param name="listB"></param>
        /// <returns></returns>
        static bool Compare(List<int> listA, IList<int> listB)
        {
            if (listA.Count != listB.Count)
                return false;
            for (int i = 0; i < listA.Count; i++)
            {
                if (listA[i] != listB[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// this program tests the operations of a theta list using a regular array list to verify integrity
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Random r = new Random();

            ThetaList<int> theta = new ThetaList<int>() ;
            List<int> list = new List<int>();
            int totalTests = 100000;

            for (int t = 0; t < totalTests; t++)
            {
                int total = r.Next(0, t);  // use randomly increaseing values
                int totalOps = r.Next(0, t); ; // 

                Console.WriteLine("start test " + t);
                Console.WriteLine("total in list: " + total);
                Console.WriteLine("total operations: " + totalOps);

                list.Clear();       //clear both lists from previous tests
                theta.Clear();      

                for (int i = 0; i < total; i++)
                    list.Add(r.Next());             // create both lists with random data
                theta.AddRange(list);


                /*---------------------*/


                for (int i = 0; i < totalOps; i++)
                {
                    int op = r.Next(0, 3);
                    if (list.Count == 0)
                        op = 0;
                    if (op == 0)
                    {
                        int index = r.Next(list.Count);
                        int item = r.Next();
                        //Console.WriteLine("inserting at: " + index + " value: " + item);
                        list.Insert(index, item);
                        theta.Insert(index, item);
                    }
                    else if (op == 1)
                    {

                        int index = r.Next(list.Count);
                        int item = r.Next();
                        //  Console.WriteLine("set at: " + index + " value: " + item);
                        list[index] = item;
                        theta[index] = item;
                    }
                    else
                    {
                        int index = r.Next(list.Count);
                        list.RemoveAt(index);
                        theta.RemoveAt(index);
                    }   
                }
                if (Compare(list, theta) == false)  // compare the lists before calling commit
                    Debugger.Break();

                theta.Commit();

                if (Compare(list, theta) == false)  // compare the lists after calling commit
                    Debugger.Break();
                Console.WriteLine("test succeeded");

            }
            Console.WriteLine("done");
            Console.ReadKey(true);
        }
    }
}
