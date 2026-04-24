using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;

[ExecuteAlways]
[DefaultExecutionOrder(-250)]
public class ProceduralWorldGenerator : MonoBehaviour, IGameSaveParticipant
{
    private enum BiomeType
    {
        Forest,
        Marsh,
        RockyHighlands,
        Barren
    }

    private enum SurfaceSemantic
    {
        Unknown,
        Soil,
        Rock,
        Wet,
        Dry,
        Foliage
    }

    private struct TerrainLayerSemantic
    {
        public SurfaceSemantic semantic;
        public float variation;
    }

    private struct PrototypeSemantic
    {
        public SurfaceSemantic semantic;
        public string loweredName;
    }

    [Serializable]
    private sealed class WorldScaleSettings
    {
        public bool enableLargeWorldScaling = true;
        [Range(0.5f, 8f)] public float terrainSizeMultiplierXZ = 4f;
        [Range(0.5f, 4f)] public float terrainHeightMultiplier = 1f;
    }

    [Serializable]
    private sealed class TerrainShapeSettings
    {
        [Range(0f, 1f)] public float seaLevel01 = 0.31f;
        public float continentFrequency = 0.00125f;
        public int continentOctaves = 4;
        public float moistureFrequency = 0.0019f;
        public int moistureOctaves = 4;
        public float temperatureFrequency = 0.0015f;
        public int temperatureOctaves = 4;
        public float erosionFrequency = 0.0014f;
        public int erosionOctaves = 4;
        public float hillFrequency = 0.0047f;
        public int hillOctaves = 5;
        public float ridgeFrequency = 0.0075f;
        public int ridgeOctaves = 5;
        public float detailFrequency = 0.025f;
        public int detailOctaves = 3;
        public float warpFrequency = 0.0017f;
        public int warpOctaves = 3;
        public float warpAmplitude = 55f;
        public float oceanThreshold = 0.48f;
        public float oceanBlend = 0.17f;
        public float landAmplitude = 0.22f;
        public float hillAmplitude = 0.17f;
        public float ridgeAmplitude = 0.31f;
        public float detailAmplitude = 0.028f;
        public float mountainGain = 1.25f;
        public float riverFrequency = 0.008f;
        public float riverWidth = 0.065f;
        public float riverDepth = 0.035f;
    }

    [Serializable]
    private sealed class CarvingSettings
    {
        public int hydraulicDropletCount = 7500;
        public int dropletLifetime = 40;
        [Range(0f, 1f)] public float inertia = 0.04f;
        public float capacity = 4f;
        public float minSlope = 0.008f;
        [Range(0f, 1f)] public float depositRate = 0.12f;
        [Range(0f, 1f)] public float erodeRate = 0.22f;
        [Range(0f, 1f)] public float evaporation = 0.035f;
        public float gravity = 3.8f;
        public int thermalRelaxationPasses = 2;
        public float thermalTalus = 0.0048f;
        [Range(0f, 1f)] public float thermalStrength = 0.42f;
    }

    [Serializable]
    private sealed class TextureSettings
    {
        [Range(0f, 1f)] public float rockSlopeStart = 0.42f;
        [Range(0f, 1f)] public float highRockStart = 0.72f;
        [Range(0f, 1f)] public float marshMoistureThreshold = 0.62f;
        [Range(0f, 1f)] public float dryMoistureThreshold = 0.36f;
        [Range(0f, 1f)] public float riverWetBoost = 0.7f;
        [Range(0f, 1f)] public float valleyWetBoost = 0.45f;
        [Range(0f, 1f)] public float ridgeRockBoost = 0.5f;
        [Range(0f, 1f)] public float shoreBand = 0.03f;
    }

    [Serializable]
    private sealed class VegetationSettings
    {
        public bool clearExistingTrees = true;
        public bool clearExistingDetails = true;
        public float baseTreeSpacing = 8.5f;
        [Range(0f, 2f)] public float treeDensityMultiplier = 1f;
        [Range(0f, 2f)] public float detailDensityMultiplier = 1f;
        public int maxDetailDensityPerCell = 10;
        [Range(0f, 1f)] public float maxTreeSlope01 = 0.42f;
        [Range(0f, 1f)] public float maxDetailSlope01 = 0.58f;
        [Range(0f, 1f)] public float maxTreeHeight01 = 0.9f;
        [Range(0f, 1f)] public float treeRiverExclusionThreshold = 0.12f;
        [Range(0f, 1f)] public float detailRiverExclusionThreshold = 0.2f;
        [Range(0f, 1f)] public float treeEdgeSuppression = 0.85f;
        [Range(0f, 1f)] public float detailEdgeSuppression = 0.9f;
        public float treePatchFrequency = 0.0045f;
        [Range(0.2f, 3f)] public float treePatchContrast = 1.3f;
        public float detailPatchFrequency = 0.016f;
        [Range(0.2f, 3f)] public float detailPatchContrast = 1.5f;
    }

    [Serializable]
    private sealed class PlayAreaSettings
    {
        public bool enableCenterFlattening = true;
        [Range(0.05f, 0.5f)] public float centerRadius01 = 0.2f;
        [Range(0.01f, 0.3f)] public float centerBlend01 = 0.16f;
        [Range(0f, 1f)] public float flattenStrength = 0.6f;
        [Range(0f, 1f)] public float targetHeight01 = 0.45f;
        [Range(0f, 1f)] public float preservedNoise = 0.35f;
    }

    [Serializable]
    private sealed class BoundarySettings
    {
        public bool enableEdgeBarriers = true;
        [Range(0.4f, 0.99f)] public float barrierStart01 = 0.8f;
        [Range(0.01f, 0.3f)] public float barrierBlend01 = 0.15f;
        [Range(0f, 1f)] public float edgeHeightBoost = 0.28f;
        public float edgeRidgeFrequency = 0.014f;
        public int edgeRidgeOctaves = 3;
        [Range(0f, 1f)] public float edgeRidgeStrength = 0.12f;
        [Range(0.1f, 1f)] public float finalBoundaryScale = 0.33f;
    }

    [Serializable]
    private sealed class SpawnSettings
    {
        public bool useSafeSpawnSearch = true;
        [Range(0.05f, 0.5f)] public float playerSearchRadius01 = 0.24f;
        [Range(0f, 1f)] public float maxSlope01 = 0.32f;
        [Range(0f, 1f)] public float maxRiverMask = 0.09f;
        [Range(0f, 1f)] public float maxCaveMask = 0.15f;
        [Range(0f, 1f)] public float maxEdgeBarrierMask = 0.2f;
        [Range(0f, 0.2f)] public float minLandHeightAboveSea = 0.03f;
        public int searchAttempts = 96;
        public float monsterMinDistance = 45f;
        public float monsterMaxDistance = 120f;
    }

    [Serializable]
    private sealed class SurfaceLayerBindings
    {
        public bool useExplicitLayerBindings = true;
        public bool strictExplicitLayerBindings = true;
        public TerrainLayer groundLayer;
        public TerrainLayer rockLayer;
        public TerrainLayer wetLayer;
        public TerrainLayer dryLayer;
        public TerrainLayer foliageLayer;
    }

    [Serializable]
    private sealed class GrassPrototypeDefinition
    {
        public GameObject prefab;
        [Range(0.1f, 4f)] public float minWidth = 0.9f;
        [Range(0.1f, 4f)] public float maxWidth = 1.35f;
        [Range(0.1f, 4f)] public float minHeight = 0.9f;
        [Range(0.1f, 4f)] public float maxHeight = 1.35f;
        [Range(0f, 2f)] public float noiseSpread = 0.2f;
    }

    [Serializable]
    private sealed class DetailPrototypeBindings
    {
        public bool configureGrassFromPrefabs = true;
        public bool replaceExistingGrassLikePrototypes = true;
        public List<GrassPrototypeDefinition> grassPrefabs = new List<GrassPrototypeDefinition>();
    }

    [Serializable]
    private sealed class CaveSettings
    {
        public bool enableCaves = true;
        [Range(0f, 1f)] public float cavesChancePerWorld = 0.7f;
        public int minEntrances = 1;
        public int maxEntrances = 4;
        public int attemptsPerEntrance = 28;
        public float minEntranceSeparation = 70f;
        public float entranceRadius = 12f;
        [Range(0.001f, 0.2f)] public float entranceDepth01 = 0.035f;
        public float tunnelLength = 85f;
        public float tunnelRadius = 8f;
        [Range(0f, 1f)] public float maxEntranceSlope01 = 0.36f;
        [Range(0f, 1f)] public float maxEntranceRiverMask = 0.12f;
        [Range(0f, 1f)] public float maxEntranceEdgeMask = 0.25f;
        [Range(0f, 1f)] public float maxEntranceCenterMask = 0.7f;
        public bool spawnEntrancePrefabs = true;
        public bool spawnUndergroundPrefabs = true;
        public List<GameObject> entrancePrefabs = new List<GameObject>();
        public List<GameObject> undergroundPrefabs = new List<GameObject>();
        public float undergroundMinDepth = 14f;
        public float undergroundMaxDepth = 34f;
        public float undergroundHorizontalJitter = 8f;
        public string generatedCaveRootName = "__GeneratedCaves";
    }

    private struct CaveSpawnPoint
    {
        public Vector3 entranceWorld;
        public Vector3 undergroundWorld;
        public Quaternion rotation;
    }

    [Header("References")]
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private TerrainCollider targetTerrainCollider;

    [Header("Seed")]
    [SerializeField] private bool useSaveSeedInPlayMode = true;
    [SerializeField] private bool persistSeedToSaveWhenChanged = true;
    [SerializeField] private int editorSeed = 6419127;

    [Header("Generation Trigger")]
    [SerializeField] private bool generateOnPlayStart = true;
    [SerializeField] private bool autoRegenerateInEditMode;
    [SerializeField] private bool randomizeEditorSeedBeforeGenerate;
    [SerializeField] private bool regenerateNow;
    [SerializeField] private bool rebuildNavMeshAfterGeneration = true;
    [SerializeField] private bool forceNavMeshUsePhysicsColliders = true;
    [SerializeField] private bool excludeNonReadableMeshesFromNavMeshBuild = true;

    [Header("Runtime Placement")]
    [SerializeField] private bool snapActorsToTerrainAfterGeneration = true;
    [SerializeField] private float playerHeightOffset = 0.2f;
    [SerializeField] private float monsterHeightOffset = 0.2f;
    [SerializeField] private bool snapMonsterToNavMeshAfterBuild = true;
    [SerializeField] private float monsterNavMeshSnapDistance = 20f;

    [Header("Terrain Synthesis")]
    [SerializeField] private TerrainShapeSettings terrainShape = new TerrainShapeSettings();
    [SerializeField] private CarvingSettings carving = new CarvingSettings();
    [SerializeField] private TextureSettings texturePainting = new TextureSettings();
    [SerializeField] private VegetationSettings vegetation = new VegetationSettings();
    [SerializeField] private PlayAreaSettings playArea = new PlayAreaSettings();
    [SerializeField] private BoundarySettings boundaries = new BoundarySettings();
    [SerializeField] private SpawnSettings spawn = new SpawnSettings();
    [SerializeField] private WorldScaleSettings worldScale = new WorldScaleSettings();
    [SerializeField] private SurfaceLayerBindings surfaceLayers = new SurfaceLayerBindings();
    [SerializeField] private DetailPrototypeBindings detailBindings = new DetailPrototypeBindings();
    [SerializeField] private CaveSettings caves = new CaveSettings();

    private int lastGeneratedSeed = int.MinValue;
    private bool hasGeneratedAtLeastOnce;
    private bool isGenerating;
    [SerializeField] private bool hasAuthoringTerrainSize;
    [SerializeField] private Vector3 authoringTerrainSize = Vector3.zero;

    private float[,] generatedHeights;
    private float[,] generatedMoisture;
    private float[,] generatedTemperature;
    private float[,] generatedRiverMask;
    private float[,] generatedCaveMask;
    private float[,] generatedEdgeBarrierMask;
    private BiomeType[,] generatedBiomes;
    private readonly List<CaveSpawnPoint> caveSpawnPlan = new List<CaveSpawnPoint>(8);

    private void Reset()
    {
        AutoAssignTerrain();
    }

    private void OnEnable()
    {
        AutoAssignTerrain();
        EnsureGeneratorSettings();
        if (!CanRunOnCurrentObject())
            return;

        if (Application.isPlaying)
        {
            if (generateOnPlayStart)
                GenerateWorld(randomizeSeed: false, force: false);
        }
        else if (autoRegenerateInEditMode)
        {
            GenerateWorld(randomizeSeed: false, force: false);
        }
    }

    private void OnValidate()
    {
        AutoAssignTerrain();
        EnsureGeneratorSettings();
        if (!CanRunOnCurrentObject() || isGenerating)
            return;

        if (regenerateNow)
        {
            regenerateNow = false;
            GenerateWorld(randomizeSeed: randomizeEditorSeedBeforeGenerate, force: true);
            return;
        }

        if (!Application.isPlaying && autoRegenerateInEditMode)
            GenerateWorld(randomizeSeed: false, force: false);
    }

    [ContextMenu("Generate Terrain")]
    public void GenerateTerrain()
    {
        GenerateWorld(randomizeSeed: false, force: true, forceNavMeshRebuild: true);
    }

    [ContextMenu("Randomize Seed And Generate Terrain")]
    public void RandomizeSeedAndGenerate()
    {
        GenerateWorld(randomizeSeed: true, force: true, forceNavMeshRebuild: true);
    }

    public void GenerateWorld(bool randomizeSeed, bool force)
    {
        GenerateWorld(randomizeSeed, force, forceNavMeshRebuild: false);
    }

    public void GenerateWorld(bool randomizeSeed, bool force, bool forceNavMeshRebuild)
    {
        if (isGenerating)
            return;

        AutoAssignTerrain();
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return;

        if (!CanRunOnCurrentObject())
            return;

        int seed = ResolveSeed(randomizeSeed, out bool seedChanged);
        if (!force && hasGeneratedAtLeastOnce && seed == lastGeneratedSeed)
            return;

        isGenerating = true;
        try
        {
            GenerateFromSeed(seed, forceNavMeshRebuild);
            hasGeneratedAtLeastOnce = true;
            lastGeneratedSeed = seed;
            editorSeed = seed;

            if (Application.isPlaying && useSaveSeedInPlayMode && seedChanged && persistSeedToSaveWhenChanged)
                Game.SaveGameState();
        }
        finally
        {
            isGenerating = false;
        }
    }

    private void GenerateFromSeed(int seed, bool forceNavMeshRebuild)
    {
        TerrainData terrainData = targetTerrain.terrainData;
        EnsureTerrainSizeScaled(terrainData);
        terrainData.RefreshPrototypes();

        BuildHeightAndBiomeMaps(terrainData, seed);
        ApplyHydraulicCarving(seed);
        ApplyThermalRelaxation();
        BuildCavePlanAndCarve(seed, terrainData.size);

        terrainData.SetHeightsDelayLOD(0, 0, generatedHeights);
        terrainData.SyncHeightmap();

        ApplyTexturePainting(terrainData);
        ApplyTrees(terrainData, seed);
        ConfigureDetailPrototypesFromBindings(terrainData);
        ApplyDetails(terrainData, seed);

        targetTerrain.Flush();

        if (targetTerrainCollider != null)
            targetTerrainCollider.terrainData = terrainData;

        Physics.SyncTransforms();
        SpawnGeneratedCaves(seed);
        Physics.SyncTransforms();
        SnapRuntimeActorsToGeneratedSurface(seed);

        bool navMeshRebuilt = false;
        if (rebuildNavMeshAfterGeneration || forceNavMeshRebuild)
            navMeshRebuilt = TryRebuildNavMesh();

        if (navMeshRebuilt && snapMonsterToNavMeshAfterBuild)
            SnapMonsterToNavMesh();

        Physics.SyncTransforms();
    }

    private void BuildHeightAndBiomeMaps(TerrainData terrainData, int seed)
    {
        int resolution = terrainData.heightmapResolution;
        generatedHeights = new float[resolution, resolution];
        generatedMoisture = new float[resolution, resolution];
        generatedTemperature = new float[resolution, resolution];
        generatedRiverMask = new float[resolution, resolution];
        generatedCaveMask = new float[resolution, resolution];
        generatedEdgeBarrierMask = new float[resolution, resolution];
        generatedBiomes = new BiomeType[resolution, resolution];

        Vector3 size = terrainData.size;
        float inv = 1f / (resolution - 1f);

        for (int y = 0; y < resolution; y++)
        {
            float ny = y * inv;
            float worldZ = ny * size.z;

            for (int x = 0; x < resolution; x++)
            {
                float nx = x * inv;
                float worldX = nx * size.x;

                float wx, wz;
                {
                    float warpX = DeterministicNoise.Fbm(seed + 11, worldX * terrainShape.warpFrequency, worldZ * terrainShape.warpFrequency, terrainShape.warpOctaves, 2f, 0.5f);
                    float warpZ = DeterministicNoise.Fbm(seed + 37, worldX * terrainShape.warpFrequency, worldZ * terrainShape.warpFrequency, terrainShape.warpOctaves, 2f, 0.5f);
                    wx = worldX + warpX * terrainShape.warpAmplitude;
                    wz = worldZ + warpZ * terrainShape.warpAmplitude;
                }

                float continental = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 101, wx * terrainShape.continentFrequency, wz * terrainShape.continentFrequency, terrainShape.continentOctaves, 2f, 0.5f);
                float moisture = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 173, wx * terrainShape.moistureFrequency, wz * terrainShape.moistureFrequency, terrainShape.moistureOctaves, 2f, 0.5f);
                float temperature = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 251, wx * terrainShape.temperatureFrequency, wz * terrainShape.temperatureFrequency, terrainShape.temperatureOctaves, 2f, 0.5f);
                float erosion = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 313, wx * terrainShape.erosionFrequency, wz * terrainShape.erosionFrequency, terrainShape.erosionOctaves, 2f, 0.5f);
                float hills = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 419, wx * terrainShape.hillFrequency, wz * terrainShape.hillFrequency, terrainShape.hillOctaves, 2f, 0.5f);
                float ridges = DeterministicNoise.RidgedFbm(seed + 557, wx * terrainShape.ridgeFrequency, wz * terrainShape.ridgeFrequency, terrainShape.ridgeOctaves, 2f, 0.55f);
                float detail = DeterministicNoise.Fbm(seed + 659, wx * terrainShape.detailFrequency, wz * terrainShape.detailFrequency, terrainShape.detailOctaves, 2f, 0.5f);

                float landMask = Smooth01((continental - terrainShape.oceanThreshold) / Mathf.Max(0.001f, terrainShape.oceanBlend));
                float mountainMask = Mathf.Clamp01((1f - erosion) * terrainShape.mountainGain * landMask);
                float elevation = terrainShape.seaLevel01
                    + landMask * terrainShape.landAmplitude
                    + hills * terrainShape.hillAmplitude
                    + ridges * mountainMask * terrainShape.ridgeAmplitude
                    + detail * terrainShape.detailAmplitude * (0.35f + mountainMask);

                float riverNoise = Mathf.Abs(DeterministicNoise.Noise2D(seed + 787, wx * terrainShape.riverFrequency, wz * terrainShape.riverFrequency));
                float riverMask = Mathf.Clamp01((terrainShape.riverWidth - riverNoise) / Mathf.Max(0.0001f, terrainShape.riverWidth));
                riverMask = riverMask * riverMask * (3f - 2f * riverMask);
                float riverCarve = riverMask * terrainShape.riverDepth * (0.3f + moisture * 0.7f);
                elevation -= riverCarve;

                float centerMask = EvaluateCenterPlayAreaMask(nx, ny);
                if (playArea.enableCenterFlattening && centerMask > 0f)
                {
                    float centerTarget = Mathf.Clamp01(playArea.targetHeight01 + (hills - 0.5f) * terrainShape.hillAmplitude * playArea.preservedNoise);
                    float flattenT = centerMask * playArea.flattenStrength;
                    elevation = Mathf.Lerp(elevation, centerTarget, flattenT);
                }

                float edgeBarrierMask = EvaluateEdgeBarrierMask(nx, ny);
                if (boundaries.enableEdgeBarriers && edgeBarrierMask > 0f)
                {
                    float edgeRidged = DeterministicNoise.RidgedFbm(seed + 991, wx * boundaries.edgeRidgeFrequency, wz * boundaries.edgeRidgeFrequency, boundaries.edgeRidgeOctaves, 2f, 0.55f);
                    float edgeBoost = (boundaries.edgeHeightBoost + edgeRidged * boundaries.edgeRidgeStrength) * boundaries.finalBoundaryScale;
                    elevation += edgeBarrierMask * edgeBoost;
                }

                elevation = Mathf.Clamp01(elevation);
                moisture = Mathf.Clamp01(moisture + riverMask * 0.3f);
                temperature = Mathf.Clamp01(temperature - (elevation - terrainShape.seaLevel01) * 0.6f);

                generatedHeights[y, x] = elevation;
                generatedMoisture[y, x] = moisture;
                generatedTemperature[y, x] = temperature;
                generatedRiverMask[y, x] = riverMask;
                generatedEdgeBarrierMask[y, x] = edgeBarrierMask;
                generatedBiomes[y, x] = ResolveBiome(elevation, moisture, temperature, erosion);
            }
        }
    }

    private void ApplyHydraulicCarving(int seed)
    {
        int mapSize = generatedHeights.GetLength(0);
        if (mapSize < 4 || carving.hydraulicDropletCount <= 0)
            return;

        var rng = new DeterministicRandom(seed ^ 0x6A09E667);
        for (int i = 0; i < carving.hydraulicDropletCount; i++)
        {
            float posX = rng.Range(1f, mapSize - 2f);
            float posY = rng.Range(1f, mapSize - 2f);
            float dirX = 0f;
            float dirY = 0f;
            float speed = 1f;
            float water = 1f;
            float sediment = 0f;

            for (int step = 0; step < carving.dropletLifetime; step++)
            {
                int cellX = (int)posX;
                int cellY = (int)posY;
                float offsetX = posX - cellX;
                float offsetY = posY - cellY;

                SampleHeightAndGradient(posX, posY, out float height, out float gradX, out float gradY);
                dirX = dirX * carving.inertia - gradX * (1f - carving.inertia);
                dirY = dirY * carving.inertia - gradY * (1f - carving.inertia);

                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len < 0.0001f)
                {
                    float angle = rng.Range(0f, Mathf.PI * 2f);
                    dirX = Mathf.Cos(angle);
                    dirY = Mathf.Sin(angle);
                }
                else
                {
                    dirX /= len;
                    dirY /= len;
                }

                posX += dirX;
                posY += dirY;
                if (posX < 1f || posX >= mapSize - 2f || posY < 1f || posY >= mapSize - 2f)
                    break;

                float newHeight = SampleHeight(posX, posY);
                float deltaHeight = newHeight - height;
                float capacity = Mathf.Max(-deltaHeight, carving.minSlope) * speed * water * carving.capacity;

                if (sediment > capacity || deltaHeight > 0f)
                {
                    float deposit = deltaHeight > 0f
                        ? Mathf.Min(deltaHeight, sediment)
                        : (sediment - capacity) * carving.depositRate;

                    sediment -= deposit;
                    AddHeightBilinear(cellX, cellY, offsetX, offsetY, deposit);
                }
                else
                {
                    float erode = Mathf.Min((capacity - sediment) * carving.erodeRate, -deltaHeight);
                    sediment += erode;
                    RemoveHeightBilinear(cellX, cellY, offsetX, offsetY, erode);
                }

                speed = Mathf.Sqrt(Mathf.Max(0f, speed * speed + deltaHeight * carving.gravity));
                water *= (1f - carving.evaporation);
                if (water < 0.01f)
                    break;
            }
        }

        ClampHeightMap();
    }

    private void ApplyThermalRelaxation()
    {
        int mapSize = generatedHeights.GetLength(0);
        if (mapSize < 4 || carving.thermalRelaxationPasses <= 0)
            return;

        for (int pass = 0; pass < carving.thermalRelaxationPasses; pass++)
        {
            float[,] scratch = (float[,])generatedHeights.Clone();
            for (int y = 1; y < mapSize - 1; y++)
            {
                for (int x = 1; x < mapSize - 1; x++)
                {
                    float h = generatedHeights[y, x];
                    float n = generatedHeights[y - 1, x];
                    float s = generatedHeights[y + 1, x];
                    float w = generatedHeights[y, x - 1];
                    float e = generatedHeights[y, x + 1];
                    float avg = (n + s + w + e) * 0.25f;
                    float delta = h - avg;

                    if (Mathf.Abs(delta) <= carving.thermalTalus)
                        continue;

                    scratch[y, x] -= delta * carving.thermalStrength;
                }
            }

            generatedHeights = scratch;
        }

        ClampHeightMap();
    }

    private void ApplyTexturePainting(TerrainData terrainData)
    {
        int layers = terrainData.alphamapLayers;
        if (layers <= 0)
            return;

        TerrainLayerSemantic[] layerSemantics = BuildTerrainLayerSemantics(terrainData.terrainLayers, layers);
        int fallbackLayerIndex = ResolveTextureFallbackLayer(terrainData.terrainLayers);
        bool strictLayerBindings = surfaceLayers.useExplicitLayerBindings
            && surfaceLayers.strictExplicitLayerBindings
            && HasAnyExplicitLayerBinding(terrainData.terrainLayers);
        int aw = terrainData.alphamapWidth;
        int ah = terrainData.alphamapHeight;
        float[,,] alpha = new float[aw, ah, layers];
        float[] weights = new float[layers];

        for (int y = 0; y < ah; y++)
        {
            float ny = y / (float)(ah - 1);
            for (int x = 0; x < aw; x++)
            {
                float nx = x / (float)(aw - 1);
                float height01 = SampleMap(generatedHeights, nx, ny);
                float moisture = SampleMap(generatedMoisture, nx, ny);
                float slope01 = EstimateSlope01(nx, ny, terrainData.size);
                float river = SampleMap(generatedRiverMask, nx, ny);
                float cave = SampleMap(generatedCaveMask, nx, ny);
                float edgeBarrier = SampleMap(generatedEdgeBarrierMask, nx, ny);
                float curvature = EstimateCurvature01(nx, ny);
                BiomeType biome = SampleBiome(nx, ny);

                float rock = Mathf.Clamp01((slope01 - texturePainting.rockSlopeStart) / Mathf.Max(0.001f, 1f - texturePainting.rockSlopeStart));
                rock = Mathf.Max(rock, Mathf.Clamp01((height01 - texturePainting.highRockStart) / Mathf.Max(0.001f, 1f - texturePainting.highRockStart)));
                float ridgeRock = Mathf.Clamp01(curvature) * texturePainting.ridgeRockBoost;
                rock = Mathf.Clamp01(Mathf.Max(rock, ridgeRock + edgeBarrier * 0.65f + cave * 0.7f));

                float wet = 0f;
                if (biome == BiomeType.Marsh)
                    wet = 0.85f;
                wet = Mathf.Max(wet, Mathf.Clamp01((moisture - texturePainting.marshMoistureThreshold) / Mathf.Max(0.001f, 1f - texturePainting.marshMoistureThreshold)) * (1f - slope01));
                float valleyWet = Mathf.Clamp01(-curvature) * texturePainting.valleyWetBoost;
                float shore = 1f - Mathf.Clamp01(Mathf.Abs(height01 - terrainShape.seaLevel01) / Mathf.Max(0.0001f, texturePainting.shoreBand));
                wet = Mathf.Clamp01(wet + river * texturePainting.riverWetBoost + valleyWet + shore * 0.3f + cave * 0.18f);

                float dry = 0f;
                if (biome == BiomeType.Barren)
                    dry = 0.75f;
                dry = Mathf.Max(dry, Mathf.Clamp01((texturePainting.dryMoistureThreshold - moisture) / Mathf.Max(0.001f, texturePainting.dryMoistureThreshold)));
                dry = Mathf.Clamp01(dry + edgeBarrier * 0.35f);

                float soil = Mathf.Clamp01(1f - rock);
                soil *= 1f - (dry * 0.35f);
                soil *= 1f - (river * 0.25f);
                soil *= 1f - (cave * 0.4f);
                float foliage = Mathf.Clamp01(soil * (1f - slope01) * (0.45f + moisture * 0.55f));
                foliage *= 1f - Mathf.Clamp01(edgeBarrier * 0.6f);
                foliage *= 1f - Mathf.Clamp01(cave * 0.95f);
                foliage *= biome == BiomeType.Barren ? 0.35f : 1f;

                Array.Clear(weights, 0, weights.Length);
                if (layers == 1)
                {
                    weights[0] = 1f;
                }
                else
                {
                    for (int i = 0; i < layers; i++)
                    {
                        TerrainLayerSemantic info = layerSemantics[i];
                        float score = info.semantic switch
                        {
                            SurfaceSemantic.Soil => soil,
                            SurfaceSemantic.Rock => rock,
                            SurfaceSemantic.Wet => wet,
                            SurfaceSemantic.Dry => dry,
                            SurfaceSemantic.Foliage => foliage,
                            _ => (soil * 0.52f) + (rock * 0.23f) + (wet * 0.18f) + (dry * 0.07f)
                        };

                        if (strictLayerBindings && info.semantic == SurfaceSemantic.Unknown)
                            score = 0f;

                        float localVariation = 0.92f + info.variation * (0.16f * DeterministicNoise.Hash01(9137 + i * 131, x, y));
                        weights[i] = Mathf.Max(0f, score * localVariation);
                    }
                }

                Normalize(weights, fallbackLayerIndex);
                for (int i = 0; i < layers; i++)
                    alpha[x, y, i] = weights[i];
            }
        }

        terrainData.SetAlphamaps(0, 0, alpha);
    }

    private void ApplyTrees(TerrainData terrainData, int seed)
    {
        TreePrototype[] prototypes = terrainData.treePrototypes;
        if (prototypes == null || prototypes.Length == 0)
            return;
        PrototypeSemantic[] prototypeSemantics = BuildTreePrototypeSemantics(prototypes);

        if (vegetation.clearExistingTrees)
            terrainData.SetTreeInstances(Array.Empty<TreeInstance>(), snapToHeightmap: true);

        var rng = new DeterministicRandom(seed ^ unchecked((int)0xBB67AE85u));
        var placedWorldPoints = new Dictionary<long, List<Vector2>>();
        var treeInstances = new List<TreeInstance>(2048);

        Vector3 size = terrainData.size;
        float spacing = Mathf.Max(2f, vegetation.baseTreeSpacing);
        int cellsX = Mathf.CeilToInt(size.x / spacing);
        int cellsZ = Mathf.CeilToInt(size.z / spacing);
        float minDist = spacing * 0.62f;
        float hashCellSize = minDist;

        for (int cz = 0; cz < cellsZ; cz++)
        {
            for (int cx = 0; cx < cellsX; cx++)
            {
                float worldX = (cx + rng.Value01()) * spacing;
                float worldZ = (cz + rng.Value01()) * spacing;
                if (worldX >= size.x || worldZ >= size.z)
                    continue;

                float nx = worldX / size.x;
                float nz = worldZ / size.z;
                float height01 = SampleMap(generatedHeights, nx, nz);
                float slope01 = EstimateSlope01(nx, nz, size);
                float moisture = SampleMap(generatedMoisture, nx, nz);
                float riverMask = SampleMap(generatedRiverMask, nx, nz);
                float caveMask = SampleMap(generatedCaveMask, nx, nz);
                float edgeBarrier = SampleMap(generatedEdgeBarrierMask, nx, nz);
                BiomeType biome = SampleBiome(nx, nz);

                if (slope01 > vegetation.maxTreeSlope01 || height01 > vegetation.maxTreeHeight01)
                    continue;

                if (riverMask >= vegetation.treeRiverExclusionThreshold)
                    continue;
                if (caveMask > 0.12f)
                    continue;

                float density = ResolveTreeDensity(biome, moisture, slope01, height01) * vegetation.treeDensityMultiplier;
                float patch = EvaluateVegetationPatch(seed + 7001, worldX, worldZ, vegetation.treePatchFrequency, vegetation.treePatchContrast);
                density *= patch;
                density *= 1f - Mathf.Clamp01(edgeBarrier * vegetation.treeEdgeSuppression);
                density *= 1f - Mathf.Clamp01(riverMask * 0.75f);
                density *= 1f - Mathf.Clamp01(caveMask * 1.25f);
                if (rng.Value01() > density)
                    continue;

                Vector2 point = new Vector2(worldX, worldZ);
                if (!IsFarEnough(point, minDist, hashCellSize, placedWorldPoints))
                    continue;

                int prototype = SelectTreePrototype(prototypes, prototypeSemantics, biome, moisture, slope01, height01, riverMask, rng);
                if (prototype < 0)
                    continue;

                RegisterPoint(point, hashCellSize, placedWorldPoints);

                treeInstances.Add(new TreeInstance
                {
                    position = new Vector3(nx, height01, nz),
                    prototypeIndex = prototype,
                    widthScale = rng.Range(0.85f, 1.3f),
                    heightScale = rng.Range(0.85f, 1.45f),
                    color = Color.Lerp(new Color(0.85f, 0.9f, 0.85f), Color.white, rng.Value01()),
                    lightmapColor = Color.white
                });
            }
        }

        terrainData.SetTreeInstances(treeInstances.ToArray(), snapToHeightmap: true);
    }

    private void ApplyDetails(TerrainData terrainData, int seed)
    {
        DetailPrototype[] details = terrainData.detailPrototypes;
        if (details == null || details.Length == 0)
            return;
        PrototypeSemantic[] detailSemantics = BuildDetailPrototypeSemantics(details);

        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;
        int maxDensity = Mathf.Max(1, vegetation.maxDetailDensityPerCell);
        Vector3 size = terrainData.size;

        for (int layer = 0; layer < details.Length; layer++)
        {
            int[,] map = new int[detailWidth, detailHeight];
            if (!vegetation.clearExistingDetails)
                map = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, layer);

            for (int y = 0; y < detailHeight; y++)
            {
                float ny = y / (float)(detailHeight - 1);
                for (int x = 0; x < detailWidth; x++)
                {
                    float nx = x / (float)(detailWidth - 1);
                    float moisture = SampleMap(generatedMoisture, nx, ny);
                    float slope = EstimateSlope01(nx, ny, terrainData.size);
                    float river = SampleMap(generatedRiverMask, nx, ny);
                    float cave = SampleMap(generatedCaveMask, nx, ny);
                    float edgeBarrier = SampleMap(generatedEdgeBarrierMask, nx, ny);
                    BiomeType biome = SampleBiome(nx, ny);

                    float biomeDensity = ResolveDetailDensity(biome, moisture, slope, layer, detailSemantics[layer]) * vegetation.detailDensityMultiplier;
                    if (slope > vegetation.maxDetailSlope01 || river >= vegetation.detailRiverExclusionThreshold || cave > 0.2f)
                    {
                        map[x, y] = 0;
                        continue;
                    }

                    float worldX = nx * size.x;
                    float worldZ = ny * size.z;
                    float patch = EvaluateVegetationPatch(seed + 8100 + layer * 47, worldX, worldZ, vegetation.detailPatchFrequency, vegetation.detailPatchContrast);
                    float microPatch = EvaluateVegetationPatch(seed + 9200 + layer * 71, worldX, worldZ, vegetation.detailPatchFrequency * 2.3f, 0.9f);
                    float clump = Mathf.Clamp01(patch * (0.65f + 0.35f * microPatch));

                    float jitter = DeterministicNoise.Hash01(seed + layer * 997, x, y);
                    float modulation = 0.5f + 0.5f * DeterministicNoise.Hash01(seed + 4000 + layer * 177, x, y);
                    float value = biomeDensity * clump;
                    value *= 1f - Mathf.Clamp01(edgeBarrier * vegetation.detailEdgeSuppression);
                    value *= 1f - Mathf.Clamp01(river * 0.8f);
                    value *= 1f - Mathf.Clamp01(cave * 1.2f);
                    value = Mathf.Clamp01((value - jitter * 0.65f) * 2.1f) * modulation;
                    map[x, y] = Mathf.RoundToInt(value * maxDensity);
                }
            }

            terrainData.SetDetailLayer(0, 0, layer, map);
        }
    }

    private void ConfigureDetailPrototypesFromBindings(TerrainData terrainData)
    {
        if (terrainData == null || !detailBindings.configureGrassFromPrefabs || detailBindings.grassPrefabs == null || detailBindings.grassPrefabs.Count == 0)
            return;

        var list = new List<DetailPrototype>(terrainData.detailPrototypes ?? Array.Empty<DetailPrototype>());
        if (detailBindings.replaceExistingGrassLikePrototypes)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (IsGrassLikeDetailPrototype(list[i]))
                    list.RemoveAt(i);
            }
        }

        for (int i = 0; i < detailBindings.grassPrefabs.Count; i++)
        {
            GrassPrototypeDefinition source = detailBindings.grassPrefabs[i];
            if (source == null || source.prefab == null)
                continue;

            bool exists = false;
            for (int p = 0; p < list.Count; p++)
            {
                if (list[p] != null && list[p].prototype == source.prefab)
                {
                    exists = true;
                    break;
                }
            }

            if (exists)
                continue;

            var detail = new DetailPrototype
            {
                usePrototypeMesh = true,
                prototype = source.prefab,
                renderMode = DetailRenderMode.VertexLit,
                minWidth = Mathf.Max(0.05f, source.minWidth),
                maxWidth = Mathf.Max(0.05f, source.maxWidth),
                minHeight = Mathf.Max(0.05f, source.minHeight),
                maxHeight = Mathf.Max(0.05f, source.maxHeight),
                noiseSpread = Mathf.Max(0f, source.noiseSpread),
                healthyColor = Color.white,
                dryColor = new Color(0.88f, 0.88f, 0.88f)
            };

            list.Add(detail);
        }

        terrainData.detailPrototypes = list.ToArray();
        terrainData.RefreshPrototypes();
    }

    private bool IsGrassLikeDetailPrototype(DetailPrototype detail)
    {
        if (!detail.usePrototypeMesh && (detail.renderMode == DetailRenderMode.Grass || detail.renderMode == DetailRenderMode.GrassBillboard))
            return true;

        string lowered = GetDetailPrototypeLabel(detail);
        if (ContainsKeyword(lowered, "grass", "fern", "reed", "shrub", "bush", "plant", "weed", "flower"))
            return true;

        return IsConfiguredGrassPrototype(detail);
    }

    private bool IsConfiguredGrassPrototype(DetailPrototype detail)
    {
        if (detailBindings.grassPrefabs == null || detailBindings.grassPrefabs.Count == 0 || detail.prototype == null)
            return false;

        for (int i = 0; i < detailBindings.grassPrefabs.Count; i++)
        {
            GrassPrototypeDefinition binding = detailBindings.grassPrefabs[i];
            if (binding != null && binding.prefab != null && ReferenceEquals(binding.prefab, detail.prototype))
                return true;
        }

        return false;
    }

    private void BuildCavePlanAndCarve(int seed, Vector3 terrainSize)
    {
        caveSpawnPlan.Clear();
        if (generatedCaveMask != null)
            Array.Clear(generatedCaveMask, 0, generatedCaveMask.Length);

        if (!caves.enableCaves || generatedHeights == null)
            return;

        var rng = new DeterministicRandom(seed ^ unchecked((int)0x4A7C15E6u));
        if (rng.Value01() > caves.cavesChancePerWorld)
            return;

        int minEntrances = Mathf.Max(0, caves.minEntrances);
        int maxEntrances = Mathf.Max(minEntrances, caves.maxEntrances);
        int plannedEntrances = rng.Range(minEntrances, maxEntrances + 1);
        if (plannedEntrances <= 0)
            return;

        int attemptsPerEntrance = Mathf.Max(8, caves.attemptsPerEntrance);
        for (int i = 0; i < plannedEntrances; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < attemptsPerEntrance; attempt++)
            {
                if (!TryPickCaveEntrance(ref rng, terrainSize, out float nx, out float ny, out Vector2 tunnelDir))
                    continue;

                CaveSpawnPoint plan = CarveCaveAndCreateSpawnPoint(seed, i, nx, ny, tunnelDir, terrainSize, ref rng);
                caveSpawnPlan.Add(plan);
                placed = true;
                break;
            }

            if (!placed)
                continue;
        }

        ClampHeightMap();
    }

    private bool TryPickCaveEntrance(ref DeterministicRandom rng, Vector3 terrainSize, out float nx, out float ny, out Vector2 tunnelDir)
    {
        nx = 0.5f;
        ny = 0.5f;
        tunnelDir = Vector2.right;

        float angle = rng.Range(0f, Mathf.PI * 2f);
        float radius01 = Mathf.Lerp(0.25f, 0.93f, Mathf.Sqrt(rng.Value01()));
        nx = 0.5f + Mathf.Cos(angle) * radius01 * 0.5f;
        ny = 0.5f + Mathf.Sin(angle) * radius01 * 0.5f;
        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);

        float height01 = SampleMap(generatedHeights, nx, ny);
        float slope01 = EstimateSlope01(nx, ny, terrainSize);
        float riverMask = SampleMap(generatedRiverMask, nx, ny);
        float caveMask = SampleMap(generatedCaveMask, nx, ny);
        float edgeMask = SampleMap(generatedEdgeBarrierMask, nx, ny);
        float centerMask = EvaluateCenterPlayAreaMask(nx, ny);

        if (height01 < terrainShape.seaLevel01 + 0.04f)
            return false;
        if (slope01 > caves.maxEntranceSlope01)
            return false;
        if (riverMask > caves.maxEntranceRiverMask || caveMask > 0.08f || edgeMask > caves.maxEntranceEdgeMask || centerMask > caves.maxEntranceCenterMask)
            return false;

        Vector2 world = new Vector2(nx * terrainSize.x, ny * terrainSize.z);
        float minSep = Mathf.Max(0f, caves.minEntranceSeparation);
        for (int i = 0; i < caveSpawnPlan.Count; i++)
        {
            CaveSpawnPoint existing = caveSpawnPlan[i];
            Vector2 other = new Vector2(existing.entranceWorld.x, existing.entranceWorld.z) - new Vector2(targetTerrain.transform.position.x, targetTerrain.transform.position.z);
            if ((other - world).sqrMagnitude < minSep * minSep)
                return false;
        }

        Vector2 fromCenter = new Vector2(nx - 0.5f, ny - 0.5f);
        Vector2 outward = fromCenter.sqrMagnitude > 0.0001f ? fromCenter.normalized : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 randomDir = new Vector2(Mathf.Cos(angle + 0.8f), Mathf.Sin(angle + 0.8f));
        tunnelDir = (outward * 0.72f + randomDir * 0.28f).normalized;
        if (tunnelDir.sqrMagnitude < 0.0001f)
            tunnelDir = Vector2.right;

        return true;
    }

    private CaveSpawnPoint CarveCaveAndCreateSpawnPoint(int seed, int caveIndex, float nx, float ny, Vector2 tunnelDir, Vector3 terrainSize, ref DeterministicRandom rng)
    {
        int width = generatedHeights.GetLength(1);
        int height = generatedHeights.GetLength(0);
        float baseScalePx = Mathf.Max(width - 1f, height - 1f) / Mathf.Max(1f, Mathf.Max(terrainSize.x, terrainSize.z));

        float centerX = nx * (width - 1f);
        float centerY = ny * (height - 1f);
        float entranceRadiusPx = Mathf.Max(2f, caves.entranceRadius * baseScalePx);
        float tunnelRadiusPx = Mathf.Max(1.5f, caves.tunnelRadius * baseScalePx);
        float tunnelLengthPx = Mathf.Max(4f, caves.tunnelLength * baseScalePx);
        float entranceDepth = caves.entranceDepth01 * rng.Range(0.85f, 1.2f);

        StampCaveCarve(seed + caveIndex * 301, centerX, centerY, entranceRadiusPx, entranceDepth, 0.95f);

        int steps = Mathf.Max(8, Mathf.CeilToInt(tunnelLengthPx / Mathf.Max(1f, tunnelRadiusPx * 0.6f)));
        Vector2 perp = new Vector2(-tunnelDir.y, tunnelDir.x);
        for (int step = 1; step <= steps; step++)
        {
            float t = step / (float)steps;
            float jitter = (DeterministicNoise.Noise2D(seed + caveIndex * 911, centerX + step * 0.37f, centerY + step * 0.53f) * 0.5f)
                * Mathf.Lerp(0.5f, 2.4f, t);
            float px = centerX + tunnelDir.x * tunnelLengthPx * t + perp.x * jitter;
            float py = centerY + tunnelDir.y * tunnelLengthPx * t + perp.y * jitter;
            float radius = Mathf.Lerp(entranceRadiusPx * 0.78f, tunnelRadiusPx, t);
            float depth = entranceDepth * Mathf.Lerp(0.45f, 1.4f, t);
            StampCaveCarve(seed + caveIndex * 301 + step * 23, px, py, radius, depth, Mathf.Lerp(0.82f, 1f, t));
        }

        ClampHeightMap();

        Vector3 terrainPos = targetTerrain != null ? targetTerrain.transform.position : Vector3.zero;
        float entranceWorldY = terrainPos.y + SampleMap(generatedHeights, nx, ny) * terrainSize.y;
        Vector3 entranceWorld = new Vector3(terrainPos.x + nx * terrainSize.x, entranceWorldY, terrainPos.z + ny * terrainSize.z);

        Vector3 forward = new Vector3(tunnelDir.x, 0f, tunnelDir.y).normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        float undergroundForward = caves.tunnelLength * rng.Range(0.35f, 0.72f) + rng.Range(-caves.undergroundHorizontalJitter, caves.undergroundHorizontalJitter);
        float undergroundDepth = rng.Range(caves.undergroundMinDepth, caves.undergroundMaxDepth);
        Vector3 undergroundWorld = entranceWorld + forward * undergroundForward;
        undergroundWorld.y = entranceWorld.y - Mathf.Max(2f, undergroundDepth);

        return new CaveSpawnPoint
        {
            entranceWorld = entranceWorld,
            undergroundWorld = undergroundWorld,
            rotation = Quaternion.LookRotation(forward, Vector3.up)
        };
    }

    private void StampCaveCarve(int seed, float centerX, float centerY, float radiusPx, float depth01, float maskStrength)
    {
        int width = generatedHeights.GetLength(1);
        int height = generatedHeights.GetLength(0);
        int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusPx - 1f));
        int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(centerX + radiusPx + 1f));
        int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusPx - 1f));
        int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(centerY + radiusPx + 1f));
        float invR = 1f / Mathf.Max(0.0001f, radiusPx);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = (x - centerX) * invR;
                float dy = (y - centerY) * invR;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 1f)
                    continue;

                float falloff = 1f - dist;
                falloff = falloff * falloff * (3f - 2f * falloff);
                float micro = 0.82f + 0.36f * DeterministicNoise.Hash01(seed, x, y);
                float carve = depth01 * falloff * micro;
                generatedHeights[y, x] = Mathf.Clamp01(generatedHeights[y, x] - carve);
                generatedCaveMask[y, x] = Mathf.Max(generatedCaveMask[y, x], Mathf.Clamp01(falloff * Mathf.Max(0f, maskStrength)));
            }
        }
    }

    private void SpawnGeneratedCaves(int seed)
    {
        ClearGeneratedCaveObjects();
        if (!caves.enableCaves || caveSpawnPlan.Count == 0)
            return;

        bool canSpawnEntrances = caves.spawnEntrancePrefabs && ContainsSpawnablePrefabs(caves.entrancePrefabs);
        bool canSpawnUnderground = caves.spawnUndergroundPrefabs && ContainsSpawnablePrefabs(caves.undergroundPrefabs);
        if (!canSpawnEntrances && !canSpawnUnderground)
            return;

        Transform root = GetOrCreateGeneratedCaveRoot();
        var rng = new DeterministicRandom(seed ^ unchecked((int)0x9E3779B9u));

        for (int i = 0; i < caveSpawnPlan.Count; i++)
        {
            CaveSpawnPoint cave = caveSpawnPlan[i];
            if (canSpawnEntrances)
            {
                GameObject entrancePrefab = PickDeterministicPrefab(caves.entrancePrefabs, ref rng);
                if (entrancePrefab != null)
                    Instantiate(entrancePrefab, cave.entranceWorld, cave.rotation, root);
            }

            if (canSpawnUnderground)
            {
                GameObject undergroundPrefab = PickDeterministicPrefab(caves.undergroundPrefabs, ref rng);
                if (undergroundPrefab != null)
                    Instantiate(undergroundPrefab, cave.undergroundWorld, cave.rotation, root);
            }
        }
    }

    private void ClearGeneratedCaveObjects()
    {
        Transform root = transform.Find(caves.generatedCaveRootName);
        if (root == null)
            return;

        if (Application.isPlaying)
            Destroy(root.gameObject);
        else
            DestroyImmediate(root.gameObject);
    }

    private Transform GetOrCreateGeneratedCaveRoot()
    {
        Transform existing = transform.Find(caves.generatedCaveRootName);
        if (existing != null)
            return existing;

        var go = new GameObject(caves.generatedCaveRootName);
        go.transform.SetParent(transform, worldPositionStays: false);
        return go.transform;
    }

    private static bool ContainsSpawnablePrefabs(List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0)
            return false;

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (prefabs[i] != null)
                return true;
        }

        return false;
    }

    private static GameObject PickDeterministicPrefab(List<GameObject> prefabs, ref DeterministicRandom rng)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        int start = rng.Range(0, prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject candidate = prefabs[(start + i) % prefabs.Count];
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private BiomeType ResolveBiome(float elevation01, float moisture, float temperature, float erosion)
    {
        float shore = terrainShape.seaLevel01 + 0.018f;
        if (elevation01 <= shore && moisture > 0.58f)
            return BiomeType.Marsh;

        if (elevation01 > terrainShape.seaLevel01 + 0.34f || erosion < 0.3f)
            return BiomeType.RockyHighlands;

        if (moisture < 0.32f || temperature < 0.24f)
            return BiomeType.Barren;

        return BiomeType.Forest;
    }

    private float ResolveTreeDensity(BiomeType biome, float moisture, float slope01, float elevation01)
    {
        if (slope01 > vegetation.maxTreeSlope01 || elevation01 < terrainShape.seaLevel01 + 0.01f || elevation01 > vegetation.maxTreeHeight01)
            return 0f;

        return biome switch
        {
            BiomeType.Forest => Mathf.Lerp(0.35f, 0.95f, moisture),
            BiomeType.Marsh => Mathf.Lerp(0.25f, 0.65f, moisture) * (1f - slope01),
            BiomeType.RockyHighlands => Mathf.Clamp01(0.2f - slope01 * 0.2f),
            BiomeType.Barren => 0.05f,
            _ => 0f
        };
    }

    private int SelectTreePrototype(TreePrototype[] prototypes, PrototypeSemantic[] semantics, BiomeType biome, float moisture, float slope01, float elevation01, float riverMask, DeterministicRandom rng)
    {
        if (prototypes == null || prototypes.Length == 0)
            return -1;

        float total = 0f;
        for (int i = 0; i < prototypes.Length; i++)
        {
            total += EvaluateTreePrototypeWeight(semantics[i], biome, moisture, slope01, elevation01, riverMask);
        }

        if (total <= 0.00001f)
            return -1;

        float pick = rng.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < prototypes.Length; i++)
        {
            cumulative += EvaluateTreePrototypeWeight(semantics[i], biome, moisture, slope01, elevation01, riverMask);
            if (pick <= cumulative)
                return i;
        }

        return prototypes.Length - 1;
    }

    private float EvaluateTreePrototypeWeight(PrototypeSemantic semantic, BiomeType biome, float moisture, float slope01, float elevation01, float riverMask)
    {
        if (slope01 > vegetation.maxTreeSlope01 || elevation01 > vegetation.maxTreeHeight01 || riverMask >= vegetation.treeRiverExclusionThreshold)
            return 0f;

        float baseBiomeWeight = biome switch
        {
            BiomeType.Forest => Mathf.Lerp(0.4f, 1f, moisture),
            BiomeType.Marsh => Mathf.Lerp(0.35f, 0.85f, moisture),
            BiomeType.RockyHighlands => 0.25f,
            BiomeType.Barren => 0.12f,
            _ => 0.3f
        };

        float semanticWeight = semantic.semantic switch
        {
            SurfaceSemantic.Foliage => biome == BiomeType.Forest ? 1f : (biome == BiomeType.Marsh ? 0.85f : 0.42f),
            SurfaceSemantic.Wet => biome == BiomeType.Marsh ? 1f : 0.35f,
            SurfaceSemantic.Dry => biome == BiomeType.Barren ? 1f : 0.3f,
            SurfaceSemantic.Rock => biome == BiomeType.RockyHighlands ? 0.75f : 0.28f,
            SurfaceSemantic.Soil => 0.7f,
            _ => 0.55f
        };

        if (ContainsKeyword(semantic.loweredName, "pine", "fir", "spruce"))
            semanticWeight *= biome == BiomeType.RockyHighlands ? 1.15f : 0.95f;
        else if (ContainsKeyword(semantic.loweredName, "willow", "mangrove", "reed"))
            semanticWeight *= biome == BiomeType.Marsh ? 1.2f : 0.75f;
        else if (ContainsKeyword(semantic.loweredName, "dead", "dry", "cactus"))
            semanticWeight *= biome == BiomeType.Barren ? 1.2f : 0.6f;

        float flatness = 1f - Mathf.Clamp01(slope01 / Mathf.Max(0.001f, vegetation.maxTreeSlope01));
        float riverPenalty = 1f - Mathf.Clamp01(riverMask * 1.2f);
        return baseBiomeWeight * semanticWeight * (0.45f + flatness * 0.55f) * riverPenalty;
    }

    private float ResolveDetailDensity(BiomeType biome, float moisture, float slope, int layer, PrototypeSemantic semantic)
    {
        float layerBias = 0.75f + 0.25f / (layer + 1f);
        float flatness = 1f - Mathf.Clamp01(slope * 1.25f);

        float baseDensity = biome switch
        {
            BiomeType.Forest => Mathf.Lerp(0.5f, 1f, moisture),
            BiomeType.Marsh => Mathf.Lerp(0.42f, 0.95f, moisture),
            BiomeType.RockyHighlands => 0.2f,
            BiomeType.Barren => 0.12f,
            _ => 0.25f
        };

        float semanticBias = semantic.semantic switch
        {
            SurfaceSemantic.Foliage => biome == BiomeType.Forest ? 1f : (biome == BiomeType.Marsh ? 0.9f : 0.55f),
            SurfaceSemantic.Wet => biome == BiomeType.Marsh ? 1f : 0.4f,
            SurfaceSemantic.Dry => biome == BiomeType.Barren ? 0.95f : 0.42f,
            SurfaceSemantic.Rock => biome == BiomeType.RockyHighlands ? 0.65f : 0.28f,
            SurfaceSemantic.Soil => 0.75f,
            _ => 0.62f
        };

        return baseDensity * flatness * layerBias * semanticBias;
    }

    private TerrainLayerSemantic[] BuildTerrainLayerSemantics(TerrainLayer[] layers, int requiredCount)
    {
        int count = Mathf.Max(0, requiredCount);
        var result = new TerrainLayerSemantic[count];

        int explicitGround = GetTerrainLayerIndex(layers, surfaceLayers.groundLayer);
        int explicitRock = GetTerrainLayerIndex(layers, surfaceLayers.rockLayer);
        int explicitWet = GetTerrainLayerIndex(layers, surfaceLayers.wetLayer);
        int explicitDry = GetTerrainLayerIndex(layers, surfaceLayers.dryLayer);
        int explicitFoliage = GetTerrainLayerIndex(layers, surfaceLayers.foliageLayer);
        bool strictExplicit = surfaceLayers.useExplicitLayerBindings
            && surfaceLayers.strictExplicitLayerBindings
            && (explicitGround >= 0 || explicitRock >= 0 || explicitWet >= 0 || explicitDry >= 0 || explicitFoliage >= 0);

        for (int i = 0; i < count; i++)
        {
            string name = i < (layers?.Length ?? 0) ? GetTerrainLayerLabel(layers[i]) : string.Empty;
            SurfaceSemantic semantic = SurfaceSemantic.Unknown;
            if (surfaceLayers.useExplicitLayerBindings)
            {
                if (i == explicitGround) semantic = SurfaceSemantic.Soil;
                else if (i == explicitRock) semantic = SurfaceSemantic.Rock;
                else if (i == explicitWet) semantic = SurfaceSemantic.Wet;
                else if (i == explicitDry) semantic = SurfaceSemantic.Dry;
                else if (i == explicitFoliage) semantic = SurfaceSemantic.Foliage;
            }

            if (semantic == SurfaceSemantic.Unknown && !strictExplicit)
                semantic = ClassifySurfaceSemantic(name);

            float variation = 0.35f + 0.65f * HashString01(name, i * 163);
            result[i] = new TerrainLayerSemantic { semantic = semantic, variation = variation };
        }

        return result;
    }

    private bool HasAnyExplicitLayerBinding(TerrainLayer[] layers)
    {
        return GetTerrainLayerIndex(layers, surfaceLayers.groundLayer) >= 0
            || GetTerrainLayerIndex(layers, surfaceLayers.rockLayer) >= 0
            || GetTerrainLayerIndex(layers, surfaceLayers.wetLayer) >= 0
            || GetTerrainLayerIndex(layers, surfaceLayers.dryLayer) >= 0
            || GetTerrainLayerIndex(layers, surfaceLayers.foliageLayer) >= 0;
    }

    private int ResolveTextureFallbackLayer(TerrainLayer[] layers)
    {
        int idx = GetTerrainLayerIndex(layers, surfaceLayers.groundLayer);
        if (idx >= 0)
            return idx;

        idx = GetTerrainLayerIndex(layers, surfaceLayers.foliageLayer);
        if (idx >= 0)
            return idx;

        idx = GetTerrainLayerIndex(layers, surfaceLayers.rockLayer);
        if (idx >= 0)
            return idx;

        return 0;
    }

    private PrototypeSemantic[] BuildTreePrototypeSemantics(TreePrototype[] prototypes)
    {
        var result = new PrototypeSemantic[prototypes.Length];
        for (int i = 0; i < prototypes.Length; i++)
        {
            string name = GetTreePrototypeLabel(prototypes[i]);
            result[i] = new PrototypeSemantic
            {
                semantic = ClassifySurfaceSemantic(name),
                loweredName = name
            };
        }

        return result;
    }

    private PrototypeSemantic[] BuildDetailPrototypeSemantics(DetailPrototype[] details)
    {
        var result = new PrototypeSemantic[details.Length];
        for (int i = 0; i < details.Length; i++)
        {
            string name = GetDetailPrototypeLabel(details[i]);
            SurfaceSemantic semantic = ClassifySurfaceSemantic(name);
            if (IsConfiguredGrassPrototype(details[i]))
                semantic = SurfaceSemantic.Foliage;

            result[i] = new PrototypeSemantic
            {
                semantic = semantic,
                loweredName = name
            };
        }

        return result;
    }

    private int GetTerrainLayerIndex(TerrainLayer[] layers, TerrainLayer layer)
    {
        if (layers == null || layer == null)
            return -1;

        for (int i = 0; i < layers.Length; i++)
        {
            if (ReferenceEquals(layers[i], layer))
                return i;
        }

        return -1;
    }

    private static string GetTerrainLayerLabel(TerrainLayer layer)
    {
        if (layer == null)
            return string.Empty;

        string name = layer.name ?? string.Empty;
        string textureName = layer.diffuseTexture != null ? layer.diffuseTexture.name : string.Empty;
        return (name + " " + textureName).ToLowerInvariant();
    }

    private static string GetTreePrototypeLabel(TreePrototype prototype)
    {
        if (prototype.prefab != null)
            return prototype.prefab.name.ToLowerInvariant();

        return "tree";
    }

    private static string GetDetailPrototypeLabel(DetailPrototype detail)
    {
        string protoName = detail.prototype != null ? detail.prototype.name : string.Empty;
        string textureName = detail.prototypeTexture != null ? detail.prototypeTexture.name : string.Empty;
        return (protoName + " " + textureName).ToLowerInvariant();
    }

    private static SurfaceSemantic ClassifySurfaceSemantic(string loweredName)
    {
        if (string.IsNullOrWhiteSpace(loweredName))
            return SurfaceSemantic.Unknown;

        if (ContainsKeyword(loweredName, "rock", "cliff", "stone", "gravel", "scree", "boulder", "slate"))
            return SurfaceSemantic.Rock;

        if (ContainsKeyword(loweredName, "mud", "mire", "marsh", "swamp", "wet", "bog", "river", "reed", "moss"))
            return SurfaceSemantic.Wet;

        if (ContainsKeyword(loweredName, "sand", "dry", "dust", "ash", "dead", "desert", "dune"))
            return SurfaceSemantic.Dry;

        if (ContainsKeyword(loweredName, "grass", "fern", "bush", "leaf", "forest", "foliage", "plant", "flower", "weed", "shrub", "tree", "pine", "oak"))
            return SurfaceSemantic.Foliage;

        if (ContainsKeyword(loweredName, "soil", "dirt", "ground", "earth", "loam"))
            return SurfaceSemantic.Soil;

        return SurfaceSemantic.Unknown;
    }

    private static bool ContainsKeyword(string value, params string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (value.Contains(keywords[i]))
                return true;
        }

        return false;
    }

    private static float HashString01(string value, int salt)
    {
        unchecked
        {
            uint h = 2166136261u ^ (uint)salt;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    h ^= value[i];
                    h *= 16777619u;
                }
            }

            return (h & 0x00FFFFFFu) * (1f / 16777216f);
        }
    }

    private BiomeType SampleBiome(float nx, float ny)
    {
        int width = generatedBiomes.GetLength(1);
        int height = generatedBiomes.GetLength(0);
        int x = Mathf.Clamp(Mathf.RoundToInt(nx * (width - 1)), 0, width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(ny * (height - 1)), 0, height - 1);
        return generatedBiomes[y, x];
    }

    private float SampleMap(float[,] map, float nx, float ny)
    {
        int width = map.GetLength(1);
        int height = map.GetLength(0);
        float x = Mathf.Clamp01(nx) * (width - 1);
        float y = Mathf.Clamp01(ny) * (height - 1);
        return SampleMapBilinear(map, x, y);
    }

    private float EstimateSlope01(float nx, float ny, Vector3 terrainSize)
    {
        int width = generatedHeights.GetLength(1);
        int height = generatedHeights.GetLength(0);

        float x = Mathf.Clamp01(nx) * (width - 1);
        float y = Mathf.Clamp01(ny) * (height - 1);

        float hL = SampleMapBilinear(generatedHeights, Mathf.Max(0f, x - 1f), y);
        float hR = SampleMapBilinear(generatedHeights, Mathf.Min(width - 1f, x + 1f), y);
        float hD = SampleMapBilinear(generatedHeights, x, Mathf.Max(0f, y - 1f));
        float hU = SampleMapBilinear(generatedHeights, x, Mathf.Min(height - 1f, y + 1f));

        float dx = (hR - hL) * terrainSize.y;
        float dz = (hU - hD) * terrainSize.y;
        float horizontal = Mathf.Max(terrainSize.x, terrainSize.z) / Mathf.Max(2f, width - 1f);
        float gradient = Mathf.Sqrt(dx * dx + dz * dz) / Mathf.Max(0.0001f, horizontal);
        return Mathf.Clamp01(gradient / 45f);
    }

    private void SampleHeightAndGradient(float x, float y, out float height, out float gradX, out float gradY)
    {
        int width = generatedHeights.GetLength(1);
        int heightCount = generatedHeights.GetLength(0);

        int x0 = Mathf.Clamp((int)x, 0, width - 2);
        int y0 = Mathf.Clamp((int)y, 0, heightCount - 2);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float tx = x - x0;
        float ty = y - y0;

        float h00 = generatedHeights[y0, x0];
        float h10 = generatedHeights[y0, x1];
        float h01 = generatedHeights[y1, x0];
        float h11 = generatedHeights[y1, x1];

        height = h00 * (1f - tx) * (1f - ty)
            + h10 * tx * (1f - ty)
            + h01 * (1f - tx) * ty
            + h11 * tx * ty;

        gradX = (h10 - h00) * (1f - ty) + (h11 - h01) * ty;
        gradY = (h01 - h00) * (1f - tx) + (h11 - h10) * tx;
    }

    private float SampleHeight(float x, float y)
    {
        return SampleMapBilinear(generatedHeights, x, y);
    }

    private static float SampleMapBilinear(float[,] map, float x, float y)
    {
        int width = map.GetLength(1);
        int height = map.GetLength(0);
        int x0 = Mathf.Clamp((int)x, 0, width - 1);
        int y0 = Mathf.Clamp((int)y, 0, height - 1);
        int x1 = Mathf.Min(x0 + 1, width - 1);
        int y1 = Mathf.Min(y0 + 1, height - 1);

        float tx = x - x0;
        float ty = y - y0;
        float a = Mathf.Lerp(map[y0, x0], map[y0, x1], tx);
        float b = Mathf.Lerp(map[y1, x0], map[y1, x1], tx);
        return Mathf.Lerp(a, b, ty);
    }

    private void AddHeightBilinear(int x, int y, float tx, float ty, float amount)
    {
        if (amount <= 0f)
            return;

        generatedHeights[y, x] += amount * (1f - tx) * (1f - ty);
        generatedHeights[y, x + 1] += amount * tx * (1f - ty);
        generatedHeights[y + 1, x] += amount * (1f - tx) * ty;
        generatedHeights[y + 1, x + 1] += amount * tx * ty;
    }

    private void RemoveHeightBilinear(int x, int y, float tx, float ty, float amount)
    {
        if (amount <= 0f)
            return;

        RemoveFromCell(y, x, amount * (1f - tx) * (1f - ty));
        RemoveFromCell(y, x + 1, amount * tx * (1f - ty));
        RemoveFromCell(y + 1, x, amount * (1f - tx) * ty);
        RemoveFromCell(y + 1, x + 1, amount * tx * ty);
    }

    private void RemoveFromCell(int y, int x, float amount)
    {
        generatedHeights[y, x] = Mathf.Max(0f, generatedHeights[y, x] - amount);
    }

    private void ClampHeightMap()
    {
        int width = generatedHeights.GetLength(1);
        int height = generatedHeights.GetLength(0);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                generatedHeights[y, x] = Mathf.Clamp01(generatedHeights[y, x]);
        }
    }

    private static void Normalize(float[] weights, int fallbackIndex = 0)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += Mathf.Max(0f, weights[i]);

        if (total <= 0.00001f)
        {
            if (weights.Length > 0)
            {
                int idx = Mathf.Clamp(fallbackIndex, 0, weights.Length - 1);
                weights[idx] = 1f;
            }
            return;
        }

        for (int i = 0; i < weights.Length; i++)
            weights[i] = Mathf.Max(0f, weights[i]) / total;
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private float EvaluateCenterPlayAreaMask(float nx, float ny)
    {
        float dx = nx - 0.5f;
        float dy = ny - 0.5f;
        float dist01 = Mathf.Sqrt(dx * dx + dy * dy) / 0.70710677f;
        float t = (dist01 - playArea.centerRadius01) / Mathf.Max(0.0001f, playArea.centerBlend01);
        return playArea.enableCenterFlattening ? 1f - Smooth01(t) : 0f;
    }

    private float EvaluateEdgeBarrierMask(float nx, float ny)
    {
        float edge01 = Mathf.Max(Mathf.Abs(nx - 0.5f), Mathf.Abs(ny - 0.5f)) * 2f;
        float t = (edge01 - boundaries.barrierStart01) / Mathf.Max(0.0001f, boundaries.barrierBlend01);
        return boundaries.enableEdgeBarriers ? Smooth01(t) : 0f;
    }

    private float EstimateCurvature01(float nx, float ny)
    {
        int width = generatedHeights.GetLength(1);
        int height = generatedHeights.GetLength(0);

        float x = Mathf.Clamp01(nx) * (width - 1);
        float y = Mathf.Clamp01(ny) * (height - 1);
        float h = SampleMapBilinear(generatedHeights, x, y);
        float n = SampleMapBilinear(generatedHeights, x, Mathf.Max(0f, y - 1f));
        float s = SampleMapBilinear(generatedHeights, x, Mathf.Min(height - 1f, y + 1f));
        float w = SampleMapBilinear(generatedHeights, Mathf.Max(0f, x - 1f), y);
        float e = SampleMapBilinear(generatedHeights, Mathf.Min(width - 1f, x + 1f), y);
        float laplacian = ((n + s + w + e) * 0.25f) - h;
        return Mathf.Clamp(laplacian * 180f, -1f, 1f);
    }

    private static float EvaluateVegetationPatch(int seed, float worldX, float worldZ, float frequency, float contrast)
    {
        float f = Mathf.Max(0.00001f, frequency);
        float broad = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 13, worldX * f, worldZ * f, 4, 2f, 0.5f);
        float fine = 0.5f + 0.5f * DeterministicNoise.Fbm(seed + 29, worldX * f * 2.4f, worldZ * f * 2.4f, 3, 2f, 0.5f);
        float patch = Mathf.Clamp01((broad * 0.75f) + (fine * 0.25f));
        return Mathf.Pow(Mathf.Clamp01(patch), Mathf.Max(0.2f, contrast));
    }

    private bool IsFarEnough(Vector2 point, float minDist, float cellSize, Dictionary<long, List<Vector2>> pointHash)
    {
        int cx = Mathf.FloorToInt(point.x / cellSize);
        int cy = Mathf.FloorToInt(point.y / cellSize);
        float minDistSqr = minDist * minDist;

        for (int ny = -1; ny <= 1; ny++)
        {
            for (int nx = -1; nx <= 1; nx++)
            {
                long key = HashCell(cx + nx, cy + ny);
                if (!pointHash.TryGetValue(key, out List<Vector2> points))
                    continue;

                for (int i = 0; i < points.Count; i++)
                {
                    if ((points[i] - point).sqrMagnitude < minDistSqr)
                        return false;
                }
            }
        }

        return true;
    }

    private void RegisterPoint(Vector2 point, float cellSize, Dictionary<long, List<Vector2>> pointHash)
    {
        int cx = Mathf.FloorToInt(point.x / cellSize);
        int cy = Mathf.FloorToInt(point.y / cellSize);
        long key = HashCell(cx, cy);
        if (!pointHash.TryGetValue(key, out List<Vector2> points))
        {
            points = new List<Vector2>(4);
            pointHash.Add(key, points);
        }

        points.Add(point);
    }

    private static long HashCell(int x, int y)
    {
        return ((long)x << 32) ^ (uint)y;
    }

    private int ResolveSeed(bool randomizeSeed, out bool seedChanged)
    {
        seedChanged = false;

        if (Application.isPlaying && useSaveSeedInPlayMode)
        {
            int seed;
            if (randomizeSeed)
            {
                seed = CreateNonZeroSeed();
                Game.SetWorldSeed(seed, persist: false);
                seedChanged = true;
            }
            else
            {
                seed = Game.EnsureWorldSeed(persistIfNew: false);
            }

            if (Game.State != null)
            {
                Game.State.EnsureInitialized();
                Game.State.World.Seed = seed;
                Game.State.World.HasSeed = true;
            }

            return seed;
        }

        if (randomizeSeed)
        {
            editorSeed = CreateNonZeroSeed();
            seedChanged = true;
        }
        else if (editorSeed == 0)
        {
            editorSeed = 1;
            seedChanged = true;
        }

        return editorSeed;
    }

    private static int CreateNonZeroSeed()
    {
        int seed = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        return seed == 0 ? 1 : seed;
    }

    private void EnsureGeneratorSettings()
    {
        if (boundaries.finalBoundaryScale <= 0f)
            boundaries.finalBoundaryScale = 0.33f;
        else
            boundaries.finalBoundaryScale = Mathf.Min(boundaries.finalBoundaryScale, 0.33f);

        if (vegetation.treePatchFrequency <= 0f)
            vegetation.treePatchFrequency = 0.0045f;

        if (vegetation.detailPatchFrequency <= 0f)
            vegetation.detailPatchFrequency = 0.016f;

        if (worldScale.terrainSizeMultiplierXZ <= 0f)
            worldScale.terrainSizeMultiplierXZ = 4f;

        if (worldScale.enableLargeWorldScaling && worldScale.terrainSizeMultiplierXZ < 4f)
            worldScale.terrainSizeMultiplierXZ = 4f;

        if (worldScale.terrainHeightMultiplier <= 0f)
            worldScale.terrainHeightMultiplier = 1f;

        if (spawn.maxCaveMask <= 0f)
            spawn.maxCaveMask = 0.15f;

        if (caves.maxEntrances < caves.minEntrances)
            caves.maxEntrances = caves.minEntrances;
        if (caves.attemptsPerEntrance < 1)
            caves.attemptsPerEntrance = 1;
        if (caves.tunnelLength <= 0f)
            caves.tunnelLength = 85f;
        if (caves.tunnelRadius <= 0f)
            caves.tunnelRadius = 8f;
        if (string.IsNullOrWhiteSpace(caves.generatedCaveRootName))
            caves.generatedCaveRootName = "__GeneratedCaves";
    }

    private void EnsureTerrainSizeScaled(TerrainData terrainData)
    {
        if (terrainData == null)
            return;

        if (!hasAuthoringTerrainSize || authoringTerrainSize.x <= 0f || authoringTerrainSize.z <= 0f)
        {
            authoringTerrainSize = terrainData.size;
            hasAuthoringTerrainSize = true;
        }

        Vector3 targetSize = authoringTerrainSize;
        if (worldScale.enableLargeWorldScaling)
        {
            float xz = Mathf.Max(0.1f, worldScale.terrainSizeMultiplierXZ);
            targetSize.x = authoringTerrainSize.x * xz;
            targetSize.z = authoringTerrainSize.z * xz;
            targetSize.y = authoringTerrainSize.y * Mathf.Max(0.1f, worldScale.terrainHeightMultiplier);
        }

        if ((terrainData.size - targetSize).sqrMagnitude > 0.0001f)
            terrainData.size = targetSize;
    }

    private void AutoAssignTerrain()
    {
        if (targetTerrain == null)
            targetTerrain = GetComponent<Terrain>() ?? FindFirstObjectByType<Terrain>();

        if (targetTerrainCollider == null && targetTerrain != null)
            targetTerrainCollider = targetTerrain.GetComponent<TerrainCollider>();
    }

    private bool CanRunOnCurrentObject()
    {
        if (!isActiveAndEnabled)
            return false;

        if (Application.isPlaying)
            return true;

        return gameObject.scene.IsValid();
    }

    private bool TryRebuildNavMesh()
    {
        Type navSurfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (navSurfaceType == null)
            return false;

        UnityEngine.Object[] found = FindObjectsByType(navSurfaceType, FindObjectsSortMode.None);
        if (found == null || found.Length == 0)
            return false;

        int rebuiltCount = 0;
        PropertyInfo useGeometryProperty = navSurfaceType.GetProperty("useGeometry", BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo navMeshDataProperty = navSurfaceType.GetProperty("navMeshData", BindingFlags.Instance | BindingFlags.Public);
        MethodInfo buildMethod = navSurfaceType.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo collectSourcesMethod = navSurfaceType.GetMethod("CollectSources", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo calculateWorldBoundsMethod = navSurfaceType.GetMethod("CalculateWorldBounds", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getBuildSettingsMethod = navSurfaceType.GetMethod("GetBuildSettings", BindingFlags.Instance | BindingFlags.Public);
        MethodInfo removeDataMethod = navSurfaceType.GetMethod("RemoveData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo addDataMethod = navSurfaceType.GetMethod("AddData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (buildMethod == null)
            return false;

        for (int i = 0; i < found.Length; i++)
        {
            if (!(found[i] is Component surface) || surface == null)
                continue;

            if (useGeometryProperty != null && useGeometryProperty.CanWrite && (forceNavMeshUsePhysicsColliders || Application.isPlaying))
            {
                Type geometryEnum = useGeometryProperty.PropertyType;
                if (geometryEnum != null && geometryEnum.IsEnum)
                {
                    try
                    {
                        object physicsColliders = Enum.Parse(geometryEnum, "PhysicsColliders", ignoreCase: false);
                        useGeometryProperty.SetValue(surface, physicsColliders);
                    }
                    catch
                    {
                        // If the package API changes, fall back to the existing surface setting.
                    }
                }
            }

            bool rebuiltWithFilteredSources = false;
            if (excludeNonReadableMeshesFromNavMeshBuild
                && collectSourcesMethod != null
                && calculateWorldBoundsMethod != null
                && getBuildSettingsMethod != null
                && navMeshDataProperty != null
                && removeDataMethod != null
                && addDataMethod != null)
            {
                try
                {
                    object sourcesObject = collectSourcesMethod.Invoke(surface, null);
                    if (sourcesObject is List<NavMeshBuildSource> sources)
                    {
                        for (int s = sources.Count - 1; s >= 0; s--)
                        {
                            NavMeshBuildSource source = sources[s];
                            if (source.shape == NavMeshBuildSourceShape.Mesh
                                && source.sourceObject is Mesh mesh
                                && !mesh.isReadable)
                            {
                                sources.RemoveAt(s);
                            }
                        }

                        object boundsObject = calculateWorldBoundsMethod.Invoke(surface, new object[] { sources });
                        object settingsObject = getBuildSettingsMethod.Invoke(surface, null);
                        if (boundsObject is Bounds worldBounds && settingsObject is NavMeshBuildSettings buildSettings)
                        {
                            NavMeshData data = NavMeshBuilder.BuildNavMeshData(buildSettings, sources, worldBounds, surface.transform.position, surface.transform.rotation);
                            if (data != null)
                            {
                                data.name = surface.gameObject.name;
                                removeDataMethod.Invoke(surface, null);
                                navMeshDataProperty.SetValue(surface, data);
                                if (surface is Behaviour behaviour && behaviour.isActiveAndEnabled)
                                    addDataMethod.Invoke(surface, null);

                                rebuiltWithFilteredSources = true;
                            }
                        }
                    }
                }
                catch
                {
                    rebuiltWithFilteredSources = false;
                }
            }

            if (rebuiltWithFilteredSources)
            {
                rebuiltCount++;
                continue;
            }

            buildMethod.Invoke(surface, null);
            rebuiltCount++;
        }

        return rebuiltCount > 0;
    }

    private void SnapRuntimeActorsToGeneratedSurface(int seed)
    {
        if (!Application.isPlaying || !snapActorsToTerrainAfterGeneration || targetTerrain == null || targetTerrain.terrainData == null)
            return;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector3? snappedPlayerPos = null;

        if (player != null)
        {
            Vector3 targetPlayerPos = player.transform.position;
            if (spawn.useSafeSpawnSearch && TryFindSafeSpawnNearCenter(seed ^ 0x2B3C4D5E, out Vector3 safePlayerPos))
                targetPlayerPos = safePlayerPos;

            if (TrySnapPositionToTerrain(targetPlayerPos, playerHeightOffset, out Vector3 snappedPos))
            {
                snappedPlayerPos = snappedPos;
                player.ApplySavedPose(snappedPos, player.transform.rotation);
                if (Game.State != null)
                {
                    Game.State.PlayerPos = snappedPos;
                    Game.State.PlayerRot = player.transform.rotation;
                }
            }
        }

        MonsterController monster = FindFirstObjectByType<MonsterController>();
        if (monster == null)
            return;

        Vector3 targetMonsterPos = monster.transform.position;
        if (spawn.useSafeSpawnSearch)
        {
            if (snappedPlayerPos.HasValue &&
                TryFindSafeSpawnAroundPoint(seed ^ unchecked((int)0xA5A5A5A5u), snappedPlayerPos.Value, spawn.monsterMinDistance, spawn.monsterMaxDistance, out Vector3 safeMonsterPos))
            {
                targetMonsterPos = safeMonsterPos;
            }
            else if (TryFindSafeSpawnNearCenter(seed ^ unchecked((int)0xC3EF34A1u), out Vector3 fallbackMonsterPos))
            {
                targetMonsterPos = fallbackMonsterPos;
            }
        }

        if (TrySnapPositionToTerrain(targetMonsterPos, monsterHeightOffset, out Vector3 snappedMonsterPos))
        {
            monster.ApplySavedPose(snappedMonsterPos, monster.transform.rotation);
            if (Game.State != null && Game.State.MonsterBrainState != null)
                Game.State.MonsterBrainState.MonsterPosition = snappedMonsterPos;
        }
    }

    private bool TryFindSafeSpawnNearCenter(int seed, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        if (TryFindSafeSpawnAroundNormalized(seed, 0.5f, 0.5f, spawn.playerSearchRadius01, preferOuterRing: false, out float nx, out float ny))
            return TryNormalizedToWorldPosition(nx, ny, out worldPosition);

        return false;
    }

    private bool TryFindSafeSpawnAroundPoint(int seed, Vector3 aroundWorldPos, float minDistance, float maxDistance, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!TryWorldToNormalized(aroundWorldPos, out float centerX, out float centerY))
            return false;

        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        TerrainData terrainData = targetTerrain.terrainData;
        float radiusX = Mathf.Max(0f, maxDistance) / Mathf.Max(1f, terrainData.size.x);
        float radiusY = Mathf.Max(0f, maxDistance) / Mathf.Max(1f, terrainData.size.z);
        float radius01 = Mathf.Max(radiusX, radiusY);

        bool found = false;
        float bestScore = float.MaxValue;
        float bestNx = 0f;
        float bestNy = 0f;

        var rng = new DeterministicRandom(seed ^ unchecked((int)0x8B8B8B8Bu));
        int attempts = Mathf.Max(16, spawn.searchAttempts);
        float minDistSqr = Mathf.Max(0f, minDistance) * Mathf.Max(0f, minDistance);
        float maxDistSqr = Mathf.Max(minDistSqr, maxDistance * maxDistance);

        for (int i = 0; i < attempts; i++)
        {
            float angle = rng.Range(0f, Mathf.PI * 2f);
            float r01 = Mathf.Sqrt(rng.Value01());
            float rx = Mathf.Cos(angle) * radius01 * r01;
            float ry = Mathf.Sin(angle) * radius01 * r01;
            float nx = centerX + rx;
            float ny = centerY + ry;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
                continue;

            if (!TryNormalizedToWorldPosition(nx, ny, out Vector3 candidate))
                continue;

            float distSqr = (new Vector2(candidate.x - aroundWorldPos.x, candidate.z - aroundWorldPos.z)).sqrMagnitude;
            if (distSqr < minDistSqr || distSqr > maxDistSqr)
                continue;

            if (!IsSpawnLocationValid(nx, ny, out float score))
                continue;

            score += Mathf.Abs(distSqr - (minDistSqr + maxDistSqr) * 0.5f) * 0.0004f;
            if (score < bestScore)
            {
                bestScore = score;
                bestNx = nx;
                bestNy = ny;
                found = true;
            }
        }

        return found && TryNormalizedToWorldPosition(bestNx, bestNy, out worldPosition);
    }

    private bool TryFindSafeSpawnAroundNormalized(int seed, float centerX, float centerY, float radius01, bool preferOuterRing, out float bestNx, out float bestNy)
    {
        bestNx = 0.5f;
        bestNy = 0.5f;
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        bool found = false;
        float bestScore = float.MaxValue;
        var rng = new DeterministicRandom(seed ^ unchecked((int)0x6E6E6E6Eu));
        int attempts = Mathf.Max(16, spawn.searchAttempts);

        for (int i = 0; i < attempts; i++)
        {
            float angle = rng.Range(0f, Mathf.PI * 2f);
            float radial = preferOuterRing ? Mathf.Lerp(0.65f, 1f, rng.Value01()) : Mathf.Sqrt(rng.Value01());
            float nx = centerX + Mathf.Cos(angle) * radius01 * radial;
            float ny = centerY + Mathf.Sin(angle) * radius01 * radial;
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f)
                continue;

            if (!IsSpawnLocationValid(nx, ny, out float score))
                continue;

            float distFromCenter = Mathf.Sqrt((nx - centerX) * (nx - centerX) + (ny - centerY) * (ny - centerY));
            score += preferOuterRing ? Mathf.Abs(radius01 - distFromCenter) * 0.35f : distFromCenter * 0.22f;

            if (score < bestScore)
            {
                bestScore = score;
                bestNx = nx;
                bestNy = ny;
                found = true;
            }
        }

        return found;
    }

    private bool IsSpawnLocationValid(float nx, float ny, out float score)
    {
        score = float.MaxValue;
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        float height01 = SampleMap(generatedHeights, nx, ny);
        float slope01 = EstimateSlope01(nx, ny, targetTerrain.terrainData.size);
        float riverMask = SampleMap(generatedRiverMask, nx, ny);
        float caveMask = SampleMap(generatedCaveMask, nx, ny);
        float edgeMask = SampleMap(generatedEdgeBarrierMask, nx, ny);

        if (height01 < terrainShape.seaLevel01 + spawn.minLandHeightAboveSea)
            return false;
        if (slope01 > spawn.maxSlope01)
            return false;
        if (riverMask > spawn.maxRiverMask)
            return false;
        if (caveMask > spawn.maxCaveMask)
            return false;
        if (edgeMask > spawn.maxEdgeBarrierMask)
            return false;

        float centerPreference = 1f - EvaluateCenterPlayAreaMask(nx, ny);
        score = slope01 * 1.2f + riverMask * 1.6f + caveMask * 1.3f + edgeMask * 1.1f + centerPreference * 0.22f;
        return true;
    }

    private bool TryNormalizedToWorldPosition(float nx, float ny, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        float x = terrainPos.x + Mathf.Clamp01(nx) * terrainData.size.x;
        float z = terrainPos.z + Mathf.Clamp01(ny) * terrainData.size.z;
        worldPosition = new Vector3(x, terrainPos.y, z);
        return true;
    }

    private bool TryWorldToNormalized(Vector3 worldPos, out float nx, out float ny)
    {
        nx = 0.5f;
        ny = 0.5f;
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        if (terrainData.size.x <= 0f || terrainData.size.z <= 0f)
            return false;

        nx = Mathf.Clamp01((worldPos.x - terrainPos.x) / terrainData.size.x);
        ny = Mathf.Clamp01((worldPos.z - terrainPos.z) / terrainData.size.z);
        return true;
    }

    private bool TrySnapPositionToTerrain(Vector3 worldPosition, float heightOffset, out Vector3 snappedPosition)
    {
        snappedPosition = worldPosition;
        if (!TrySampleTerrainHeight(worldPosition, out float terrainY))
            return false;

        snappedPosition.y = terrainY + Mathf.Max(0f, heightOffset);
        return true;
    }

    private void SnapMonsterToNavMesh()
    {
        if (!Application.isPlaying)
            return;

        MonsterController monster = FindFirstObjectByType<MonsterController>();
        if (monster == null)
            return;

        if (!NavMesh.SamplePosition(monster.transform.position, out NavMeshHit hit, Mathf.Max(1f, monsterNavMeshSnapDistance), NavMesh.AllAreas))
            return;

        monster.ApplySavedPose(hit.position, monster.transform.rotation);
        if (Game.State != null && Game.State.MonsterBrainState != null)
            Game.State.MonsterBrainState.MonsterPosition = hit.position;
    }

    private bool TrySampleTerrainHeight(Vector3 worldPosition, out float terrainWorldY)
    {
        terrainWorldY = 0f;
        if (targetTerrain == null || targetTerrain.terrainData == null)
            return false;

        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 size = terrainData.size;
        if (size.x <= 0f || size.z <= 0f)
            return false;

        float nx = Mathf.Clamp01((worldPosition.x - terrainPos.x) / size.x);
        float nz = Mathf.Clamp01((worldPosition.z - terrainPos.z) / size.z);
        terrainWorldY = terrainPos.y + terrainData.GetInterpolatedHeight(nx, nz);
        return true;
    }

    public void OnBeforeGameSaved(GameState state)
    {
        if (state == null)
            return;

        state.EnsureInitialized();
        int seed = hasGeneratedAtLeastOnce
            ? lastGeneratedSeed
            : (Application.isPlaying && useSaveSeedInPlayMode ? Game.EnsureWorldSeed(persistIfNew: false) : editorSeed);

        state.World.Seed = seed;
        state.World.HasSeed = true;
    }

    public void OnAfterGameLoaded(GameState state)
    {
        if (!Application.isPlaying)
            return;

        GenerateWorld(randomizeSeed: false, force: false);
    }
}
