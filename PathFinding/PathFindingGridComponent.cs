using UnityEngine;
using System.Collections;

public class PathfindingGridComponent : MonoBehaviour
{

    public float cellSize = 1.0f;
    public int rows;
    public int columns ;

    // Debug.
    public bool m_bDebug = true;
    public Color m_DebugColor = Color.white;

    public PathFindingGrid FindingGrid;
   

    // Use this for initialization
    void Awake()
    {
        FindingGrid = new PathFindingGrid();

        FindingGrid.InitGrid(transform.position, rows, columns, cellSize);

    }

    void Start()
    {
        RefreshCellState();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void RefreshCellState()
    {
        FindingGrid.ResetCellBlockState();

        ObstacleComponent[] ObstacleArray = (ObstacleComponent[])UnityEngine.GameObject.FindObjectsOfType(typeof(ObstacleComponent));
        foreach (ObstacleComponent obstacle in ObstacleArray)
        {
            if (obstacle.GetComponent<Collider>() == null)
            {
                continue;
            }
            Bounds bounds = obstacle.GetComponent<Collider>().bounds;
            FindingGrid.SetCellStateInRect(bounds, -1);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = m_DebugColor;
        if (m_bDebug)
        {
            BaseGrid.DebugDraw(transform.position, rows, columns, cellSize, Gizmos.color);
            if (FindingGrid != null)
            {
                Vector3 cellPos;
                Vector3 size;
                for (int i = 0; i < FindingGrid.GetNodesNumber(); i++)
                {
                    if (FindingGrid.IsNodeBlocked(i))
                    {
                        cellPos = FindingGrid.GetCellCenter(i);
                        size = new Vector3(FindingGrid.CellSize, 0.3f, FindingGrid.CellSize);
                        UnityEngine.Gizmos.DrawCube(cellPos, size);
                    }
                }
            }
        }
    }
}