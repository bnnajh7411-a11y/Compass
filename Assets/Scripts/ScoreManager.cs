using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
    private Text statusText;
    private SplineGuide guide;
    private float currentAccuracy;
    private float currentTrailPrecision;
    private int coveredGuideSamples;
    private int totalGuideSamples;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void ResetSingleton()
    {
        Instance = null;
    }

    public void BindStatusText(Text text)
    {
        statusText = text;
        Refresh();
    }

    public void BindGuide(SplineGuide splineGuide)
    {
        guide = splineGuide;
        totalGuideSamples = guide != null ? guide.AccuracySampleCount : 0;
        Refresh();
    }

    public void Clear()
    {
        checkpoints.Clear();
        currentAccuracy = 0f;
        currentTrailPrecision = 0f;
        coveredGuideSamples = 0;
        totalGuideSamples = guide != null ? guide.AccuracySampleCount : 0;
        Refresh();
    }

    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || checkpoints.Contains(checkpoint))
        {
            return;
        }

        checkpoints.Add(checkpoint);
        Refresh();
    }

    public void UnregisterCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null)
        {
            return;
        }

        if (checkpoints.Remove(checkpoint))
        {
            Refresh();
        }
    }

    public void EvaluateTrail(Vector3[] trailPositions, int positionCount)
    {
        totalGuideSamples = guide != null ? guide.AccuracySampleCount : 0;

        if (guide == null || trailPositions == null || positionCount < 2 || totalGuideSamples == 0)
        {
            currentAccuracy = 0f;
            currentTrailPrecision = 0f;
            coveredGuideSamples = 0;
            Refresh();
            return;
        }

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint == null || checkpoint.isPassed)
            {
                continue;
            }

            if (checkpoint.IsTouchedByTrail(trailPositions, positionCount, guide.CheckpointHitTolerance))
            {
                checkpoint.MarkPassed();
            }
        }

        coveredGuideSamples = guide.CountCoveredSamples(trailPositions, positionCount);
        currentTrailPrecision = CalculateTrailPrecision(trailPositions, positionCount);

        float coverageRatio = totalGuideSamples > 0 ? (float)coveredGuideSamples / totalGuideSamples : 0f;
        currentAccuracy = coverageRatio * currentTrailPrecision * 100f;
        Refresh();
    }

    public float GetAccuracy()
    {
        return currentAccuracy;
    }

    public string GetProgressText()
    {
        float trailPrecisionPercent = currentTrailPrecision * 100f;
        return $"Accuracy {currentAccuracy:0}% ({coveredGuideSamples}/{totalGuideSamples}) | Line {trailPrecisionPercent:0}%";
    }

    public void Refresh()
    {
        if (statusText != null)
        {
            statusText.text = GetProgressText();
        }
    }

    private float CalculateTrailPrecision(Vector3[] trailPositions, int positionCount)
    {
        if (guide == null || trailPositions == null || positionCount <= 0)
        {
            return 0f;
        }

        float perfectTolerance = guide.PerfectTrailTolerance;
        float missTolerance = guide.MissTrailTolerance;
        float totalScore = 0f;

        for (int i = 0; i < positionCount; i++)
        {
            float distanceToGuide = guide.GetDistanceToGuide(trailPositions[i]);
            totalScore += EvaluatePointScore(distanceToGuide, perfectTolerance, missTolerance);
        }

        return totalScore / positionCount;
    }

    private static float EvaluatePointScore(float distanceToGuide, float perfectTolerance, float missTolerance)
    {
        if (distanceToGuide <= perfectTolerance)
        {
            return 1f;
        }

        if (distanceToGuide >= missTolerance)
        {
            return 0f;
        }

        float normalizedDistance = (distanceToGuide - perfectTolerance) / (missTolerance - perfectTolerance);
        return 1f - normalizedDistance;
    }
}
