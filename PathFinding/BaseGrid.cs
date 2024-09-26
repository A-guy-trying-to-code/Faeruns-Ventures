using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BaseGrid 
{
    protected float width, height, cellSize;
    protected int numberOfRows, numberOfColumns, numberOfCells;
    public Vector3 gridOrigin, gridCenter;
    protected int[,] m_CellState;
    private Vector3 XVector, ZVector;
    public int targetQuadrant = 1;
    public float cellSizeX, cellSizeZ;


    public int PublicRows
    {
        get { return numberOfRows; }
    }
    public int PublicColumns
    {
        get { return numberOfColumns; }
    }

    public float Width
    {
        get { return width; }
    }

    public float Height
    {
        get { return height; }
    }

    public Vector3 Origin
    {
        get { return gridOrigin; }
    }

    public int numCell
    {
        get { return numberOfCells; }
    }

    public float Left;
    public float Right;
    public float Top;
    public float Bottom;
    

    public float CellSize
    {
        get { return cellSize; }
    }

    public Vector3 Center
    {
        get { return gridCenter; }
    }

    public BaseGrid()
    {
        m_CellState = null;
    }
    ~BaseGrid()
    {
        m_CellState = null;
    }

    public virtual void InitGrid(Vector3 origin, int numRows, int numCols, float inputCellSize)
    {
        gridOrigin = origin;
        XVector = new Vector3(1.0f, 0.0f, 0.0f);
        ZVector = new Vector3(0.0f, 0.0f, 1.0f);
        cellSize = inputCellSize;
        cellSizeX = inputCellSize;
        cellSizeZ = inputCellSize;


        if (numRows < 0)
        {
            ZVector *= -1;
            numRows *= -1;
            cellSizeZ *= -1.0f;
            if (numCols < 0)
            {
                targetQuadrant = 3;
                cellSizeX *= -1.0f;
                XVector *= -1;
                numCols *= -1;
            }
            else
            {
                targetQuadrant = 4;
            }
        }
        else if(numCols < 0)
        {
            targetQuadrant = 2;
            cellSizeX *= -1.0f;
            XVector *= -1;
            numCols *= -1;
        }

        Top = cellSizeZ > 0 ? origin.z + numRows * inputCellSize : origin.z;
        Bottom = cellSizeZ > 0 ? origin.z : origin.z - numRows * inputCellSize;
        Left = cellSizeX > 0 ? origin.x : origin.x - numCols * inputCellSize;
        Right = cellSizeX > 0 ? origin.x + numCols * inputCellSize : origin.x;


        numberOfRows = numRows;
        numberOfColumns = numCols;
        width = numberOfColumns * cellSizeX;
        height = numberOfRows * cellSizeZ;

        numberOfCells = numberOfRows * numberOfColumns;
        //gridCenter = gridOrigin + (width / 2.0f) * XVector + (height / 2.0f) * ZVector;
        m_CellState = new int[numberOfColumns, numberOfRows];

        
        int i, j;
        for (i = 0; i < numCols; i++)
        {
            for (j = 0; j < numRows; j++)
            {
                m_CellState[i, j] = 0;
            }
        }
    }

    
    public int GetRow(int index)
    {
        int row = index / numberOfColumns;
        return row;
    }

    public int GetColumn(int index)
    {
        int col = index % numberOfColumns;
        return col;
    }
    public int GetCellIndex(Vector3 pos)
    {
        if (BeInBoundary(pos) == false)
        {
            return -1;
        }

        Vector3 nodeDeltaPos = pos - gridOrigin;
        int col = (int)(nodeDeltaPos.x / cellSize);
        int row = (int)(nodeDeltaPos.z / cellSize);
        if(row < 0)
        {
            row *= -1;
        }
        if (col < 0)
        {
            col *= -1;
        }
        int index = row * numberOfColumns + col;

        return index;
    }
    public Vector3 GetCellPosition(int index)
    {
        int row = GetRow(index);
        int col = GetColumn(index);
        float horizontalDistance = col * cellSizeX;
        float verticalDistance = row * cellSizeZ;
        

        Vector3 pos = gridOrigin + new Vector3(horizontalDistance, 0.0f, verticalDistance);
        return pos;
    }
    public Vector3 GetCellCenter(int index)
    {
        Vector3 cellCenterPosition = GetCellPosition(index);
        
        cellCenterPosition.x += (cellSizeX / 2.0f);
        cellCenterPosition.z += (cellSizeZ / 2.0f);
        return cellCenterPosition;
    }
    public void SetCellState(int index, int inputState)
    {
        int col = GetColumn(index);
        int row = GetRow(index);
        if (BeInBoundary(col, row) == false)
        {
            return;
        }

        m_CellState[col, row] = inputState;
    }

    public int GetCellState(int index)
    {
        int col = GetColumn(index);
        int row = GetRow(index);
       
        return m_CellState[col, row];
    }

    public bool BeInBoundary(Vector3 pos)
    {
        bool bBound = true;
        if (pos.x < Left || pos.x > Right || pos.z > Top || pos.z < Bottom)
        {
            bBound = false;
        }
        return bBound;
    }
    public bool BeInBoundary(int index)
    {
        return (index >= 0 && index < numberOfCells);
    }
    public bool BeInBoundary(int col, int row)
    {
        if (col < 0 || col >= numberOfColumns)
        {
            return false;
        }
        else if (row < 0 || row >= numberOfRows)
        {
            return false;
        }
        else
        {
            return true;
        }
    }


    public static void DebugDraw(Vector3 origin, int numRows, int numCols, float cellSize, Color color)
    {
        Vector3 xVec = new Vector3(1.0f, 0.0f, 0.0f);
        Vector3 zVec = new Vector3(0.0f, 0.0f, 1.0f);

        if (numRows < 0)
        {
            zVec *= -1;
            numRows *= -1;
        }
        if (numCols < 0)
        {
            xVec *= -1;
            numCols *= -1;
        }
        float width = (numCols * cellSize);
        float height = (numRows * cellSize);

        for (int i = 0; i < numRows + 1; i++)
        {
            Vector3 startPos = origin + i * cellSize * zVec;
            Vector3 endPos = startPos + width * xVec;
            Debug.DrawLine(startPos, endPos, color);
        }

        for (int i = 0; i < numCols + 1; i++)
        {
            Vector3 startPos = origin + i * cellSize * xVec;
            Vector3 endPos = startPos + height * zVec;
            Debug.DrawLine(startPos, endPos, color);
        }
    }
}
