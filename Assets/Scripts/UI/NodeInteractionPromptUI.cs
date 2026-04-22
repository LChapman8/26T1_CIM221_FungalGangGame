using TMPro;
using UnityEngine;

public class NodeInteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TMP_Text promptText;

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
        promptText.text = currentNode.RepairPromptText;

        promptRoot.position = screenPos + (Vector3)screenOffset;
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
        {
            promptRoot.gameObject.SetActive(false);
        }
    }
}