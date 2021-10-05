using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMap : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
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

    public float MoistureJungle = 1f;
    public float MoistureForest = 0.8f;
    public float MoistureGrasslands = 0.5f;
    public float MoisturePlains = 0.2f;

    // Tiles with height above x is a y
    public float HeightMountain = 1f;
    public float HeightHill = 0.2f;
    public float HeightFlat = 0.0f;

    public readonly int NumRows = 30;
    public readonly int NumColoums = 60;

    public bool allowWrapEastWest = true;
    public bool allowWrapNorthSouth = false;

    private Hex[,] hexes;
    private Dictionary<Hex, GameObject> hexToGameObjectMap;

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
                        GameObject.Instantiate(JunglePrefab, p, Quaternion.identity, HexGO.transform);
                    }
                    else if (h.Moisture >= MoistureForest)
                    {
                        mr.material = MatGrasslands;
                        // spawn forests
                        Vector3 p = HexGO.transform.position;
                        if (h.Elevation > HeightHill)
                            p.y += 0.25f;
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
                }
                else if (h.Elevation >= HeightHill)
                {
                    mf.mesh = MeshHill;
                }
                else if (h.Elevation >= HeightFlat)
                {
                    mf.mesh = MeshFlat;
                }
                else
                {
                    mr.material = MatOcean;
                    mf.mesh = MeshWater;
                }

                
                // HexGO.GetComponentInChildren<TextMesh>().text = string.Format("{0}, {1}, {2}", column, row, (int)(h.Elevation * 10));
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

}  // class
