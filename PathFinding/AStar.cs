using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using static PathNode;
using System.IO;
using UnityEngine.XR;
using TMPro;
using UnityEngine.Experimental.GlobalIllumination;

public class AStar 
{
    public enum eFindingStatus
    {
        Waiting = -1,
        Finding = 0,
        Succeed = 1,
        Failed = 2
    };
    protected BinaryHeap<PathNode> OpenHeap;
    protected NodePool<PathNode> GridNodesPool;
    protected PathNode startNode;
    protected PathNode goalNode;
    public LinkedList<PathNode> PathNodes;
    private eFindingStatus m_FindingStatus;
    private TerrainInterface NodeTerrainInterface;

    private WaypointsSystem AStarWayPointMap;
    protected List<PathNode> openWayPointList;
    public List<PathNode> pathWayPoint;
    public NodePool<PathNode> NodePool
    {
        get { return GridNodesPool; }
    }
    public LinkedList<PathNode> SolutionPath
    {
        get { return PathNodes; }
    }
    public eFindingStatus FindingStatus
    {
        get { return m_FindingStatus; }
    }

    public AStar()
    {

    }

    public void InitAStar(TerrainInterface TerrainRepresentation)
    {
        NodeTerrainInterface = TerrainRepresentation;

        OpenHeap = new BinaryHeap<PathNode>();
        PathNodes = new LinkedList<PathNode>();
        GridNodesPool = new NodePool<PathNode>();
        NodeTerrainInterface.FillPoolWithNodes(GridNodesPool);
        m_FindingStatus = eFindingStatus.Waiting;
    }
    public void InitAStarWithWayPoints(WaypointsSystem WayPointsMap)
    {
        AStarWayPointMap = WayPointsMap;
        openWayPointList= new List<PathNode>();
        pathWayPoint = new List<PathNode>();
        

        //OpenHeap = new BinaryHeap<PathNode>();
        //GridNodesPool = new NodePool<PathNode>();
    }
    private void UncheckAllNode(bool bCheckBlock = true)
    {
        foreach (PathNode node in GridNodesPool)
        {
            if (bCheckBlock && node.State == PathNode.eNodeState.Block)
            {
                continue;
            }
            node.State = PathNode.eNodeState.UnCheck;
        }
    }

    private bool IsGoal(PathNode node)
    {
        bool bGoal = (node.publicIndex == goalNode.publicIndex ? true : false);
        return bGoal;
    }
    private void OpenNode(PathNode node)
    {
        node.State = PathNode.eNodeState.Open;
        OpenHeap.Add(node);
    }
    private void CloseNode(PathNode node)
    {
        node.State = PathNode.eNodeState.Close;
    }

    private void UpdateNode(PathNode currentNode, PathNode parentNode, float fGValue)
    {
        currentNode.publicGValue = fGValue;
        currentNode.publicHValue = NodeTerrainInterface.GetHValue(currentNode.publicIndex, goalNode.publicIndex);
        currentNode.publicFValue = currentNode.publicGValue + currentNode.publicHValue;
        currentNode.Parent = parentNode;
    }
    private void RecordNode(PathNode recordNode, PathNode parentNode)
    {
        recordNode.publicGValue = NodeTerrainInterface.GetGValue(parentNode.publicIndex, recordNode.publicIndex);
        recordNode.publicGValue += parentNode.publicGValue;
        recordNode.publicHValue = NodeTerrainInterface.GetHValue(recordNode.publicIndex, goalNode.publicIndex);
        recordNode.publicFValue = recordNode.publicGValue + recordNode.publicHValue;
        recordNode.Parent = parentNode;
    }
    private PathNode GetNode(int index)
    {
        return GridNodesPool.GetItemFromPool(index);
    }
    private void BuildPath()
    {
        PathNode current = goalNode;
        while (current != null)
        {
            PathNodes.AddFirst(current);
            current = current.Parent;
        }
    }
    private void BuildWayPointPath()
    {
        pathWayPoint.Clear();
        PathNode current = goalNode;
        while (current != null)
        {
            pathWayPoint.Add(current);
            current = current.Parent;
        }
        pathWayPoint.Reverse();
    }
    public bool PerformAStarForWayPoint(Vector3 startPosition, Vector3 targetPosition)
    {
        startNode = AStarWayPointMap.GetNodeFromPos(startPosition);
        goalNode = AStarWayPointMap.GetNodeFromPos(targetPosition);

        if (startNode == null || goalNode == null)
        {
            return false;
        }
        else if(startNode == goalNode)
        {
            pathWayPoint.Clear();
            pathWayPoint.Add(startNode);
            return true;
        }

        openWayPointList.Clear();
        AStarWayPointMap.ClearAStarInfo();
        openWayPointList.Add(startNode);
        PathNode currentNode;
        PathNode neighborNode;
        while (openWayPointList.Count > 0)
        {
            currentNode = GetLeastCoastingNode();
            if (currentNode == null)
            {
                UnityEngine.Debug.Log("LeastCostingNode error");
                return false;
            }
            if(currentNode == goalNode)
            {
                BuildWayPointPath(); 
                return true;
            }

            int numNeighbors = currentNode.neighborsWayPoints.Count;
            Vector3 nodeDir;
            for (int i = 0; i < numNeighbors; i++) 
            {
                neighborNode = currentNode.neighborsWayPoints[i];
                if(neighborNode.State == eNodeState.Close)
                {
                    continue;
                }
                else if(neighborNode.State == eNodeState.Open)
                {
                    nodeDir = currentNode.wayPointPos - neighborNode.wayPointPos;
                    float newGValue = currentNode.publicGValue + nodeDir.magnitude;
                    if(newGValue < neighborNode.publicGValue)
                    {
                        neighborNode.publicGValue = newGValue;
                        neighborNode.publicFValue = neighborNode.publicGValue+ neighborNode.publicHValue;
                        neighborNode.Parent = currentNode;
                    }
                    continue;
                }
                neighborNode.State = eNodeState.Open;
                nodeDir = neighborNode.wayPointPos - currentNode.wayPointPos;
                neighborNode.publicGValue = currentNode.publicGValue + nodeDir.magnitude;
                nodeDir = neighborNode.wayPointPos - goalNode.wayPointPos;
                neighborNode.publicHValue = nodeDir.magnitude;
                neighborNode.publicFValue = neighborNode.publicGValue + neighborNode.publicHValue;
                neighborNode.Parent = currentNode;
                openWayPointList.Add(neighborNode);
            }
            currentNode.State = eNodeState.Close;
        }
        return false;
    }
    private PathNode GetLeastCoastingNode()
    {
        PathNode bestNode = null;
        float max = 100000.0f;
        foreach (PathNode node in openWayPointList)
        {
            if (node.publicFValue < max)
            {
                max = node.publicFValue;
                bestNode = node;
            }
        }
        openWayPointList.Remove(bestNode);

        return bestNode;
    }

    private eFindingStatus PerformAStarCycle()
    {
        
        if (OpenHeap.Count == 0)
        {
            return eFindingStatus.Failed;
        }
        
        PathNode currentNode = OpenHeap.PopRoot(); //Get leaset costing node
        CloseNode(currentNode);

        if (IsGoal(currentNode))
        {
            BuildPath();
            return eFindingStatus.Succeed;
        }
        

        int i;
        int[] neighbors = null;
        int numNeighbors = NodeTerrainInterface.GetNeighborAmount(currentNode.publicIndex, ref neighbors);
        int tempNeighborIndex = 0;
        float fTempGValue = 0.0f;
        PathNode neighborNode;
        for (i = 0; i < numNeighbors; i++)
        {
            tempNeighborIndex = neighbors[i];
            if (tempNeighborIndex == (int)PathFindingGrid.NeighborDirection.NoNeighbor)
            {
                continue;
            }

            neighborNode = GetNode(tempNeighborIndex);

            switch (neighborNode.State)
            {
                case PathNode.eNodeState.Close:
                    continue;
                case PathNode.eNodeState.Block:
                    neighborNode.publicHValue += 999999;
                    
                    continue;
                case PathNode.eNodeState.UnCheck:
                    RecordNode(neighborNode, currentNode);
                    OpenNode(neighborNode);
                    break;
                case PathNode.eNodeState.Open:
                    fTempGValue = NodeTerrainInterface.GetGValue(currentNode.publicIndex, neighborNode.publicIndex);
                    fTempGValue += currentNode.publicGValue;
                    if (fTempGValue < neighborNode.publicGValue)
                    {
                        UpdateNode(neighborNode, currentNode, fTempGValue);
                        OpenHeap.Remove(neighborNode);
                        OpenHeap.Add(neighborNode);
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false, "Error");
                    break;
            };
        }
        return eFindingStatus.Finding;
    }
    public int PerformCalculation(int totalCycles)
    {
        if (m_FindingStatus != eFindingStatus.Finding)
        {
            return 0;
        }
        
        int cyclesWaited = 0;

        while (cyclesWaited < totalCycles)
        {
            m_FindingStatus = PerformAStarCycle();
            cyclesWaited++;
            if (m_FindingStatus == eFindingStatus.Failed || m_FindingStatus == eFindingStatus.Succeed)
            {
                break;
            }
        }

        return cyclesWaited;
    }

    public void InitCalculation(int startNodeIndex, int goalNodeIndex)
    {
        if (startNodeIndex < 0 || goalNodeIndex < 0)
        {
            m_FindingStatus = eFindingStatus.Failed;
            return;
        }

        UncheckAllNode();
        OpenHeap.Clear();
        PathNodes.Clear();
        startNode = GetNode(startNodeIndex);
        goalNode = GetNode(goalNodeIndex);

        startNode.publicGValue = 0.0f;
        startNode.publicHValue = NodeTerrainInterface.GetHValue(startNodeIndex, goalNodeIndex);
        startNode.publicFValue = startNode.publicHValue;
        startNode.Parent = null;
        OpenNode(startNode);

        m_FindingStatus = eFindingStatus.Finding;
    }


    public static void DebugDrawPath(List<PathNode> PathPoints, Color color)
    {
        int i = 0;
        int iLen = PathPoints.Count;
        for (i = 0; i < iLen - 1; i++)
        {
            UnityEngine.Debug.DrawLine(PathPoints[i].wayPointPos, PathPoints[i + 1].wayPointPos, color);
        }
    }
}
