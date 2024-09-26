using UnityEngine;
using System.Collections;

public interface TerrainInterface
{
    int GetNeighborAmount(int index, ref int[] neighbors);
    int GetNodesNumber();
    float GetHValue(int sIndex, int dIndex);
    float GetGValue(int sIndex, int dIndex);
    bool IsNodeBlocked(int index);
    int GetNodeIndex(Vector3 pos);
    Vector3 GetNodePosition(int index);
    float GetTerrainHeight(Vector3 position);
    void FillPoolWithNodes(NodePool<PathNode> pool);
}
