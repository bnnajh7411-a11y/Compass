using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class BGMManager : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        GameManager.RegisterBackgroundMusic(audioSource);
    }

    private void OnDestroy()
    {
        GameManager.UnregisterBackgroundMusic(audioSource);
    }
}
