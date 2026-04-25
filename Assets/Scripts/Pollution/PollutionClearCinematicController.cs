using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PollutionClearCinematicController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private PollutionManager pollutionManager;

    [Header("Timing")]
    [SerializeField] private float overviewHoldBeforeFade = 0.5f;
    [SerializeField] private float pollutionFadeDuration = 2f;
    [SerializeField] private float overviewHoldAfterFade = 0.5f;

    public event System.Action OnFinalClearFinished;

    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (overviewCamera != null)
            overviewCamera.enabled = false;
    }

    public void PlayForNode(NetworkNode node)
    {
        if (isPlaying || node == null)
            return;

        StartCoroutine(PlayRoutine(node));
    }

    private IEnumerator PlayRoutine(NetworkNode node)
    {
        isPlaying = true;

        List<PollutionCell> cellsToFade = pollutionManager.GetProtectedAreaCells(node);

        if (cellsToFade.Count == 0)
        {
            isPlaying = false;
            yield break;
        }

        Time.timeScale = 0f;

        if (gameplayCamera != null)
            gameplayCamera.enabled = false;

        if (overviewCamera != null)
            overviewCamera.enabled = true;

        yield return WaitUnscaled(overviewHoldBeforeFade);

        pollutionManager.RemoveCellsFromTracking(cellsToFade);

        foreach (PollutionCell cell in cellsToFade)
        {
            if (cell != null)
                StartCoroutine(cell.FadeOutAndDestroy(pollutionFadeDuration));
        }

        yield return WaitUnscaled(pollutionFadeDuration + overviewHoldAfterFade);

        if (overviewCamera != null)
            overviewCamera.enabled = false;

        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        Time.timeScale = 1f;
        isPlaying = false;
    }

    public void PlayForAllPollution()
    {
        if (isPlaying)
            return;

        StartCoroutine(PlayAllRoutine());
    }

    private IEnumerator PlayAllRoutine()
    {
        isPlaying = true;

        List<PollutionCell> cellsToFade = pollutionManager.GetAllActiveCells();

        if (cellsToFade.Count == 0)
        {
            isPlaying = false;
            yield break;
        }

        Time.timeScale = 0f;

        if (gameplayCamera != null)
            gameplayCamera.enabled = false;

        if (overviewCamera != null)
            overviewCamera.enabled = true;

        yield return WaitUnscaled(overviewHoldBeforeFade);

        pollutionManager.RemoveCellsFromTracking(cellsToFade);

        foreach (PollutionCell cell in cellsToFade)
        {
            if (cell != null)
                StartCoroutine(cell.FadeOutAndDestroy(pollutionFadeDuration));
        }

        yield return WaitUnscaled(pollutionFadeDuration + overviewHoldAfterFade);

        if (overviewCamera != null)
            overviewCamera.enabled = false;

        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        Time.timeScale = 1f;
        isPlaying = false;

        OnFinalClearFinished?.Invoke();
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}