using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NodeInteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject promptContainer;
    [SerializeField] private GameObject healthBarContainer;
    [SerializeField] private Image healthFillImage;

    [Header("Health Bar Colors")]
    [SerializeField] private Color lowHealthColor = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color highHealthColor = new Color(0.3f, 0.9f, 0.45f);

    [Header("Behavior")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 30f);
    [SerializeField] private bool hideWhenOffscreen = true;

    private NetworkNode currentNode;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        HidePrompt();
    }

    private void LateUpdate()
    {
        if (currentNode == null || !currentNode.CanShowRepairPrompt)
        {
            HidePrompt();
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                HidePrompt();
                return;
            }
        }

        Vector3 screenPos = targetCamera.WorldToScreenPoint(currentNode.PromptWorldPosition);

        if (hideWhenOffscreen)
        {
            bool behindCamera = screenPos.z < 0f;
            bool offscreen =
                screenPos.x < 0f || screenPos.x > Screen.width ||
                screenPos.y < 0f || screenPos.y > Screen.height;

            if (behindCamera || offscreen)
            {
                HidePrompt();
                return;
            }
        }

        promptRoot.gameObject.SetActive(true);
        promptRoot.position = screenPos + (Vector3)screenOffset;

        bool isRepairing = currentNode.IsBeingActivelyRepaired;

        if (promptContainer != null)
            promptContainer.SetActive(!isRepairing);

        if (healthBarContainer != null)
            healthBarContainer.SetActive(isRepairing);

        if (!isRepairing)
        {
            promptText.text = currentNode.RepairPromptText;
        }
        else if (healthFillImage != null)
        {
            float fill = currentNode.HealthNormalized;
            healthFillImage.fillAmount = fill;
            healthFillImage.color = Color.Lerp(lowHealthColor, highHealthColor, fill);
        }
    }

    public void ShowForNode(NetworkNode node)
    {
        currentNode = node;
    }

    public void ClearNode(NetworkNode node)
    {
        if (currentNode == node)
        {
            currentNode = null;
            HidePrompt();
        }
    }

    private void HidePrompt()
    {
        if (promptRoot != null)
            promptRoot.gameObject.SetActive(false);

        if (promptContainer != null)
            promptContainer.SetActive(false);

        if (healthBarContainer != null)
            healthBarContainer.SetActive(false);
    }
}