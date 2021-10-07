using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit
{
    public Hex Hex { get; protected set; }  // Hex the unit is in

    public delegate void UnitMovedDelegate(Hex oldHex, Hex newHex);

    public event UnitMovedDelegate OnUnitMoved;

    public string Name = "Dwarf";
    public int HitPoints = 100;
    public int Strength = 8;
    public int Movement = 2;
    public int Stamina = 2;  // Movement remaining

    public void SetHex(Hex newHex)
    {

        Hex oldHex = Hex;
        if (Hex != null)
            Hex.RemoveUnit(this);

        Hex = newHex;

        newHex.AddUnit(this);

        if (OnUnitMoved != null)
        {
            OnUnitMoved(oldHex, newHex);
        }
    }

    public void DoTurn()
    {
        Debug.Log("Doing turn");
        // TESTING move right
        Hex oldHex = Hex;
        Hex newHex = oldHex.HexMap.getHexAt(oldHex.Q - 1, oldHex.R);

        SetHex(newHex);
    }
}
