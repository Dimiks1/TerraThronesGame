using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HexGridConfig;
using Random = UnityEngine.Random;

public class HexGridGenerator : MonoBehaviour
{
    private BorderGenerator borderGenerator;
    private RoadGenerator roadGenerator;
    private RiverGenerator riverGenerator;
    private LakeGenerator lakeGenerator;

    [Header("Configuration")]
    [SerializeField] public HexGridConfig gridConfig;

    public Dictionary<Vector2Int, HexTile> hexGrid = new();

    public List<Vector2Int> greenBaseCoords = new List<Vector2Int>();
    public List<Vector2Int> yellowBaseCoords = new List<Vector2Int>();

    public List<Vector2Int> placedBonusCoords = new List<Vector2Int>();

    public static readonly Vector2Int[] hexDirs = new Vector2Int[]
    {
    new Vector2Int(+1, 0),  // 0
    new Vector2Int(+1, -1), // 1
    new Vector2Int(0, -1),  // 2
    new Vector2Int(-1, 0),  // 3
    new Vector2Int(-1, +1), // 4
    new Vector2Int(0, +1)   // 5
    };

    // Axial neighbor offsets (pointy-top axial coords): 6 соседей вокруг хекса
    private static readonly Vector2Int[] _axialNeighborOffsets = new Vector2Int[]
    {
    new Vector2Int(1, 0),
    new Vector2Int(1, -1),
    new Vector2Int(0, -1),
    new Vector2Int(-1, 0),
    new Vector2Int(-1, 1),
    new Vector2Int(0, 1)
    };

    //шумок
    List<Vector2Int> mountainCenters = new List<Vector2Int>();
    Dictionary<Vector2Int, int> mountainRadiusByCenter = new Dictionary<Vector2Int, int>();

    //шум леса
    List<Vector2Int> forestClusterCenters = new List<Vector2Int>();
    Dictionary<Vector2Int, int> forestClusterRadiusByCenter = new Dictionary<Vector2Int, int>();

    void Start()
    {
        if (gridConfig == null)
        {
            Debug.LogError("HexGridConfig not assigned!");
            return;
        }
        GenerateperfectHexGrid();
        new BasePlacer(this).PlacePlayerBases();

        borderGenerator = new BorderGenerator(this);
        borderGenerator.GenerateBorders();

        var bonusPlacer = new BonusPlacer(this);
        bonusPlacer.PlaceBonuses();

        roadGenerator = new RoadGenerator(this);
        roadGenerator.GenerateRoads();

        riverGenerator = new RiverGenerator(this);
        riverGenerator.GenerateRivers();

        lakeGenerator = new LakeGenerator(this, riverGenerator.RiverOccupied);
        lakeGenerator.GenerateLakes();

        //new HeightGenerator(this).ApplyHeights();
        //new ForestGenerator(this).GenerateForests();
        //new WheatGenerator(this).GenerateWheat();
        ApplyHeightsUsingNoise();
        InitForestClusterSeed();
        ApplyForestClusters();
        ApplyWheatFromNoise();

    }

    void GenerateperfectHexGrid()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        hexGrid.Clear();

        for (int q = -gridConfig.gridRadius; q <= gridConfig.gridRadius; q++)
        {
            int r1 = Mathf.Max(-gridConfig.gridRadius, -q - gridConfig.gridRadius);
            int r2 = Mathf.Min(gridConfig.gridRadius, -q + gridConfig.gridRadius);

            for (int r = r1; r <= r2; r++)
            {
                CreateHex(q, r, gridConfig.grassTile);
            }
        }
    }

    private HexTile BuildHex(Vector2Int coord, HexTileType type, Vector3 pos, Quaternion rot)
    {
        var go = Instantiate(type.modelPrefab, pos, rot, transform);
        go.name = $"{type.tileName}_{coord.x}_{coord.y}";
        go.transform.localScale = type.modelPrefab.transform.localScale;

        var rend = go.GetComponent<Renderer>() ?? go.GetComponentInChildren<Renderer>();
        if (rend && type.material) rend.sharedMaterial = type.material;

        if (!go.TryGetComponent<Collider>(out _))
        {
            var mf = type.modelPrefab.GetComponent<MeshFilter>() ?? type.modelPrefab.GetComponentInChildren<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        var tile = go.AddComponent<HexTile>();
        tile.Initialize(coord, type);
        hexGrid[coord] = tile;
        return tile;
    }

    void CreateHex(int q, int r, HexTileType tileType)
    {
        Vector2Int coord = new Vector2Int(q, r);
        if (hexGrid.ContainsKey(coord)) return;

        Quaternion baseRot = GetHexRotation();
        Vector3 position = CalculatePerfectPosition(q, r);

        var prefab = tileType.modelPrefab;
        BuildHex(coord, tileType, position, baseRot);
    }

    public void ReplaceHexTile(Vector2Int coord, HexTileType newTileType)
    {
        if (hexGrid.TryGetValue(coord, out HexTile oldTile))
        {
            if (oldTile.gameObject.GetComponent<ProtectedHex>() != null)
                return;

            Destroy(oldTile.gameObject);

            Vector3 position = CalculatePerfectPosition(coord.x, coord.y);

            Quaternion rot = GetRandomRotationForTile(coord, newTileType);
            BuildHex(coord, newTileType, position, rot);
        }
    }

    public bool IsForbiddenTile(HexTile tile)
    {
        if (tile == null || tile.TileType == null) return false;
        string id = tile.TileType.tileId;
        return id == "4" || id == "5" || id == "6";
    }

    public bool IsCoordForbidden(Vector2Int coord)
    {
        if (!hexGrid.TryGetValue(coord, out HexTile tile))
            return true;

        if (tile == null)
            return true;

        if (tile.gameObject.GetComponent<ProtectedHex>() != null)
            return true;

        return IsForbiddenTile(tile);
    }

    public int AxialDistance(Vector2Int coord)
    {
        int q = coord.x;
        int r = coord.y;
        int s = -q - r;
        return Mathf.Max(Mathf.Abs(q), Mathf.Abs(r), Mathf.Abs(s));
    }

    public int AxialDistance(Vector2Int a, Vector2Int b)
    {
        int dq = a.x - b.x;
        int dr = a.y - b.y;
        int ds = -dq - dr;
        return Mathf.Max(Mathf.Abs(dq), Mathf.Abs(dr), Mathf.Abs(ds));
    }

    public Vector3 CalculatePerfectPosition(int q, int r)
    {
        float size = gridConfig.hexSize;
        float x = Mathf.Sqrt(3f) * size * (q + r * 0.5f);
        float z = 1.5f * size * r;
        return new Vector3(x, 0f, z);
    }

    Quaternion GetHexRotation()
    {
        return Quaternion.Euler(0f, 0f, 0f);
    }

    Quaternion GetRandomRotationForTile(Vector2Int coord, HexTileType tileType)
    {
        const int steps = 6;          // 0..5 → 0°,60°,...,300°
        int k = UnityEngine.Random.Range(0, steps);
        float angle = k * 60f;
        return GetHexRotation() * Quaternion.Euler(0f, angle, 0f);
    }

    public void ReplaceHexTileWithRotation(Vector2Int coord, HexTileType newTileType, Quaternion rotation)
    {
        if (!hexGrid.TryGetValue(coord, out HexTile oldTile))
            return;

        if (oldTile == null)
            return;

        if (oldTile.gameObject.GetComponent<ProtectedHex>() != null)
            return;

        Destroy(oldTile.gameObject);

        Vector3 position = CalculatePerfectPosition(coord.x, coord.y);
        BuildHex(coord, newTileType, position, rotation);
    }

    public List<Vector2Int> FindPath(
    Vector2Int start,
    Vector2Int goal,
    bool treatProtectedAsWalkable = false,
    HashSet<Vector2Int> banned = null)
    {
        if (start == goal)
            return new List<Vector2Int> { start };

        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = AxialDistance(start, goal) };

        while (open.Count > 0)
        {
            Vector2Int current = GetBestOpenNode(open, gScore, fScore);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            open.Remove(current);

            foreach (var dir in hexDirs)
            {
                Vector2Int neighbor = current + dir;

                if (!hexGrid.ContainsKey(neighbor))
                    continue;

                if (neighbor != goal && banned != null && banned.Contains(neighbor))
                    continue;

                if (!IsWalkableForPath(neighbor, goal, treatProtectedAsWalkable))
                    continue;

                int tentativeG = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + AxialDistance(neighbor, goal);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }

        return null;
    }

    private Vector2Int GetBestOpenNode(
    List<Vector2Int> open,
    Dictionary<Vector2Int, int> gScore,
    Dictionary<Vector2Int, int> fScore)
    {
        Vector2Int current = open[0];
        int bestF = fScore.TryGetValue(current, out int currentF) ? currentF : int.MaxValue;
        int bestG = gScore.TryGetValue(current, out int currentG) ? currentG : int.MaxValue;

        for (int i = 1; i < open.Count; i++)
        {
            Vector2Int node = open[i];
            int nodeF = fScore.TryGetValue(node, out int f) ? f : int.MaxValue;
            int nodeG = gScore.TryGetValue(node, out int g) ? g : int.MaxValue;

            if (nodeF < bestF || (nodeF == bestF && nodeG < bestG))
            {
                current = node;
                bestF = nodeF;
                bestG = nodeG;
            }
        }

        return current;
    }

    private List<Vector2Int> ReconstructPath(
    Dictionary<Vector2Int, Vector2Int> cameFrom,
    Vector2Int current)
    {
        var path = new List<Vector2Int> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private bool IsWalkableForPath(
    Vector2Int coord,
    Vector2Int goal,
    bool treatProtectedAsWalkable)
    {
        if (coord == goal)
            return true;

        if (!hexGrid.TryGetValue(coord, out HexTile tile) || tile == null)
            return false;

        if (!IsCoordForbidden(coord))
            return true;

        if (!treatProtectedAsWalkable)
            return false;

        return tile.gameObject.GetComponent<ProtectedHex>() != null;
    }

    public HexTile GetHexAt(Vector2Int coord)
    {
        if (hexGrid.TryGetValue(coord, out HexTile hex))
            return hex;

        return null;
    }

    public Dictionary<Vector2Int, HexTile> GetHexGrid()
    {
        return hexGrid;
    }

    float SampleHeight01(Vector2Int coord)
    {
        // 1) берём «идеальные» мировые координаты твоего гекса (xz-плоскость) 
        Vector3 p = CalculatePerfectPosition(coord.x, coord.y); // (x,0,z)

        // 2) лёгкий domain warp (опционально, для ломаной изолиний)
        float wx = Mathf.PerlinNoise(p.x * .03f, p.z * .03f);
        float wz = Mathf.PerlinNoise((p.x + 100f) * .03f, (p.z + 100f) * .03f);
        float dx = (wx - 0.5f) * 2f * gridConfig.domainWarpStrength;
        float dz = (wz - 0.5f) * 2f * gridConfig.domainWarpStrength;

        // 3) fBm выборка
        float nx = (p.x + gridConfig.heightOffset.x + dx) * gridConfig.heightScale;
        float nz = (p.z + gridConfig.heightOffset.y + dz) * gridConfig.heightScale;
        return Mathf.Clamp01(FBm(nx, nz)); // 0..1
    }

    float FBm(float x, float y)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int o = 0; o < Mathf.Max(1, gridConfig.heightOctaves); o++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
            norm += amp;
            amp *= Mathf.Clamp01(gridConfig.heightPersistence);
            freq *= Mathf.Max(1.0f, gridConfig.heightLacunarity);
        }
        return (norm > 0f) ? sum / norm : 0f; // 0..1
    }

    public void ApplyHeightsUsingNoise()
    {
        if (gridConfig == null || !gridConfig.useHeightNoise) return;

        // Построим центры гор один раз
        BuildMountainCenters();

        float tHill = Mathf.Clamp01(gridConfig.hillThreshold);
        float tMountain = Mathf.Clamp01(gridConfig.mountainThreshold);
        float tPeak = Mathf.Clamp01(gridConfig.peakThreshold);

        float step = (gridConfig.heightStep > 0f) ? gridConfig.heightStep
                                                  : Mathf.Max(0.5f, gridConfig.hexSize * 0.5f);

        foreach (var kv in hexGrid)
        {
            var coord = kv.Key;
            var tile = kv.Value;
            if (tile == null) continue;

            // не трогаем инфраструктуру/запреты
            if (tile.gameObject.GetComponent<ProtectedHex>() != null) continue;
            if (IsCoordForbidden(coord)) continue;

            // работаем по «суше»
            if (tile.TileType != gridConfig.grassTile) continue;

            // БАЗОВЫЙ шум — чтобы сохранить мелкие холмы в «равнинах»
            float h01 = SampleHeight01(coord); // твой нынешний fBm 0..1

            // --- ГОРЫ: ищем ближайший центр и ступенчато назначаем высоту по кольцам
            float y = 0f;
            Vector2Int nearest = default;
            int bestD = int.MaxValue, R = -1;

            // Быстрый проход (центров обычно немного)
            for (int i = 0; i < mountainCenters.Count; i++)
            {
                var c0 = mountainCenters[i];
                int d = AxialDistance(coord, c0);
                if (d < bestD)
                {
                    bestD = d;
                    R = mountainRadiusByCenter[c0];
                    nearest = c0;
                }
            }

            bool inMountain = (bestD <= R && R > 0);
            if (inMountain)
            {
                // нормированная радиальная дистанция 0..1 от центра
                float r01 = (R <= 0) ? 1f : (bestD / (float)R);
                // 3 кольца: [0..0.33)->1.5, [0.33..0.66)->1.0, [0.66..1.0]->0.5
                if (r01 < 0.33f) y = 1.5f * step;     // вершина
                else if (r01 < 0.66f) y = 1.0f * step;     // горный пояс
                else y = 0.5f * step;     // предгорья
            }
            else
            {
                // ВНЕ гор — оставляем вариант с «мелкими холмами»
                if (h01 >= tHill)
                {
                    // шанс сохранить редкие одиночные холмы
                    if (Random.value < gridConfig.hillsKeepChance)
                    {
                        // дискретизируем как «холм» (0.5) или (реже) «гора»/«вершина», если очень высоко
                        if (h01 < tMountain) y = 0.5f * step;
                        else if (h01 < tPeak) y = 1.0f * step;
                        else y = 1.5f * step; // сверхредкие «островные вершинки»
                    }
                    else y = 0f;
                }
            }

            // применяем
            var tr = tile.transform;
            tr.position = new Vector3(tr.position.x, y, tr.position.z);
        }
    }

    void BuildMountainCenters()
    {
        mountainCenters.Clear();
        mountainRadiusByCenter.Clear();
        if (!gridConfig.useMountains) return;

        // соберём кандидатов по маске
        var hexes = hexGrid; if (hexes == null || hexes.Count == 0) return;
        float scale = Mathf.Max(1e-4f, gridConfig.mountainMaskScale);

        // 1) вычисляем значение маски по всем клеткам
        var maskVal = new Dictionary<Vector2Int, float>(hexes.Count);
        foreach (var kv in hexes)
        {
            var c = kv.Key;
            // пропускаем запрещённые/защищённые/не траву (центр горы не ставим в воду/дорогу и т.п.)
            var tile = kv.Value;
            if (tile == null) continue;
            if (tile.gameObject.GetComponent<ProtectedHex>() != null) continue;          // не на инфраструктуре
            if (IsCoordForbidden(c)) continue;                                           // общие запреты
            if (tile.TileType != gridConfig.grassTile) continue;                         // горы только на «суше»
            Vector3 p = CalculatePerfectPosition(c.x, c.y);
            float m = FBmMount((p.x + gridConfig.mountainMaskOffset.x) * scale,
                   (p.z + gridConfig.mountainMaskOffset.y) * scale);
            maskVal[c] = m;
        }

        // 2) отфильтруем низкие значения и выберем локальные максимумы
        var neigh = _axialNeighborOffsets; // уже есть в твоём классе. :contentReference[oaicite:3]{index=3}
        var rawPeaks = new List<Vector2Int>();
        foreach (var kv in maskVal)
        {
            if (kv.Value < gridConfig.mountainMaskThreshold) continue;
            bool isPeak = true;
            for (int i = 0; i < neigh.Length; i++)
            {
                var n = kv.Key + neigh[i];
                if (maskVal.TryGetValue(n, out float mv) && mv > kv.Value) { isPeak = false; break; }
            }
            if (isPeak) rawPeaks.Add(kv.Key);
        }

        // 3) разрежаем пики (минимальный разнос), сортируя по силе маски
        rawPeaks.Sort((a, b) => maskVal[b].CompareTo(maskVal[a]));
        int minSpace = Mathf.Max(1, gridConfig.mountainMinSpacing);
        int limit = Mathf.Max(1, gridConfig.mountainMaxCenters);

        foreach (var c in rawPeaks)
        {
            bool farEnough = true;
            for (int i = 0; i < mountainCenters.Count; i++)
            {
                if (AxialDistance2(c, mountainCenters[i]) < minSpace) { farEnough = false; break; }
            }
            if (!farEnough) continue;
            mountainCenters.Add(c);

            // радиус в гекса̀х — от диапазона, но детерминированно (по хешу)
            float h = Hash01(c);
            int rMin = Mathf.Max(2, gridConfig.mountainRadiusRange.x);
            int rMax = Mathf.Max(rMin + 1, gridConfig.mountainRadiusRange.y);
            int R = Mathf.RoundToInt(Mathf.Lerp(rMin, rMax, h));
            mountainRadiusByCenter[c] = R;

            if (mountainCenters.Count >= limit) break;
        }
    }

    float Hash01(Vector2Int c)
    {
        unchecked
        {
            int h = c.x * 73856093 ^ c.y * 19349663;
            h ^= (h << 13); h ^= (h >> 17); h ^= (h << 5);
            return Mathf.Abs(h) / (float)int.MaxValue;
        }
    }

    // Hex-расстояние для аксиальных координат (q,r) (pointy-top)
    int AxialDistance2(Vector2Int a, Vector2Int b)
    {
        int dq = a.x - b.x;
        int dr = a.y - b.y;
        int ds = (-a.x - a.y) - (-b.x - b.y);
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }
    float Ridged01(float x, float y)
    {
        return 1f - Mathf.Abs(2f * Mathf.PerlinNoise(x, y) - 1f);
    }

    float FBmMount(float x, float y)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int o = 0; o < Mathf.Max(1, gridConfig.mountainOctaves); o++)
        {
            // используем ridged как базу, чтобы тянуться к «гребням»
            float n = Ridged01(x * freq, y * freq);
            sum += amp * n; norm += amp;
            amp *= gridConfig.mountainPersistence;
            freq *= gridConfig.mountainLacunarity;
        }
        return (norm > 0f) ? sum / norm : 0f;
    }

    void InitForestClusterSeed()
    {
        if (!gridConfig.randomizeForestClusterSeed)
        {
            // фиксированный сид — используем базовый оффсет
            gridConfig.forestClusterOffset = gridConfig.forestClusterBaseOffset;
            return;
        }

        // генерим сид и оффсет для текущего запуска
        gridConfig.forestClusterSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        // создаём локальный генератор на основе сида, чтобы не портить общий Random
        var sr = new System.Random(gridConfig.forestClusterSeed);
        float jx = (float)sr.NextDouble() * 2f - 1f; // [-1..1]
        float jy = (float)sr.NextDouble() * 2f - 1f;

        gridConfig.forestClusterOffset =
            gridConfig.forestClusterBaseOffset +
            new Vector2(jx, jy) * gridConfig.forestClusterSeedJitter;
    }
    bool IsGrassTile(HexTile t)
    {
        if (t == null || t.TileType == null || gridConfig.grassTile == null) return false;
        if (t.TileType == gridConfig.grassTile) return true; // прямой реф

        // подстраховка по id/имени (если есть расхождение по инстансу)
        return (!string.IsNullOrEmpty(t.TileType.tileId) && t.TileType.tileId == "1")
            || (!string.IsNullOrEmpty(t.TileType.tileName) && t.TileType.tileName == "grass");
    }
    public void ApplyForestClusters()
    {
        if (gridConfig == null || !gridConfig.useForestClusters) return;

        BuildForestClusterCenters();

        float tS = Mathf.Clamp01(gridConfig.forestClusterSparseThreshold);
        float tM = Mathf.Clamp01(gridConfig.forestClusterMediumThreshold);
        float tD = Mathf.Clamp01(gridConfig.forestClusterDenseThreshold);

        foreach (var kv in hexGrid)
        {
            var c = kv.Key; var tile = kv.Value;
            if (tile == null) continue;

            // базовые фильтры
            if (tile.gameObject.GetComponent<ProtectedHex>() != null) continue;
            if (IsCoordForbidden(c)) continue;
            if (!IsGrassTile(tile)) continue;

            // зачистка старого оверлея
            var exist = tile.transform.Find(ForestChildNameCluster);
            if (exist != null) Destroy(exist.gameObject);

            // ближайший центр
            Vector2Int nearest = default;
            int bestD = int.MaxValue, R = -1;
            for (int i = 0; i < forestClusterCenters.Count; i++)
            {
                var c0 = forestClusterCenters[i];
                int d = AxialDistance(c, c0);
                if (d < bestD) { bestD = d; nearest = c0; R = forestClusterRadiusByCenter[c0]; }
            }

            GameObject prefab = null;
            bool inCluster = (bestD <= R && R > 0);

            if (inCluster)
            {
                var set = PickSetForCenter(nearest); // может быть null => фолбэк ниже

                float r01 = (R <= 0) ? 1f : (bestD / (float)R);
                float j = (Hash01(nearest) - 0.5f) * 0.08f; // «дрожь» границ
                float inner = 0.33f + j;
                float middle = 0.66f + j;

                if (r01 < inner) prefab = set?.dense ?? gridConfig.forestDenseOverlayCluster;
                else if (r01 < middle) prefab = set?.medium ?? gridConfig.forestMediumOverlayCluster;
                else prefab = set?.sparse ?? gridConfig.forestSparseOverlayCluster;
            }
            else
            {
                // одиночки вне массивов
                float f = SampleForestClusterMask01(c);
                if (f >= tS && Random.value < gridConfig.forestSinglesChanceCluster)
                {
                    // для одиночек сет выберем детерминированно по клетке — добавит «пестроты»
                    var singleSet = (Hash01(c) < gridConfig.forestSetAProbability) ? gridConfig.forestSetA : gridConfig.forestSetB;

                    if (f >= tD) prefab = singleSet?.dense ?? gridConfig.forestDenseOverlayCluster;
                    else if (f >= tM) prefab = singleSet?.medium ?? gridConfig.forestMediumOverlayCluster;
                    else prefab = singleSet?.sparse ?? gridConfig.forestSparseOverlayCluster;
                }
            }

            if (prefab == null) continue;

            var child = Instantiate(prefab, tile.transform);
            child.name = ForestChildNameCluster;
            child.transform.localPosition = new Vector3(0f, gridConfig.forestOverlayYCluster, 0f);

            // === дискретная ротация, шаг 60° ===
            const int steps = 6;
            int k = Mathf.Abs((c.x * 73856093 ^ c.y * 19349663)) % steps; // детерминированно по клетке
                                                                          // (если нужна чистая случайность в рамках одного запуска, замени на: int k = Random.Range(0, steps);)
            float angle = k * 60f;
            child.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    float SampleForestClusterMask01(Vector2Int c)
    {
        Vector3 p = CalculatePerfectPosition(c.x, c.y);
        float nx = (p.x + gridConfig.forestClusterOffset.x) * gridConfig.forestClusterMaskScale;
        float nz = (p.z + gridConfig.forestClusterOffset.y) * gridConfig.forestClusterMaskScale;
        float m = Mathf.Clamp01(
            FBmForestClusterMask(nx, nz));
        if (gridConfig.forestClusterMaskContrast > 1f)
            m = Mathf.Pow(m, gridConfig.forestClusterMaskContrast);
        return m;
    }

    float FBmForestClusterMask(float x, float y)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int o = 0; o < Mathf.Max(1, gridConfig.forestClusterMaskOctaves); o++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
            norm += amp;
            amp *= gridConfig.forestClusterMaskPersistence;
            freq *= gridConfig.forestClusterMaskLacunarity;
        }
        return (norm > 0f) ? sum / norm : 0f;
    }

    ForestOverlaySet PickSetForCenter(Vector2Int centerCoord)
    {
        if (forestClusterSetByCenter.TryGetValue(centerCoord, out var s)) return s;

        // детерминированный «рандом» от центра
        int h = centerCoord.x * 73856093 ^ centerCoord.y * 19349663;
        float p = Mathf.Abs(h % 10000) / 10000f;

        var chosen = (p < gridConfig.forestSetAProbability) ? gridConfig.forestSetA : gridConfig.forestSetB;
        // подстрахуемся, если какой-то из сетов не заполнен
        if (chosen == null || (chosen.sparse == null && chosen.medium == null && chosen.dense == null))
        {
            chosen = gridConfig.forestSetA ?? gridConfig.forestSetB;
        }
        forestClusterSetByCenter[centerCoord] = chosen;
        return chosen;
    }

    Dictionary<Vector2Int, ForestOverlaySet> forestClusterSetByCenter = new();

    const string ForestChildNameCluster = "ForestOverlayCluster";

    void BuildForestClusterCenters()
    {
        forestClusterCenters.Clear();
        forestClusterRadiusByCenter.Clear();
        if (!gridConfig.useForestClusters) return;
        if (hexGrid == null || hexGrid.Count == 0) return;

        // 1) Маска по допустимым клеткам
        var mask = new Dictionary<Vector2Int, float>(hexGrid.Count);
        foreach (var kv in hexGrid)
        {
            var c = kv.Key; var t = kv.Value;
            if (t == null) continue;
            if (t.gameObject.GetComponent<ProtectedHex>() != null) continue;
            if (IsCoordForbidden(c)) continue;
            if (t.TileType != gridConfig.grassTile) continue; // только трава
            float m = SampleForestClusterMask01(c);
            if (m >= gridConfig.forestClusterMaskThreshold) mask[c] = m;
        }

        // 2) Локальные максимумы на маске
        var peaks = new List<Vector2Int>();
        foreach (var kv in mask)
        {
            bool peak = true;
            for (int i = 0; i < _axialNeighborOffsets.Length; i++)
            {
                var n = kv.Key + _axialNeighborOffsets[i];
                if (mask.TryGetValue(n, out float mv) && mv > kv.Value) { peak = false; break; }
            }
            if (peak) peaks.Add(kv.Key);
        }

        // 3) Разрежение и радиусы
        peaks.Sort((a, b) => mask[b].CompareTo(mask[a]));
        int minSpace = Mathf.Max(1, gridConfig.forestClusterMinSpacing);
        int limit = Mathf.Max(1, gridConfig.forestClusterMaxCenters);

        foreach (var c in peaks)
        {
            bool farEnough = true;
            for (int i = 0; i < forestClusterCenters.Count; i++)
            {
                if (AxialDistance(c, forestClusterCenters[i]) < minSpace) { farEnough = false; break; }
            }
            if (!farEnough) continue;
            forestClusterCenters.Add(c);

            int rMin = Mathf.Max(2, gridConfig.forestClusterRadiusRange.x);
            int rMax = Mathf.Max(rMin + 1, gridConfig.forestClusterRadiusRange.y);
            int R = Mathf.RoundToInt(Mathf.Lerp(rMin, rMax, Hash01(c)));
            forestClusterRadiusByCenter[c] = R;

            if (forestClusterCenters.Count >= limit) break;
        }
    }

    public void ApplyWheatFromNoise()
    {
        if (gridConfig == null || !gridConfig.useWheatNoise) return;
        if (gridConfig.wheatOverlay == null) return;

        float t = Mathf.Clamp01(gridConfig.wheatThreshold);

        foreach (var kv in hexGrid)
        {
            var c = kv.Key;
            var tile = kv.Value;
            if (tile == null) continue;

            // база/река/дорога/озеро/бонусы — не трогаем
            if (tile.gameObject.GetComponent<ProtectedHex>() != null) continue;
            if (IsCoordForbidden(c)) continue;
            if (!IsGrassTile(tile)) continue;

            // НЕ ставим на лес и (опционально) рядом с лесом
            if (HasForestOverlay(tile)) continue;

            // сносим прошлую пшеницу (если перегенерация)
            var exist = tile.transform.Find(WheatChildName);
            if (exist != null) Destroy(exist.gameObject);

            // берём тот же сэмплер, что и «старый лес» (SampleForest01)
            float f = SampleForest01(c);
            if (f < t) continue;

            // инстансим
            var child = Instantiate(gridConfig.wheatOverlay, tile.transform);
            child.name = WheatChildName;
            child.transform.localPosition = new Vector3(0f, gridConfig.wheatOverlayY, 0f);

            // лёгкая дискретная ротация для разнообразия
            int steps = 6;
            int k = Mathf.Abs((c.x * 73856093 ^ c.y * 19349663)) % steps;
            float angle = k * 60f;
            child.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    float SampleForest01(Vector2Int coord)
    {
        Vector3 p = CalculatePerfectPosition(coord.x, coord.y); // уже есть в генераторе
        float nx = (p.x + gridConfig.forestOffset.x) * gridConfig.forestScale;
        float nz = (p.z + gridConfig.forestOffset.y) * gridConfig.forestScale;
        return Mathf.Clamp01(FBmForest(nx, nz));
    }

    float FBmForest(float x, float y)
    {
        float amp = 1f, freq = 1f, sum = 0f, norm = 0f;
        for (int o = 0; o < Mathf.Max(1, gridConfig.forestOctaves); o++)
        {
            sum += amp * Mathf.PerlinNoise(x * freq, y * freq);
            norm += amp;
            amp *= Mathf.Clamp01(gridConfig.forestPersistence);
            freq *= Mathf.Max(1.0f, gridConfig.forestLacunarity);
        }
        return (norm > 0f) ? sum / norm : 0f; // 0..1
    }

    bool HasForestOverlay(HexTile tile)
    {
        if (tile == null) return false;
        // оба имени у тебя уже есть:
        // const string ForestChildName = "ForestOverlay";
        // const string ForestChildNameCluster = "ForestOverlayCluster";
        return tile.transform.Find(ForestChildName) != null
            || tile.transform.Find(ForestChildNameCluster) != null;
    }
    const string WheatChildName = "WheatOverlay";

    const string ForestChildName = "ForestOverlay";

    private void OnDrawGizmos()
    {
        if (roadGenerator == null)
            return;

        DrawUnifiedRoadGizmos(roadGenerator);
    }

    private void DrawUnifiedRoadGizmos(RoadGenerator roadGenerator)
    {
        if (roadGenerator == null || roadGenerator._unifiedAdj == null)
            return;

        Gizmos.color = Color.white;

        foreach (var pair in roadGenerator._unifiedAdj)
        {
            Vector2Int fromCoord = pair.Key;

            if (!hexGrid.TryGetValue(fromCoord, out HexTile fromTile) || fromTile == null)
                continue;

            foreach (var toCoord in pair.Value)
            {
                // Рисуем каждое ребро только один раз.
                if (fromCoord.x > toCoord.x ||
                    (fromCoord.x == toCoord.x && fromCoord.y > toCoord.y))
                    continue;

                if (!hexGrid.TryGetValue(toCoord, out HexTile toTile) || toTile == null)
                    continue;

                Vector3 from = fromTile.transform.position + Vector3.up * 1.2f;
                Vector3 to = toTile.transform.position + Vector3.up * 1.2f;

                Gizmos.DrawLine(from, to);
            }
        }

        foreach (var coord in roadGenerator._unifiedRoadCoordsSet)
        {
            if (!hexGrid.TryGetValue(coord, out HexTile tile) || tile == null)
                continue;

            int degree = roadGenerator._unifiedAdj.TryGetValue(coord, out var neighbors)
                ? neighbors.Count
                : 0;

            Gizmos.color = degree > 2 ? Color.magenta : Color.cyan;

            float size = Mathf.Max(0.03f, gridConfig.hexSize * 0.04f);
            Gizmos.DrawSphere(tile.transform.position + Vector3.up * 1.2f, size);
        }
    }

    public class ProtectedHex : MonoBehaviour { } // Hexes that we want to protect from replacing.
    public HashSet<Vector2Int> bonusAdjacencyProtected = new HashSet<Vector2Int>(); // Hexes that we want protect only from specific generator.
}
