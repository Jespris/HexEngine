using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionPanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        mouseController = GameObject.FindObjectOfType<MouseController>();
    }

    public Text Title;
    public Text Movement;
    public Text HexPath;

    MouseController mouseController;
    public GameObject CityBuildButton;

    // Update is called once per frame
    void Update()
    {   
        Unit unit = mouseController.SelectedUnit;

        if (unit != null)
        {
            Title.text = unit.Name;
            Movement.text = string.Format("{0}/{1}", unit.MovementRemaining, unit.Movement);

            Hex[] hexPath = unit.GetHexPath();
            HexPath.text = hexPath == null ? "0" : string.Format("{0}", (hexPath.Length - 1));

            if (unit.isSettler && unit.Hex.City == null)
            {
                CityBuildButton.SetActive(true);
            }
            else
            {
                CityBuildButton.SetActive(false);
            }
        }
    }
}
