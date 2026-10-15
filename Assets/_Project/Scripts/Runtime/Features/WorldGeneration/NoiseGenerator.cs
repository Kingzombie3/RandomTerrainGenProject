using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Runtime.Features.WorldGeneration
{
    public class NoiseGenerator : INoiseGenerator
    {
        public float[,] GenerateHeightMap(
            int width, 
            int height, 
            float scale, 
            int octaves, 
            float persistence,
            float lacunarity, 
            Vector2 offset)
        {
            float[,] heightMap = new float[width, height];
            
            if (scale <= 0)
            {
                scale = 0.0001f;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float frequency = 1f;
                    float amplitude = 1f;
                    float noiseHeight = 0f;
                    float maxPossibleHeight = 0f;

                    for (int i = 0; i < octaves; i++)
                    {
                        float sampleX = (x + offset.x) / scale * frequency;
                        float sampleY = (y + offset.y) / scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

                        //float centeredValue = perlinValue * 2f - 1f;

                        //noiseHeight += centeredValue * amplitude;

                        // Запоминаем максимально возможную высоту для последующей нормализации
                        maxPossibleHeight += amplitude;

                        // Изменяем параметры для следующей, более мелкой октавы
                        amplitude *= persistence; // Амплитуда затухает (мелкие детали меньше влияют)
                        frequency *= lacunarity;  // Частота растет (детали становятся мельче)

                        noiseHeight += perlinValue * amplitude;

                        maxPossibleHeight += amplitude;
                        amplitude *= persistence;
                        frequency += lacunarity;

                        float normalizedHeight = (noiseHeight / maxPossibleHeight + 1f) / 2f;
                        //float noiseValue = GetFractalNoiseValue(sampleX, sampleY, octaves, persistence, lacunarity);

                        heightMap[y, x] = normalizedHeight;
                    }

                    //heightMap[y, x] = Mathf.InverseLerp(- maxPossibleHeight, maxPossibleHeight, noiseHeight);
                }
            }

            return heightMap;
        }
    }
}
