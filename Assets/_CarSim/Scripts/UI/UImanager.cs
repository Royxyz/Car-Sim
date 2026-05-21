using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject raceHUD;
    public GameObject pauseMenu;

    [Header("Backend Dependencies")]
    public InputManager inputManager;
    public ECUData ecuData;

    [Header("UI Controls")]
    public Toggle filterToggle;
    public Toggle tcsToggle;


    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleUIState;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleUIState;
    }

    private void Start()
    {
        if (inputManager != null && filterToggle != null)
            filterToggle.isOn = inputManager.applyInputFiltering;
            
        if (ecuData != null && tcsToggle != null)
            tcsToggle.isOn = ecuData.enableTractionControl;
    }

    private void Update()
    {
        if(inputManager.controls.Global.Quit.WasPressedThisFrame())
        {
            QuitGame();
        }
        
    }

    private void HandleUIState(GameState state)
    {
        raceHUD.SetActive(false);
        pauseMenu.SetActive(false);

        if (state == GameState.Race)
            raceHUD.SetActive(true);
        else if (state == GameState.Paused)
            pauseMenu.SetActive(true);
    }

    public void ResumeGame()
    {
        GameManager.Instance.TogglePause(); 
    }

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; 
        #endif
    }
    public void OnInputFilterToggled(bool isOn)
    {
        if (inputManager != null) 
            inputManager.applyInputFiltering = isOn;
    }

    public void OnTCSToggled(bool isOn)
    {
        if (ecuData != null) 
            ecuData.enableTractionControl = isOn;
    }
}