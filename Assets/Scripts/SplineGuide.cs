using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineGuide : MonoBehaviour
{
    public SplineContainer splineContainer;
    public GameObject checkpointPrefab;
    public int resolution = 20;

    [SerializeField] private Transform checkpointRoot;

    private readonly List<Checkpoint> generatedCheckpoints = new List<Checkpoint>();
    private bool hasBuilt;

    void Awake()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
        }
    }

    void Start()
    {
        if (!hasBuilt)
        {
            GenerateCheckpoints();
        }
    }

    public void GenerateCheckpoints()
    {
        GenerateCheckpoints(false);
    }

    public void RebuildCheckpoints()
    {
        GenerateCheckpoints(true);
    }

    public float CalculateAccuracy()
    {
        if (ScoreManager.Instance != null)
        {
            return ScoreManager.Instance.GetAccuracy();
        }

        int totalCount = 0;
        int passedCount = 0;

        foreach (Checkpoint checkpoint in generatedCheckpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            totalCount++;
            if (checkpoint.isPassed)
            {
                passedCount++;
            }
        }

        if (totalCount == 0)
        {
            return 0f;
        }

        return (float)passedCount / totalCount * 100f;
    }

    private void GenerateCheckpoints(bool force)
    {
        if (splineContainer == null)
        {
            Debug.LogWarning($"{name}: SplineContainer is missing.", this);
            return;
        }

        if (hasBuilt && !force)
        {
            return;
        }

        if (checkpointPrefab == null)
        {
            checkpointPrefab = CreateDefaultCheckpointPrefab();
        }

        Transform root = EnsureCheckpointRoot();
        ClearGeneratedCheckpoints(root);

        int count = Mathf.Max(2, resolution);
        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            Vector3 worldPos = splineContainer.EvaluatePosition(t);

            GameObject checkpointObject = Instantiate(checkpointPrefab, worldPos, Quaternion.identity, root);
            checkpointObject.SetActive(true);

            Checkpoint checkpoint = checkpointObject.GetComponent<Checkpoint>();
            if (checkpoint == null)
            {
                continue;
            }

            checkpoint.Initialize(i, this);
            generatedCheckpoints.Add(checkpoint);
            ScoreManager.Instance?.RegisterCheckpoint(checkpoint);
        }

        hasBuilt = true;
        ScoreManager.Instance?.Refresh();
    }

    private void ClearGeneratedCheckpoints(Transform root)
    {
        ScoreManager.Instance?.Clear();
        generatedCheckpoints.Clear();

        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private Transform EnsureCheckpointRoot()
    {
        if (checkpointRoot != null)
        {
            return checkpointRoot;
        }

        Transform existing = transform.Find("Generated Checkpoints");
        if (existing != null)
        {
            checkpointRoot = existing;
            return checkpointRoot;
        }

        GameObject rootObject = new GameObject("Generated Checkpoints");
        rootObject.transform.SetParent(transform, false);
        checkpointRoot = rootObject.transform;
        return checkpointRoot;
    }

    private GameObject CreateDefaultCheckpointPrefab()
    {
        GameObject checkpointObject = new GameObject("CheckpointTemplate");
        checkpointObject.transform.SetParent(transform, false);
        checkpointObject.SetActive(false);

        SpriteRenderer spriteRenderer = checkpointObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 10;

        checkpointObject.transform.localScale = Vector3.one * 0.18f;

        CircleCollider2D collider = checkpointObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        checkpointObject.AddComponent<Checkpoint>();
        return checkpointObject;
    }
}
