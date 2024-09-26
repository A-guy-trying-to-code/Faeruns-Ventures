using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodePool<T> : IEnumerable<T>
{
    public struct NodeInfo
    {
        public int nodeIndex;
        public bool isActive;
        public T m_Item;
    };
    private int defaultSize;
    private int maxSize;
    private bool canResize;
    private NodeInfo[] nodeInfoArray;



    public NodePool()
    {
        defaultSize = 0;
        nodeInfoArray = null;
        canResize = true;
    }

    private int GetUnActiveElement()
    {
        int i = 0;
        for (i = 0; i < maxSize; i++)
        {
            if (nodeInfoArray[i].isActive == false)
            {
                break;
            }
        }
        if (i == maxSize)
        {
            if (canResize)
            {
                int iNewSize = maxSize + defaultSize;
                NodeInfo[] temp = new NodeInfo[iNewSize];
                Array.Copy(nodeInfoArray, temp, maxSize);
                nodeInfoArray = temp;
                return maxSize;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            return i;
        }
    }

    private int FindElementIndexByIndex(int index)
    {
        int i = 0;
        for (i = 0; i < maxSize; i++)
        {
            if (nodeInfoArray[i].nodeIndex == index)
            {
                break;
            }
        }
        if (i < maxSize)
        {
            return i;
        }
        else
        {
            return -1;
        }
    }

    public void InitPool(int iDefaultSize, bool bResizeable)
    {
        if (iDefaultSize == 0)
        {
            iDefaultSize = 32;
        }
        defaultSize = iDefaultSize;
        maxSize = defaultSize;

        canResize = bResizeable;
        nodeInfoArray = new NodeInfo[maxSize];

        int i = 0;
        for (i = 0; i < maxSize; i++)
        {
            nodeInfoArray[i].nodeIndex = -1;
            nodeInfoArray[i].isActive = false;
            nodeInfoArray[i].m_Item = default(T);
        }
    }

    public void AddElementToPool(T item, int inputNodeIndex)
    {
        if (FindElementIndexByIndex(inputNodeIndex) >= 0)
        {
            // Already in pool.
            return;
        }

        int index = GetUnActiveElement();
        if (index < 0)
        {
            return;
        }

        nodeInfoArray[index].nodeIndex = inputNodeIndex;
        nodeInfoArray[index].isActive = true;
        nodeInfoArray[index].m_Item = item;
    }

    
    public T GetItemFromPool(int index)
    {
        int id = FindElementIndexByIndex(index);
        
        if (id < 0)
        {
            return default(T);
        }
        return nodeInfoArray[id].m_Item;
    }

    

    public IEnumerator<T> GetEnumerator()
    {
        foreach (NodeInfo item in nodeInfoArray)
        {
            if (item.isActive)
            {
                yield return item.m_Item;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
