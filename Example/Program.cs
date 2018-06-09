using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThetaList;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            ThetaList<int> theta = new ThetaList<int>();

            int largeNumber = 1000000;

            for (int i = 0; i < largeNumber; i++)
                theta.Add(i);
            Console.WriteLine("Stating " + largeNumber + " inserts on a theta list");

            // all the operations you perform are cached in a red black tree
            // the size of the tree is bound by the amount of the operations you perform
            for (int i = 0; i < largeNumber; i++)
                theta.Insert(i, i);

            // loopups and sets correspond to the operations you have performed
            theta[5] = 0;
            Console.WriteLine("value at index 5 is : " + theta[5]);

            theta.RemoveAt(5);

            // call commit to apply the red black tree back to the arraylist.
            // this will leave the tree empty and the theata list behaves exacly like an array list for lookups and set operations
            theta.Commit();        // calling commit will not reallocate the array , unless it's capcity is exceeded

            Console.WriteLine("done");



            // let's compare it with a regular list
            List<int> list = new List<int>();
            for (int i = 0; i < largeNumber; i++)
                list.Add(i);

            Console.WriteLine("Stating "+ largeNumber + " inserts on a regular array list, this is gonna take some time...");

            for (int i = 0; i < largeNumber; i++)
                list.Insert(i, i);
            Console.WriteLine("done");
            Console.WriteLine("Press any key to end");
            Console.ReadKey(true);
        }
    }
}
