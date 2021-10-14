using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapContinent : HexMap
{
    public override void GenerateMap()
    {
        // First, call base version to make all the hexes
        base.GenerateMap();

        // MapSeed 
        Random.InitState(0);
        // Make some kind of landmass (raised area)
        // MakeRandomLandMasses(8, 2, 8);
        AddContinents(4, 6);

        // Add lumpyness (noise?)
        AddPerlinNoiseToMap(1.5f);
        // AddPerlinNoiseToMap(0.5f);

        // Set meshy to mountain/hill/flat/water based on height

        // Simulate rainfall, and set plains/grassland + forest
        AddMoistureToMap(1f);

        // Now make sure all the hex visuals are updated to match the hex data
        UpdateHexVisuals();

        // Spawn Units
        Unit unit = new Unit();
        // for testing, turn on isSettler for this unit
        unit.isSettler = true;
        City city = new City();

        SpawnUnitAt(unit, UnitDwarfPrefab, 29, 23);
        SpawnCityAt(city, CityPrefab, 30, 23);

    }

    void ElevateHexArea(int q, int r, int radius, float elevationChange = 0.7f)
    {
        // constriction; q + r + s = 0
        Hex centerHex = getHexAt(q, r);

        Hex[] areaHexes = GetHexesWithinRangeOf(centerHex, radius);

        foreach (Hex h in areaHexes)
        {
            if (h.Elevation < 0)  // set the elevation to 0 if its water currently
                h.Elevation = 0;
            h.Elevation = elevationChange * Mathf.Lerp(1f, 0.25f, Hex.Distance(centerHex, h) / radius) + Random.Range(-elevationChange / 4, elevationChange / 2);
        }
    }

    void MakeRandomLandMasses(int nr, int minRadius, int maxRadius)
    {
        for (int i = 0; i < nr; i++)
        {
            int randomRadius = Random.Range(minRadius, maxRadius);
            int randomCol = Random.Range(0, NumColoums);
            int randomRow = Random.Range(randomRadius, NumRows - randomRadius);
            ElevateHexArea(randomCol, randomRow, randomRadius);
        }
    }

    void AddContinents(int nr, int continentBlobbyness)
    {
        List<Hex> continentCenters = new List<Hex>();
        bool continentCenterFound = false;
        int minimumDistanceBetweenContinents = NumColoums / (nr * 2);
        int randomCol = 0;
        int randomRow = 0;
        Hex hexOnRandomSquare = getHexAt(0, 0);
        for (int i = 0; i < nr; i++)
        {
            while (!continentCenterFound)
            {
                randomCol = Random.Range(0, NumColoums - 1);
                randomRow = Random.Range(0, NumRows - 1);
                hexOnRandomSquare = getHexAt(randomCol, randomRow);
                continentCenterFound = true;
                foreach (Hex continentCenter in continentCenters)
                {
                    if (Hex.Distance(hexOnRandomSquare, continentCenter) < minimumDistanceBetweenContinents)
                    {
                        continentCenterFound = false;
                        // Debug.Log("Continent too close to another continent!");
                    }
                }
                if (continentCenterFound)
                {
                    // Debug.Log("New Continent placement found! On: " + randomCol + ", " + randomRow);
                    continentCenters.Add(hexOnRandomSquare);
                }
            }
            int LandSize = 5;
            int maxRadiusFromCenter = LandSize * 2;
            for (int k = 0; k < continentBlobbyness; k++)
            {
                int newRandomCol = (randomCol + Random.Range(-maxRadiusFromCenter, maxRadiusFromCenter)) % NumColoums;
                int newRandomRow = (randomRow + Random.Range(-maxRadiusFromCenter, maxRadiusFromCenter)) % NumRows;
                if (newRandomRow < LandSize)
                    newRandomRow = LandSize;
                if (newRandomRow > NumRows - LandSize)
                    newRandomRow = NumRows - LandSize;
                // Debug.Log("New center of landmass on current continent: " + newRandomCol + ", " + newRandomRow);
                ElevateHexArea(newRandomCol, newRandomRow, Random.Range(2, LandSize));
                // Debug.Log("Adding land to continent...");
            }
            continentCenterFound = false;
        }
    }

    void AddPerlinNoiseToMap(float noiseScale)
    {
        float noiseResolution = 0.01f;
        Vector2 noiseOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

        for (int column = 0; column < NumColoums; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                Hex h = getHexAt(column, row);
                float x = ((float)column / NumColoums / noiseResolution) + noiseOffset.x * 1000;
                float y = ((float)row / NumColoums / noiseResolution) + noiseOffset.y * 1000;

                float n = Mathf.PerlinNoise(x, y) - 0.5f;
                h.Elevation += n * noiseScale;
            }
        }
    }

    void AddMoistureToMap(float moistureScale)
    {
        float noiseResolution = 0.1f;
        Vector2 noiseOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

        for (int column = 0; column < NumColoums; column++)
        {
            for (int row = 0; row < NumRows; row++)
            {
                Hex h = getHexAt(column, row);
                float x = ((float)column / NumColoums / noiseResolution) + noiseOffset.x * 1000;
                float y = ((float)row / NumColoums / noiseResolution) + noiseOffset.y * 1000;

                float n = Mathf.PerlinNoise(x, y);
                h.Moisture = n * moistureScale;
            }
        }
    }
}
