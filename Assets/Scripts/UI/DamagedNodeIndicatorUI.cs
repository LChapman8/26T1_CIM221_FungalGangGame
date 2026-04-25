using System.Collections.Generic;
using UnityEngine;

public class DamagedNodeIndicatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FungalNetworkManager networkManager;
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform arrowPrefab;
    [SerializeField] private RectTransform arrowContainer;
    [SerializeField] private PollutionClearCinematicController cinematicController;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothSpeed = 12f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Overlap")]
    [SerializeField] private float overlapRadius = 48f;

    private struct ArrowCandidate
    {
        public NetworkNode node;
        public Vector2 edgePosition;
        public Vector2 direction;
        public float sqrDistanceToPlayer;
    }

    [Header("Screen Edge")]
    [SerializeField] private float edgePadding = 60f;
    [SerializeField] private bool hideWhenTargetOnScreen = true;

    private readonly List<RectTransform> arrows = new();
    private readonly List<ArrowCandidate> candidates = new();

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FungalNetworkManager.Instance;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (arrowContainer == null)
            arrowContainer = transform as RectTransform;

        if (cinematicController == null)
            cinematicController = FindFirstObjectByType<PollutionClearCinematicController>();
    }

    private void LateUpdate()
    {
        if (cinematicController != null && cinematicController.IsPlaying)
        {
            HideAll();
            return;
        }

        if (networkManager == null || targetCamera == null || arrowPrefab == null || arrowContainer == null)
        {
            HideAll();
            return;
        }

        candidates.Clear();

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        for (int i = 0; i < networkManager.Nodes.Count; i++)
        {
            NetworkNode node = networkManager.Nodes[i];

            if (node == null || node.IsFullyHealed)
                continue;

            Vector3 screenPos = targetCamera.WorldToScreenPoint(node.WorldPosition);

            bool behindCamera = screenPos.z < 0f;
            bool onScreen =
                !behindCamera &&
                screenPos.x >= edgePadding &&
                screenPos.x <= Screen.width - edgePadding &&
                screenPos.y >= edgePadding &&
                screenPos.y <= Screen.height - edgePadding;

            if (hideWhenTargetOnScreen && onScreen)
                continue;

            if (behindCamera)
                screenPos *= -1f;

            Vector2 direction = ((Vector2)screenPos - screenCenter).normalized;

            Vector2 edgePosition = screenCenter + direction * 10000f;
            edgePosition.x = Mathf.Clamp(edgePosition.x, edgePadding, Screen.width - edgePadding);
            edgePosition.y = Mathf.Clamp(edgePosition.y, edgePadding, Screen.height - edgePadding);

            float sqrDistance = player != null
                ? (node.WorldPosition - player.position).sqrMagnitude
                : 0f;

            candidates.Add(new ArrowCandidate
            {
                node = node,
                edgePosition = edgePosition,
                direction = direction,
                sqrDistanceToPlayer = sqrDistance
            });
        }

        candidates.Sort((a, b) => a.sqrDistanceToPlayer.CompareTo(b.sqrDistanceToPlayer));

        int arrowIndex = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            ArrowCandidate candidate = candidates[i];

            bool overlapsKeptArrow = false;

            for (int j = 0; j < arrowIndex; j++)
            {
                float sqrOverlapDistance =
                    ((Vector2)arrows[j].position - candidate.edgePosition).sqrMagnitude;

                if (sqrOverlapDistance < overlapRadius * overlapRadius)
                {
                    overlapsKeptArrow = true;
                    break;
                }
            }

            if (overlapsKeptArrow)
                continue;

            RectTransform arrow = GetArrow(arrowIndex);
            arrowIndex++;

            float positionT = 1f - Mathf.Exp(-positionSmoothSpeed * Time.unscaledDeltaTime);
            float rotationT = 1f - Mathf.Exp(-rotationSmoothSpeed * Time.unscaledDeltaTime);

            bool wasInactive = !arrow.gameObject.activeSelf;

            if (wasInactive)
                arrow.position = candidate.edgePosition;
            else
                arrow.position = Vector3.Lerp(arrow.position, candidate.edgePosition, positionT);

            float angle = Mathf.Atan2(candidate.direction.y, candidate.direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            arrow.rotation = wasInactive
                ? targetRotation
                : Quaternion.Slerp(arrow.rotation, targetRotation, rotationT);

            arrow.gameObject.SetActive(true);
        }

        for (int i = arrowIndex; i < arrows.Count; i++)
        {
            arrows[i].gameObject.SetActive(false);
        }
    }

    private RectTransform GetArrow(int index)
    {
        while (arrows.Count <= index)
        {
            RectTransform arrow = Instantiate(arrowPrefab, arrowContainer);
            arrow.gameObject.SetActive(false);
            arrows.Add(arrow);
        }

        return arrows[index];
    }

    private void HideAll()
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            arrows[i].gameObject.SetActive(false);
        }
    }
}