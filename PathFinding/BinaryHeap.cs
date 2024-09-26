using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class BinaryHeap<T> : ICollection<T> where T : IComparable<T>
{
    private const int defaultSize = 300; //origin defaultSize was 32
    private T[] pathNodeArray = null;
    private int count = 0;
    private int maxSize = 0;
    private bool isSorted = false;


    public bool IsReadOnly
    {
        get { return false; }
    }
    public int Count
    {
        get { return count; }
    }
    public int MaxSize
    {
        get { return maxSize; }
    }

    public BinaryHeap()
    {
        maxSize = defaultSize;
        pathNodeArray = new T[maxSize];
        isSorted = false;
        count = 0;
    }
    ~BinaryHeap()
    {
        pathNodeArray = null;
    }


    private void ResizeHeap()
    {
        int newSize = maxSize + defaultSize;
        maxSize = newSize;
        T[] temp = new T[newSize];
        Array.Copy(pathNodeArray, temp, count);
        pathNodeArray = temp;
    }

    private int Parent(int index)
    {
        int iPIndex = (index - 1) >> 1; //get parent index from child
        return iPIndex;
    }
    private int Child1(int index)
    {
        int iChild = index * 2 + 1; //bottom left child index
        return iChild;
    }
    private int Child2(int index)
    {
        int iChild = index * 2 + 2; //bottom right child index
        return iChild;
    }

    private void UpHeap(int iStartIndex)
    {
        int currentIndex = iStartIndex;
        T item = pathNodeArray[currentIndex];
        int pIndex = Parent(currentIndex);
        while (pIndex > -1 && item.CompareTo(pathNodeArray[pIndex]) < 0)
        {
            pathNodeArray[currentIndex] = pathNodeArray[pIndex]; //move node up the heap
            currentIndex = pIndex;

            pIndex = Parent(currentIndex);
        }
        pathNodeArray[currentIndex] = item;
        isSorted = false;
    }

    private void DownHeap()
    {
        int nIndex = 0;
        int sIndex = 0;
        int ch1, ch2;
        T item = pathNodeArray[sIndex];
        while (true)
        {
            ch1 = Child1(sIndex);
            if (ch1 >= count) break;
            ch2 = Child2(sIndex);
            if (ch2 >= count)
            {
                nIndex = ch1;
            }
            else
            {
                nIndex = pathNodeArray[ch1].CompareTo(pathNodeArray[ch2]) < 0 ? ch1 : ch2;
            }
            if (item.CompareTo(pathNodeArray[nIndex]) > 0)
            {
                pathNodeArray[sIndex] = pathNodeArray[nIndex]; //Swap nodes
                sIndex = nIndex;
            }
            else
            {
                break;
            }
        }
        pathNodeArray[sIndex] = item;
        isSorted = false;
    }

    private void EnsureSort()
    {
        if (isSorted) return;
        Array.Sort(pathNodeArray, 0, count);
        isSorted = true;
    }

    public void Clear()
    {
        count = 0;
        pathNodeArray = new T[maxSize];
    }

    public void Add(T item)
    {
        if (count >= maxSize)
        {
            ResizeHeap();
        }
        pathNodeArray[count] = item; //add new item at the end
        UpHeap(count);
        count++;
    }

    public T PopRoot()
    {
        if (count == 0)
        {
            return default(T);
        }

        T root = pathNodeArray[0];
        count--;

        // Move last node to top.
        pathNodeArray[0] = pathNodeArray[count];
        pathNodeArray[count] = default(T);
        DownHeap();
        return root;
    }

    public bool Remove(T item)
    {
        EnsureSort();
        int i = Array.BinarySearch<T>(pathNodeArray, 0, count, item); //search for the index of item
        if (i < 0) return false; // Not found
        Array.Copy(pathNodeArray, i + 1, pathNodeArray, i, count - i); 
        pathNodeArray[count] = default(T);
        count--;
        return true;
    }

    public bool Contains(T item)
    {
        EnsureSort();
        bool bFound = true;
        if (Array.BinarySearch<T>(pathNodeArray, 0, count, item) < 0)
        {
            bFound = false;
        }
        return bFound;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        EnsureSort();
        Array.Copy(pathNodeArray, array, count);
    }

    public IEnumerator<T> GetEnumerator()
    {
        EnsureSort();
        for (int i = 0; i < count; i++)
        {
            yield return pathNodeArray[i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

}
