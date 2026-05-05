using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SimulationController))]
public class InputManager : MonoBehaviour, IVehicleInput
{
    private CarController controls;
    private SimulationController sim;

    [Header("Assist Toggles")]
    public bool applyToKeyboardOnly = true;
    public bool enableSteeringAssist = true;
    public bool enableTractionControl = true;
    public bool enableThrottleSmoothing = true;
    public bool enableBrakeSmoothing = true;

    [Header("Digital Steering Filter")]
    [Tooltip("How fast the virtual steering wheel turns when holding A/D.")]
    public float steerTurnSpeed = 3.0f;
    [Tooltip("How fast the virtual steering wheel centers when letting go.")]
    public float steerReturnSpeed = 5.0f;
    [Tooltip("Reduces maximum steering angle at high speeds to prevent snap oversteer. (X = Speed km/h, Y = Max Steer Multiplier 0-1)")]
    public AnimationCurve speedSteerLimit = AnimationCurve.Linear(0f, 1f, 200f, 0.25f);

    [Header("Digital Pedals Filter")]
    [Tooltip("Time it takes for the keyboard throttle to reach 100%. Prevents instant tire spinning.")]
    public float throttleSmoothSpeed = 5.0f;
    [Tooltip("Time it takes for the keyboard brake to reach 100%.")]
    public float brakeSmoothSpeed = 5.0f;

    [Header("Traction Control System (TCS)")]
    [Tooltip("The longitudinal slip threshold before the ECU cuts power.")]
    public float tcsSlipThreshold = 0.12f;
    [Tooltip("How aggressively the throttle is cut when slip is detected.")]
    public float tcsAggressiveness = 10.0f;

    public bool isTcsActive { get; private set; }
    public float filteredSteering { get; private set; }
    public float filteredThrottle { get; private set; }
    public float filteredBrake { get; private set; }
    public float Steering => filteredSteering;
    public float Throttle => filteredThrottle;
    public float Brake => filteredBrake;
    public float Clutch { get; private set; }
    public float Handbrake { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    private void Awake()
    {
        controls = new CarController();
        sim = GetComponent<SimulationController>();
    }

    private void OnEnable() { controls.Driving.Enable(); }
    private void OnDisable() { controls.Driving.Disable(); }

    private void Update()
    {
        ProcessInputs(Time.deltaTime);
    }

    private void ProcessInputs(float dt)
    {
        float rawSteer = controls.Driving.Steering.ReadValue<float>();
        float rawThrottle = controls.Driving.Throttle.ReadValue<float>();
        float rawBrake = controls.Driving.Brake.ReadValue<float>();
        
        Clutch = controls.Driving.Clutch.ReadValue<float>();
        Handbrake = controls.Driving.Handbrake.ReadValue<float>();
        ShiftUp = controls.Driving.ShiftUp.WasPressedThisFrame();
        ShiftDown = controls.Driving.ShiftDown.WasPressedThisFrame();

        bool isKeyboard = false;
        if (controls.Driving.Steering.activeControl != null)
        {
            isKeyboard = controls.Driving.Steering.activeControl.device is Keyboard;
        }

        bool applyAssists = !applyToKeyboardOnly || isKeyboard;

        if (applyAssists && enableSteeringAssist)
        {
            float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
            float maxAllowedSteer = speedSteerLimit.Evaluate(speedKmh);
            float targetSteer = rawSteer * maxAllowedSteer;

            if (Mathf.Abs(rawSteer) > 0.01f)
            {
                filteredSteering = Mathf.MoveTowards(filteredSteering, targetSteer, steerTurnSpeed * dt);
            }
            else
            {
                filteredSteering = Mathf.MoveTowards(filteredSteering, 0f, steerReturnSpeed * dt);
            }
        }
        else
        {
            filteredSteering = rawSteer;
        }

        if (applyAssists && enableBrakeSmoothing)
        {
            filteredBrake = Mathf.MoveTowards(filteredBrake, rawBrake, brakeSmoothSpeed * dt);
        }
        else
        {
            filteredBrake = rawBrake;
        }

        float targetThrottle = rawThrottle;

        if (applyAssists && enableThrottleSmoothing)
        {
            targetThrottle = Mathf.MoveTowards(filteredThrottle, rawThrottle, throttleSmoothSpeed * dt);
        }

        if (enableTractionControl && targetThrottle > 0.01f)
        {
            float maxDrivenSlip = GetMaxDrivenLongitudinalSlip();

            if (maxDrivenSlip > tcsSlipThreshold)
            {
                isTcsActive = true;
                float slipExcess = maxDrivenSlip - tcsSlipThreshold;
                float throttleCut = slipExcess * tcsAggressiveness;

                targetThrottle = Mathf.Clamp01(targetThrottle - throttleCut);
            }
            else
            {
                isTcsActive = false;
            }
        }
        else
        {
            isTcsActive = false;
        }

        filteredThrottle = applyAssists ? targetThrottle : rawThrottle;
    }
    private float GetMaxDrivenLongitudinalSlip()
{
    float maxSlip = 0f;
    DriveType driveType = sim.drivetrain.drivetrainData.driveType;

    for (int i = 0; i < 4; i++)
    {
        if (driveType == DriveType.FWD && i > 1) continue; 
        if (driveType == DriveType.RWD && i < 2) continue;

        if (sim.corners[i] != null && sim.corners[i].contact.isGrounded)
        {
            float slip = Mathf.Abs(sim.corners[i].tire.dynamicLongSlip); 
            if (slip > maxSlip)
            {
                maxSlip = slip;
            }
        }
    }
    return maxSlip;
}
}