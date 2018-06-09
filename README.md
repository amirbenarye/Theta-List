# Theta-List 

A neat data structure that combines the best properties of array lists and red-black trees. it was created to be used in [Graph And Chart](http://u3d.as/FAT)

Array lists are a great data structure , They have very fast lookup no size limit and are very cache friendly. Their down side is when removing or inserting items. for example if we have an array list with 1 million objects the following code may take several hours to complete:

`for(int i=0; i<1000000; i++)	
	list.Insert(0,i); `

Theta-List uses a red-black tree to store all insert and remove operations. That way you can apply them to the array list at once, Thus reducing the complexity and time of insertion and removal. The tree is a light weight and stores only newly applied operations, the rest of data is stored in the array.
 
Theta-lists can be used just like any IList\<T\> at any time. When you call commit , all the operation in the RB tree will be applied to the array allowing fast lookup times, just like in array list.  Unlike the code one above, the following code will finish instantly :
 
		` for(int i=0; i<1000000; i++)
				theta.Insert(0,i);		// equivalent to RB tree insert , the RB tree stores only new opertions while the rest of the data is stored in the array
		  Console.WriteLine(theta[0]); // equivalent to RB tree lookup
		  theta.Commit();				// applies all the new operations to the array with O(n) time complexity
		  Console.WriteLine(theta[0]); // equivalent to array list lookup ` 

Calling commit will not reacllocate the underlying array unless it's capacity is exceeded, just like an array list.In a most cases calling commit is as efficient as one insert operation on a regular array list.

These are the time complexities for all methods with n being the amount of items in the list :

Insert - O(log(n)) 
Remove - O(log(n))
Commit - O(n)

Get/Set operations:
After calling Commit - O(1)
Before calling Commit - O(log(n));