using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PollutionCell : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damagePulseInterval = 2.0f;
    //[SerializeField] private float playerDamagePerPulse = 5f;
    [SerializeField] private float nodeDamagePerPulse = 5f;
    private float damagePulseTimer;

    [Header("Visual Pulse")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 0.65f;
    [SerializeField] private float minScale = 6f;
    [SerializeField] private float maxScale = 9f;

    private PollutionManager manager;
    private Vector2Int gridPos;

    //private readonly HashSet<PlayerController> playersInside = new();
    private readonly HashSet<NetworkNode> nodesInside = new();

    //private readonly List<PlayerController> playerSnapshot = new();
    private readonly List<NetworkNode> nodeSnapshot = new();

    public void Initialize(PollutionManager pollutionManager, Vector2Int position)
    {
        manager = pollutionManager;
        gridPos = position;
    }

    private void Reset()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
    }

    private void Update()
    {
        AnimateVisual();
        UpdateDamagePulse(Time.deltaTime);
    }

    private void AnimateVisual()
    {
        if (visualRenderer == null)
            return;

        float t = (Mathf.Sin(Time.time * pulseSpeed + transform.position.x * 0.7f + transform.position.y * 0.35f) + 1f) * 0.5f;

        Color c = visualRenderer.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        visualRenderer.color = c;

        float scale = Mathf.Lerp(minScale, maxScale, t);
        visualRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdateDamagePulse(float dt)
    {
        if (dt <= 0f || damagePulseInterval <= 0f)
            return;

        damagePulseTimer += dt;

        while (damagePulseTimer >= damagePulseInterval)
        {
            damagePulseTimer -= damagePulseInterval;
            ApplyDamagePulse();
        }
    }

    private void ApplyDamagePulse()
    {
        //if (playersInside.Count > 0 && FungalNetworkManager.Instance != null)
        //{
        //    playerSnapshot.Clear();
        //    playerSnapshot.AddRange(playersInside);

        //    if (playerSnapshot.Count > 0)
        //    {
        //        FungalNetworkManager.Instance.ApplyDirectPlayerDamage(playerDamagePerPulse);
        //    }
        //}

        if (nodesInside.Count > 0)
        {
            nodeSnapshot.Clear();
            nodeSnapshot.AddRange(nodesInside);

            for (int i = 0; i < nodeSnapshot.Count; i++)
            {
                NetworkNode node = nodeSnapshot[i];

                if (node == null || !node.isActiveAndEnabled)
                    continue;

                if (!node.IsDestroyed)
                {
                    if (!node.IsBeingActivelyRepaired)
                    {
                        node.Damage(nodeDamagePerPulse);

                        if (this == null || !isActiveAndEnabled)
                            return;
                    }
                }
            }
        }
    }

    public IEnumerator FadeOutAndDestroy(float duration)
    {
        if (visualRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Color startColor = visualRenderer.color;
        Vector3 startScale = visualRenderer.transform.localScale;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / duration);

            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, progress);
            visualRenderer.color = c;

            visualRenderer.transform.localScale = Vector3.Lerp(
                startScale,
                startScale * 1.25f,
                progress
            );

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //if (other.CompareTag("Player"))
        //{
        //    PlayerController pc = other.GetComponent<PlayerController>();
        //    if (pc != null)
        //        playersInside.Add(pc);
        //}

        NetworkNode node = other.GetComponent<NetworkNode>();
        if (node != null)
            nodesInside.Add(node);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //if (other.CompareTag("Player"))
        //{
        //    PlayerController pc = other.GetComponent<PlayerController>();
        //    if (pc != null)
        //        playersInside.Remove(pc);
        //}

        NetworkNode node = other.GetComponent<NetworkNode>();
        if (node != null)
            nodesInside.Remove(node);
    }

    private void OnDisable()
    {
        damagePulseTimer = 0f;
        //playersInside.Clear();
        nodesInside.Clear();
        //playerSnapshot.Clear();
        nodeSnapshot.Clear();
    }
}