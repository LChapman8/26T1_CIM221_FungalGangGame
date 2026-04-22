using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FungalNetworkManager networkManager;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;

    [Header("Colors")]
    [SerializeField] private Color lowHealthColor = new Color(0.8f, 0.15f, 0.15f);
    [SerializeField] private Color highHealthColor = new Color(0.3f, 0.9f, 0.45f);

    [Header("Animation")]
    [SerializeField] private bool smoothBar = true;
    [SerializeField] private float lerpSpeed = 8f;

    private float targetFill = 1f;
    private float currentFill = 1f;

    private void Awake()
    {
        if (networkManager == null)
        {
            networkManager = FungalNetworkManager.Instance;
        }
    }

    private void OnEnable()
    {
        if (networkManager == null)
        {
            networkManager = FungalNetworkManager.Instance;
        }

        if (networkManager != null)
        {
            networkManager.OnNetworkHealthChanged += HandleHealthChanged;
            HandleHealthChanged(networkManager.NetworkHealthNormalized);
        }
    }

    private void OnDisable()
    {
        if (networkManager != null)
        {
            networkManager.OnNetworkHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (fillImage == null) return;

        if (smoothBar)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);

            if (Mathf.Abs(currentFill - targetFill) < 0.001f)
            {
                currentFill = targetFill;
            }
        }
        else
        {
            currentFill = targetFill;
        }

        fillImage.fillAmount = currentFill;
        fillImage.color = Color.Lerp(lowHealthColor, highHealthColor, currentFill);

        if (percentText != null)
        {
            percentText.text = $"{Mathf.RoundToInt(currentFill * 100f)}%";
        }
    }

    private void HandleHealthChanged(float normalizedHealth)
    {
        targetFill = Mathf.Clamp01(normalizedHealth);
    }
}