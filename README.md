# Theta-List 

A neat data structure that combines the best of array lists and red-black trees. it was created in order to be used in [Graph And Chart](http://u3d.as/FAT)

Array lists are a great data structure , They have very fast lookup no size limit and are very cache friendly. The down side of using an array list is when removing or inserting items inside the list . for example if we have an array list with 1 million objects the following code may take several hours to complete:

`for(int i=0; i<1000000; i++)	
	list.Insert(0,i); `

Theta-List uses a red-black tree to store insert and remove operations and then apply them to the array list at once. Thus reducing the complexity and time of insertion and removal. The tree stores only new operations while the rest of the list is stored in an array, making the tree as light weight as possible.
 
Theta-lists can be used just like any IList\<T\> at any time. However when you are done with modifying the theta-list , you can all Commit() and turn it back into an array list.
 
		` for(int i=0; i<1000000; i++)
				theta.Insert(0,i);
		  theta.Commit(); ` 
		  
Calling commit will not reacllocate the underlying array unless it's capacity is exceeded. just like in an array list. Making the commit method as efficient like one insert operation on an array list.

These are the time complexities for all methods with n being the amount of items in the list :

Insert - O(log(n)) 
Remove - O(log(n))
Commit - O(n)

Get/Set operations:
After calling Commit - O(1)
Before calling Commit - O(log(n));