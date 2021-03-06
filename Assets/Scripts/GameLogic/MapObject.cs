using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QPath;
public class MapObject
{
    public MapObject()
    {

    }

    public string Name;
    public int HitPoints = 100;
    public bool isAttackable = true;
    public int FactionID = 0;
    public Hex Hex { get; protected set; }  // Hex the GO is in

    public delegate void ObjectMovedDelegate(Hex oldHex, Hex newHex);
    public event ObjectMovedDelegate OnObjectMoved;

    virtual public void SetHex(Hex newHex)
    {
        Hex oldHex = Hex;

        Hex = newHex;

        if (OnObjectMoved != null)
        {
            OnObjectMoved(oldHex, newHex);
        }
    }
}
