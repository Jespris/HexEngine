using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QPath
{
    public class IQPathTileGraph
    { 
        Dictionary<IQPathTile, IQPathTile[]> neighbours;
        // The graph's job is to keep a list of all neighbours leaving a tile
        public IQPathTileGraph(IQPathWorld world)
        {
            
        }
    }
}
