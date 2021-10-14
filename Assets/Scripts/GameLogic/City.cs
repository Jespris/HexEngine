using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class City : MapObject
{
    public City()
    {
        Name = "Turku";
    }

    override public void SetHex(Hex newHex)
    {
        if (Hex != null)
        {
            // Will city ever leave a hex and enter a new one?
            Hex.RemoveCity(this);
        }

        base.SetHex(newHex);

        newHex.AddCity(this);
    }
}
