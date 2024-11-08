// FloodFill.cs
using UnityEngine;
using System.Collections.Generic;
public class FloodFill
{
    public static uint[,] FloodFillMatrix(ref sbyte[,] wallMap, Vector2Int startNode)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(startNode);
        uint[,] matrix = new uint[wallMap.GetLength(0), wallMap.GetLength(1)];
        while (queue.Count > 0)
        {
            Vector2Int currentNode = queue.Dequeue();
            List<Vector2Int> neighbors = GetNeighbors(currentNode, ref matrix);
            if (wallMap[currentNode.x, currentNode.y] == 0)
            {
                uint max = 0; // TODO: Watchout for overflow
                foreach (Vector2Int neighbor in neighbors)
                {
                    if (matrix[neighbor.x, neighbor.y] > max)
                    {
                        max = matrix[neighbor.x, neighbor.y];
                    }
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
                matrix[currentNode.x, currentNode.y] = max + 1;
            }

        }
        return matrix;
    }
    public static List<Vector2Int> GetNeighbors(Vector2Int node, ref uint[,] matrix){
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (node.x > 0 ) neighbors.Add(new Vector2Int(node.x - 1, node.y));
        if (node.x < matrix.GetLength(0) - 1) neighbors.Add(new Vector2Int(node.x + 1, node.y));
        if (node.y > 0) neighbors.Add(new Vector2Int(node.x, node.y - 1));
        if (node.y < matrix.GetLength(1) - 1) neighbors.Add(new Vector2Int(node.x, node.y + 1));
        return neighbors;
    }
}