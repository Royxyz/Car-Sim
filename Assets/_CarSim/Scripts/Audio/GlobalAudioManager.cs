using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(GameManager))]
public class GlobalAudioManager : MonoBehaviour
{
    [Header("FMOD Global Events")]
    public EventReference bgmEvent;
    public EventReference ambienceEvent;

    private EventInstance bgmInstance;
    private EventInstance ambienceInstance;

    void Start()
    {
        if (!bgmEvent.IsNull)
        {
            bgmInstance = RuntimeManager.CreateInstance(bgmEvent);
            bgmInstance.start();
        }

        if (!ambienceEvent.IsNull)
        {
            ambienceInstance = RuntimeManager.CreateInstance(ambienceEvent);
            ambienceInstance.start();
        }

        GameManager.OnStateChanged += HandleGameStateChange;

        if (GameManager.Instance != null)
        {
            HandleGameStateChange(GameManager.Instance.CurrentState);
        }
    }

    private void HandleGameStateChange(GameState state)
    {
        int stateIndex = (int)state;
        if (bgmInstance.isValid())
        {
            bgmInstance.setParameterByName("GameState", stateIndex);
        }

        if (ambienceInstance.isValid())
        {
            ambienceInstance.setParameterByName("GameState", stateIndex);
        }
    }

    void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleGameStateChange;

        if (bgmInstance.isValid())
        {
            bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            bgmInstance.release();
        }

        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambienceInstance.release();
        }
    }
}