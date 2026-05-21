using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public static event Action<GameState> OnStateChanged;

    private CarController globalControls;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        globalControls = new CarController();
    }

    private void OnEnable()
    {
        globalControls.Global.Enable();
        globalControls.Global.TogglePause.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        globalControls.Global.Disable();
        globalControls.Global.TogglePause.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void Start()
    {
        ChangeState(GameState.Race);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;

        Time.timeScale = (newState == GameState.Paused || newState == GameState.MainMenu) ? 0f : 1f;
        OnStateChanged?.Invoke(newState);
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Race) ChangeState(GameState.Paused);
        else if (CurrentState == GameState.Paused) ChangeState(GameState.Race);
    }
}