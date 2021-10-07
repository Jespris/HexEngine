using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMouseController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    bool isDraggingCamera = false;
    Vector3 LastMousePosition;
    public float ZoomSpeed = 5f;

    // Update is called once per frame
    void Update()
    {
        // Right now, all we need are camera controls

        // Click and drag

        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        // What is the point at which mouse ray intersects y = 0?

        float rayLength = (mouseRay.origin.y / mouseRay.direction.y);
        Vector3 hitPos = mouseRay.origin - (mouseRay.direction * rayLength);
        
        // Find ray from mouse pos
        if (Input.GetMouseButtonDown(1)) 
        {
            // Mouse button just went down => start drag
            isDraggingCamera = true;

            LastMousePosition = hitPos;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            // Mouse up => stop drag
            isDraggingCamera = false;

            LastMousePosition = hitPos;
        }

        if (isDraggingCamera)
        {
            Vector3 diff = LastMousePosition - hitPos;
            Camera.main.transform.Translate(diff, Space.World);
            mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            rayLength = (mouseRay.origin.y / mouseRay.direction.y);
            hitPos = mouseRay.origin - (mouseRay.direction * rayLength);
        }

        // Zooming to mousepoint on map
        float scrollAmount = -Input.GetAxis("Mouse ScrollWheel");
        float minHeight = 2f;
        float maxHeight = 20f;
        
        if (Mathf.Abs(scrollAmount) > 0.1f)
        {
            // Move camera towards hitPos
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
}
