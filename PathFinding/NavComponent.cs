using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static UnityEngine.GraphicsBuffer;

public class NavComponent : MonoBehaviour
{
    public PathfindingManager pathfindingManager;
    public PathFindingGrid NavGrid;
    public AStar AStarSystem;

    [HideInInspector]
    public bool canMove = false;

    private List<PathNode> resultList;
    private List<PathNode> resultList2;

    [HideInInspector]
    public float movementStoppingDistance;
    [HideInInspector]
    public float originalmovementStoppingDistance;
    [HideInInspector]
    public bool canRotate = false;
    [HideInInspector]
    public Vector3 targetPosition;

    private bool bUpdateBlock;
    private Animator characterAnimator;
    public Animator myAnimator
    {
        get { return characterAnimator; }
        set { characterAnimator = value; }
    }
    public float speed;
    private int numberOfPathNode;
    private int seekPointIndex;
    private bool AStarResult = false;
    public bool myAStarResult
    {
        get { return AStarResult; }
        set { AStarResult = value; }
    }
    
    public float turnDirAmount; //how mach turnDir we want
    public float CombatTurnDirAmount;

    //Debug Draw
    [HideInInspector]
    public Vector3 currentStartDrawPos;
    [HideInInspector]
    public Vector3 nextStartDrawPos;
    
    void Awake()
    {
        NavGrid = null;
        AStarSystem = new AStar();

        if (this.name == "HYDRA")
        {
            originalmovementStoppingDistance = 17F;
        }
        else
        {
            originalmovementStoppingDistance = 0.1f;
        }
        movementStoppingDistance = originalmovementStoppingDistance;
        characterAnimator = GetComponent<Animator>();
        bUpdateBlock = false;
        resultList=new List<PathNode>();
    }
    void Start()
    {
        if (name == "HYDRA")
        {
            movementStoppingDistance = 17f;
        }
        InitAStarAndWayPoints(pathfindingManager.myWPS());
    }
    
    public void InitAStarAndNavGrid(PathFindingGrid grid)
    {
        NavGrid = grid;
        AStarSystem.InitAStar(NavGrid);
    }
    public void InitAStarAndWayPoints(WaypointsSystem wps)
    {

        AStarSystem.InitAStarWithWayPoints(wps);
    }
    public bool MoveToWayPoint(Vector3 startPosition, Vector3 targetPosition)
    {
        if (AStarSystem.PerformAStarForWayPoint(startPosition, targetPosition))
        {
            PathNode final = new PathNode();
            final.wayPointPos = targetPosition;
            final.wayPointPos = pathfindingManager.GetTerrainLevelPos(final.wayPointPos);

            AStarSystem.pathWayPoint.Add(final);
            seekPointIndex = 0;
            characterAnimator.SetBool("IsRunning", true);
            AStarResult = true;
        }
        else
        {
            Debug.Log("AStarResult false");
        }
        return true;

    }

    public void CalculateAStarPath(Vector3 targetPosition)
    {
        int startIndex = NavGrid.GetNodeIndex(transform.position);//send in pos get index
        int destinationIndex = NavGrid.GetNodeIndex(targetPosition);//send in pos get index
        AStarSystem.InitCalculation(startIndex, destinationIndex);
        AStarSystem.PerformCalculation(2000);

        
        resultList.Clear();
        foreach (PathNode node in AStarSystem.SolutionPath)
        {
            node.wayPointPos = NavGrid.GetNodePosition(node.publicIndex);
            resultList.Add(node);
        }
        seekPointIndex = 0;
        canMove = false;
        characterAnimator.SetBool("IsRunning", false);
        return ;
    }
    public void AStarPathChasinigPlayer(Vector3 targetPosition)
    {
        int startIndex = NavGrid.GetNodeIndex(transform.position);//send in pos get index
        int destinationIndex = NavGrid.GetNodeIndex(targetPosition);//send in pos get index
        AStarSystem.InitCalculation(startIndex, destinationIndex);
        AStarSystem.PerformCalculation(10000);
        resultList.Clear();
        foreach (PathNode node in AStarSystem.SolutionPath)
        {
            node.wayPointPos = NavGrid.GetNodePosition(node.publicIndex);
            resultList.Add(node);
        }

        seekPointIndex = 0;
        resultList2 = resultList;
        canMove = true;
        characterAnimator.SetBool("IsRunning", true);
    }
    public float CalculateDistanceForEnemy(Vector3 targetPosition)
    {
        float distance = 0;
        
        int startIndex = NavGrid.GetNodeIndex(transform.position);//send in pos get index
        int destinationIndex = NavGrid.GetNodeIndex(targetPosition);//send in pos get index
        AStarSystem.InitCalculation(startIndex, destinationIndex);
        AStarSystem.PerformCalculation(2000);

        List<PathNode> nodes=AStarSystem.SolutionPath.ToList();
        for (int i = 0; i < nodes.Count - 1; i++) 
        {
            distance += Vector3.Distance(nodes[i].wayPointPos, nodes[i+1].wayPointPos);
        }

        //foreach (PathNode node in AStarSystem.SolutionPath)
        //{
        //    if (node.Parent == null) 
        //    {
        //        Debug.Log("distance: " + distance);
        //        distance += Vector3.Distance(transform.position, node.wayPointPos); //currentPos + 1st node
        //        Debug.Log("distance now: " + distance);
        //    }
        //    else
        //    {
        //        Debug.Log("distance: " + distance);
        //        distance += Vector3.Distance(node.Parent.wayPointPos, node.wayPointPos); //rest of the path
        //        Debug.Log("distance now: " + distance);
        //    }
        //}

        return distance;
    }
    private void SmoothPath(ref Vector3[] path)
    {
        List<Vector3> pathList = new List<Vector3>();
        pathList = path.ToList();

        for (int i = 0; i < (pathList.Count - 2); )
        {
            while (HasLineOfSightGrid(path[i], path[i + 2]))
            {
                pathList.Remove(path[i + 1]);
                path = pathList.ToArray();
                if (i == (pathList.Count - 2))
                    break;
            }
            if (i == (pathList.Count - 2))
                break;
            i++;
            
        }
    }
    public bool HasLineOfSight(Vector3 currentNode, Vector3 grandNode)
    {
        RaycastHit hit;
        int LayerMaskConstruct=1<<9;
        if (Physics.Linecast(currentNode, grandNode, out hit, LayerMaskConstruct))
        {
            return false;
        }
        else
            return true;
    }
    public bool HasLineOfSightGrid(Vector3 currentNode, Vector3 grandNode)
    {
        currentNode.y = transform.position.y - 1;
        grandNode.y = transform.position.y - 1;
        int layerMaskConstruct = 1 << 9;
        
        RaycastHit hit;
        if (Physics.Linecast(currentNode, grandNode, out hit, layerMaskConstruct))
        {
            return false;
        }
        else
            return true;
    }
    public void Seek(List<PathNode> resultPath)
    {
        resultPath.Last().wayPointPos.y = transform.position.y;
        Vector3 currentPos = transform.position;
        PathNode currentSeekingNode = resultPath[seekPointIndex];
        currentSeekingNode.wayPointPos.y = transform.position.y;
        if (/*currentSeekingNode == resultPath.Last() ||*/ Vector3.Distance(transform.position, resultPath.Last().wayPointPos) <= movementStoppingDistance)
        {
            AStarResult = false;
            canMove = false;
            seekPointIndex = 0;
            characterAnimator.SetBool("IsRunning", false);
            movementStoppingDistance = originalmovementStoppingDistance;
            return;
        }
        //PathNode currentSeekingNode = resultPath[seekPointIndex];
        //if (Time.frameCount % 5 == 0)
        //{
        //    int startIndex = seekPointIndex;
        //    for (int i = startIndex + 1; i < resultPath.Count; i++)
        //    {
        //        if (HasLineOfSight(currentPos, resultPath[i].wayPointPos))
        //        {
        //            seekPointIndex = i;
        //            currentSeekingNode = resultPath[i];
        //        }
        //    }
        //}

        Vector3 targetDir = currentSeekingNode.wayPointPos - transform.position; //rotate while moving
        Vector3 turningDir = targetDir - transform.forward;
        turningDir.Normalize();
        turningDir.y = 0; //body won't turn
        transform.forward += turningDir * turnDirAmount;

        transform.position = Vector3.MoveTowards(transform.position, currentSeekingNode.wayPointPos, speed * Time.deltaTime);
        pathfindingManager.currentCharacter.myFSM.CalculateMovement(); //decrease movement
        RaycastHit groundHit; //stick to the floor
        int layerMaskTerrain = 1 << 6;
        Vector3 tempPos = transform.position;
        tempPos.y += 100;
        Physics.Raycast(tempPos, Vector3.down, out groundHit, 1000.0f, layerMaskTerrain);
        tempPos = transform.position;
        tempPos.y = groundHit.point.y;
        transform.position = tempPos;

        if (currentSeekingNode != resultPath.Last() && Vector3.Distance(currentPos, currentSeekingNode.wayPointPos) <= 2.0f)
        {
            seekPointIndex++;
            currentSeekingNode = resultPath[seekPointIndex];
        }
    }

    public void WayPointSeek(List<PathNode> resultPath)
    {
        resultPath.Last().wayPointPos.y = transform.position.y;
        Vector3 currentPos = transform.position;
        PathNode currentSeekingNode = resultPath[seekPointIndex];
        currentSeekingNode.wayPointPos.y = transform.position.y;
        
        if (currentSeekingNode == resultPath.Last() && Vector3.Distance(transform.position, resultPath.Last().wayPointPos) <= movementStoppingDistance)
        {
            AStarResult = false;
            canMove = false;
            seekPointIndex = 0;
            characterAnimator.SetBool("IsRunning", false);
            movementStoppingDistance = originalmovementStoppingDistance;
            return;
        }
        if (Time.frameCount % 5 == 0)
        {
            int startIndex = seekPointIndex;
            for (int i = startIndex + 1; i < resultPath.Count; i++)
            {
                Vector3 terrainLevelPos = pathfindingManager.GetTerrainLevelPos(currentPos);

                if (HasLineOfSight(terrainLevelPos, resultPath[i].wayPointPos))
                {
                    seekPointIndex = i;
                    currentSeekingNode = resultPath[i];
                }
            }
        }
        Vector3 targetDir = currentSeekingNode.wayPointPos - transform.position;
        Vector3 turningDir = targetDir - transform.forward;
        turningDir.Normalize();
        turningDir.y = 0; //body won't turn
        transform.forward += turningDir * turnDirAmount;

        transform.position = Vector3.MoveTowards(transform.position, currentSeekingNode.wayPointPos, speed * Time.deltaTime);

        RaycastHit groundHit;
        int layerMaskTerrain = 1 << 6;
        Vector3 tempPos = transform.position;
        tempPos.y += 100;
        Physics.Raycast(tempPos, Vector3.down, out groundHit, 1000.0f, layerMaskTerrain);
        tempPos = transform.position;
        tempPos.y = groundHit.point.y;
        transform.position = tempPos;

        if (currentSeekingNode != resultPath.Last() && Vector3.Distance(currentPos, currentSeekingNode.wayPointPos) <= 0.1f) 
        {
            seekPointIndex++;
        }
    }
    void Update()
    {
        if (NavGrid != null)
        {
            bUpdateBlock = false;
            if (bUpdateBlock == false)
            {
                NavGrid.SyncNodeState(AStarSystem.NodePool);
                bUpdateBlock = true;
            }
        }
        if (AStarResult)
        {
            WayPointSeek(AStarSystem.pathWayPoint);
        }
        
        if (canMove)
        {
            AStar.DebugDrawPath(resultList, Color.green);
            Seek(resultList2);
        }
        if (canRotate)
        {
            RotateCharacterTowards(targetPosition);
        }

    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (AStarResult)
        {
            for (int i = 0; i < AStarSystem.pathWayPoint.Count - 1; i++)
            {
                Gizmos.DrawLine(AStarSystem.pathWayPoint[i].wayPointPos, AStarSystem.pathWayPoint[i+1].wayPointPos);
            }
        }
    }
    public Vector3[] GetPath()
    {
        Vector3[] Path = new Vector3[resultList.Count];
        for (int i = 0; i < resultList.Count; i++)
        {
            Path[i] = resultList[i].wayPointPos;
        }
        return Path;
    }
    public List<PathNode> GetPathNodes()
    {
        return resultList;
    }
    public void SetPathNodes(List<PathNode> pathNodes)
    {
        resultList2 = pathNodes;
        characterAnimator.SetBool("IsRunning", true);
        canMove = true;
    }
    public void RotateCharacterTowards(Vector3 targetPosition)
    {
        Vector3 targetDir = targetPosition - transform.position;
        Vector3 turningDir = targetDir - transform.forward;
        turningDir.Normalize();
        turningDir.y = 0; //body won't turn
        transform.forward += turningDir * CombatTurnDirAmount;
        if (Vector3.Angle(transform.forward, targetDir) < 1) 
        {
            transform.forward = targetDir;
            canRotate = false;
        }
        
    }
}
