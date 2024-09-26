using System.Net;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    public Transform characterTransform;
    protected float maxRadiusFromChar;
    public Terrain _terrain;
    protected int layerMask;
    protected float moveMinX;
    protected float moveMaxX;
    protected float moveMinZ;
    protected float moveMaxZ;
    public Transform target;
    public Camera _camera;
    private float angle = 1.0f;
    private float zoomSpeed = 200.0f;
    private float zoomMove = 0f;
    private float zoomMax = 46f;
    private float zoomMin = 15f;
    private float distance;
    private float moveSpeed = 0.2f;
    private Vector3 moveForward;
    private float fH;
    private float fV;
    private float fX;
    private float scrollWheelValue;
    private Vector3 movePos;
    private Vector3 vMove;
    private bool bMove = false;
    private bool bCombatMove = false;
    //private bool bZoomUp = false;
    //private bool bZoomDown = false;
    private float zoomDis = 100f;
    bool isOverUI;
    RaycastHit hit;
    Ray ray;
    private void Start()
    {
        target.position = characterTransform.position + Vector3.up;
        layerMask = LayerMask.GetMask("Terrain");
        maxRadiusFromChar = 40.0f;
        moveMinX = _terrain.terrainData.size.x * 0.3f;
        moveMaxX = _terrain.terrainData.size.x * 0.7f;
        moveMinZ = _terrain.terrainData.size.z * 0.25f;
        moveMaxZ = _terrain.terrainData.size.z * 0.75f;       
    }
    void FixedUpdate()
    {
        ray = new Ray(target.position, Vector3.down * 5);
        if (Physics.Raycast(ray, out hit, layerMask))
        {
            if (hit.distance < 0.5f)
            {
                target.position += (1f - hit.distance) * Vector3.up * Time.deltaTime;
            }
            if (hit.distance > 1.5f)
            {
                target.position += (hit.distance - 1f) * Vector3.down * Time.deltaTime;
            }
        }
        else
        {
            target.position += Vector3.up * Time.deltaTime;
        }
        Debug.DrawRay(target.position, Vector3.down * 5);        
    }
    protected void Update()
    {
        MouseZoom();
        if (bMove)
        {
            Move();           
        }
        if (bCombatMove)
        {
            CombatMove();
        }
        //if (bZoomUp)
        //{
        //    ZoomUp();
        //}
        //if (bZoomDown)
        //{
        //    ZoomDown();
        //}
    }
    void LateUpdate()
    {
        KeyboardMove();
        KeyboardRotate();
        MouseRotate();
        //MouseZoom();     
    }
    protected void ZoomDown()
    {
        float dis = Vector3.Distance(_camera.transform.position, target.position);
        if (dis <= zoomDis)
        {
            zoomMove = 0;
            zoomDis = 100f;
            //bZoomDown = false;
        }        
    }
    protected void ZoomUp()
    {
        float dis = Vector3.Distance(_camera.transform.position, target.position);
        if (dis >= 45f)
        {
            zoomMove = 0;
            //bZoomUp = false;
            //bZoomDown = true;
        }
    }
    protected void CombatMove()
    {       
        float mDis = Vector3.Distance(target.position, movePos);
        float speed = 1f;
        if (mDis < 1f)
        {
            speed = 0f;
            bCombatMove = false;           
        }
        target.position += vMove * speed * Time.deltaTime;
    }
    public void OnMove(Vector3 movePos)
    {
        //zoomDis = Vector3.Distance(_camera.transform.position, target.position);
        //bZoomDown = false;
        //bZoomMax = true;
        //zoomMove = -1.5f;
        bMove = false;
        vMove = movePos - target.position;
        vMove.y += 0.5f;
        this.movePos = movePos + Vector3.up;
        bCombatMove = true;
    }
    public void OnMove(bool bZoom)
    {       
        if (bZoom)
        {
            zoomMove = -1.5f;
            ZoomUp();
            //_camera.transform.position += _camera.transform.forward * zoomMove * zoomSpeed * Time.deltaTime;
            //bZoomDown = true;
            ////bZoomUp = true;
        }
        //else if (!bZoomDown)
        //{
        //    //bZoomUp = false;
        //    zoomDis = Vector3.Distance(_camera.transform.position, target.position);
        //}
        //else if (bZoomDown)
        //{
        //    //bZoomUp = false;
        //    zoomMove = 1.5f;
        //    ZoomDown();
        //}       
        bMove = true;
    }
    protected void Move()
    {
        Vector3 vMove = characterTransform.position - target.position;
        float mDis = Vector3.Distance(target.position, characterTransform.position);
        float speed = mDis * 0.3f;
        if (mDis < 0.5f)
        {
            speed = 0f;
        }
        target.position += vMove * speed * Time.deltaTime;
    }
    private void KeyboardMove()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            bMove = true;
            //bZoomDown = false;
            OnMove(false);
        }
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            bMove = false;
            //bZoomDown = false;
            bCombatMove = false;
            fH = Input.GetAxis("Horizontal");
            fV = Input.GetAxis("Vertical");
            target.position = new Vector3(Mathf.Clamp(target.position.x, moveMinX, moveMaxX), target.position.y, Mathf.Clamp(target.position.z, moveMinZ, moveMaxZ));
            target.position = new Vector3(Mathf.Clamp(target.position.x, characterTransform.position.x - maxRadiusFromChar, characterTransform.position.x + maxRadiusFromChar), target.position.y, Mathf.Clamp(target.position.z, characterTransform.position.z - maxRadiusFromChar, characterTransform.position.z + maxRadiusFromChar));
            moveForward = target.transform.forward;
            moveForward.y = 0;
            target.transform.position += target.transform.right * fH * moveSpeed + moveForward * fV * moveSpeed;
        }
    }
    private void KeyboardRotate()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            bMove = false;
            //bZoomDown = false;
            bCombatMove = false;
            target.transform.RotateAround(target.position, Vector3.up, angle);           
        }
        else if (Input.GetKey(KeyCode.E))
        {
            bMove = false;
            //bZoomDown = false;
            bCombatMove = false;
            target.transform.RotateAround(target.position, Vector3.down, angle);
        }
    }
    private void MouseRotate()
    {
        if (Input.GetMouseButton(2))
        {
            bMove = false;
            //bZoomDown = false;
            bCombatMove = false;
            fX = Input.GetAxis("Mouse X");
            target.transform.RotateAround(target.position, Vector3.up * fX, angle);
        }
    }
    private void MouseZoom()
    {
        isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if(isOverUI)
        {
            return;
        }

        distance = Vector3.Distance(_camera.transform.position, target.position);
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            bMove = false;
            //bZoomDown = false;
            bCombatMove = false;
            scrollWheelValue = Input.GetAxis("Mouse ScrollWheel");            
            zoomMove = scrollWheelValue * 1.8f;           
        }
        if (zoomMove != 0)
        {
            distance = Vector3.Distance(_camera.transform.position, target.position);            
            if (distance >= zoomMax && zoomMove < 0)
            {
                zoomMove = 0;
                return;
            }
            if (distance <= zoomMin && zoomMove > 0)
            {
                zoomMove = 0;
                return;
            }
            _camera.transform.position += _camera.transform.forward * zoomMove * zoomSpeed * Time.deltaTime;
            if (zoomMove >= 0.05f || zoomMove <= -0.05f) zoomMove *= 0.95f;
            else zoomMove = 0;           
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(_camera.transform.position, target.position);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        Gizmos.DrawWireSphere(target.position, 1);
    }
}
