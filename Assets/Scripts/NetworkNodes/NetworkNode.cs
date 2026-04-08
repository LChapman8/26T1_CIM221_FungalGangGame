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

    public event Action<NetworkNode> OnNodeHealthChanged;

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
    }

    private void Start()
    {
        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.RegisterNode(this);
        }
    }

    private void OnDisable()
    {
        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.UnregisterNode(this);
        }
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

        if (FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.NotifyNodeHealthChanged(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = channelLockPoint != null ? channelLockPoint.position : transform.position;
        Gizmos.DrawWireSphere(pos, 0.15f);
    }
}