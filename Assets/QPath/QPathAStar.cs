using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;


namespace QPath
{
    public class QPathAStar
    {
        Queue<IQPathTile> path;

        IQPathWorld world;
        IQPathUnit unit;
        IQPathTile startTile;
        IQPathTile endTile;
        CostEstimateDelegate costEstimateFunc;

        public QPathAStar(IQPathWorld world, IQPathUnit unit, IQPathTile startTile, IQPathTile endTile, CostEstimateDelegate costEstimateFunc)
        {
            // Do setup

            this.world = world;
            this.unit = unit;
            this.startTile = startTile;
            this.endTile = endTile;
            this.costEstimateFunc = costEstimateFunc;
        }    

        public void DoWork()
        {
            path = new Queue<IQPathTile>();

            HashSet<IQPathTile> closedSet = new HashSet<IQPathTile>();

            PathfindingPriorityQueue<IQPathTile> openSet = new PathfindingPriorityQueue<IQPathTile>();
            openSet.Enqueue(startTile, 0);

            Dictionary<IQPathTile, IQPathTile> came_From = new Dictionary<IQPathTile, IQPathTile>();

            Dictionary<IQPathTile, float> g_score = new Dictionary<IQPathTile, float>();
            g_score[startTile] = 0;

            Dictionary<IQPathTile, float> f_score = new Dictionary<IQPathTile, float>();
            f_score[startTile] = costEstimateFunc(startTile, endTile);

            while (openSet.Count > 0)
            {
                IQPathTile current = openSet.Dequeue();

                // check if goal is where we are
                if (current == endTile)
                {
                    Reconstruct_Path(came_From, current);
                    return;
                }

                closedSet.Add(current);

                foreach (IQPathTile edge_neighbour in current.GetNeighbours())
                {
                    IQPathTile neighbour = edge_neighbour;

                    if (closedSet.Contains(neighbour))
                    {
                        continue;  // ignore this already completed neighbour
                    }

                    float total_pathfinding_cost_to_neighbour = neighbour.AggregateCostToEnter(g_score[current], current, unit);

                    if (total_pathfinding_cost_to_neighbour < 0)
                    {
                        // impassable/invalid terrain
                        continue;

                    }

                    float tentative_g_score = total_pathfinding_cost_to_neighbour;

                    if (openSet.Contains(neighbour) && tentative_g_score >= g_score[neighbour])
                    {
                        continue; // skip, shorter path already found
                    }

                    came_From[neighbour] = current;
                    g_score[neighbour] = tentative_g_score;
                    f_score[neighbour] = g_score[neighbour] + costEstimateFunc(neighbour, endTile);

                    openSet.EnqueueOrUpdate(neighbour, f_score[neighbour]);
                } // foreach
            } // while
        }

        public void Reconstruct_Path(Dictionary<IQPathTile, IQPathTile> cameFrom, IQPathTile current)
        {
            // So at this point, current IS the goal.
            // So what we want to do is go backwards through the dictionaries, until we reach the "end", which is the starting node
            Queue<IQPathTile> total_path = new Queue<IQPathTile>();
            total_path.Enqueue(current); // This final step is the path to the goal

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                total_path.Enqueue(current);
            }

            // Now, total_path is a queue that is running backwards from the end tile to the start tile, so reverse
            path = new Queue<IQPathTile>(total_path.Reverse());
        }

        public IQPathTile[] GetList()
        {
            return path.ToArray();
        }


    }
}