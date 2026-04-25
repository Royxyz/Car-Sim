using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowertrainTester : MonoBehaviour
{
    public PowerTrain powerTrain;
    public AutoControllerLogicData autoLogicData;
    
    [Tooltip("Keep this at 20. This runs the drivetrain at 1000Hz when FixedUpdate is 50Hz.")]
    [Range(1, 100)]
    public int physicsSubsteps = 20;

    [Header("Dyno Variables")]
    public float dynoInertia = 5f;
    public float appliedDynoLoad = 0f;

    [Header("Telemetry (Read Only)")]
    public string activeTest = "None";
    public float currentThrottle;
    public float engineRPM;
    public int currentGear;
    public float clutchEngagement;
    public float transInputRPM;
    public float wheelRPM;
    public float dynoLoadTorque;

    private AutoController autoController;
    private bool isTesting = false;
    private float testTimer = 0f;
    private float testDuration = 0f;
    private float nextSampleTime = 0f;
    
    private float dynoIntegralError = 0f;
    private float previousRpmError = 0f;

    private struct TelemetrySnapshot
    {
        public float time;
        public float throttle;
        public float engineRpm;
        public int gear;
        public float clutchEngagement;
        public float transInputRpm;
        public float rollerRpm;
        public float loadTorque;
    }
    private List<TelemetrySnapshot> recordedData = new List<TelemetrySnapshot>();

    private enum TestType { None, PowerSweep, LoadShock, AutoShiftRun, RpmSweep }
    private TestType currentTestType = TestType.None;

    private void Start()
    {
        powerTrain.Initialize();
        autoController = new AutoController();
        autoController.Initialize(powerTrain, autoLogicData);
    }

    private void Update()
    {
        if (Keyboard.current == null || isTesting) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            StartTest(TestType.PowerSweep, 8f, "Testing max torque output. 4th Gear, 100% Throttle, light load.");

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            StartTest(TestType.LoadShock, 6f, "Testing clutch slip. 3rd Gear, steady throttle, massive load spike at t=2s.");

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            StartTest(TestType.AutoShiftRun, 12f, "Testing TCU logic. 1st through 4th gear run under moderate load.");

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            StartTest(TestType.RpmSweep, 15f, "Testing raw engine mapping. Forcing a slow 1000 -> 7500 RPM sweep at full throttle using active dyno braking.");
    }

    private void FixedUpdate()
    {
        if (!isTesting) return;

        float dt = Time.fixedDeltaTime / physicsSubsteps;

        for (int i = 0; i < physicsSubsteps; i++)
        {
            testTimer += dt;
            engineRPM = powerTrain.engineRPM; 
            
            ExecuteTestProfile(dt);

            // FIX: Only allow the auto-controller to run during AutoShift tests or idle.
            // In manual/dyno tests, the autocontroller will fight our forced gear and clutch logic.
            if (currentTestType == TestType.AutoShiftRun || currentTestType == TestType.None)
            {
                autoController.UpdateController(dt);
            }
            else
            {
                // Force clutch fully engaged for mechanical dyno sweeps
                powerTrain.clutch.engagement = 1f; 
            }

            float ratio = powerTrain.transmission.GetTotalRatio();
            wheelRPM = 0f;
            if (Mathf.Abs(ratio) > 0.001f)
            {
                wheelRPM = powerTrain.transmissionInputRPM / ratio;
            }

            dynoLoadTorque = Mathf.Sign(wheelRPM) * appliedDynoLoad;

            if (Mathf.Abs(wheelRPM) < 1f) dynoLoadTorque = 0f;

            powerTrain.UpdatePhysics(currentThrottle, dynoLoadTorque, dynoInertia, dt);
        }

        currentGear = powerTrain.transmission.currentGear;
        clutchEngagement = powerTrain.clutch.engagement;
        transInputRPM = powerTrain.transmissionInputRPM;

        if (testTimer >= nextSampleTime)
        {
            RecordSample();
            nextSampleTime += 0.01f;
        }

        if (testTimer >= testDuration)
        {
            StopTestAndSave();
        }
    }

    private void StartTest(TestType testType, float duration, string description)
    {
        powerTrain.Initialize();
        
        currentTestType = testType;
        testDuration = duration;
        testTimer = 0f;
        nextSampleTime = 0f;
        dynoIntegralError = 0f;
        previousRpmError = 0f;
        isTesting = true;
        recordedData.Clear();
        
        activeTest = testType.ToString();
        Debug.Log($"<color=green>STARTED TEST: {activeTest} ({duration}s)</color>\n{description}");

        if (currentTestType == TestType.PowerSweep || currentTestType == TestType.RpmSweep)
        {
            while(powerTrain.transmission.currentGear < 4) powerTrain.transmission.ShiftUp();
            while(powerTrain.transmission.currentGear > 4) powerTrain.transmission.ShiftDown();
        }
        else if (currentTestType == TestType.LoadShock)
        {
            while(powerTrain.transmission.currentGear < 3) powerTrain.transmission.ShiftUp();
            while(powerTrain.transmission.currentGear > 3) powerTrain.transmission.ShiftDown();
        }
        else if (currentTestType == TestType.AutoShiftRun)
        {
            while(powerTrain.transmission.currentGear > 1) powerTrain.transmission.ShiftDown();
            if (powerTrain.transmission.currentGear == 0) powerTrain.transmission.ShiftUp();
        }
    }

    private void ExecuteTestProfile(float dt)
    {
        switch (currentTestType)
        {
            case TestType.PowerSweep:
                currentThrottle = 1f;
                appliedDynoLoad = 50f + (Mathf.Abs(wheelRPM) * 0.1f);
                break;

            case TestType.LoadShock:
                currentThrottle = 0.6f;
                if (testTimer < 2.0f) appliedDynoLoad = 50f; 
                else if (testTimer < 4.0f) appliedDynoLoad = 600f; 
                else appliedDynoLoad = 50f; 
                break;

            case TestType.AutoShiftRun:
                currentThrottle = autoController.isShifting ? 0f : 1f;
                appliedDynoLoad = 150f + (Mathf.Abs(wheelRPM) * 0.2f);
                break;
                
            case TestType.RpmSweep:
                currentThrottle = 1f;
                
                float startRpm = 1000f;
                float endRpm = powerTrain.engine._engineData.redlineRPM;
                float targetRpm = Mathf.Lerp(startRpm, endRpm, testTimer / testDuration);
                
                float rpmError = engineRPM - targetRpm;
                
                float pGain = 1.5f;
                float iGain = 5.0f;
                float dGain = 0.02f;
                
                dynoIntegralError += rpmError * dt;
                dynoIntegralError = Mathf.Clamp(dynoIntegralError, -2000f, 2000f); 

                float derivative = (rpmError - previousRpmError) / dt;
                previousRpmError = rpmError;
                
                appliedDynoLoad = (rpmError * pGain) + (dynoIntegralError * iGain) + (derivative * dGain);
                appliedDynoLoad = Mathf.Clamp(appliedDynoLoad, 0f, 5000f);
                break;
        }
    }

    private void RecordSample()
    {
        recordedData.Add(new TelemetrySnapshot
        {
            time = testTimer,
            throttle = currentThrottle,
            engineRpm = engineRPM,
            gear = currentGear,
            clutchEngagement = clutchEngagement,
            transInputRpm = transInputRPM,
            rollerRpm = wheelRPM,
            loadTorque = dynoLoadTorque
        });
    }

    private void StopTestAndSave()
    {
        isTesting = false;
        activeTest = "None";
        currentThrottle = 0f;
        appliedDynoLoad = 0f;

        string timestamp = System.DateTime.Now.ToString("HHmmss");
        string filePath = Path.Combine(Application.dataPath, $"../Dyno_{currentTestType}_{timestamp}.csv");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Time (s),Throttle,Engine RPM,Gear,Clutch Engaged,Trans Input RPM,Roller RPM,Load Torque (Nm)");
            foreach (var data in recordedData)
            {
                writer.WriteLine($"{data.time:F2},{data.throttle:F2},{data.engineRpm:F0},{data.gear},{data.clutchEngagement:F2},{data.transInputRpm:F0},{data.rollerRpm:F0},{data.loadTorque:F2}");
            }
        }

        Debug.Log($"<color=cyan>TEST COMPLETE! Saved to: {filePath}</color>");
    }
}