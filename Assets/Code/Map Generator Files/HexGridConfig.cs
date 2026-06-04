using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HexGridConfig", menuName = "Game/Hex Grid Config")]
public class HexGridConfig : ScriptableObject
{
    [Header("Based tiles")]
    public HexTileType grassTile;
    public HexTileType bonusNeutralTile;
    public HexTileType greenBaseTile;
    public HexTileType yellowBaseTile;

    [Header("Border tiles")]
    public HexTileType borderOuterTile;
    public HexTileType borderInnerTile;

    [Header("Border variants")]
    public HexTileType[] borderOuterVariants;
    public HexTileType[] borderInnerVariants;

    [Header("Bonus tiles")]
    public HexTileType bonusTileForGreen;
    public HexTileType bonusTileForYellow;

    [Header("Instructions for generating")]
    public int gridRadius = 14;
    public float hexSize = 1f;
    public int basesPerPlayer = 2;

    [System.Serializable]
    public class RoadVariant
    {
        [Tooltip("Доп. смещение поворота в шагах по 60° (0..5) чтобы согласовать каноническое направление модели")]
        public int baseRotation = 0;

        public string name;                 // описание в инспекторе
        public HexTileType prefab;          // префаб/тип для этого варианта
        [Tooltip("Direction indices (0..5) for canonical orientation, e.g. 0,1 for corner; 0,3 for straight.")]
        public List<int> maskDirs = new List<int>();
    }

    [Header("Road prefab variants (assign variants with canonical directions)")]
    public List<RoadVariant> roadVariants = new List<RoadVariant>();

    [System.Serializable]
    public class RiverVariant
    {
        [Tooltip("Доп. смещение поворота в шагах по 60° (0..5), чтобы согласовать модель")]
        public int baseRotation = 0;

        public string name;
        public HexTileType prefab;

        [Tooltip("Direction indices (0..5) for canonical orientation, e.g. 0,1 for corner; 0,3 for straight.")]
        public List<int> maskDirs = new List<int>();
    }

    [Header("River prefab variants")]
    public List<RiverVariant> riverVariants = new List<RiverVariant>();

    [System.Serializable]
    public class LakeVariant
    {
        [Tooltip("Доп. смещение поворота в шагах по 60° (0..5), чтобы согласовать каноническое направление модели")]
        public int baseRotation = 0;

        public string name;
        public HexTileType prefab;

        [Tooltip("Direction indices (0..5) for canonical orientation")]
        public List<int> maskDirs = new List<int>();
    }

    [Header("Lake prefab variants")]
    public List<LakeVariant> lakeVariants = new List<LakeVariant>();

    //[Header("Height Noise")]
    //public float heightScale = 0.08f;

    //[Range(1, 8)]
    //public int heightOctaves = 4;

    //[Range(0f, 1f)]
    //public float heightPersistence = 0.5f;

    //public float heightLacunarity = 2.0f;

    //public Vector2 heightOffset = new Vector2(123.4f, 987.6f);

    //[Range(0f, 3f)]
    //public float domainWarpStrength = 0.0f;

    //[Header("Height Thresholds")]
    //[Range(0f, 1f)]
    //public float hillThreshold = 0.55f;

    //[Range(0f, 1f)]
    //public float mountainThreshold = 0.75f;

    //[Range(0f, 1f)]
    //public float peakThreshold = 0.90f;

    //[Header("Height Output")]
    //public float heightStep = 1f;

    //[Header("Forest Clusters")]
    //public bool useForestClusters = true;

    //public float forestClusterMaskScale = 0.02f;

    //[Range(0f, 1f)]
    //public float forestClusterMaskThreshold = 0.6f;

    //public int forestClusterMaskOctaves = 3;
    //public float forestClusterMaskPersistence = 0.55f;
    //public float forestClusterMaskLacunarity = 2.0f;

    //public float forestClusterMaskContrast = 1.0f;

    //[Range(1, 12)]
    //public int forestClusterMinSpacing = 5;

    //public int forestClusterMaxCenters = 128;

    //public Vector2Int forestClusterRadiusRange = new Vector2Int(4, 9);

    //[Range(0f, 1f)]
    //public float forestClusterSparseThreshold = 0.55f;

    //[Range(0f, 1f)]
    //public float forestClusterMediumThreshold = 0.70f;

    //[Range(0f, 1f)]
    //public float forestClusterDenseThreshold = 0.85f;

    //public float forestOverlayYCluster = 0f;

    //[System.Serializable]
    //public class ForestOverlaySet
    //{
    //    public GameObject sparse;
    //    public GameObject medium;
    //    public GameObject dense;
    //}

    //[Header("Forest Cluster Sets")]
    //public ForestOverlaySet forestSetA;
    //public ForestOverlaySet forestSetB;

    //[Range(0f, 1f)]
    //public float forestSetAProbability = 0.5f;

    //[Header("Forest Cluster Seeding")]
    //public bool randomizeForestClusterSeed = true;
    //public int forestClusterSeed = 0;
    //public Vector2 forestClusterBaseOffset = new Vector2(4321f, 765f);
    //public Vector2 forestClusterOffset = new Vector2(4321f, 765f);
    //public float forestClusterSeedJitter = 1000f;

    //[Header("Wheat")]
    //public bool useWheatNoise = true;

    //[Range(0f, 1f)]
    //public float wheatThreshold = 0.58f;

    //public GameObject wheatOverlay;
    //public float wheatOverlayY = 0f;

    //[Header("Base Forest/Wheat Noise")]
    //public float forestScale = 0.06f;

    //[Range(1, 8)]
    //public int forestOctaves = 4;

    //[Range(0f, 1f)]
    //public float forestPersistence = 0.5f;

    //public float forestLacunarity = 2.0f;

    //public Vector2 forestOffset = new Vector2(4321.0f, 765.0f);

    // НАЧАЛО ШУМА ПЕРЛИНА
    [Header("Height (Perlin/fBm)")]
    public bool useHeightNoise = true;

    // Масштаб, октавы и fBm-параметры
    [Tooltip("Чем меньше, тем более широкие формы рельефа")]
    public float heightScale = 0.08f;
    [Range(1, 8)] public int heightOctaves = 4;
    [Range(0f, 1f)] public float heightPersistence = 0.5f;
    public float heightLacunarity = 2.0f;

    // Сдвиг шума (seed) и необязательный domain warp
    public Vector2 heightOffset = new Vector2(123.4f, 987.6f);
    [Range(0f, 3f)] public float domainWarpStrength = 0.0f;

    // Пороговые значения для 4-х уровней
    [Header("Height thresholds (0..1 noise)")]
    [Range(0f, 1f)] public float hillThreshold = 0.55f;     // -> 0.5 блока
    [Range(0f, 1f)] public float mountainThreshold = 0.75f; // -> 1.0 блок
    [Range(0f, 1f)] public float peakThreshold = 0.90f;     // -> 1.5 блока

    // «Высота блока» (шаг по Y), всё дискретизируем относительно него
    [Header("Height output")]
    public float heightStep = 0f;

    //АДДОНЧИК ==============

    [Header("Mountains (clustered tiers)")]
    public bool useMountains = true;

    // Маска гор ("где можно гору") — низкая частота
    public float mountainMaskScale = 0.018f;
    [Range(0f, 1f)] public float mountainMaskThreshold = 0.62f; // выше — считаем «гора возможна»
    public int mountainOctaves = 3;
    public float mountainPersistence = 0.55f;
    public float mountainLacunarity = 2.0f;

    // Поиск центров
    [Range(1, 12)] public int mountainMinSpacing = 5; // минимальный разнос центров (в гекса̀х)
    public int mountainMaxCenters = 128;              // защита от перебора

    // Радиусы колец (в гекса̀х)
    public Vector2Int mountainRadiusRange = new Vector2Int(4, 9); // R выбираем детерминированно на центр

    // Сохранить «рассеянные холмы» вне гор
    [Range(0f, 1f)] public float hillsKeepChance = 0.75f; // шанс оставить холм от базового шума вне гор

    [Header("Mountains Seeding")]
    public bool randomizeMountainSeed = true;     // рандомизировать при старте
    public int mountainSeed = 0;                  // сохранённый сид (для дебага)
    public Vector2 mountainBaseOffset = new Vector2(1234f, 5678f); // базовый оффсет
    public float mountainSeedJitter = 1000f;      // амплитуда сдвига
    public Vector2 mountainMaskOffset = Vector2.zero; // РЕАЛЬНЫЙ оффсет, который будет использоваться

    [Header("Mountain footers (decor)")]
    public GameObject mountainFooterOverlay;   // префаб декора
    public float mountainFooterY = 0f;         // посадка по Y (локально)
    [Range(0f, 1f)] public float mountainFooterChance = 0.7f; // вероятность появления
    [Tooltip("Насколько сильно уводить объект за кромку (в долях hexSize). 0.0 = ровно до ребра")]
    public float mountainFooterOverlap = 0.05f; // 5% от hexSize «вгрызания» в склон
    [Tooltip("Макс. случайное отклонение поворота вокруг оси Y, в градусах")]
    public float mountainFooterRandomYaw = 12f;



    // ЛЕСОК =====================================================================================

    [Header("Forest (Perlin/fBm)")]
    public bool useForestNoise = true;

    // Шум леса
    public float forestScale = 0.06f;
    [Range(1, 8)] public int forestOctaves = 4;
    [Range(0f, 1f)] public float forestPersistence = 0.5f;
    public float forestLacunarity = 2.0f;
    public Vector2 forestOffset = new Vector2(4321.0f, 765.0f); // seed для леса отдельно от высоты

    // Пороги плотности (0..1)
    [Range(0f, 1f)] public float forestSparseThreshold = 0.55f;   // ≥ -> редкий
    [Range(0f, 1f)] public float forestMediumThreshold = 0.70f;   // ≥ -> средний
    [Range(0f, 1f)] public float forestDenseThreshold = 0.85f;   // ≥ -> густой

    // Буферы (в гекса̀х)
    [Header("Forest buffers")]
    [Range(0, 3)] public int forestBufferFromRiver = 1;  // просвет от реки
    [Range(0, 3)] public int forestBufferFromRoad = 1;  // просвет от дорог
    [Range(0, 3)] public int forestBufferFromBonus = 1;  // просвет от бонус-зданий (их соседей)

    // Префабы оверлея (дочерние объекты на grass)
    [Header("Forest overlay prefabs (children)")]
    public GameObject forestSparseOverlay;   // редкий
    public GameObject forestMediumOverlay;   // средний
    public GameObject forestDenseOverlay;    // густой

    // Вертикальная подкладка, если нужно чуть «посадить» деревья
    public float forestOverlayY = 0f;

    [Header("Wheat (uses old forest noise)")]
    public bool useWheatNoise = true;
    [Range(0f, 1f)] public float wheatThreshold = 0.58f; // подбери под свои вкусы
    public GameObject wheatOverlay;                     // префаб поля
    public float wheatOverlayY = 0f;
    [Range(0, 2)] public int wheatBufferFromForest = 0;  // просвет вокруг леса (0=не нужен)


    // FLLJYXBR R KTCDV

    [Header("Forest Clusters (tiers)")]
    public bool useForestClusters = true;

    // Маска, где возможны лесные массивы
    public float forestClusterMaskScale = 0.02f;
    [Range(0f, 1f)]
    public float forestClusterMaskThreshold = 0.6f;
    public int forestClusterMaskOctaves = 3;
    public float forestClusterMaskPersistence = 0.55f;
    public float forestClusterMaskLacunarity = 2.0f;
    public float forestClusterMaskContrast = 1.0f; // 1=без изм., >1 усиливает пики

    // Центры массивов
    [Range(1, 12)]
    public int forestClusterMinSpacing = 5;   // мин. разнос центров (гекса)
    public int forestClusterMaxCenters = 128; // защита от перебора

    // Радиусы колец (в гекса̀х) — внешний радиус массива
    public Vector2Int forestClusterRadiusRange = new Vector2Int(4, 9);

    // Пороги плотности (для одиночек вне массивов также используем их)
    [Range(0f, 1f)]
    public float forestClusterSparseThreshold = 0.55f;
    [Range(0f, 1f)]
    public float forestClusterMediumThreshold = 0.70f;
    [Range(0f, 1f)]
    public float forestClusterDenseThreshold = 0.85f;

    // Префабы-оверлеи для кластеров (отдельно от обычного леса)
    public GameObject forestSparseOverlayCluster;
    public GameObject forestMediumOverlayCluster;
    public GameObject forestDenseOverlayCluster;
    public float forestOverlayYCluster = 0f;

    // Буферы- просветы (в гекса̀х) — отдельно для кластерного слоя
    [Range(0, 3)]
    public int forestBufferFromRiverCluster = 1;
    [Range(0, 3)]
    public int forestBufferFromRoadCluster = 1;
    [Range(0, 3)]
    public int forestBufferFromBonusCluster = 1;

    // «Одиночки» вне массивов (редкие вкрапления)
    [Range(0f, 1f)]
    public float forestSinglesChanceCluster = 0.35f;

    // Отдельный сид/сдвиг для кластерного леса
    public Vector2 forestClusterOffset = new Vector2(4321.0f, 765.0f);

    [System.Serializable]
    public class ForestOverlaySet
    {
        public GameObject sparse;
        public GameObject medium;
        public GameObject dense;
    }

    [Header("Forest Cluster Sets")]
    public ForestOverlaySet forestSetA; // trees_A_small/medium/large
    public ForestOverlaySet forestSetB; // trees_B_small/medium/large
    [Range(0f, 1f)] public float forestSetAProbability = 0.5f; // шанс выбрать A (иначе B)

    [Header("Forest Cluster Seeding")]
    public bool randomizeForestClusterSeed = true; // рандомизировать при старте
    public int forestClusterSeed = 0;             // сохранённый сид (для отладки)
    public Vector2 forestClusterBaseOffset = new Vector2(4321f, 765f); // базовый оффсет
    public float forestClusterSeedJitter = 1000f; // «амплитуда» сдвига
}

