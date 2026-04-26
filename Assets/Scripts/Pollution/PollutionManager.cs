using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PollutionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D pollutionBounds;
    [SerializeField] private PollutionCell pollutionCellPrefab;
    [SerializeField] private FungalNetworkManager networkManager;
    [SerializeField] private PollutionClearCinematicController clearCinematicController;

    [Header("Grid")]
    [SerializeField] private float cellSize = 2f;

    [Header("Startup")]
    [SerializeField] private int initialSeedCount = 4;

    [Header("Spread")]
    [SerializeField] private float baseSpreadInterval = 2.5f;
    [SerializeField] private float minSpreadInterval = 0.9f;
    [SerializeField] private float baseSpreadChance = 0.18f;
    [SerializeField] private float extraSpreadChanceAtMaxCollapse = 0.22f;
    [SerializeField] private bool use8Directions = false;

    [Header("Safe Area Cleanup")]
    [SerializeField] private bool clearProtectedAreasAtStart = true;
    [SerializeField] private int randomRemoteClearRadiusInCells = 2;
    [SerializeField] private int randomRemoteClearAttempts = 20;
    [SerializeField] private float randomRemoteMinDistanceFromNode = 8f;

    [Header("Cleanup")]
    [SerializeField] private bool clearAllPollutionWhenAllNodesRestored = true;
    private bool finalPollutionClearStarted;

    private readonly Dictionary<Vector2Int, PollutionCell> activeCells = new();
    private readonly HashSet<Vector2Int> validGridCells = new();
    private readonly HashSet<NetworkNode> subscribedNodes = new();

    public event System.Action<float> OnPollutionCoverageChanged;

    public float PollutionCoverageNormalized
    {
        get
        {
            if (validGridCells.Count == 0)
                return 0f;

            return Mathf.Clamp01((float)activeCells.Count / validGridCells.Count);
        }
    }

    public float PollutionCoveragePercent => PollutionCoverageNormalized * 100f;

    public bool IsPollutionGridFull =>
        validGridCells.Count > 0 && activeCells.Count >= validGridCells.Count;

    private void BroadcastPollutionCoverageChanged()
    {
        OnPollutionCoverageChanged?.Invoke(PollutionCoverageNormalized);
    }

    private static readonly Vector2Int[] CardinalDirs =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private static readonly Vector2Int[] DiagonalDirs =
    {
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<FungalNetworkManager>();
    }

    private void Start()
    {
        if (pollutionBounds == null)
        {
            Debug.LogError("PollutionManager requires a bounds collider.");
            enabled = false;
            return;
        }

        BuildValidGrid();
        SubscribeToNodes();

        SpawnInitialSeeds();

        if (networkManager != null)
            networkManager.OnNetworkHealthChanged += HandleNetworkHealthChanged;

        if (clearProtectedAreasAtStart)
        {
            ClearAllProtectedAreas();
        }

        StartCoroutine(SpreadRoutine());
    }

    private void OnDestroy()
    {
        UnsubscribeFromNodes();

        if (networkManager != null)
            networkManager.OnNetworkHealthChanged -= HandleNetworkHealthChanged;
    }

    private void HandleNetworkHealthChanged(float normalizedHealth)
    {
        TryClearAllPollutionIfAllNodesRestored();
    }

    private IEnumerator SpreadRoutine()
    {
        while (true)
        {
            float interval = GetCurrentSpreadInterval();
            yield return new WaitForSeconds(interval);

            if (activeCells.Count == 0)
                continue;

            SpreadStep();
        }
    }

    private void SubscribeToNodes()
    {
        subscribedNodes.Clear();

        if (networkManager == null)
            return;

        for (int i = 0; i < networkManager.Nodes.Count; i++)
        {
            NetworkNode node = networkManager.Nodes[i];
            if (node == null || subscribedNodes.Contains(node))
                continue;

            node.OnReachedFullHealth += HandleNodeReachedFullHealth;
            subscribedNodes.Add(node);
        }
    }

    private void UnsubscribeFromNodes()
    {
        foreach (NetworkNode node in subscribedNodes)
        {
            if (node != null)
                node.OnReachedFullHealth -= HandleNodeReachedFullHealth;
        }

        subscribedNodes.Clear();
    }

    private void HandleNodeReachedFullHealth(NetworkNode node)
    {
        if (node == null)
            return;

        if (networkManager != null && networkManager.AllNodesFullyRestored)
        {
            TryClearAllPollutionIfAllNodesRestored();
            return;
        }

        if (clearCinematicController != null)
        {
            clearCinematicController.PlayForNode(node);
        }
        else
        {
            ClearProtectedArea(node);
        }

        ClearRandomRemoteArea(node, randomRemoteClearRadiusInCells, randomRemoteClearAttempts);

        if (clearAllPollutionWhenAllNodesRestored &&
            networkManager != null &&
            networkManager.AllNodesFullyRestored)
        {
            if (clearCinematicController != null)
            {
                clearCinematicController.PlayForAllPollution();
            }
            else
            {
                ClearAllPollution();
            }

            return;
        }
    }

    private void BuildValidGrid()
    {
        validGridCells.Clear();

        Bounds b = pollutionBounds.bounds;

        int minX = Mathf.FloorToInt(b.min.x / cellSize);
        int maxX = Mathf.CeilToInt(b.max.x / cellSize);
        int minY = Mathf.FloorToInt(b.min.y / cellSize);
        int maxY = Mathf.CeilToInt(b.max.y / cellSize);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector2 worldPos = GridToWorld(gridPos);

                if (pollutionBounds.OverlapPoint(worldPos))
                {
                    validGridCells.Add(gridPos);
                }
            }
        }
    }

    private void SpawnInitialSeeds()
    {
        if (validGridCells.Count == 0)
        {
            Debug.LogWarning("No valid pollution cells found inside bounds.");
            return;
        }

        List<Vector2Int> cellPool = new(validGridCells);

        for (int i = 0; i < initialSeedCount && cellPool.Count > 0; i++)
        {
            int index = Random.Range(0, cellPool.Count);
            Vector2Int randomCell = cellPool[index];
            cellPool.RemoveAt(index);

            if (!HasCell(randomCell) && !IsProtectedCell(randomCell))
            {
                SpawnCell(randomCell);
            }
        }
    }

    private void SpreadStep()
    {
        List<Vector2Int> currentCells = new(activeCells.Keys);
        List<Vector2Int> cellsToSpawn = new();

        float spreadChance = GetCurrentSpreadChance();

        for (int i = 0; i < currentCells.Count; i++)
        {
            Vector2Int origin = currentCells[i];

            TryAddNeighbor(origin, CardinalDirs, spreadChance, cellsToSpawn);

            if (use8Directions)
                TryAddNeighbor(origin, DiagonalDirs, spreadChance, cellsToSpawn);
        }

        for (int i = 0; i < cellsToSpawn.Count; i++)
        {
            Vector2Int pos = cellsToSpawn[i];

            if (!HasCell(pos) && !IsProtectedCell(pos))
                SpawnCell(pos);
        }
    }

    private void TryAddNeighbor(
        Vector2Int origin,
        Vector2Int[] dirs,
        float spreadChance,
        List<Vector2Int> cellsToSpawn)
    {
        for (int i = 0; i < dirs.Length; i++)
        {
            if (Random.value > spreadChance)
                continue;

            Vector2Int next = origin + dirs[i];

            if (!CanSpawnPollutionAt(next))
                continue;

            if (!cellsToSpawn.Contains(next))
                cellsToSpawn.Add(next);
        }
    }

    private bool HasAnyFreeUnprotectedCell()
    {
        foreach (Vector2Int cell in validGridCells)
        {
            if (!HasCell(cell) && !IsProtectedCell(cell))
                return true;
        }

        return false;
    }

    private bool CanSpawnPollutionAt(Vector2Int gridPos)
    {
        if (!IsValidGridCell(gridPos))
            return false;

        if (HasCell(gridPos))
            return false;

        if (!IsProtectedCell(gridPos))
            return true;

        return !HasAnyFreeUnprotectedCell();
    }

    private PollutionCell SpawnCell(Vector2Int gridPos)
    {
        if (!CanSpawnPollutionAt(gridPos))
            return null;

        Vector3 worldPos = GridToWorld(gridPos);
        PollutionCell cell = Instantiate(pollutionCellPrefab, worldPos, Quaternion.identity, transform);
        cell.Initialize(this, gridPos);
        activeCells.Add(gridPos, cell);
        BroadcastPollutionCoverageChanged();
        return cell;
    }

    public void RemoveCell(Vector2Int gridPos)
    {
        if (!activeCells.TryGetValue(gridPos, out PollutionCell cell))
            return;

        activeCells.Remove(gridPos);
        BroadcastPollutionCoverageChanged();

        if (cell != null)
            Destroy(cell.gameObject);
    }

    public void ClearRandomCells(int amount)
    {
        if (amount <= 0 || activeCells.Count == 0)
            return;

        List<Vector2Int> keys = new(activeCells.Keys);

        for (int i = 0; i < amount && keys.Count > 0; i++)
        {
            int index = Random.Range(0, keys.Count);
            Vector2Int selected = keys[index];
            keys.RemoveAt(index);

            RemoveCell(selected);
        }
    }

    public void ClearAllPollution()
    {
        List<Vector2Int> keys = new(activeCells.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            RemoveCell(keys[i]);
        }
    }

    public void ClearProtectedArea(NetworkNode node)
    {
        if (node == null)
            return;

        List<Vector2Int> toRemove = new();

        foreach (Vector2Int cell in activeCells.Keys)
        {
            if (IsCellInsideNodeSafeArea(cell, node))
                toRemove.Add(cell);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            RemoveCell(toRemove[i]);
        }
    }

    public void ClearAllProtectedAreas()
    {
        if (networkManager == null)
            return;

        for (int i = 0; i < networkManager.Nodes.Count; i++)
        {
            NetworkNode node = networkManager.Nodes[i];
            if (node != null && node.IsFullyHealed)
            {
                ClearProtectedArea(node);
            }
        }
    }

    public void ClearRandomRemoteArea(NetworkNode sourceNode, int radiusInCells, int attempts = 20)
    {
        if (validGridCells.Count == 0)
            return;

        List<Vector2Int> candidates = new(validGridCells);

        for (int i = 0; i < attempts && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            Vector2Int center = candidates[index];
            candidates.RemoveAt(index);

            Vector3 worldCenter = GridToWorld(center);

            if (sourceNode != null)
            {
                float distance = Vector2.Distance(sourceNode.WorldPosition, worldCenter);
                if (distance < randomRemoteMinDistanceFromNode)
                    continue;
            }

            ClearArea(center, radiusInCells);
            return;
        }
    }

    public List<PollutionCell> GetProtectedAreaCells(NetworkNode node)
    {
        List<PollutionCell> cells = new();

        if (node == null)
            return cells;

        foreach (var pair in activeCells)
        {
            if (IsCellInsideNodeSafeArea(pair.Key, node))
                cells.Add(pair.Value);
        }

        return cells;
    }

    public List<PollutionCell> GetAllActiveCells()
    {
        return new List<PollutionCell>(activeCells.Values);
    }

    public void RemoveCellsFromTracking(List<PollutionCell> cells)
    {
        List<Vector2Int> toRemove = new();

        foreach (var pair in activeCells)
        {
            if (cells.Contains(pair.Value))
                toRemove.Add(pair.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            activeCells.Remove(toRemove[i]);
        }
    }

    public void ClearArea(Vector2Int center, int radiusInCells)
    {
        if (radiusInCells < 0)
            return;

        List<Vector2Int> toRemove = new();

        foreach (Vector2Int cell in activeCells.Keys)
        {
            if (Mathf.Abs(cell.x - center.x) <= radiusInCells &&
                Mathf.Abs(cell.y - center.y) <= radiusInCells)
            {
                toRemove.Add(cell);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            RemoveCell(toRemove[i]);
        }
    }

    public bool IsProtectedCell(Vector2Int gridPos)
    {
        if (networkManager == null)
            return false;

        for (int i = 0; i < networkManager.Nodes.Count; i++)
        {
            NetworkNode node = networkManager.Nodes[i];
            if (node == null || !node.IsFullyHealed)
                continue;

            if (IsCellInsideNodeSafeArea(gridPos, node))
                return true;
        }

        return false;
    }

    public void ClearAllPollutionWithCinematic()
    {
        if (clearCinematicController != null)
        {
            clearCinematicController.PlayForAllPollution();
        }
        else
        {
            ClearAllPollution();
        }
    }

    private bool IsCellInsideNodeSafeArea(Vector2Int gridPos, NetworkNode node)
    {
        Vector2 worldPos = GridToWorld(gridPos);
        return Vector2.Distance(worldPos, node.WorldPosition) <= node.SafeAreaRadius;
    }

    public bool HasCell(Vector2Int gridPos)
    {
        return activeCells.ContainsKey(gridPos);
    }

    public bool IsValidGridCell(Vector2Int gridPos)
    {
        return validGridCells.Contains(gridPos);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize + cellSize * 0.5f,
            gridPos.y * cellSize + cellSize * 0.5f,
            0f
        );
    }

    public float GetCurrentSpreadInterval()
    {
        float collapse = GetNetworkCollapseRatio();
        return Mathf.Lerp(baseSpreadInterval, minSpreadInterval, collapse);
    }

    public float GetCurrentSpreadChance()
    {
        float collapse = GetNetworkCollapseRatio();
        return baseSpreadChance + extraSpreadChanceAtMaxCollapse * collapse;
    }

    private float GetNetworkCollapseRatio()
    {
        if (networkManager == null)
            return 0f;

        return 1f - networkManager.NetworkHealthNormalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (pollutionBounds == null)
            return;

        Gizmos.color = new Color(0.3f, 0.9f, 0.3f, 0.4f);
        Bounds b = pollutionBounds.bounds;
        Gizmos.DrawWireCube(b.center, b.size);
    }

    private void TryClearAllPollutionIfAllNodesRestored()
    {
        if (finalPollutionClearStarted)
            return;

        if (!clearAllPollutionWhenAllNodesRestored)
            return;

        if (networkManager == null || !networkManager.AllNodesFullyRestored)
            return;

        finalPollutionClearStarted = true;

        if (clearCinematicController != null)
            clearCinematicController.PlayForAllPollution();
        else
            ClearAllPollution();
    }
}