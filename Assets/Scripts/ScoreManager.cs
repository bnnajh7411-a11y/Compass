using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreManager : MonoBehaviour
{
    private const float BalancedProgressBlend = 0.55f;

    public static ScoreManager Instance { get; private set; }

    private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
    private AccuracyGaugeView accuracyGauge;
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

    public void BindAccuracyGauge(AccuracyGaugeView gauge)
    {
        accuracyGauge = gauge;
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

    public void ResetAttempt()
    {
        if (guide != null)
        {
            guide.RebuildCheckpoints();
            return;
        }

        Clear();
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

    public void EvaluateTrail(Vector3[] trailPositions, int positionCount, IReadOnlyList<int> strokeStartIndices)
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

            if (checkpoint.IsTouchedByTrail(trailPositions, positionCount, strokeStartIndices, guide.CheckpointHitTolerance))
            {
                checkpoint.MarkPassed();
            }
        }

        int stageIndex = GameManager.SelectedStageIndex;
        coveredGuideSamples = guide.CountCoveredSamples(trailPositions, positionCount, strokeStartIndices);
        currentTrailPrecision = CalculateTrailPrecision(trailPositions, positionCount, strokeStartIndices, stageIndex);

        float coverageRatio = totalGuideSamples > 0 ? (float)coveredGuideSamples / totalGuideSamples : 0f;
        float checkpointProgress = CalculateCheckpointProgress();
        float stabilizedCheckpointProgress = StabilizeCheckpointProgress(checkpointProgress, coverageRatio);
        float earlyProgressRamp = CalculateEarlyProgressRamp(coverageRatio);

        currentAccuracy = CalculateStageAccuracy(
            stageIndex,
            coverageRatio,
            currentTrailPrecision,
            stabilizedCheckpointProgress) * earlyProgressRamp * 100f;
        Refresh();
    }

    public float GetAccuracy()
    {
        return currentAccuracy;
    }

    public void Refresh()
    {
        if (accuracyGauge != null)
        {
            accuracyGauge.SetAccuracy(currentAccuracy);
        }
    }

    private float CalculateTrailPrecision(Vector3[] trailPositions, int positionCount, IReadOnlyList<int> strokeStartIndices, int stageIndex)
    {
        if (guide == null || trailPositions == null || positionCount <= 0)
        {
            return 0f;
        }

        float perfectTolerance = guide.PerfectTrailTolerance;
        float missTolerance = guide.MissTrailTolerance;
        float falloffExponent = GetPrecisionFalloffExponent(stageIndex);
        float sampleMergeDistance = Mathf.Max(perfectTolerance * 0.75f, 0.02f);
        float sampleMergeDistanceSq = sampleMergeDistance * sampleMergeDistance;
        int strokeCount = strokeStartIndices != null && strokeStartIndices.Count > 0 ? strokeStartIndices.Count : 1;
        float totalScore = 0f;
        int sampleCount = 0;

        for (int strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
        {
            int startIndex = strokeStartIndices != null && strokeStartIndices.Count > 0
                ? Mathf.Clamp(strokeStartIndices[strokeIndex], 0, positionCount)
                : 0;
            int endExclusive = strokeIndex + 1 < strokeCount
                ? Mathf.Clamp(strokeStartIndices[strokeIndex + 1], 0, positionCount)
                : positionCount;

            if (startIndex >= endExclusive)
            {
                continue;
            }

            Vector3 lastAcceptedSample = trailPositions[startIndex];
            totalScore += EvaluatePointScore(
                guide.GetDistanceToGuide(lastAcceptedSample),
                perfectTolerance,
                missTolerance,
                falloffExponent);
            sampleCount++;

            for (int i = startIndex + 1; i < endExclusive; i++)
            {
                Vector3 candidate = trailPositions[i];
                if ((candidate - lastAcceptedSample).sqrMagnitude <= sampleMergeDistanceSq)
                {
                    continue;
                }

                totalScore += EvaluatePointScore(
                    guide.GetDistanceToGuide(candidate),
                    perfectTolerance,
                    missTolerance,
                    falloffExponent);
                sampleCount++;
                lastAcceptedSample = candidate;
            }
        }

        return sampleCount > 0 ? totalScore / sampleCount : 0f;
    }

    private static float EvaluatePointScore(float distanceToGuide, float perfectTolerance, float missTolerance, float falloffExponent)
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
        float remainingScore = 1f - normalizedDistance;
        return Mathf.Pow(remainingScore, falloffExponent);
    }

    private static float CalculateStageAccuracy(int stageIndex, float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        coverageRatio = Mathf.Clamp01(coverageRatio);
        trailPrecision = Mathf.Clamp01(trailPrecision);
        checkpointProgress = Mathf.Clamp01(checkpointProgress);

        // Each stage uses its own rubric so the score is not driven by one shared formula.
        float stageScore;

        switch (Mathf.Clamp(stageIndex, GameManager.FirstStageIndex, GameManager.LastStageIndex))
        {
            case 1:
                stageScore = CalculateStageOneAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
            case 2:
                stageScore = CalculateStageTwoAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
            case 3:
                stageScore = CalculateStageThreeAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
            case 4:
                stageScore = CalculateStageFourAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
            case 5:
                stageScore = CalculateStageFiveAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
            case 6:
                stageScore = CalculateStageSixAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
            default:
                stageScore = CalculateStageTwoAccuracy(coverageRatio, trailPrecision, checkpointProgress);
                break;
        }

        return FinalizeStageAccuracy(stageIndex, stageScore, coverageRatio, trailPrecision, checkpointProgress);
    }

    private static float CalculateStageOneAccuracy(float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        float balancedCoverage = ApplyBalancedProgressCurve(coverageRatio);
        float checkpointSupport = ApplyBalancedProgressCurve(checkpointProgress);
        return Mathf.Clamp01((balancedCoverage * 0.55f) + (trailPrecision * 0.2f) + (checkpointSupport * 0.25f));
    }

    private static float CalculateStageTwoAccuracy(float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        float coreBlend = (coverageRatio * 0.4f) + (trailPrecision * 0.35f);
        float syncBonus = CalculateBalancedSynergy(coverageRatio, trailPrecision) * 0.15f;
        return Mathf.Clamp01(coreBlend + syncBonus + (ApplyBalancedProgressCurve(checkpointProgress) * 0.1f));
    }

    private static float CalculateStageThreeAccuracy(float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        float precisionFocus = Mathf.Pow(trailPrecision, 1.02f);
        float coverageSupport = ApplyBalancedProgressCurve(coverageRatio);
        float balanceBonus = CalculateBalancedSynergy(trailPrecision, coverageRatio);
        return Mathf.Clamp01((precisionFocus * 0.35f) + (coverageSupport * 0.35f) + (ApplyBalancedProgressCurve(checkpointProgress) * 0.18f) + (balanceBonus * 0.12f));
    }

    private static float CalculateStageFourAccuracy(float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        float completionGate = ApplyBalancedProgressCurve(Mathf.Min(coverageRatio, checkpointProgress));
        float routeStability = CalculateBalancedSynergy(coverageRatio, trailPrecision);
        return Mathf.Clamp01((completionGate * 0.2f) + (routeStability * 0.4f) + (ApplyBalancedProgressCurve(checkpointProgress) * 0.24f) + (ApplyBalancedProgressCurve(coverageRatio) * 0.16f));
    }

    private static float CalculateStageFiveAccuracy(float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        float rawScore = (trailPrecision * 0.39f) + (coverageRatio * 0.36f) + (checkpointProgress * 0.25f);
        float missPenalty = ((1f - trailPrecision) * 0.05f) + ((1f - coverageRatio) * 0.03f);
        return Mathf.Clamp01(rawScore - missPenalty);
    }

    private static float CalculateStageSixAccuracy(float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        float harmonicCore = CalculateHarmonicMean(coverageRatio, trailPrecision);
        float weakestLink = Mathf.Min(coverageRatio, Mathf.Min(trailPrecision, checkpointProgress));
        float stabilityBoost = ApplyBalancedProgressCurve(weakestLink);
        float balanceBonus = CalculateBalancedSynergy(checkpointProgress, trailPrecision);
        return Mathf.Clamp01((harmonicCore * 0.32f) + (stabilityBoost * 0.18f) + (ApplyBalancedProgressCurve(checkpointProgress) * 0.24f) + (ApplyBalancedProgressCurve(coverageRatio) * 0.16f) + (balanceBonus * 0.1f));
    }

    private static float CalculateHarmonicMean(float first, float second)
    {
        float sum = first + second;
        if (sum <= Mathf.Epsilon)
        {
            return 0f;
        }

        return (2f * first * second) / sum;
    }

    private static float ApplyBalancedProgressCurve(float value)
    {
        value = Mathf.Clamp01(value);
        float smoothedValue = value * value * (3f - (2f * value));
        return Mathf.Lerp(value, smoothedValue, BalancedProgressBlend);
    }

    private static float CalculateBalancedSynergy(float first, float second)
    {
        return ApplyBalancedProgressCurve(Mathf.Clamp01(first * second));
    }

    private static float GetPrecisionFalloffExponent(int stageIndex)
    {
        switch (Mathf.Clamp(stageIndex, GameManager.FirstStageIndex, GameManager.LastStageIndex))
        {
            case 1:
                return 2.2f;
            case 2:
                return 3f;
            case 3:
                return 2.6f;
            case 4:
                return 3f;
            case 5:
                return 3.4f;
            case 6:
                return 3.8f;
            default:
                return 4f;
        }
    }

    private static float FinalizeStageAccuracy(int stageIndex, float stageScore, float coverageRatio, float trailPrecision, float checkpointProgress)
    {
        stageScore = Mathf.Clamp01(stageScore);
        float finalizedScore;

        switch (Mathf.Clamp(stageIndex, GameManager.FirstStageIndex, GameManager.LastStageIndex))
        {
            case 3:
                finalizedScore = ApplyLateStageLeniency(stageScore, coverageRatio, trailPrecision, checkpointProgress, 0.82f, 0.34f, 0.92f, 0.88f, 0.9f);
                break;
            case 4:
                finalizedScore = ApplyLateStageLeniency(stageScore, coverageRatio, trailPrecision, checkpointProgress, 0.83f, 0.32f, 0.93f, 0.89f, 0.92f);
                break;
            case 5:
                finalizedScore = ApplyLateStageLeniency(stageScore, coverageRatio, trailPrecision, checkpointProgress, 0.84f, 0.3f, 0.94f, 0.9f, 0.93f);
                break;
            case 6:
                finalizedScore = ApplyLateStageLeniency(stageScore, coverageRatio, trailPrecision, checkpointProgress, 0.85f, 0.28f, 0.95f, 0.91f, 0.94f);
                break;
            default:
                finalizedScore = stageScore;
                break;
        }

        return ApplyStageAccuracyBoost(stageIndex, finalizedScore);
    }

    private static float ApplyLateStageLeniency(
        float stageScore,
        float coverageRatio,
        float trailPrecision,
        float checkpointProgress,
        float boostStart,
        float boostStrength,
        float perfectCoverageThreshold,
        float perfectPrecisionThreshold,
        float perfectCheckpointThreshold)
    {
        if (coverageRatio >= perfectCoverageThreshold
            && trailPrecision >= perfectPrecisionThreshold
            && checkpointProgress >= perfectCheckpointThreshold)
        {
            return 1f;
        }

        float boostedScore = Mathf.Lerp(stageScore, 1f, Mathf.InverseLerp(boostStart, 1f, stageScore) * boostStrength);
        return Mathf.Clamp01(boostedScore);
    }

    private static float ApplyStageAccuracyBoost(int stageIndex, float score)
    {
        switch (Mathf.Clamp(stageIndex, GameManager.FirstStageIndex, GameManager.LastStageIndex))
        {
            case 3:
            case 4:
            case 5:
                return Mathf.Clamp01(score * 1.1f);
            case 6:
                return Mathf.Clamp01(score * 1.05f);
            default:
                return Mathf.Clamp01(score);
        }
    }

    private static float StabilizeCheckpointProgress(float checkpointProgress, float coverageRatio)
    {
        float checkpointRamp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.28f, coverageRatio));
        return checkpointProgress * checkpointRamp;
    }

    private static float CalculateEarlyProgressRamp(float coverageRatio)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.025f, 0.2f, coverageRatio));
    }

    private float CalculateCheckpointProgress()
    {
        if (checkpoints.Count == 0)
        {
            return 0f;
        }

        int validCheckpointCount = 0;
        int passedCheckpointCount = 0;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            Checkpoint checkpoint = checkpoints[i];
            if (checkpoint == null)
            {
                continue;
            }

            validCheckpointCount++;
            if (checkpoint.isPassed)
            {
                passedCheckpointCount++;
            }
        }

        if (validCheckpointCount == 0)
        {
            return 0f;
        }

        return (float)passedCheckpointCount / validCheckpointCount;
    }
}
