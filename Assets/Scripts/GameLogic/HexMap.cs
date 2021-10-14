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

    public bool DeveloperMode = false;

    public bool AnimationIsPlaying = true; 

    private void Update()
    {
        // TESTING: Hit spacebar to advance a turn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine("DoUnitMoves");
        }
    }

    #region UnitMoves
    IEnumerator DoAllUnitMoves()
    {
        if (units != null)
            {
                foreach (Unit u in units)
                {
                    yield return DoUnitMoves( u );
                }
            }
    }

    public void EndTurn()
    {
        // First check if units have enqueued moves 
            // Do those moves
        // Now, are any units waiting for orders, if so, halt EndTurn()

        // Heal units that are resting
        // Reset unit movement
        foreach (Unit u in units)
        {
            u.RefreshMovement();
        }
    }

    public IEnumerator DoUnitMoves(Unit u)
    {
        while (u.DoMove())
        {
            Debug.Log("DoMove returned true, will be called again");
            // TODO: check if animation is playing, if so wait for it to finish
            while (AnimationIsPlaying)
            {
                yield return null;  // wait a frame
            }
        }
    }
    #endregion

    #region Class Variables
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
    public GameObject CityPrefab;

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
    private Dictionary<GameObject, Hex> gameObjectToHexMap;

    // TODO: Separate unit lists for each faction
    private HashSet<Unit> units;
    private Dictionary<Unit, GameObject> unitToGameObjectMap;

    private HashSet<City> cities;
    private Dictionary<City, GameObject> cityToGameObjectMap;
    #endregion

    #region Getting hexes, gameobjects, and positions for those
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

    public Hex GetHexFromGameObject(GameObject hexGO)
    {
        if (gameObjectToHexMap.ContainsKey(hexGO))
        {
            return gameObjectToHexMap[hexGO];
        }

        return null;
    }

    public GameObject GetHexGO(Hex h)
    {
        if (hexToGameObjectMap.ContainsKey(h))
        {
            return hexToGameObjectMap[h];
        }
        return null;
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
    #endregion
    virtual public void GenerateMap()
    {

        hexes = new Hex[NumColoums, NumRows];
        hexToGameObjectMap = new Dictionary<Hex, GameObject>();
        gameObjectToHexMap = new Dictionary<GameObject, Hex>();
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
                gameObjectToHexMap[HexGO] = h;

                h.TerrainType = Hex.TERRAIN_TYPE.OCEAN;
                h.ElevationType = Hex.ELEVATION_TYPE.WATER;
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

                h.FeatureType = Hex.FEATURE_TYPE.NONE;
                h.ElevationType = Hex.ELEVATION_TYPE.FLAT;
                h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;

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

                        h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;
                        h.FeatureType = Hex.FEATURE_TYPE.RAINFOREST;

                        GameObject.Instantiate(JunglePrefab, p, Quaternion.identity, HexGO.transform);
                    }
                    else if (h.Moisture >= MoistureForest)
                    {
                        mr.material = MatGrasslands;
                        // spawn forests
                        Vector3 p = HexGO.transform.position;
                        if (h.Elevation > HeightHill)
                            p.y += 0.25f;

                        h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;
                        h.FeatureType = Hex.FEATURE_TYPE.FOREST;

                        GameObject.Instantiate(ForestPrefab, p, Quaternion.identity, HexGO.transform);
                    }
                    else if (h.Moisture >= MoistureGrasslands)
                    {
                        h.TerrainType = Hex.TERRAIN_TYPE.GRASSLANDS;
                        mr.material = MatGrasslands;
                    }
                    else if (h.Moisture >= MoisturePlains)
                    {
                        h.TerrainType = Hex.TERRAIN_TYPE.PLAINS;
                        mr.material = MatPlains;
                    }
                    else
                    {
                        h.TerrainType = Hex.TERRAIN_TYPE.DESERT;
                        mr.material = MatDesert;
                    }
                }

                if (h.Elevation >= HeightMountain)
                {
                    mr.material = MatMountains;
                    mf.mesh = MeshMountain;

                    h.ElevationType = Hex.ELEVATION_TYPE.MOUNTAIN;

                }
                else if (h.Elevation >= HeightHill)
                {
                    mf.mesh = MeshHill;

                    h.ElevationType = Hex.ELEVATION_TYPE.HILL;

                }
                else if (h.Elevation >= HeightFlat)
                {
                    mf.mesh = MeshFlat;
                    h.ElevationType = Hex.ELEVATION_TYPE.FLAT;
                }
                else
                {
                    mr.material = MatOcean;
                    mf.mesh = MeshWater;

                    h.ElevationType = Hex.ELEVATION_TYPE.WATER;

                }

                if (DeveloperMode)
                    HexGO.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}\n{2}", column, row, h.BaseMovementCostToEnter(false, false, false, false));
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

    #region MapObject Spawners
    public void SpawnUnitAt(Unit unit, GameObject prefab, int q, int r)
    {
        if (units == null)
        {
            units = new HashSet<Unit>();
            unitToGameObjectMap = new Dictionary<Unit, GameObject>();
        }
        Hex myHex = getHexAt(q, r);
        GameObject myHexGO = hexToGameObjectMap[myHex];
        unit.SetHex(myHex);

        GameObject unitGO = (GameObject)Instantiate(prefab, myHexGO.transform.position, Quaternion.identity, myHexGO.transform);
        unit.OnObjectMoved += unitGO.GetComponent<UnitView>().OnUnitMoved;

        units.Add(unit);
        unitToGameObjectMap.Add(unit, unitGO);
    }

    public void SpawnCityAt ( City city, GameObject prefab, int q , int r)
    {
        Debug.Log("Spawning city at: " + q + ", " + r);

        if (cities == null)
        {
            cities = new HashSet<City>();
            cityToGameObjectMap = new Dictionary<City, GameObject>();
        }

        Hex myHex = getHexAt(q, r);
        GameObject myHexGO = hexToGameObjectMap[myHex];
        try
        {
            city.SetHex(myHex);
        }
        catch (UnityException e)
        {
            Debug.LogError(e.Message);
            return;
        }
        GameObject cityGO = (GameObject)Instantiate(prefab, myHexGO.transform.position, Quaternion.identity, myHexGO.transform);

        cities.Add(city);
        cityToGameObjectMap.Add(city, cityGO);
    }
    #endregion
}  // class
