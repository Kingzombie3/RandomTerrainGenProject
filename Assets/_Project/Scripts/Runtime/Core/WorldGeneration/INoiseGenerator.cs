using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Runtime.Core.WorldGeneration
{
    public interface INoiseGenerator
    {
        public UniTask<float[,]> GenerateHeightMap(
            int width,
            int height,
            float scale,
            int octaves,
            float persistence,
            float lacunarity,
            Vector2 offset,
            Vector2 worldOffset,
            float chunkSize);
    }
}
