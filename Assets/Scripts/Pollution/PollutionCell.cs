using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PollutionCell : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float playerDamagePerSecond = 12f;
    [SerializeField] private float nodeDamagePerSecond = 18f;

    [Header("Visual Pulse")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float minAlpha = 0.28f;
    [SerializeField] private float maxAlpha = 0.45f;
    [SerializeField] private float minScale = 0.92f;
    [SerializeField] private float maxScale = 1.08f;

    private PollutionManager manager;
    private Vector2Int gridPos;

    private readonly HashSet<PlayerController> playersInside = new();
    private readonly HashSet<NetworkNode> nodesInside = new();

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
        ApplyDamage(Time.deltaTime);
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

    private void ApplyDamage(float dt)
    {
        if (dt <= 0f)
            return;

        if (playersInside.Count > 0 && FungalNetworkManager.Instance != null)
        {
            FungalNetworkManager.Instance.ApplyDirectPlayerDamage(playerDamagePerSecond * dt);
        }

        if (nodesInside.Count > 0)
        {
            foreach (NetworkNode node in nodesInside)
            {
                if (node != null && !node.IsDestroyed)
                {
                    node.Damage(nodeDamagePerSecond * dt);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
                playersInside.Add(pc);
        }

        NetworkNode node = other.GetComponent<NetworkNode>();
        if (node != null)
            nodesInside.Add(node);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
                playersInside.Remove(pc);
        }

        NetworkNode node = other.GetComponent<NetworkNode>();
        if (node != null)
            nodesInside.Remove(node);
    }

    private void OnDisable()
    {
        playersInside.Clear();
        nodesInside.Clear();
    }
}