using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FungalNetworkManager networkManager;
    [SerializeField] private PollutionManager pollutionManager;
    [SerializeField] private PollutionClearCinematicController pollutionClearCinematic;
    [SerializeField] private WinConditionPopUp resultPopUp;

    private bool gameEnded;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<FungalNetworkManager>();

        if (pollutionManager == null)
            pollutionManager = FindFirstObjectByType<PollutionManager>();

        if (pollutionClearCinematic == null)
            pollutionClearCinematic = FindFirstObjectByType<PollutionClearCinematicController>();

        if (resultPopUp == null)
            resultPopUp = FindFirstObjectByType<WinConditionPopUp>();
    }

    private void OnEnable()
    {
        if (networkManager != null)
            networkManager.OnNetworkHealthChanged += HandleNetworkHealthChanged;

        if (pollutionManager != null)
            pollutionManager.OnPollutionCoverageChanged += HandlePollutionCoverageChanged;

        if (pollutionClearCinematic != null)
            pollutionClearCinematic.OnFinalClearFinished += HandleFinalClearFinished;
    }

    private void OnDisable()
    {
        if (networkManager != null)
            networkManager.OnNetworkHealthChanged -= HandleNetworkHealthChanged;

        if (pollutionManager != null)
            pollutionManager.OnPollutionCoverageChanged -= HandlePollutionCoverageChanged;

        if (pollutionClearCinematic != null)
            pollutionClearCinematic.OnFinalClearFinished -= HandleFinalClearFinished;
    }

    private void HandleFinalClearFinished()
    {
        if (gameEnded)
            return;

        if (networkManager == null || !networkManager.AllNodesFullyRestored)
            return;

        EndGameWin();
    }

    private void HandleNetworkHealthChanged(float normalizedHealth)
    {
        if (gameEnded)
            return;

        if (normalizedHealth <= 0f)
            EndGameOver();
    }

    private void HandlePollutionCoverageChanged(float normalizedPollution)
    {
        if (gameEnded)
            return;

        if (pollutionManager != null && pollutionManager.IsPollutionGridFull)
            EndGameOver();
    }

    private void EndGameWin()
    {
        gameEnded = true;
        Time.timeScale = 0f;

        if (resultPopUp != null)
            resultPopUp.ShowWin();
    }

    private void EndGameOver()
    {
        gameEnded = true;
        Time.timeScale = 0f;

        if (resultPopUp != null)
            resultPopUp.ShowGameOver();
    }
}