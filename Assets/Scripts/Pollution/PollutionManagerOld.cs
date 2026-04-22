//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class PollutionManagerOld : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private Collider2D pollutionBounds;
//    [SerializeField] private PollutionCell pollutionCellPrefab;
//    [SerializeField] private FungalNetworkManager networkManager;

//    [Header("Grid")]
//    [SerializeField] private float cellSize = 2f;

//    [Header("Startup")]
//    [SerializeField] private int initialSeedCount = 5;
//    // [SerializeField] private int maxInitialSeedAttempts = 100;

//    [Header("Spread")]
//    [SerializeField] private float baseSpreadInterval = 1.25f;
//    [SerializeField] private float minSpreadInterval = 0.25f;
//    [SerializeField] private float baseSpreadChance = 0.35f;
//    [SerializeField] private float extraSpreadChanceAtMaxCollapse = 0.4f;
//    [SerializeField] private bool use8Directions = false;

//    [Header("Cleanup")]
//    [SerializeField] private bool clearAllPollutionWhenAllNodesRestored = true;

//    private readonly Dictionary<Vector2Int, PollutionCell> activeCells = new();
//    private readonly HashSet<Vector2Int> validGridCells = new();

//    private bool networkHasBeenDamagedAtLeastOnce = false;

//    private static readonly Vector2Int[] CardinalDirs =
//    {
//        new Vector2Int(1, 0),
//        new Vector2Int(-1, 0),
//        new Vector2Int(0, 1),
//        new Vector2Int(0, -1)
//    };

//    private static readonly Vector2Int[] DiagonalDirs =
//    {
//        new Vector2Int(1, 1),
//        new Vector2Int(1, -1),
//        new Vector2Int(-1, 1),
//        new Vector2Int(-1, -1)
//    };

//    private void Awake()
//    {
//        if (networkManager == null)
//            networkManager = FindFirstObjectByType<FungalNetworkManager>();
//    }

//    private void Start()
//    {
//        if (pollutionBounds == null)
//        {
//            Debug.LogError("PollutionManager requires a bounds collider.");
//            enabled = false;
//            return;
//        }

//        BuildValidGrid();
//        SpawnInitialSeeds();
//        StartCoroutine(SpreadRoutine());
//    }

//    private void Update()
//    {
//        if (networkManager != null && !networkHasBeenDamagedAtLeastOnce)
//        {
//            if (networkManager.NetworkHealthNormalized < 0.999f)
//            {
//                networkHasBeenDamagedAtLeastOnce = true;
//            }
//        }

//        if (clearAllPollutionWhenAllNodesRestored &&
//            networkHasBeenDamagedAtLeastOnce &&
//            networkManager != null &&
//            networkManager.AllNodesFullyRestored &&
//            activeCells.Count > 0)
//        {
//            ClearAllPollution();
//        }
//    }

//    private IEnumerator SpreadRoutine()
//    {
//        while (true)
//        {
//            float interval = GetCurrentSpreadInterval();
//            yield return new WaitForSeconds(interval);

//            if (activeCells.Count == 0)
//                continue;

//            SpreadStep();
//        }
//    }

//    private void BuildValidGrid()
//    {
//        validGridCells.Clear();

//        Bounds b = pollutionBounds.bounds;

//        int minX = Mathf.FloorToInt(b.min.x / cellSize);
//        int maxX = Mathf.CeilToInt(b.max.x / cellSize);
//        int minY = Mathf.FloorToInt(b.min.y / cellSize);
//        int maxY = Mathf.CeilToInt(b.max.y / cellSize);

//        for (int x = minX; x <= maxX; x++)
//        {
//            for (int y = minY; y <= maxY; y++)
//            {
//                Vector2Int gridPos = new Vector2Int(x, y);
//                Vector2 worldPos = GridToWorld(gridPos);

//                if (pollutionBounds.OverlapPoint(worldPos))
//                {
//                    validGridCells.Add(gridPos);
//                }
//            }
//        }
//    }

//    private void SpawnInitialSeeds()
//    {
//        if (validGridCells.Count == 0)
//        {
//            Debug.LogWarning("No valid pollution cells found inside bounds.");
//            return;
//        }

//        List<Vector2Int> cellPool = new(validGridCells);

//        for (int i = 0; i < initialSeedCount && cellPool.Count > 0; i++)
//        {
//            int index = Random.Range(0, cellPool.Count);
//            Vector2Int randomCell = cellPool[index];
//            cellPool.RemoveAt(index);

//            if (!HasCell(randomCell))
//            {
//                SpawnCell(randomCell);
//            }
//        }
//    }

//    private void SpreadStep()
//    {
//        List<Vector2Int> currentCells = new(activeCells.Keys);
//        List<Vector2Int> cellsToSpawn = new();

//        float spreadChance = GetCurrentSpreadChance();

//        for (int i = 0; i < currentCells.Count; i++)
//        {
//            Vector2Int origin = currentCells[i];

//            TryAddNeighbor(origin, CardinalDirs, spreadChance, cellsToSpawn);

//            if (use8Directions)
//                TryAddNeighbor(origin, DiagonalDirs, spreadChance, cellsToSpawn);
//        }

//        for (int i = 0; i < cellsToSpawn.Count; i++)
//        {
//            if (!HasCell(cellsToSpawn[i]))
//                SpawnCell(cellsToSpawn[i]);
//        }
//    }

//    private void TryAddNeighbor(
//        Vector2Int origin,
//        Vector2Int[] dirs,
//        float spreadChance,
//        List<Vector2Int> cellsToSpawn)
//    {
//        for (int i = 0; i < dirs.Length; i++)
//        {
//            if (Random.value > spreadChance)
//                continue;

//            Vector2Int next = origin + dirs[i];

//            if (!IsValidGridCell(next))
//                continue;

//            if (HasCell(next))
//                continue;

//            if (!cellsToSpawn.Contains(next))
//                cellsToSpawn.Add(next);
//        }
//    }

//    private PollutionCell SpawnCell(Vector2Int gridPos)
//    {
//        Vector3 worldPos = GridToWorld(gridPos);
//        PollutionCell cell = Instantiate(pollutionCellPrefab, worldPos, Quaternion.identity, transform);
//        cell.Initialize(this, gridPos);
//        activeCells.Add(gridPos, cell);
//        return cell;
//    }

//    public void RemoveCell(Vector2Int gridPos)
//    {
//        if (!activeCells.TryGetValue(gridPos, out PollutionCell cell))
//            return;

//        activeCells.Remove(gridPos);

//        if (cell != null)
//            Destroy(cell.gameObject);
//    }

//    public void ClearRandomCells(int amount)
//    {
//        if (amount <= 0 || activeCells.Count == 0)
//            return;

//        List<Vector2Int> keys = new(activeCells.Keys);

//        for (int i = 0; i < amount && keys.Count > 0; i++)
//        {
//            int index = Random.Range(0, keys.Count);
//            Vector2Int selected = keys[index];
//            keys.RemoveAt(index);

//            RemoveCell(selected);
//        }
//    }

//    public void ClearAllPollution()
//    {
//        List<Vector2Int> keys = new(activeCells.Keys);
//        for (int i = 0; i < keys.Count; i++)
//        {
//            RemoveCell(keys[i]);
//        }
//    }

//    public bool HasCell(Vector2Int gridPos)
//    {
//        return activeCells.ContainsKey(gridPos);
//    }

//    public bool IsValidGridCell(Vector2Int gridPos)
//    {
//        return validGridCells.Contains(gridPos);
//    }

//    public Vector3 GridToWorld(Vector2Int gridPos)
//    {
//        return new Vector3(
//            gridPos.x * cellSize + cellSize * 0.5f,
//            gridPos.y * cellSize + cellSize * 0.5f,
//            0f
//        );
//    }

//    public float GetCurrentSpreadInterval()
//    {
//        float collapse = GetDestroyedNodeRatio();
//        return Mathf.Lerp(baseSpreadInterval, minSpreadInterval, collapse);
//    }

//    public float GetCurrentSpreadChance()
//    {
//        float collapse = GetDestroyedNodeRatio();
//        return baseSpreadChance + extraSpreadChanceAtMaxCollapse * collapse;
//    }

//    private float GetDestroyedNodeRatio()
//    {
//        if (networkManager == null || networkManager.Nodes.Count == 0)
//            return 0f;

//        int destroyed = 0;
//        int total = networkManager.Nodes.Count;

//        for (int i = 0; i < total; i++)
//        {
//            if (networkManager.Nodes[i].IsDestroyed)
//                destroyed++;
//        }

//        return total <= 0 ? 0f : (float)destroyed / total;
//    }

//    private void OnDrawGizmosSelected()
//    {
//        if (pollutionBounds == null)
//            return;

//        Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.4f);
//        Bounds b = pollutionBounds.bounds;
//        Gizmos.DrawWireCube(b.center, b.size);
//    }
//}