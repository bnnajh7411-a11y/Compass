using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
    private Text statusText;

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

    public void Clear()
    {
        checkpoints.Clear();
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

    public float GetAccuracy()
    {
        int total = 0;
        int passed = 0;

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            total++;
            if (checkpoint.isPassed)
            {
                passed++;
            }
        }

        if (total == 0)
        {
            return 0f;
        }

        return (float)passed / total * 100f;
    }

    public string GetProgressText()
    {
        int total = 0;
        int passed = 0;

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            total++;
            if (checkpoint.isPassed)
            {
                passed++;
            }
        }

        float accuracy = total == 0 ? 0f : (float)passed / total * 100f;
        return $"정확도 {accuracy:0}% ({passed}/{total})";
    }

    public void Refresh()
    {
        if (statusText != null)
        {
            statusText.text = GetProgressText();
        }
    }
}
