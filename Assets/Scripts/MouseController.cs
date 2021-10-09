using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Update_CurrentFunc = Update_DetectModeStart;
    }
    // Generic bookkeeping variables
    Vector3 LastMousePos;  // From Input.mousePosition

    // Camera dragging variables
    Vector3 LastMouseGroundPlanePosition;
    Vector3 cameraTargetOffset;
    public float ZoomSpeed = 2f;
    int mouseDragThreshold = 4;  // Threshold for camera movement to start a camera drag

    // Unit movement
    Unit selectedUnit = null; 

    delegate void UpdateFunc();
    UpdateFunc Update_CurrentFunc;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelUpdateFunc();
        }

        Update_CurrentFunc();

        LastMousePos = Input.mousePosition;

        Update_ScrollZoom();
    }

    void CancelUpdateFunc()
    {
        Update_CurrentFunc = Update_DetectModeStart;

        // Cleanup of any UI stuff associated with modes
    }

    void Update_DetectModeStart()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Left mouse button just went down
            // This doesn't do anything by itself
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // Left mouse up

            // Are we clicking on a unit? If so, select it
        }
        else if (Input.GetMouseButton(0) && Vector3.Distance(Input.mousePosition, LastMousePos) > mouseDragThreshold)
        {
            // TODO: Consider adding a threshold for unintended mouse movement to not accidentaly activate camera drag
            // Left mouse is held down AND the mouse moved => camera drag
            Update_CurrentFunc = Update_CameraDrag;
            LastMouseGroundPlanePosition = MouseToGroundPlane(Input.mousePosition);
            Update_CurrentFunc();
        }
        else if (selectedUnit != null && Input.GetMouseButton(1))
        {
            // A unit is selected and right mouse is down, unit is in movemode, show a path to mouse pos
        }
    }

    Vector3 MouseToGroundPlane(Vector3 mousePos)
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(mousePos);
        // What is the point at which mouse ray intersects y = 0?

        float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
        return mouseRay.origin - (mouseRay.direction * rayLength);
    }

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
    
    void Update_UnitMovement()
    {
        if (Input.GetMouseButtonUp(1))
        {
            // Complete unit movement
            // Copy pathfinding path to unit's movement queue
            CancelUpdateFunc();
            return;
        }
    }
}
