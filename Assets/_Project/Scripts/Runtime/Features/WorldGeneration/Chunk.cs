using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Runtime.Features.WorldGeneration
{
    public class Chunk
    {
        public Vector2Int Coordinate { get; }
        public Terrain TerrainInstance { get; }

        public Chunk(Vector2Int coordinate, Terrain terrainInstance)
        {
            Coordinate = coordinate;
            TerrainInstance = terrainInstance;
        }
    }
}
