using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FungalNetworkManager networkManager;
    [SerializeField] private PollutionClearCinematicController pollutionClearCinematic;
    [SerializeField] private WinConditionPopUp winPopUp;

    private bool winTriggered;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<FungalNetworkManager>();

        if (pollutionClearCinematic == null)
            pollutionClearCinematic = FindFirstObjectByType<PollutionClearCinematicController>();

        if (winPopUp == null)
            winPopUp = FindFirstObjectByType<WinConditionPopUp>();
    }

    private void OnEnable()
    {
        if (pollutionClearCinematic != null)
            pollutionClearCinematic.OnFinalClearFinished += HandleFinalClearFinished;
    }

    private void OnDisable()
    {
        if (pollutionClearCinematic != null)
            pollutionClearCinematic.OnFinalClearFinished -= HandleFinalClearFinished;
    }

    private void HandleFinalClearFinished()
    {
        if (winTriggered)
            return;

        if (networkManager == null || !networkManager.AllNodesFullyRestored)
            return;

        winTriggered = true;

        Time.timeScale = 0f;

        if (winPopUp != null)
            winPopUp.Show();
    }
}