using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QPath;

public class Unit : IQPathUnit
{
    public Hex Hex { get; protected set; }  // Hex the unit is in

    public delegate void UnitMovedDelegate(Hex oldHex, Hex newHex);

    public event UnitMovedDelegate OnUnitMoved;

    Queue<Hex> hexPath;

    bool MOVEMENT_RULES_LIKE_CIV6 = false;

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
        
        // Grab the first hex from the hexPath
        if (hexPath == null || hexPath.Count == 0)
        {
            // no path queued up
            return;
        }

        Hex newHex = hexPath.Dequeue();  // grabs the first element in the queue

        SetHex(newHex);
    }

    public void DUMMY_PATHFINDING_FUNCTION()
    {
        IQPathTile[] pathTiles = QPath.QPath.FindPath(Hex.HexMap, this, Hex, Hex.HexMap.getHexAt(Hex.Q - 3, Hex.R), Hex.CostEstimate);

        Debug.Log("Got tile path with length: " + pathTiles.Length);

        Hex[] pathHexes = System.Array.ConvertAll( pathTiles, a => (Hex)a );

        Debug.Log("Got hex path with length: " + pathHexes.Length);

        SetHexPath(pathHexes);
    }

    public void SetHexPath(Hex[] hexPath)
    {
        this.hexPath = new Queue<Hex>(hexPath);
        this.hexPath.Dequeue();  // throw the first hex out because it's the one we're standing on
    }

    public int MovementCostToEnterHex(Hex hex)
    {
        // TODO: override base movement cost based on our movement mode + tile type
        return hex.BaseMovementCostToEnter();
    }

    public float AggregateTurnsToEnterHex(Hex hex, float turnsToDate)
    {
        // The issue is: if you are trying to enter a tile with a movement cost greater than current stamina left,
        // this will either result in a cheaper-than expected turn cost, or a more expensive than expected turn cost

        float baseTurnsToEnterHex = MovementCostToEnterHex(hex) / Movement;
        if (baseTurnsToEnterHex < 0)
        {
            // impassable terrain
            return -99999;
        }

        if (baseTurnsToEnterHex > 1)
            baseTurnsToEnterHex = 1;

        float turnsRemaining = Stamina / Movement;

        float turnsToDateWhole = Mathf.Floor(turnsToDate);
        float turnsToDateFraction = turnsToDate - turnsToDateWhole;

        if ((turnsToDateFraction < 0.01f && turnsToDateFraction > 0)  || turnsToDateFraction > 0.99f)
        {
            Debug.LogError("Looks like we've got floating point drift in movement calculation");
             
            if (turnsToDateFraction < 0.01f)
                turnsToDateFraction = 0;
            if (turnsToDateFraction > 0.99f)
            {
                turnsToDateWhole += 1;
                turnsToDateFraction = 0;
            }
        }

        float turnsUsedAfterThisMove = turnsToDateFraction + baseTurnsToEnterHex;

        if (turnsUsedAfterThisMove > 1)
        {
            // we have hit the situation where we don't have enough stamina to do this move
            if (MOVEMENT_RULES_LIKE_CIV6)
            {
                // We aren't allowed to enter tile this move
                if (turnsToDateFraction == 0)
                {
                    // we have full movement but this isn't enough to enter the tile
                    // We're good to go.
                }
                else
                {
                    // We aren't on a fresh turn, therefore: 
                    // sit idle for remainder of turn OR change path?
                    turnsToDateWhole += 1;
                    turnsToDateFraction = 0;
                }

                turnsUsedAfterThisMove = baseTurnsToEnterHex;
            }
            else
            {
                // Civ5 - style movement rules state that we can always enter a tile, evene if we don't have enough movement left
                turnsUsedAfterThisMove = 1;
            }
        }

        // turnsUsedAfterThisMove is now some value between [0, 1] (this includes fractional part of moves from previous turns).

        // Do we return the turns THIS move is going to take? NO, function is "aggregate", so return the total turn cost of turnsToDate + turns for this move.
        
        return turnsToDateWhole + turnsUsedAfterThisMove;
    }

    // Turn cost to enter a hex (i.e. 0.5 turns if a movement cost is 1 and we hace max 2 movement
    public float CostToEnterHex(IQPathTile sourceTile, IQPathTile destinationTile)
    {
        return 1f;
    }
}
