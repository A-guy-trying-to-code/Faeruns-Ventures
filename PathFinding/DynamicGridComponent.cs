using UnityEngine;
using System.Collections;

public class DynamicGridComponent : MonoBehaviour
{

    public float cellSize = 2.0f;
    protected int rows = 60;
    protected int columns = 60;

    // Debug.
    public bool m_bDebug = true;
    public Color m_DebugColor = Color.white;
    

    public PathFindingGrid DynamicGrid;


    public int Rows
    {
        get { return rows; }
    }
    public int Columns
    {
        get { return columns; }
    }
    void Awake()
    {
        DynamicGrid = new PathFindingGrid();
    }

    void Start()
    {
        //RefreshCellState();
    }

   
    void Update()
    {
        
        
    }
    public int CaculateRows(float destZ)
    {
        //rows = (int)((destZ - transform.parent.position.z) / cellSize);
        //rows = rows < 0 ? rows - 1 : rows + 1;
        //rows *= 2;
        rows = (int)(destZ < transform.parent.position.z? -45.0f : 45.0f);
        Debug.Log("rows: " + rows);
        return rows;
    }
    public int CaculateColumns(float destX)
    {
        //columns = (int)((destX - transform.parent.position.x) / cellSize);
        //columns = columns < 0 ? columns - 1 : columns + 1;
        //columns *= 2;
        columns = (int)(destX < transform.parent.position.x ? -45.0f : 45.0f);
        Debug.Log("cols: " + columns);
        return columns;

    }

    public void RefreshCellState()
    {
        DynamicGrid.ResetCellBlockState();

        ObstacleComponent[] ObstacleArray = (ObstacleComponent[])FindObjectsOfType(typeof(ObstacleComponent));
        //float rightMostBoundX=DynamicGrid.Right, leftMostBoundX=DynamicGrid.Left, upMostBoundZ=DynamicGrid.Top, bottomMostBoundZ=DynamicGrid.Bottom;
        foreach (ObstacleComponent obstacle in ObstacleArray)
        {
            if (obstacle.enabled == false)
            {
                continue;
            }
            else /*if (obstacle.GetComponent<Collider>() != null)*/
            {
                if(obstacle.tag=="Player" || obstacle.tag == "EnemyCombatants")
                {
                    //Bounds bounds = obstacle.GetComponent<Collider>().bounds;
                    //DynamicGrid.SetCellStateInRect(bounds, -1);


                    int posIndex = DynamicGrid.GetCellIndex(obstacle.transform.position);
                    DynamicGrid.SetCellState(posIndex, -1);
                    continue;
                }
            }
            
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = m_DebugColor;
        if (m_bDebug)
        {
            if (DynamicGrid != null)
            {
                Vector3 cellPos;
                Vector3 size;
                for (int i = 0; i < DynamicGrid.GetNodesNumber(); i++)
                {
                    if (DynamicGrid.IsNodeBlocked(i))
                    {
                        cellPos = DynamicGrid.GetCellCenter(i);
                        size = new Vector3(DynamicGrid.CellSize, 0.3f, DynamicGrid.CellSize);
                        Gizmos.DrawCube(cellPos, size);
                    }
                }
            }
            
        }
    }
}