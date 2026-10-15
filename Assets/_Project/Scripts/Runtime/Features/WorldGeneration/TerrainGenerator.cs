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
            int halfSize = gridSize / 2;

            for (int x = -halfSize; x <= halfSize; x++)
            {
                for (int z = -halfSize; z <= halfSize; z++)
                {
                    Vector2Int chunkCoord = new Vector2Int(x, z);
                    Vector3 spawnPosition = new Vector3(x * _chunkSize, 0, z * _chunkSize);

                    

                    Terrain terrainInstance = Instantiate(
                        _chunkPrefab,
                        spawnPosition,
                        Quaternion.identity,
                        transform);

                    // ?? ИСПРАВЛЕНИЕ ДЛЯ URP: Создаем TerrainData правильным образом
                    TerrainData tData = Instantiate(_terrainDataTemplate);

                    // Сначала задаем разрешения
                    tData.heightmapResolution = _mapResolution;
                    tData.size = new Vector3(_chunkSize, _chunkSize, _chunkSize);

                    // Назначаем наш URP Terrain Layer
                    if (_terrainLayer != null)
                    {
                        tData.terrainLayers = new TerrainLayer[] { _terrainLayer };
                    }

                    

                    // Привязываем данные к террейну до генерации высот
                    terrainInstance.terrainData = tData;

                    // ?? КРИТИЧЕСКИЙ ХАК ДЛЯ URP В РАНТАЙМЕ: 
                    // Принудительно заставляем террейн пересоздать свои скрытые материалы под URP,
                    // иначе он останется фиолетовым
                    terrainInstance.Flush();

                    // Вычисляем правильное смещение чанка (chunkOffset вместо глобального _offset!)
                    Vector2 chunkOffset = _offset + new Vector2(x * (_mapResolution - 1), z * (_mapResolution - 1));

                    // Генерируем карту высот с ПРАВИЛЬНЫМ chunkOffset
                    float[,] heights = _noiseGenerator.GenerateHeightMap(
                        _mapResolution,
                        _mapResolution,
                        _scale,
                        _octaves,
                        _persistence,
                        _lacunarity,
                        chunkOffset); // Передаем chunkOffset, чтобы чанки были РАЗНЫМИ

                    tData.SetHeights(0, 0, heights);
                    terrainInstance.materialTemplate = _terrainMaterial;
                    // Еще раз обновляем рендер после применения высот
                    terrainInstance.Flush();

                    Chunk newChunk = new Chunk(chunkCoord, terrainInstance);
                    _activeChunks.Add(newChunk);
                }
            }

            await UniTask.Yield();
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
