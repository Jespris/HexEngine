using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    Vector3 oldPosition;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 oldPosition = this.transform.position;
    }


    // Update is called once per frame
    void Update()
    {
        // TODO: code to click and drag camera
        // WASD
        // Zoom
        // PAN to Hex

        CheckIfCameraMoved();
    }

    public void PanToHex(Hex hex)
    {
        // TODO: move camera to this hex
    }

    void CheckIfCameraMoved()
    {
        if (oldPosition != this.transform.position)
        {
            // Something moved the camera
            oldPosition = this.transform.position;

            // TODO: Probably HexMap will have a dictionary of all these
            HexBehaviour[] hexes = GameObject.FindObjectsOfType<HexBehaviour>();
            foreach (HexBehaviour hex in hexes)
            {
                hex.UpdatePosition();
            }
        }
    }
}
