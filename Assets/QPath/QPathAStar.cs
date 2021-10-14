using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;
using System.Linq;


namespace QPath
{
    public class QPathAStar<T> where T : IQPathTile
    {
        Queue<T> path;

        IQPathWorld world;
        IQPathUnit unit;
        T startTile;
        T endTile;
        CostEstimateDelegate costEstimateFunc;

        public QPathAStar(IQPathWorld world, IQPathUnit unit, T startTile, T endTile, CostEstimateDelegate costEstimateFunc)
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
            path = new Queue<T>();

            HashSet<T> closedSet = new HashSet<T>();

            PathfindingPriorityQueue<T> openSet = new PathfindingPriorityQueue<T>();
            openSet.Enqueue(startTile, 0);

            Dictionary<T, T> came_From = new Dictionary<T, T>();

            Dictionary<T, float> g_score = new Dictionary<T, float>();
            g_score[startTile] = 0;

            Dictionary<T, float> f_score = new Dictionary<T, float>();
            f_score[startTile] = costEstimateFunc(startTile, endTile);

            while (openSet.Count > 0)
            {
                T current = openSet.Dequeue();

                // check if goal is where we are
                if (System.Object.ReferenceEquals(current, endTile))
                {
                    Reconstruct_Path(came_From, current);
                    return;
                }

                closedSet.Add(current);

                foreach (T edge_neighbour in current.GetNeighbours())
                {
                    T neighbour = edge_neighbour;

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

        public void Reconstruct_Path(Dictionary<T, T> cameFrom, T current)
        {
            // So at this point, current IS the goal.
            // So what we want to do is go backwards through the dictionaries, until we reach the "end", which is the starting node
            Queue<T> total_path = new Queue<T>();
            total_path.Enqueue(current); // This final step is the path to the goal

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                total_path.Enqueue(current);
            }

            // Now, total_path is a queue that is running backwards from the end tile to the start tile, so reverse
            path = new Queue<T>(total_path.Reverse());
        }

        public T[] GetList()
        {
            return path.ToArray();
        }


    }
}