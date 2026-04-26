using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PollutionHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PollutionManager pollutionManager;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;

    [Header("Colors")]
    [SerializeField] private Color lowPollutionColor = new Color(0.3f, 0.9f, 0.45f);
    [SerializeField] private Color highPollutionColor = new Color(0.8f, 0.15f, 0.15f);

    [Header("Animation")]
    [SerializeField] private bool smoothBar = true;
    [SerializeField] private float lerpSpeed = 8f;

    private float targetFill;
    private float currentFill;

    private void Awake()
    {
        if (pollutionManager == null)
            pollutionManager = FindFirstObjectByType<PollutionManager>();
    }

    private void OnEnable()
    {
        if (pollutionManager == null)
            pollutionManager = FindFirstObjectByType<PollutionManager>();

        if (pollutionManager != null)
        {
            pollutionManager.OnPollutionCoverageChanged += HandlePollutionChanged;
            HandlePollutionChanged(pollutionManager.PollutionCoverageNormalized);
        }
    }

    private void OnDisable()
    {
        if (pollutionManager != null)
            pollutionManager.OnPollutionCoverageChanged -= HandlePollutionChanged;
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        if (smoothBar)
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * lerpSpeed);

            if (Mathf.Abs(currentFill - targetFill) < 0.001f)
                currentFill = targetFill;
        }
        else
        {
            currentFill = targetFill;
        }

        fillImage.fillAmount = currentFill;
        fillImage.color = Color.Lerp(lowPollutionColor, highPollutionColor, currentFill);

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(currentFill * 100f)}%";
    }

    private void HandlePollutionChanged(float normalizedPollution)
    {
        targetFill = Mathf.Clamp01(normalizedPollution);
    }
}