using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class NetworkNode : MonoBehaviour
{
    [Header("Node Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Repair")]
    [SerializeField] private KeyCode repairKey = KeyCode.E;
    [SerializeField] private float fullRepairTime = 4.5f;
    [SerializeField] private Transform channelLockPoint;

    [Header("Prompt")]
    [SerializeField] private Transform promptAnchor;
    [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 1.25f, 0f);
    private NodeInteractionPromptUI promptUI;

    [Header("Debug")]
    [SerializeField] private bool startFullyRepaired = true;

    private float repairRatePerSecond;
    private PlayerController playerInRange;
    private bool isChanneling;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthNormalized => maxHealth <= 0 ? 0f : currentHealth / maxHealth;
    public bool IsDestroyed => currentHealth <= 0.001f;
    public bool IsFullyHealed => currentHealth >= maxHealth - 0.001f;

    public bool IsPlayerInRange => playerInRange != null;
    public bool CanShowRepairPrompt => playerInRange != null && !IsFullyHealed;
    public string RepairPromptText => $"Hold {repairKey}";
    public Vector3 PromptWorldPosition =>
        (promptAnchor != null ? promptAnchor.position : transform.position) + promptWorldOffset;

    public event Action<NetworkNode> OnNodeHealthChanged;
    public event Action<NetworkNode> OnPromptStateChanged;

    private void Awake()
    {
        repairRatePerSecond = maxHealth / fullRepairTime;

        currentHealth = startFullyRepaired ? maxHealth : 0f;

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.RegisterNode(this);
        }

        NotifyPromptStateChanged();
    }

    private void Start()
    {
        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.RegisterNode(this);
        }

        if (promptUI == null)
        {
            promptUI = FindFirstObjectByType<NodeInteractionPromptUI>();
        }

        NotifyPromptStateChanged();
    }

    private void OnDisable()
    {
        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.UnregisterNode(this);
        }

        NotifyPromptStateChanged();
    }

    private void Update()
    {
        if (playerInRange == null)
        {
            StopChanneling();
            return;
        }

        bool canRepair = !IsFullyHealed;
        bool holdingRepair = Input.GetKey(repairKey);

        if (holdingRepair && canRepair)
        {
            StartChanneling();
            Heal(repairRatePerSecond * Time.deltaTime);
        }
        else
        {
            StopChanneling();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            playerInRange = pc;
            NotifyPromptStateChanged();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null && pc == playerInRange)
        {
            StopChanneling();
            playerInRange = null;
            NotifyPromptStateChanged();
        }
    }

    private void StartChanneling()
    {
        if (playerInRange == null) return;

        if (!isChanneling)
        {
            isChanneling = true;

            Vector3 lockPos = channelLockPoint != null
                ? channelLockPoint.position
                : playerInRange.transform.position;

            playerInRange.SetControlLock(true, lockPos);
        }
    }

    private void StopChanneling()
    {
        if (!isChanneling) return;

        isChanneling = false;

        if (playerInRange != null)
        {
            playerInRange.SetControlLock(false);
        }
    }

    public float Damage(float amount)
    {
        if (amount <= 0f || IsDestroyed) return 0f;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        float actualDamage = oldHealth - currentHealth;

        NotifyHealthChanged();
        return actualDamage;
    }

    public float Heal(float amount)
    {
        if (amount <= 0f || IsFullyHealed) return 0f;

        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        float actualHealing = currentHealth - oldHealth;

        NotifyHealthChanged();
        return actualHealing;
    }

    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void RestoreFully()
    {
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    private void NotifyHealthChanged()
    {
        OnNodeHealthChanged?.Invoke(this);
        NotifyPromptStateChanged();

        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.NotifyNodeHealthChanged(this);
        }
    }

    private void NotifyPromptStateChanged()
    {
        OnPromptStateChanged?.Invoke(this);

        if (promptUI == null)
        {
            promptUI = FindFirstObjectByType<NodeInteractionPromptUI>();
        }

        if (promptUI == null) return;

        if (CanShowRepairPrompt)
        {
            promptUI.ShowForNode(this);
        }
        else
        {
            promptUI.ClearNode(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 lockPos = channelLockPoint != null ? channelLockPoint.position : transform.position;
        Gizmos.DrawWireSphere(lockPos, 0.15f);

        Gizmos.color = Color.cyan;
        Vector3 promptPos = (promptAnchor != null ? promptAnchor.position : transform.position) + promptWorldOffset;
        Gizmos.DrawWireSphere(promptPos, 0.12f);
    }
}