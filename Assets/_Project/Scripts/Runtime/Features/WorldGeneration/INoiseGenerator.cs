using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Runtime.Features.WorldGeneration
{
    public interface INoiseGenerator
    {
        public float[,] GenerateHeightMap(
            int width,
            int height,
            float scale,
            int octaves,
            float persistence,
            float lacunarity,
            Vector2 offset);
    }
}
