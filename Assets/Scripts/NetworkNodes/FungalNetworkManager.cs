using UnityEngine;
using System.Collections.Generic;

public class FungalNetworkManager : MonoBehaviour
{
    public static FungalNetworkManager Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private PlayerController player;

    [Header("Scaling")]
    [SerializeField] private float minMoveMultiplier = 0.7f;
    [SerializeField] private float maxMoveMultiplier = 1.3f;
    [SerializeField] private float minJumpMultiplier = 0.7f;
    [SerializeField] private float maxJumpMultiplier = 1.25f;

    [Header("Debug")]
    [SerializeField] private bool autoFindPlayer = true;

    private readonly List<NetworkNode> nodes = new();

    public IReadOnlyList<NetworkNode> Nodes => nodes;

    public float TotalMaxHealth
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < nodes.Count; i++)
            {
                total += nodes[i].MaxHealth;
            }
            return total;
        }
    }

    public float TotalCurrentHealth
    {
        get
        {
            float total = 0f;
            for (int i = 0; i < nodes.Count; i++)
            {
                total += nodes[i].CurrentHealth;
            }
            return total;
        }
    }

    public float NetworkHealthNormalized
    {
        get
        {
            float max = TotalMaxHealth;
            if (max <= 0f) return 0f;
            return TotalCurrentHealth / max;
        }
    }

    public float NetworkHealthPercent => NetworkHealthNormalized * 100f;

    public bool AllNodesFullyRestored
    {
        get
        {
            if (nodes.Count == 0) return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!nodes[i].IsFullyHealed)
                    return false;
            }

            return true;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (autoFindPlayer && player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }
    }

    private void Start()
    {
        ApplyPlayerScaling();
    }

    public void RegisterNode(NetworkNode node)
    {
        if (node == null || nodes.Contains(node)) return;

        nodes.Add(node);
        ApplyPlayerScaling();
    }

    public void UnregisterNode(NetworkNode node)
    {
        if (node == null) return;

        if (nodes.Remove(node))
        {
            ApplyPlayerScaling();
        }
    }

    public void NotifyNodeHealthChanged(NetworkNode changedNode)
    {
        ApplyPlayerScaling();

        // Future additions:
        // - tell pollution manager to change spread speed
        // - update UI
        // - check win condition
        if (AllNodesFullyRestored)
        {
            Debug.Log("All nodes restored. Trigger win state and clear pollution.");
        }
    }

    public void ApplyDirectPlayerDamage(float damageAmount)
    {
        if (damageAmount <= 0f) return;

        List<NetworkNode> livingNodes = GetLivingNodes();
        if (livingNodes.Count == 0) return;

        float remaining = damageAmount;
        int safety = 0;

        while (remaining > 0.001f && livingNodes.Count > 0 && safety < 100)
        {
            safety++;
            float split = remaining / livingNodes.Count;
            float dealtThisPass = 0f;

            for (int i = livingNodes.Count - 1; i >= 0; i--)
            {
                NetworkNode node = livingNodes[i];
                float actual = node.Damage(split);
                dealtThisPass += actual;

                if (node.IsDestroyed)
                {
                    livingNodes.RemoveAt(i);
                }
            }

            if (dealtThisPass <= 0.001f)
                break;

            remaining -= dealtThisPass;
        }
    }

    public void ApplyDirectPlayerDamagePercent(float percentOfTotalNetwork)
    {
        float totalDamage = TotalMaxHealth * Mathf.Clamp01(percentOfTotalNetwork / 100f);
        ApplyDirectPlayerDamage(totalDamage);
    }

    public void HealAllNodes(float amount)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Heal(amount);
        }

        ApplyPlayerScaling();
    }

    public void DamageAllNodes(float amount)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].Damage(amount);
        }

        ApplyPlayerScaling();
    }

    public void RestoreAllNodesFully()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].RestoreFully();
        }

        ApplyPlayerScaling();
    }

    public void ApplyPlayerScaling()
    {
        if (player == null) return;

        float t = NetworkHealthNormalized;

        float moveMult = Mathf.Lerp(minMoveMultiplier, maxMoveMultiplier, t);
        float jumpMult = Mathf.Lerp(minJumpMultiplier, maxJumpMultiplier, t);

        player.SetMovementMultiplier(moveMult);
        player.SetJumpMultiplier(jumpMult);
    }

    private List<NetworkNode> GetLivingNodes()
    {
        List<NetworkNode> living = new();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (!nodes[i].IsDestroyed)
            {
                living.Add(nodes[i]);
            }
        }

        return living;
    }
}
