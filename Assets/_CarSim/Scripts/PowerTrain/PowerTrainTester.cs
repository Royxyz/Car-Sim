using UnityEngine;
using UnityEngine.InputSystem;

public class PowertrainTester : MonoBehaviour
{
    public PowerTrain powerTrain;
    public AutoControllerLogicData autoLogicData;
    
    [Range(1, 100)]
    public int physicsSubsteps = 20;
    
    public float baseWheelLoad = 500f;
    public float speedDependentDrag = 0.5f;
    public float fakeWheelInertia = 5f;

    [Header("Telemetry (Read Only in Play Mode)")]
    public float currentThrottle;
    public float engineRPM;
    public int currentGear;
    public float clutchEngagement;
    public bool isClutchLocked;
    public float transInputRPM;
    public float wheelRPM;
    public float currentLoadTorque;

    private AutoController autoController;

    private void Start()
    {
        powerTrain.Initialize();
        autoController = new AutoController();
        autoController.Initialize(powerTrain, autoLogicData);
        
        powerTrain.transmission.ShiftUp(); 
    }

    private void Update()
    {
        currentThrottle = 0f;
        if (Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed))
        {
            currentThrottle = 1f;
        }
        
        if (autoController.isShifting)
        {
            currentThrottle = 0f;
        }

        if (physicsSubsteps < 1) physicsSubsteps = 1;
        float dt = Time.deltaTime / physicsSubsteps;

        for (int i = 0; i < physicsSubsteps; i++)
        {
            autoController.UpdateController(dt);

            float ratio = powerTrain.transmission.GetTotalRatio();
            wheelRPM = 0f;
            
            if (Mathf.Abs(ratio) > 0.001f)
            {
                wheelRPM = powerTrain.transmissionInputRPM / ratio;
            }

            currentLoadTorque = baseWheelLoad + (Mathf.Abs(wheelRPM) * speedDependentDrag);

            if (currentThrottle < 0.01f && powerTrain.transmissionInputRPM < 10f)
            {
                currentLoadTorque = 0f;
            }

            powerTrain.UpdatePhysics(currentThrottle, currentLoadTorque, fakeWheelInertia, dt);
        }

        engineRPM = powerTrain.engineRPM;
        currentGear = powerTrain.transmission.currentGear;
        clutchEngagement = powerTrain.clutch.engagement;
        isClutchLocked = powerTrain.clutch.isLocked;
        transInputRPM = powerTrain.transmissionInputRPM;
    }
}