using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Update_CurrentFunc = Update_DetectModeStart;

        hexMap = GameObject.FindObjectOfType<HexMap>();

        lineRend = transform.GetComponentInChildren<LineRenderer>();
    }

    public GameObject UnitSelectionPanel;

    // Generic bookkeeping variables
    Vector3 LastMousePos;  // From Input.mousePosition
    HexMap hexMap;
    Hex hexUnderMouse;
    Hex hexLastUnderMouse;

    // Camera dragging variables
    Vector3 LastMouseGroundPlanePosition;
    Vector3 cameraTargetOffset;
    public float ZoomSpeed = 2f;
    int mouseDragThreshold = 4;  // Threshold for camera movement to start a camera drag

    // Unit movement
    Unit __selectedUnit = null;
    public Unit SelectedUnit
    {
        get
        {
            return __selectedUnit;
        }
        protected set
        {
            __selectedUnit = value;
            UnitSelectionPanel.SetActive(__selectedUnit != null);
        }
    }

    Hex[] hexPath;
    LineRenderer lineRend;

    delegate void UpdateFunc();
    UpdateFunc Update_CurrentFunc;

    public LayerMask LayerForHexTiles;

    private void Update()
    {
        hexUnderMouse = MouseToHex();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelUpdateFunc();
        }

        Update_CurrentFunc();

        LastMousePos = Input.mousePosition;

        Update_ScrollZoom();

        hexLastUnderMouse = hexUnderMouse;

        if (SelectedUnit != null)
        {
            // Debug.Log("Drawing path");
            DrawPath( ( hexPath != null ) ? hexPath : SelectedUnit.GetHexPath() );
        }
        else
        {
            // Debug.Log("Clearing path");
            DrawPath(null);  // clear the path visulas
        }
    }

    void CancelUpdateFunc()
    {
        Update_CurrentFunc = Update_DetectModeStart;
        SelectedUnit = null;

        hexPath = null;
        // Cleanup of any UI stuff associated with modes
    }

    void Update_DetectModeStart()
    {
        // Check here(?) if mouse is over UI Element, if so, skip mousedown and up blocks

        if (EventSystem.current.IsPointerOverGameObject())
        {
            // TODO: Do we want to ignore all GUI objects?
            // Consider things like unit health bars, resources
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            // Left mouse button just went down
            // This doesn't do anything by itself
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Left mouse up

            // Are we clicking on a unit? If so, select it
            Unit[] us = hexUnderMouse.Units;

            // TODO: implement cycling through multiple units in the same tile

            if (us != null && us.Length > 0)
            {
                
                SelectedUnit = us[0];
                Debug.Log("Selected unit: " + SelectedUnit.Name);
                // NOTE: Selecting a unit does NOT change our mouse mode
                //Update_CurrentFunc = Update_UnitMovement;
            }
        }
        else if (SelectedUnit != null && Input.GetMouseButtonDown(1))
        {
            Debug.Log("Entering unit movement mode");
            // We have selected a unit and right mouse buttin is down => go to unit movement mode
            Update_CurrentFunc = Update_UnitMovement;
        }
        else if (Input.GetMouseButton(0) && Vector3.Distance(Input.mousePosition, LastMousePos) > mouseDragThreshold)
        {
            // TODO: Consider adding a threshold for unintended mouse movement to not accidentaly activate camera drag
            // Left mouse is held down AND the mouse moved => camera drag
            Update_CurrentFunc = Update_CameraDrag;
            LastMouseGroundPlanePosition = MouseToGroundPlane(Input.mousePosition);
            Update_CurrentFunc();
        }
        else if (SelectedUnit != null && Input.GetMouseButton(1))
        {
            // A unit is selected and right mouse is down, unit is in movemode, show a path to mouse pos
        }
    }

    #region MousePositionGetters
    Hex MouseToHex()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;

        if (Physics.Raycast(mouseRay, out hitInfo, Mathf.Infinity, LayerForHexTiles))
        {
            // something got hit
            // Debug.Log(hitInfo.collider.name);

            // the collider is child
            GameObject hexGO = hitInfo.rigidbody.gameObject;

            return hexMap.GetHexFromGameObject(hexGO);
        }
        // Debug.Log("Found nothing...");
        return null;
    }

    Vector3 MouseToGroundPlane(Vector3 mousePos)
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(mousePos);
        // What is the point at which mouse ray intersects y = 0?

        float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
        return mouseRay.origin - (mouseRay.direction * rayLength);
    }
    #endregion

    #region UpdateFunc functions
    void Update_CameraDrag()
    {
        if (Input.GetMouseButtonUp(0))
        {
            CancelUpdateFunc();
            return;
        }

        // Click and drag

        Vector3 hitPos = MouseToGroundPlane(Input.mousePosition);

        Vector3 diff = LastMouseGroundPlanePosition - hitPos;
        Camera.main.transform.Translate(diff, Space.World);

        LastMouseGroundPlanePosition = hitPos = MouseToGroundPlane(Input.mousePosition);
    }

    private void Update_ScrollZoom()
    {
        // Zooming to mousepoint on map
        float scrollAmount = -Input.GetAxis("Mouse ScrollWheel");
        float minHeight = 2f;
        float maxHeight = 20f;
        
        if (Mathf.Abs(scrollAmount) > 0.1f)
        {
            // Move camera towards hitPos
            Vector3 hitPos = MouseToGroundPlane(Input.mousePosition);
            Vector3 dir = hitPos - Camera.main.transform.position ;
            Vector3 oldCameraPos = Camera.main.transform.position;
            Camera.main.transform.Translate(dir * scrollAmount * ZoomSpeed, Space.World);
            // Limit zooming
            if (Camera.main.transform.position.y < minHeight || Camera.main.transform.position.y > maxHeight)
                Camera.main.transform.position = oldCameraPos;
        }

        // Maybe put this in an option file
        // Smooth and continous camera angle all the way
        Camera.main.transform.rotation = Quaternion.Euler
            (Mathf.Lerp(20, 90, Camera.main.transform.position.y / (maxHeight * 0.66f)),
                Camera.main.transform.rotation.eulerAngles.y,
                Camera.main.transform.rotation.eulerAngles.z
                );

        // Change first number for boundary to close-to-ground camera angle, second to high over ground camera angle
    }
    void DrawPath(Hex[] hexPath)
    {
        if ( hexPath == null || hexPath.Length == 0)
        {
            lineRend.enabled = false;
            return;
        }
        else
        {
            lineRend.enabled = true;
        }

        Vector3[] ps = new Vector3[hexPath.Length];

        for (int i = 0; i < hexPath.Length; i++)
        {
            GameObject hexGO = hexMap.GetHexGO(hexPath[i]);
            ps[i] = hexGO.transform.position + Vector3.up * 0.1f;
        }

        lineRend.positionCount = ps.Length;
        lineRend.SetPositions(ps);
    }
    
    void Update_UnitMovement()
    {
        if (Input.GetMouseButtonUp(1) || SelectedUnit == null)
        {
            // Complete unit movement
            if (SelectedUnit != null)
            {
                SelectedUnit.SetHexPath(hexPath);
                // TODO: Tell unit and/or hexMap to process unit movement 
                StartCoroutine(hexMap.DoUnitMoves(SelectedUnit));
            }
            // Copy pathfinding path to unit's movement queue
            CancelUpdateFunc();
            return;
        }

        // we have a selected unit

        // look at the hex under the mouse
        // is this a different hex than before?
        if (hexPath == null || hexUnderMouse != hexLastUnderMouse)
        {
            // if so, do pathfinding search
            hexPath = QPath.QPath.FindPath<Hex>(hexMap, SelectedUnit, SelectedUnit.Hex, hexUnderMouse, Hex.CostEstimate);
            Debug.Log("Found path with length: " + hexPath.Length);
        }
    }
    #endregion
}
