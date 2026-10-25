using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static _Project.Scripts.Runtime.Core.PerlinNoise;

namespace _Project.Scripts.Runtime.Core.WorldGeneration
{
    public class NoiseGenerator : INoiseGenerator
    {
        public async UniTask<float[,]> GenerateHeightMap(
            int width,
            int height,
            float scale,
            int octaves,
            float persistence,
            float lacunarity,
            Vector2 offset,
            Vector2 worldOffset = default,
            float chunkSize = 0f)       
        {
            if (scale <= 0) scale = 0.0001f;

            float[,] heightMap = new float[height, width];

            return await UniTask.RunOnThreadPool(() =>
            {
                float maxPossibleHeight = 0f;
                float currentAmplitude = 1f;
                for (int i = 0; i < octaves; i++)
                {
                    maxPossibleHeight += currentAmplitude;
                    currentAmplitude *= persistence;
                }

                float actualChunkSize = chunkSize > 0 ? chunkSize : width;

                //шаг сетки
                float stepX = actualChunkSize / (width - 1);
                float stepY = actualChunkSize / (height - 1);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float worldX = worldOffset.x + (x * stepX) + offset.x;
                        float worldY = worldOffset.y + (y * stepY) + offset.y;

                        float frequency = 1f;
                        float amplitude = 1f;
                        float noiseHeight = 0f;

                        for (int i = 0; i < octaves; i++)
                        {
                            float sampleX = worldX / scale * frequency;
                            float sampleY = worldY / scale * frequency;

                            float perlinValue = PurePerlinNoise.Noise(sampleX, sampleY);
                            float centeredValue = perlinValue * 2f - 1f;

                            noiseHeight += centeredValue * amplitude;

                            amplitude *= persistence;
                            frequency *= lacunarity;
                        }

                        float normalizedHeight = (noiseHeight / maxPossibleHeight + 1f) / 2f;

                        normalizedHeight = Mathf.Clamp01(normalizedHeight);
                        normalizedHeight = Mathf.Pow(normalizedHeight, 2f);

                        //unity terrain format [z,x]
                        heightMap[y, x] = normalizedHeight;
                    }
                }

                return heightMap;
            });
        }
    }
}
