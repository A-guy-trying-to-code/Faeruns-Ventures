using System.Collections.Generic;
using UnityEngine;


public class PathfindingManager : MonoBehaviour
{
    public UnityEngine.GameObject m_PathfindingTerrain;
    public UnityEngine.GameObject mainCamera;
    public UnityEngine.GameObject waypointTerrain;

    [HideInInspector]
    public Character currentCharacter;

    private WaypointsSystem wps;
    public AStar myAStar;
    public PartyManager myPartyManager;
    public WaypointsSystem myWPS()
    {
        return wps;
    }
    private PathfindingManager PFManager;
    public PathfindingManager Instance()
    {
        return PFManager;
    }

    [HideInInspector]
    public DynamicGridComponent dgc;
   
    private NavComponent navigationCharacter;
    public NavComponent myNavigationCharacter
    {
        get { return navigationCharacter; }
        set { navigationCharacter = value; }
    }
    bool isOverUI;
    List<PathNode> paths;
    void Awake()
    {
        PFManager = gameObject.GetComponent<PathfindingManager>();
        myAStar = new AStar();
        wps = waypointTerrain.GetComponent<WaypointsSystem>();
    }

    void Start()
    {
        myAStar.InitAStarWithWayPoints(wps);
        dgc = m_PathfindingTerrain.GetComponent<DynamicGridComponent>();
        navigationCharacter.currentStartDrawPos = currentCharacter.transform.position; // init starting draw pos
        foreach(Character member in myPartyManager.partyMembers)
        {
            member.myFSM.myNavComponent.currentStartDrawPos=member.transform.position;
        }
    }

    void Update()
    {
        if(currentCharacter.myFSM.CurrentState != currentCharacter.myFSM.Exploring)
        {
            if (navigationCharacter.currentStartDrawPos != null)
            {
                BaseGrid.DebugDraw(navigationCharacter.currentStartDrawPos, dgc.Rows, dgc.Columns, 2.0f, Gizmos.color);
            }
        }

    }
    bool IsOutOfMap(Vector3 dest)
    {
        RaycastHit[] wallHits;
        int layerMaskWall = 1 << 8;
        float targetDistance = Vector3.Distance(dest, currentCharacter.transform.position);
        wallHits = Physics.RaycastAll(currentCharacter.transform.position, dest- currentCharacter.transform.position,targetDistance, layerMaskWall);
        if (wallHits.Length == 1)
        {
            return true;
        }
        else
            return false;
    }
    public void ExploringMove()
    {
        Vector3 vec = Input.mousePosition;
        Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(vec);
        RaycastHit hitInfo;
        int layerMaskTerrain = 1 << 6;
        if (Physics.Raycast(ray, out hitInfo, 2000.0f, layerMaskTerrain))
        {
            if (IsOutOfMap(hitInfo.point))
            {
                return;
            }

            Vector3 targetPoint = GetTerrainLevelPos(hitInfo.point);
            navigationCharacter.MoveToWayPoint(currentCharacter.transform.position, targetPoint); //Leader move
            Vector3 memberFollowPos;
            Vector3 previousPos= new Vector3(0,0,0);
            for (int i = 0; i < myPartyManager.partyMembers.Length; i++) //members move
            {
                if (myPartyManager.partyMembers[i].name== currentCharacter.name)
                {
                    continue;
                }
                memberFollowPos = GetFollowPos(targetPoint);
                while (memberFollowPos == previousPos) 
                {
                    memberFollowPos = GetFollowPos(targetPoint);
                }
                myPartyManager.partyMembers[i].gameObject.GetComponent<NavComponent>().MoveToWayPoint(myPartyManager.partyMembers[i].transform.position, memberFollowPos);
                previousPos = memberFollowPos;
            }

        }
        
    }
    public void CombatMove()
    {
        isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        
        Vector3 vec = Input.mousePosition;
        Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(vec);
        RaycastHit hitInfo;
        int layerMaskNotWall = 1 << 8;
        layerMaskNotWall = ~layerMaskNotWall;

        if (Physics.Raycast(ray, out hitInfo, 2000.0f, layerMaskNotWall))
        {
            if (IsOutOfMap(hitInfo.point))
            {
                return;
            }

            if (!navigationCharacter.canMove)
            {
                //navigationCharacter.CalculateAStarPath(hitInfo.point);
                //paths = navigationCharacter.GetPathNodes();
                if (currentCharacter.myFSM.CurrentState != currentCharacter.myFSM.ChoosingTargetInCombat)
                {
                    DrawCurve.instance().SetPath(navigationCharacter.GetPath(), currentCharacter.transform.position, hitInfo.point, hitInfo.collider.tag, hitInfo.collider.transform.position);
                }
                //DrawCurve.instance().SetPath(navigationCharacter.GetPath(), currentCharacter.transform.position, hitInfo.point, hitInfo.collider.tag, hitInfo.collider.transform.position);

            }

            if (Input.GetMouseButtonDown(0) && (isOverUI == false) /*&& (DrawCurve.instance().fDis <= CombatManager.Instance().movement)*/) 
            {
                navigationCharacter.CalculateAStarPath(hitInfo.point);
                if (navigationCharacter.CalculateDistanceForEnemy(hitInfo.point)>CombatManager.Instance().movement)
                {
                    Debug.Log("not enough movement");
                    return;
                }
                paths = navigationCharacter.GetPathNodes();
                if (navigationCharacter.GetPathNodes().Count == 0)
                {
                    Debug.Log("astar failed");
                    return;
                }
                
                navigationCharacter.SetPathNodes(paths);
                DrawCurve.instance().Off();
                DrawCurve.instance().OnClickMove(paths[paths.Count - 1].wayPointPos);
            }
                       
        }

    }
    public Vector3 GetTerrainLevelPos(Vector3 position)
    {
        position.y += 100;
        RaycastHit hit;
        int notConstructLayermask = 1 << 9;
        notConstructLayermask = ~notConstructLayermask;
        Physics.Raycast(position, Vector3.down, out hit, 1000.0f, notConstructLayermask);
        return hit.point;
    }

    public void CreateNewGrid()
    {
        Vector3 newOrigin = FindGridOrigin();
        navigationCharacter.currentStartDrawPos = newOrigin;
        foreach (Character member in myPartyManager.partyMembers)
        {
            member.myFSM.myNavComponent.currentStartDrawPos = newOrigin;
        }
        dgc.DynamicGrid.InitGrid(newOrigin, dgc.Rows, dgc.Columns, 2.0f);
        dgc.RefreshCellState();
        navigationCharacter.InitAStarAndNavGrid(dgc.DynamicGrid);
        navigationCharacter.NavGrid.SyncNodeState(navigationCharacter.AStarSystem.NodePool);
    }
    public Vector3 GetFollowPos(Vector3 leaderPos)
    {
        Vector3 followPos = leaderPos;
        if (leaderPos.x >= currentCharacter.transform.position.x) 
        {
            followPos.x -= Random.Range(2, 5);
        }
        else
        {
            followPos.x += Random.Range(2, 5);
        }
        if (leaderPos.z >= currentCharacter.transform.position.z)
        {
            followPos.z -= Random.Range(2, 5);
        }
        else
        {
            followPos.z += Random.Range(2, 5);
        }
        followPos = GetTerrainLevelPos(followPos);
        return followPos;
    }

    public Vector3 FindGridOrigin()
    {
        Vector3 newOrigin = currentCharacter.transform.position;

        newOrigin.x = currentCharacter.transform.position.x - 60.0f;
        newOrigin.z = currentCharacter.transform.position.z - 60.0f;

        return newOrigin;
    }

}
