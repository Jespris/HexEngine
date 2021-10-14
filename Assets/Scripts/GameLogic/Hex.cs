using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using QPath;


// Hex class defines the grid position, world space position, size, neighbors, etc... of a Hex tile
// BUT does not interact with Unity directly in any way
// We're using three axis for easier hexagonal grid math, initially less intuative
// https://www.redblobgames.com/grids/hexagons/

public class Hex : IQPathTile
{
    // Constriction:  Q + R + S = 0
    // S = -(Q + R)

    #region Hex variables
    // readonly keyword means variable can only be set once
    public readonly int Q; // Column
    public readonly int R; // Row
    public readonly int S; // Third axis
    public readonly HexMap HexMap;

    // Data for map generation (and weather effects?)
    public float Elevation;
    public float Moisture;

    public bool allowWrapEastWest = true;
    public bool allowWrapNorthSouth = false;

    public enum TERRAIN_TYPE { PLAINS, GRASSLANDS, MARSH, FLOODPLAINS, DESERT, LAKE, OCEAN };
    public enum ELEVATION_TYPE { FLAT, HILL, MOUNTAIN, WATER };

    public TERRAIN_TYPE TerrainType { get; set; }
    public ELEVATION_TYPE ElevationType { get; set; }

    public enum FEATURE_TYPE { FOREST, RAINFOREST, MARSH, NONE }

    public FEATURE_TYPE FeatureType { get; set; }
    public Hex(HexMap hexmap, int q, int r)
    {
        this.Q = q;
        this.R = r;
        this.S = -(q + r);

        this.HexMap = hexmap;

        units = new HashSet<Unit>();
    }

    static readonly float HEX_WIDTH_MULTIPLIER = Mathf.Sqrt(3) / 2;
    float radius = 1f;

    HashSet<Unit> units;
    public Unit[] Units { get { return units.ToArray(); } }
    public City City { get; protected set; }
    #endregion

    #region Defining Hex Dimensions and Position
    public Vector3 Position()
    {
        // returns world space position of this hex
        return new Vector3(HexHorizontalSpacing() * (this.Q + this.R / 2f), 0, HexVerticalSpacing() * this.R);
    }

    public float HexHeight()
    {
        return radius * 2;
    }

    public float HexWidth()
    {
        return HEX_WIDTH_MULTIPLIER * HexHeight();
    }

    public float HexVerticalSpacing()
    {
        return HexHeight() * 0.75f;
    }

    public float HexHorizontalSpacing()
    {
        return HexWidth();
    }
    public Vector3 PositionFromCamera()
    {
        return HexMap.GetHexPosition(this);
    }

    public Vector3 PositionFromCamera(Vector3 cameraPosition, float numRows, float numCols)
    {
        float mapHeight = numRows * HexVerticalSpacing();
        float mapWidth = numCols * HexHorizontalSpacing();

        Vector3 position = Position();  // pos of this hextile

        if (HexMap.allowWrapEastWest)
        {
            float widthsFromCamera = (position.x - cameraPosition.x) / mapWidth;

            // We want widthsFromCamera to be (-0.5 ~ 0.5)

            // If we are at 0.6, then we want to be at -0.4
            // 0.8 => -0.2
            // 2.8 => -0.2
            // if we are at 2.2, => 0.2
            // 2.6 => -0.4
            // -0.6 => 0.4

            if (widthsFromCamera > 0)
                widthsFromCamera += 0.5f;
            else
                widthsFromCamera -= 0.5f;

            int widthsToFix = (int)widthsFromCamera;

            position.x -= widthsToFix * mapWidth;
        }

        if (HexMap.allowWrapNorthSouth)
        {
            float heigthsFromCamera = (position.z - cameraPosition.z) / mapHeight;

            // We want widthsFromCamera to be (-0.5, 0.5)
            
            if (heigthsFromCamera > 0)
                heigthsFromCamera += 0.5f;
            else
                heigthsFromCamera -= 0.5f;

            int heightsToFix = (int)heigthsFromCamera;

            position.z -= heightsToFix * mapHeight;
        }
        return position;
    }
    #endregion
    public static float CostEstimate(IQPathTile aa, IQPathTile bb)
    {
        return Distance((Hex)aa, (Hex)bb);
    }

    public static float Distance(Hex a, Hex b)
    {
        // returns distance between hexes
        // TODO: Distance getter is probably wrong
        int dQ = Mathf.Abs(a.Q - b.Q);
        if (dQ > a.HexMap.NumColoums / 2)
            dQ = a.HexMap.NumColoums - dQ;

        int dR = Mathf.Abs(a.R - b.R);
        if (dR > a.HexMap.NumRows / 2)
            dR = a.HexMap.NumRows - dR;

        return Mathf.Max(
            dQ, 
            dR, 
            Mathf.Abs(a.S - b.S));
    }

    #region Add/Remove Mapobjects
    public void AddUnit(Unit unit)
    {
        if (units == null)
            units = new HashSet<Unit>();

        units.Add(unit);  // can't throw the same object in a hashset (WONT ADD A DUPLICATE)
    }

    public void AddCity(City city)
    {
        if (this.City != null)
        {
            throw new UnityException("Trying to add city to a hex that already has one!");
        }
        this.City = city;
    }

    public void RemoveCity(City city)
    {
        if (this.City == null)
        {
            Debug.LogError("Trying to remove a city that doesn't exist!");
            return;
        }
        if (this.City != city)
        {
            Debug.LogError("Trying to remove a city that isn't ours");
            return;
        }

        this.City = null;
    }

    public void RemoveUnit(Unit unit)
    {
        if (units != null)
            units.Remove(unit);  // Wont get error if it failed to find the unit in the hashset
    }

    #endregion

    public int BaseMovementCostToEnter(bool isHillWalker, bool isForestWalker, bool isFlyer, bool isBoat)
    {
        int moveCost = 1;
        if (ElevationType == ELEVATION_TYPE.HILL && !isHillWalker)
            moveCost += 1;

        if (((FeatureType == FEATURE_TYPE.FOREST) || (FeatureType == FEATURE_TYPE.RAINFOREST)) && !isForestWalker)
            moveCost += 1;

        if (ElevationType == ELEVATION_TYPE.MOUNTAIN && !isFlyer)
            return -99;

        if (ElevationType == ELEVATION_TYPE.WATER && !isBoat && !isFlyer)
            return -99;

        return moveCost;
    }

    Hex[] neighbours;

    #region IQPathTile implementation 
    public IQPathTile[] GetNeighbours()
    {
        if (this.neighbours != null)
            return this.neighbours;
        
        List<Hex> neighbours = new List<Hex>();

        neighbours.Add(HexMap.getHexAt( Q + 1, R + 0));
        neighbours.Add(HexMap.getHexAt(Q + -1, R + 0));
        neighbours.Add(HexMap.getHexAt(Q + 0, R + 1));
        neighbours.Add(HexMap.getHexAt(Q + 0, R + -0));
        neighbours.Add(HexMap.getHexAt(Q + 1, R + -1));
        neighbours.Add(HexMap.getHexAt(Q + -1, R + 1));

        List<Hex> neighbours2 = new List<Hex>();

        foreach (Hex h in neighbours)
        {
            if (h != null)
            {
                neighbours2.Add(h);
            }
        }

        this.neighbours = neighbours2.ToArray();

        return this.neighbours;

    }

    public float AggregateCostToEnter(float costSoFar, IQPathTile sourceTile, IQPathUnit unit)
    {
        // TODO: we're ignoring source tile right now, this will have to change when we have rivers
        return ((Unit)unit).AggregateTurnsToEnterHex(this, costSoFar);
    }
    #endregion
} // class
