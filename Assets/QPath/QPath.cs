using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPath
{
    // Tile[] ourPath = QPath.FindPath( ourWorld, theUnit, startTile, endTile );

    // theUnit is an object that actually tries to path between tiles,
    // it might have special logic based on it's movement type and the type of tiles being moved through 

    // Our tiles need to be abel to return the following info:
    // 1. List of neighbours
    // 2. The cost to enter this tile from another tile

    public static class QPath
    {
        public static T[] FindPath<T>(IQPathWorld world, IQPathUnit unit, T startTile, T endTile, CostEstimateDelegate costEstimateFunc) where T : IQPathTile
        {
            if ( world == null || unit == null || startTile == null || endTile == null)
            {
                Debug.LogError("Null values passed to QPath :: FindPath");
                return null;
            }

            // Call on our path solver
            QPathAStar<T> resolver = new QPathAStar<T>(world, unit, startTile, endTile, costEstimateFunc);

            resolver.DoWork();

            return resolver.GetList();
        }
    }

    public delegate float TileEnteringCostDelegate();

    public delegate float CostEstimateDelegate(IQPathTile a, IQPathTile b);
}
