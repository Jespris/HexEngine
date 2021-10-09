using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QPath;

public class HexMap : MonoBehaviour, IQPathWorld
{
    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        // TESTING: Hit spacebar to advance a turn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (units != null)
            {
                foreach (Unit u in units)
                {
                    u.DoTurn();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (units != null)
            {
                foreach (Unit u in units)
                {
                    u.DUMMY_PATHFINDING_FUNCTION();
                }
            }
        }
    }

    public GameObject HexPrefab;

    public Mesh MeshWater;
    public Mesh MeshFlat;
    public Mesh MeshHill;
    public Mesh MeshMountain;

    public Material MatOcean;
    public Material MatPlains;
    public Material MatGrasslands;
    public Material MatMountains;
    public Material MatDesert;

    public GameObject ForestPrefab;
    public GameObject JunglePrefab;

    public GameObject UnitDwarfPrefab;

    [System.NonSerialized] public float MoistureJungle = 0.8f;
    [System.NonSerialized] public float MoistureForest = 0.6f;
    [System.NonSerialized] public float MoistureGrasslands = 0.5f;
    [System.NonSerialized] public float MoisturePlains = 0.3f;

    // Tiles with height above x is a y
    [System.NonSerialized] public float HeightMountain = 0.8f;
    [System.NonSerialized] public float HeightHill = 0.3f;
    [System.NonSerialized] public float HeightFlat = 0.0f;

    public readonly int NumRows = 30;
    public readonly int NumColoums = 60;

    [System.NonSerialized] public bool allowWrapEastWest = true;
    [System.NonSerialized] public bool allowWrapNorthSouth = false;

    private Hex[,] hexes;
    private Dictionary<Hex, GameObject> hexToGameObjectMap;

    private HashSet<Unit> units;
    private Dictionary<Unit, GameObject> UnitToGameObjectMap;

    public Hex getHexAt(int x, int y)
    {
        if (hexes == null)
        {
            Debug.LogError("Hexes array not yet instantiated!");
            return null;
        }

        if (allowWrapEastWest)
            x = (x + NumColoums) % NumColoums;
        if (allowWrapNorthSouth)
            y = (y + NumRows) % NumRows;

        try
        {
            return hexes[x, y];
        } catch
        {
            Debug.Log("Getting hex at: " + x + ", " + y + " failed!");
            return null;
        }
    }

    public Vector3 GetHexPosition(int q, int r)
    {
        Hex hex = getHexAt(q, r);

        return GetHexPosition(hex);
    }

    public Vector3 GetHexPosition(Hex hex)
    {
        return hex.PositionFromCamera(Camera.main.transform.position, NumRows, NumColoums);
    }

    virtual public void GenerateMap()
    {

        hexes = new Hex[NumColoums, NumRows];
        hexToGameObjectMap = new Dictionary<Hex, GameObject>();
        // Generate blank (ocean) map 

        for (int column = 0; column < NumColoums; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                // Instantiate a hex
                Hex h = new Hex( this, column, row);

                hexes[column, row] = h;

                h.Elevation = -0.5f;

                Vector3 pos = h.PositionFromCamera(Camera.main.transform.position, NumRows, NumColoums);

                GameObject HexGO = (GameObject)Instantiate(HexPrefab, pos, Quaternion.identity, this.transform);  // Parent is hexmap

                HexGO.name = string.Format("HEX: {0}, {1}", column, row);
                HexGO.GetComponent<HexBehaviour>().Hex = h;
                HexGO.GetComponent<HexBehaviour>().HexMap = this;

                hexToGameObjectMap[h] = HexGO;  // assign the key and gameobject to the dictionary
            }
        }

        UpdateHexVisuals();

        // StaticBatchingUtility.Combine(this.gameObject);  use if you're not moving the tiles

    }

    public void UpdateHexVisuals()
    {
        for (int column = 0; column < NumColoums; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                Hex h = hexes[column, row];
                GameObject HexGO = hexToGameObjectMap[h];

                MeshRenderer mr = HexGO.GetComponentInChildren<MeshRenderer>();
                MeshFilter mf = HexGO.GetComponentInChildren<MeshFilter>();

                h.movementCost = 1;

                // Moisutre
                if (h.Elevation >= HeightFlat && h.Elevation < HeightMountain)
                {
                    if (h.Moisture >= MoistureJungle)
                    {
                        mr.material = MatGrasslands;
                        // spawn jungle
                        Vector3 p = HexGO.transform.position;
                        if (h.Elevation > HeightHill)
                            p.y += 0.25f;
                        h.movementCost = 2;
                        GameObject.Instantiate(JunglePrefab, p, Quaternion.identity, HexGO.transform);
                    }
                    else if (h.Moisture >= MoistureForest)
                    {
                        mr.material = MatGrasslands;
                        // spawn forests
                        Vector3 p = HexGO.transform.position;
                        if (h.Elevation > HeightHill)
                            p.y += 0.25f;
                        h.movementCost = 2;
                        GameObject.Instantiate(ForestPrefab, p, Quaternion.identity, HexGO.transform);
                    }
                    else if (h.Moisture >= MoistureGrasslands)
                    {
                        mr.material = MatGrasslands;
                    }
                    else if (h.Moisture >= MoisturePlains)
                    {
                        mr.material = MatPlains;
                    }
                    else
                    {
                        mr.material = MatDesert;
                    }
                }

                if (h.Elevation >= HeightMountain)
                {
                    mr.material = MatMountains;
                    mf.mesh = MeshMountain;
                    h.movementCost = -99;
                }
                else if (h.Elevation >= HeightHill)
                {
                    mf.mesh = MeshHill;
                    h.movementCost = 2;
                }
                else if (h.Elevation >= HeightFlat)
                {
                    mf.mesh = MeshFlat;
                }
                else
                {
                    mr.material = MatOcean;
                    mf.mesh = MeshWater;
                    h.movementCost = -99;
                }

                HexGO.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}\n{2}", column, row, h.BaseMovementCostToEnter());
            }
        }
    }

    public Hex[] GetHexesWithinRangeOf(Hex centerHex, int range)
    {
        List<Hex> results = new List<Hex>();

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = Mathf.Max(-range, -dx - range); dy <= Mathf.Min(range, -dx + range); dy++)
            {
                int newRow = centerHex.R + dy;
                if (!allowWrapNorthSouth)
                {
                    if (newRow >= 0 && newRow < NumRows)
                    {
                        Hex newHex = getHexAt((centerHex.Q + dx) % NumColoums, centerHex.R + dy);
                        results.Add(newHex);
                    }
                }
            }
        }
        return results.ToArray();
    }

    public void SpawnUnitAt(Unit unit, GameObject prefab, int q, int r)
    {
        if (units == null)
        {
            units = new HashSet<Unit>();
            UnitToGameObjectMap = new Dictionary<Unit, GameObject>();
        }
        Hex myHex = getHexAt(q, r);
        GameObject myHexGO = hexToGameObjectMap[myHex];
        unit.SetHex(myHex);

        GameObject unitGO = (GameObject)Instantiate(prefab, myHexGO.transform.position, Quaternion.identity, myHexGO.transform);
        unit.OnUnitMoved += unitGO.GetComponent<UnitView>().OnUnitMoved;

        units.Add(unit);
        UnitToGameObjectMap.Add(unit, unitGO);
    }
}  // class
