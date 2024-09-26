using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PathNode;

public class WaypointsSystem : MonoBehaviour, TerrainInterface
{
    public UnityEngine.GameObject [] waypoints;
    public List<PathNode> wayPointsPathNodes;
    private WaypointsSystem theWPS;
    public WaypointsSystem Instance() { return theWPS; }
    

    void Awake()
    {
        InitWaypointSystem();
    }
    void Start()
    {

    }
    
    public void InitWaypointSystem()
    {
        FillListWithChild();
    }
    void FillListWithChild()
    {
        wayPointsPathNodes = new List<PathNode>();
        for (int i = 0; i < waypoints.Length; i++)
        {
            PathNode node = new PathNode();
            node.publicIndex = i;
            node.publicFValue = 0.0f;
            node.publicHValue = 0.0f;
            node.publicGValue = 0.0f;
            node.wayPointPos = waypoints[i].transform.position;
            node.Parent = null;
            node.neighborsWayPoints = new List<PathNode>();
            wayPointsPathNodes.Add(node);
        }
        int layerMaskConstruct = 1 << 9;
        for (int i = 0; i < waypoints.Length; i++)
        {
            for (int j = 0; j < waypoints.Length; j++)
            {
                if (i != j && (Physics.Linecast(wayPointsPathNodes[i].wayPointPos, wayPointsPathNodes[j].wayPointPos, layerMaskConstruct) != true))
                {
                    wayPointsPathNodes[i].neighborsWayPoints.Add(wayPointsPathNodes[j]);
                }
            }
        }

    }
    public void FillPoolWithNodes(NodePool<PathNode> pool)
    {
        pool.InitPool(waypoints.Length, true);

        int i;
        for (i = 0; i < waypoints.Length; i++)
        {
            PathNode node = new PathNode();
            node.publicIndex = i;
            pool.AddElementToPool(node, node.publicIndex);
        }
    }
    public float GetGValue(int parentNodeIndex, int currentNodeIndex)
    {
        Vector3 startPos = GetNodePosition(parentNodeIndex);
        Vector3 goalPos = GetNodePosition(currentNodeIndex);
        float cost = Vector3.Distance(startPos, goalPos);
        return cost;
    }

    public float GetHValue(int currentNodeIndex, int goalNodeIndex)
    {
        Vector3 startPos = GetNodePosition(currentNodeIndex);
        Vector3 goalPos = GetNodePosition(goalNodeIndex);
        float heuristicWeight = 1.0f;
        float cost = heuristicWeight * Vector3.Distance(startPos, goalPos);

        //cost = cost + Mathf.Abs(goalPos.y - startPos.y) * 1.0f; // Give extra cost to height difference
        return cost;
    }
    public Vector3 GetNodePosition(int index)
    {
        return wayPointsPathNodes[index].wayPointPos;
    }
    public int GetNodeIndex(Vector3 pos)
    {
        float minDist = 100000;
        int closetIndex = 0;
        int layerMaskWall = 1 << 8;

        for (int i = 0; i < wayPointsPathNodes.Count; i++)
        {
            if (Physics.Linecast(pos, wayPointsPathNodes[i].wayPointPos, layerMaskWall))
            {
                continue;
            }
            Vector3 deltaDir = wayPointsPathNodes[i].wayPointPos - pos;
            deltaDir.y = 0.0f;
            float currentDist = deltaDir.magnitude;

            if (currentDist < minDist)
            {
                minDist = currentDist;
                closetIndex = i;
            }
        }
        return closetIndex;
    }
    public PathNode GetNodeFromPos(Vector3 pos)
    {
        float minDist = 100000;
        int closestIndex = 0;
        int layerMaskWall = 1 << 8;

        for(int i = 0; i< wayPointsPathNodes.Count;i++)
        {
            if(Physics.Linecast(pos, wayPointsPathNodes[i].wayPointPos, layerMaskWall))
            {
                continue;
            }
            Vector3 deltaDir = wayPointsPathNodes[i].wayPointPos - pos;
            deltaDir.y = 0.0f;
            float currentDist = deltaDir.magnitude;

            if (currentDist < minDist)
            {
                minDist = currentDist;
                closestIndex = i;
            }
        }
        PathNode rightNode = wayPointsPathNodes[closestIndex];
        return rightNode;
    }
    public float GetTerrainHeight(Vector3 position)
    {
        RaycastHit hit;
        int notConstructLayermask = 1 << 9;
        notConstructLayermask = ~notConstructLayermask;
        Physics.Raycast(position, Vector3.down, out hit, 1000.0f, notConstructLayermask);
        return hit.point.y;
    }
    public void ClearAStarInfo()
    {
        foreach(PathNode node in wayPointsPathNodes)
        {
            node.publicFValue = 0.0f;
            node.publicHValue = 0.0f;
            node.publicGValue = 0.0f;
            node.Parent = null;
            node.State = eNodeState.UnCheck;
        }
    }
    public int GetNeighborAmount(int currentNodeIndex, ref int[] neighbors)
    {
        return wayPointsPathNodes[currentNodeIndex].neighborsWayPoints.Count;
        
    }
    public int GetNodesNumber()
    {
        return wayPointsPathNodes.Count;
    }
    public bool IsNodeBlocked(int index) //no use in waypoint mode
    {
        return false;
    }
    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    for (int i = 0; i < transform.childCount; i++)
    //    {
    //        Gizmos.DrawSphere(transform.GetChild(i).transform.position, 0.5f);
    //    }
    //}
}
