using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static PathFindingGrid;

public class PathFindingGrid : BaseGrid, TerrainInterface
{
    public enum NeighborDirection
    {
        NoNeighbor = -1,
        Left,
        Top,
        Right,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        No_Direction
    };
    private int neighborAmount;
    public PathFindingGrid()
    {
        neighborAmount = (int)NeighborDirection.No_Direction;

    }
    public override void InitGrid(Vector3 origin, int numRows, int numCols, float cellSize)
    {
        base.InitGrid(origin, numRows, numCols, cellSize);

    }
    public void FillPoolWithNodes(NodePool<PathNode> pool)
    {
        
        pool.InitPool(numberOfCells, true);

        int i, j;
        for (i = 0; i < numberOfColumns; i++)
        {
            for (j = 0; j < numberOfRows; j++)
            {
                PathNode node = new PathNode();
                node.publicIndex = i * numberOfRows + j; //Init node index
                pool.AddElementToPool(node, node.publicIndex);
            }
        }
    }

    public void SyncNodeState(NodePool<PathNode> pool)
    {
        int tempState = 0;
        foreach (PathNode node in pool)
        {
            tempState = GetCellState(node.publicIndex);
            if (tempState < 0)
            {
                node.State = PathNode.eNodeState.Block;
            }
        }
    }
    private int GetNeighbor(int currentNodeIndex, NeighborDirection nDirection)
    {
        Vector3 neighborPos = GetCellCenter(currentNodeIndex);


        switch (nDirection)
        {
            case NeighborDirection.Left:
                if(targetQuadrant < 3)
                    neighborPos.x -= cellSize;
                else
                    neighborPos.x += cellSize;
                break;
            case NeighborDirection.Top:
                if (targetQuadrant < 3)
                    neighborPos.z += cellSize;
                else
                    neighborPos.z -= cellSize;
                break;
            case NeighborDirection.Right:
                if (targetQuadrant < 3)
                    neighborPos.x += cellSize;
                else
                    neighborPos.x -= cellSize;
                break;
            case NeighborDirection.Bottom:
                if (targetQuadrant < 3)
                    neighborPos.z -= cellSize;
                else
                    neighborPos.z += cellSize;
                break;
            case NeighborDirection.TopLeft:
                if (targetQuadrant < 3)
                {
                    neighborPos.x -= cellSize;
                    neighborPos.z += cellSize;
                }
                else
                {
                    neighborPos.x += cellSize;
                    neighborPos.z -= cellSize;
                };
                break;
            case NeighborDirection.BottomLeft:
                if (targetQuadrant < 3)
                {
                    neighborPos.x -= cellSize;
                    neighborPos.z -= cellSize;
                }
                else
                {
                    neighborPos.x += cellSize;
                    neighborPos.z += cellSize;
                };
                break;
            case NeighborDirection.BottomRight:
                if (targetQuadrant < 3)
                {
                    neighborPos.x += cellSize;
                    neighborPos.z -= cellSize;
                }
                else
                {
                    neighborPos.x -= cellSize;
                    neighborPos.z += cellSize;
                };
                break;
            case NeighborDirection.TopRight:
                if (targetQuadrant < 3)
                {
                    neighborPos.x += cellSize;
                    neighborPos.z += cellSize;
                }
                else
                {
                    neighborPos.x -= cellSize;
                    neighborPos.z -= cellSize;
                };
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
        };

        int neighborIndex = GetCellIndex(neighborPos);
        if (!BeInBoundary(neighborIndex))
        {
            neighborIndex = (int)NeighborDirection.NoNeighbor;
        }

        return neighborIndex;
    }
    public NeighborDirection GetNeighborDirection(int index, int iNeighborIndex)
    {
        for (int i = 0; i < neighborAmount; i++)
        {
            int testNeighborIndex = GetNeighbor(index, (NeighborDirection)i);
            if (testNeighborIndex == iNeighborIndex)
            {
                return (NeighborDirection)i;
            }
        }

        return NeighborDirection.NoNeighbor;
    }

    public int GetNeighborAmount(int currentNodeIndex, ref int[] neighbors)
    {
        neighbors = new int[neighborAmount];

        for (int i = 0; i < neighborAmount; i++)
        {
            neighbors[i] = GetNeighbor(currentNodeIndex, (NeighborDirection)i); //convert int to NeighborDirection
        }

        return neighborAmount;
    }
    public int GetNodesNumber()
    {
        return numberOfCells;
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
        Vector3 pos = GetCellPosition(index);
        pos += new Vector3(cellSizeX / 2.0f, 0.0f, cellSizeZ / 2.0f);
        pos.y = 100.0f;
        pos.y = GetTerrainHeight(pos);
        return pos;
    }
    public float GetTerrainHeight(Vector3 position)
    {
        RaycastHit hit;
        int layermaskTerrain = 1 << 6;
        Physics.Raycast(position, Vector3.down, out hit, 1000.0f, layermaskTerrain);
        return hit.point.y;
    }
    public bool IsNodeBlocked(int index)
    {
        if (BeInBoundary(index) == false)
        {
            return true;
        }
        int tempState = GetCellState(index);
        if (tempState < 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public int GetNodeIndex(Vector3 pos)
    {
        int index = GetCellIndex(pos);
        if (!BeInBoundary(index))
        {
            index = -1;
        }
        return index;
    }
    public void ResetCellBlockState()
    {
        for (int i = 0; i < numberOfCells; i++)
        {
            SetCellState(i, 0);
        }
    }
    public void SetCellStateInRect(Bounds bounds, int iState)
    {
        Vector3 topLeftCorner = new Vector3(bounds.min.x, gridOrigin.y, bounds.max.z);
        Vector3 topRightCorner = new Vector3(bounds.max.x, gridOrigin.y, bounds.max.z);
        Vector3 bottomLeftCorner = new Vector3(bounds.min.x, gridOrigin.y, bounds.min.z);
        Vector3 bottomRightCorner = new Vector3(bounds.max.x, gridOrigin.y, bounds.min.z);

        Vector3 hDir = topRightCorner - topLeftCorner;
        hDir.y = 0.0f;
        hDir.Normalize();

        Vector3 vDir = topLeftCorner - bottomLeftCorner;
        vDir.y = 0.0f;
        vDir.Normalize();

        float boundWidth = bounds.size.x;
        float boundHeight = bounds.size.z;

        int numOfOverlapRow = (int)(boundHeight / cellSize) +2;
        int numOfOverlapCol = (int)(boundWidth / cellSize)+2;
        int i, j;
        int iTempCellIndex = 0;
        float fCurrentVLength = 0.0f;
        float fCurrentHLength = 0.0f;
        Vector3 tempPos;

        for (i = 0; i < numOfOverlapRow; i++)
        {
            fCurrentVLength = i * cellSize;
            for (j = 0; j < numOfOverlapCol; j++)
            {
                fCurrentHLength = j * cellSize;
                tempPos = bottomLeftCorner + hDir * fCurrentHLength + vDir * fCurrentVLength;
                tempPos.x = Mathf.Clamp(tempPos.x, bounds.min.x, bounds.max.x);
                tempPos.z = Mathf.Clamp(tempPos.z, bounds.min.z, bounds.max.z);
                if (BeInBoundary(tempPos))
                {
                    iTempCellIndex = GetCellIndex(tempPos);
                    if (iTempCellIndex > -1)
                    {
                        SetCellState(iTempCellIndex, iState);
                    }
                }
                if (fCurrentHLength > boundWidth)
                {
                    break;
                }
            }
            if (fCurrentVLength > boundHeight)
            {
                break;
            }
        }
    }

    public void ExpandGrid(Vector3 hitPoint, Vector3 currentCharPos, Vector3 newOrigin)
    {
        
        int newNumRows = (int)(Mathf.Abs((hitPoint.z - currentCharPos.z)/cellSize));
        int newNumCols = (int)(Mathf.Abs((hitPoint.x - currentCharPos.x) / cellSize));
        
        InitGrid(newOrigin, newNumRows, newNumCols, cellSize);

    }
    public Vector3 FindNewOrigin(Vector3 hitPoint, Vector3 currentCharPos)
    {
        Vector3 newOrigin = hitPoint;
        newOrigin.x = (hitPoint.x < currentCharPos.x) ? hitPoint.x : currentCharPos.x;
        newOrigin.z = (hitPoint.z < currentCharPos.z) ? hitPoint.z : currentCharPos.z;
        return newOrigin;
    }

}