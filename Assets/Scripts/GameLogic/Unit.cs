using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QPath;

public class Unit : MapObject, IQPathUnit
{
    public Unit()
    {
        Name = "Dwarf";
    }

    // List of hexes to wlak trhough, NOTE: first hex is always the one we are standing on
    List<Hex> hexPath;

    bool MOVEMENT_RULES_LIKE_CIV6 = false;

    public int Strength = 8;
    public int Movement = 2;
    public int MovementRemaining = 2;
    public bool isSettler = false;

    public Hex[] GetHexPath()
    {
        return ( this.hexPath == null ) ? null : hexPath.ToArray();
    }

    override public void SetHex(Hex newHex)
    {
        if (Hex != null)
            Hex.RemoveUnit(this);

        base.SetHex(newHex);

        newHex.AddUnit(this);
    }

    #region Unit Move and other Orders
    public bool UnitWaitingForOrders()
    {
        // returns true if we have movement left but notheing queued up
        if ( MovementRemaining > 0 && (hexPath == null || hexPath.Count == 0))
        {
            // TODO: maybe we have set unit to fortify/alert/skipturn
            return true;
        }
        return false;
    }

    // Processes one tile worth of movement for the unit
    // returns true if this should be called immediately again
    public bool DoMove()
    {
        Debug.Log("Doing move");

        if (MovementRemaining <= 0)
            return false;
        
        // Grab the first hex from the hexPath
        if (hexPath == null || hexPath.Count == 0)
        {
            // no path queued up
            return false;
        }

        Hex hexWeAreLeaving = hexPath[0];
        Hex newHex = hexPath[1];  // grabs the first element in the queue

        int costToEnter = MovementCostToEnterHex(newHex);

        if (costToEnter > MovementRemaining && MovementRemaining < Movement && MOVEMENT_RULES_LIKE_CIV6)
        {
            // we can't enter the hex this turn
            return false;
        }

        // Move to the new hex
        
        hexPath.RemoveAt(0);

        if (hexPath.Count == 1)
        {
            // The only hex left in this list is the tile we are currently standing on, clear the que
            hexPath = null;
        }

        SetHex( newHex );
        MovementRemaining = Mathf.Max(MovementRemaining - costToEnter, 0);

        return hexPath != null && MovementRemaining > 0;
    }

    public void RefreshMovement()
    {
        MovementRemaining = Movement;
    }

    /*
    public void DUMMY_PATHFINDING_FUNCTION()
    {
        Hex[] pathHexes = QPath.QPath.FindPath<Hex>(Hex.HexMap, this, Hex, Hex.HexMap.getHexAt(Hex.Q - 3, Hex.R), Hex.CostEstimate);

        Debug.Log("Got hex path with length: " + (pathHexes.Length - 1));

        SetHexPath(pathHexes);
    }
    */
    #endregion

    #region Hex Path Setting, Getting and Calculation
    public void ClearHexPath()
    {
        this.hexPath = new List<Hex>();
    }

    public void SetHexPath(Hex[] hexArray)
    {
        this.hexPath = new List<Hex>(hexArray);
    }

    public int MovementCostToEnterHex(Hex hex)
    {
        // TODO: override base movement cost based on our movement mode + tile type

        // DO SOMETHING LIKE THIS
        /*
        if (weAreAHillWalker && hex.ElevationType == Hex.ELEVATION_TYPE.HILL)
            return 1;
        */

        // TODO: implement different unit types
        return hex.BaseMovementCostToEnter(false, false, false, false);
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

        float turnsRemaining = MovementRemaining / Movement;

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
    #endregion
}
