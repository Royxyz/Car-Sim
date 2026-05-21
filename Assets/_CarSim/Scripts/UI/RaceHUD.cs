using UnityEngine;
using TMPro;

public class RaceHUD : MonoBehaviour
{
    [Header("Backend Dependencies")]
    public SimulationController sim;

    [Header("UI Elements")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI rpmText;
    public TextMeshProUGUI gearText;
    public TextMeshProUGUI boostText;

    [Header("Settings")]
    [Tooltip("How often the UI updates in seconds. 0.05 = 20hz. Saves GC overhead.")]
    public float updateRate = 0.05f;
    private float updateTimer;

    private void Update()
    {
        if (sim == null || GameManager.Instance.CurrentState != GameState.Race) return;

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateRate)
        {
            updateTimer = 0f;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        speedText.text = $"{speedKmh:0} KM/H";

        float rpm = sim.powerTrain.engineRPM;
        rpmText.text = $"{rpm:0} RPM";

        int gear = sim.powerTrain.transmission.currentGear;
        if (gear == -1) 
            gearText.text = "GEAR: R";
        else if (gear == 0) 
            gearText.text = "GEAR: N";
        else 
            gearText.text = $"GEAR: {gear}";

        float boost = sim.powerTrain.engine.currentManifoldPressure;
        boostText.text = $"BOOST: {boost:F2} BAR";
    }
}