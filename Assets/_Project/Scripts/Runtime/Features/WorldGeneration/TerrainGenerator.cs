using _Project.Scripts.Runtime.Core.WorldGeneration;
using _Project.Scripts.Runtime.Features.WorldGeneration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using Codice.Client.BaseCommands.Changelist;
using UnityEngine.UIElements;

namespace _Project.Scripts.Runtime
{
    public class TerrainGenerator : MonoBehaviour
    {
        const string GenerateTerrainKey = "Generate Terrain";

        [BoxGroup("Terrarin data")]
        [SerializeField]
        private Terrain _chunkPrefab;

        [BoxGroup("Terrarin data")]
        [SerializeField]
        private TerrainLayer _terrainLayer;

        [BoxGroup("Terrarin data")]
        [SerializeField]
        private TerrainData _terrainDataTemplate;

        [BoxGroup("Terrarin data")]
        [SerializeField]
        private Material _terrainMaterial;

        [HorizontalGroup("Pool Settings")]
        [SerializeField]
        private int gridSize;

        [HorizontalGroup("Pool Settings")]
        [SerializeField]
        private float _chunkSize = 100f;

        [HorizontalGroup("Noise Settings", Title = "Noise Settings")]
        [SerializeField]
        private int _mapResolution;

        [HorizontalGroup("Noise Settings")]
        [SerializeField]
        private float _scale;

        [HorizontalGroup("Noise Settings")]
        [SerializeField]
        private int _octaves;

        [VerticalGroup("Noise Settings vertical")]
        [SerializeField]
        private float _persistence;

        [VerticalGroup("Noise Settings vertical")]
        [SerializeField]
        private float _lacunarity;

        [VerticalGroup("Noise Settings vertical")]
        [SerializeField]
        private Vector2 _offset;

        private INoiseGenerator _noiseGenerator;

        [SerializeField]
        private List<Chunk> _activeChunks = new();

        [Inject]
        private void Construct(INoiseGenerator noiseGenerator)
        {
            _noiseGenerator = noiseGenerator;
        }

        private void Start()
        {
            GenerateTerrainAsync().Forget();
        }

        [ContextMenu(GenerateTerrainKey), ShowInInspector]
        public void GenerateWorld()
        {
            ClearOldChunks();
            GenerateTerrainAsync().Forget();
        }

        public async UniTaskVoid GenerateTerrainAsync()
        {
            ClearOldChunks();
            int halfSize = gridSize / 2;

            List<UniTask<(Vector2Int coord, Vector3 pos, float[,] heights)>> generateTasks = new();

            for (int x = -halfSize; x <= halfSize; x++)
            {
                for (int z = -halfSize; z <= halfSize; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(x, z);
                    Vector3 spawnPosition = new Vector3(x * _chunkSize, 0, z * _chunkSize);

                    int localX = x;
                    int localZ = z;

                    int resolution = _mapResolution;
                    float scale = _scale;
                    int octaves = _octaves;
                    float persistence = _persistence;
                    float lacunarity = _lacunarity;

                    // Вычисляем чистую позицию угла чанка в мировых метрах
                    Vector2 worldOffset = new Vector2(spawnPosition.x, spawnPosition.z);

                    // Передаем новые параметры в конец вызова, интерфейс UniTask<float[,]> не нарушен
                    UniTask<float[,]> heightMapTask = _noiseGenerator.GenerateHeightMap(
                        resolution,
                        resolution,
                        scale,
                        octaves,
                        persistence,
                        lacunarity,
                        _offset,
                        worldOffset,
                        _chunkSize
                    );

                    var chunkTask = CreateChunkGenerationTask(heightMapTask, chunkCoord, spawnPosition);

                    generateTasks.Add(chunkTask);
                }
            }

            var results = await UniTask.WhenAll(generateTasks);

            foreach (var result in results)
            {
                Terrain terrainInstance = Instantiate(
                       _chunkPrefab,
                        result.pos,
                        Quaternion.identity,
                        transform);

                TerrainData tData = Instantiate(_terrainDataTemplate);
                tData.heightmapResolution = _mapResolution;
                tData.size = new Vector3(_chunkSize, 250f, _chunkSize);

                if (_terrainLayer != null)
                {
                    tData.terrainLayers = new TerrainLayer[] { _terrainLayer };
                }

                terrainInstance.terrainData = tData;

                if (_terrainMaterial != null)
                {
                    terrainInstance.materialTemplate = _terrainMaterial;
                }

                // Передаем чистую матрицу высот. Unity сам построит бесшовные нормали на краях
                tData.SetHeights(0, 0, result.heights);

                int alphaWidth = tData.alphamapWidth;
                int alphaHeight = tData.alphamapHeight;

                // Исправление: В Unity размерность альфамап идет как [строки, колонки], то есть [height, width]
                float[,,] alphaMaps = new float[alphaHeight, alphaWidth, 1];

                for (int alphaY = 0; alphaY < alphaHeight; alphaY++)
                {
                    for (int alphaX = 0; alphaX < alphaWidth; alphaX++)
                    {
                        alphaMaps[alphaY, alphaX, 0] = 1f;
                    }
                }

                tData.SetAlphamaps(0, 0, alphaMaps);

                terrainInstance.Flush();

                Chunk chunk = new Chunk(result.coord, terrainInstance);
                _activeChunks.Add(chunk);
            }

            StitchTerrainNeighbors();
            Terrain.SetConnectivityDirty();
        }

        private async UniTask<(Vector2Int coord, Vector3 pos, float[,] heights)> CreateChunkGenerationTask(
                UniTask<float[,]> heightMapTask, 
                Vector2Int coordinates, 
                Vector3 position)
        {
            float[,] heights = await heightMapTask;
            return (coordinates, position, heights);
        }

        private void StitchTerrainNeighbors()
        {
            foreach (var chunk in _activeChunks)
            {
                Terrain current = chunk.TerrainInstance;
                Vector2Int coord = chunk.Coordinate; // Твой Vector2Int chunkCoord из листа

                // Ищем соседей в нашем списке активных чанков
                Terrain left = _activeChunks.Find(c => c.Coordinate == coord + Vector2Int.left)?.TerrainInstance;
                Terrain right = _activeChunks.Find(c => c.Coordinate == coord + Vector2Int.right)?.TerrainInstance;
                Terrain top = _activeChunks.Find(c => c.Coordinate == coord + Vector2Int.up)?.TerrainInstance; // В Unity Z — это Top
                Terrain bottom = _activeChunks.Find(c => c.Coordinate == coord + Vector2Int.down)?.TerrainInstance;

                // Передаем соседей в Unity Terrain (порядок аргументов: left, top, right, bottom)
                current.SetNeighbors(left, top, right, bottom);
            }
        }


        private void ClearOldChunks()
        {
            foreach (var chunk in _activeChunks)
            {
                if (chunk?.TerrainInstance != null)
                {
                    Destroy(chunk.TerrainInstance.gameObject);
                }
            }
            _activeChunks.Clear();
        }
    }
}
