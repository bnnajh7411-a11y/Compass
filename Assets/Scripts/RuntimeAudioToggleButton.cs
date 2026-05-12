using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RuntimeAudioToggleButton : MonoBehaviour
{
    private static readonly Color EnabledColor = Color.white;
    private static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.45f);

    private Image iconImage;
    private Button button;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
        button = GetComponent<Button>();

        if (button != null)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        RuntimeAudioState.MutedChanged += HandleMutedChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        RuntimeAudioState.MutedChanged -= HandleMutedChanged;
    }

    private void HandleClick()
    {
        RuntimeAudioState.Toggle();
    }

    private void HandleMutedChanged(bool isMuted)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.color = RuntimeAudioState.IsMuted ? MutedColor : EnabledColor;
    }
}
