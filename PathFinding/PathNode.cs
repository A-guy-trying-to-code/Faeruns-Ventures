using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PathNode : IComparable<PathNode> 
{
    public enum eNodeState
    {
        UnCheck = 0,
        Open = 1,
        Close = 2,
        Block = 3
    };

    private PathNode parentNode;
    private eNodeState nodeState;
    private int nodeIndex;
    private float FValue;
    private float GValue;
    private float HValue;

    public Vector3 wayPointPos;
    public List<PathNode> neighborsWayPoints;

    public float publicFValue
    {
        get { return FValue; }
        set { FValue = value; }
    }
    public float publicGValue
    {
        get { return GValue; }
        set { GValue = value; }

    }
    public float publicHValue
    {
        get { return HValue; }
        set { HValue = value; }

    }
    public int publicIndex
    {
        get { return nodeIndex; }
        set { nodeIndex = value; }

    }
    public PathNode Parent
    {
        get { return parentNode; }
        set { parentNode = value; }

    }

    public PathNode()
    {
        parentNode = null;
        nodeState = eNodeState.UnCheck;
        nodeIndex = -1;
        FValue = float.MaxValue;
        GValue = float.MaxValue;
        HValue = float.MaxValue;
    }
    public eNodeState State
    {
        get { return nodeState; }
        set { nodeState = value; }

    }
    public int CompareTo(PathNode node)
    {
        if (FValue < node.FValue)
        {
            return -1;
        }
        else if (FValue > node.FValue)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}
