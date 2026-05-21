using UnityEngine;
using UnityEngine.InputSystem;

public enum InputDeviceMode 
{ 
    Keyboard, 
    Gamepad, 
    Raw
}

[RequireComponent(typeof(SimulationController))]
public class InputManager : MonoBehaviour, IVehicleInput
{
    public CarController controls;
    private SimulationController sim;

    [Header("Device Management")]
    public bool autoDetectDevice = true;
    public InputDeviceMode currentDeviceMode = InputDeviceMode.Keyboard;
    public bool applyInputFiltering = true;

    [Header("Keyboard Filtering")]
    public float steerTurnSpeed = 3.0f;
    public float steerReturnSpeed = 5.0f;
    public float throttleSmoothSpeed = 5.0f;
    public float brakeSmoothSpeed = 5.0f;

    [Header("Gamepad Filtering")]
    public float steeringGamma = 2.0f;
    public float gamepadDampingSpeed = 15.0f;

    [Header("Global Steering Assist")]
    public AnimationCurve speedSteerLimit = AnimationCurve.Linear(0f, 1f, 200f, 0.25f);

    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Clutch { get; private set; }
    public float Handbrake { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    private void Awake()
    {
        controls = new CarController();
        sim = GetComponent<SimulationController>();
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged += HandleGameStateChange;
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Race)
            controls.Driving.Enable();
        controls.Driving.ResetCar.performed += ResetVehicle;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged -= HandleGameStateChange;
        controls.Driving.Disable();
        controls.Driving.ResetCar.performed -= ResetVehicle;
    }

    private void HandleGameStateChange(GameState state)
    {
        if (state == GameState.Race) controls.Driving.Enable();
        else controls.Driving.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Race) return;
        ProcessInputs(Time.deltaTime); 
    }

    private void ResetVehicle(InputAction.CallbackContext context)
    {
        sim.rb.linearVelocity = Vector3.zero;
        sim.rb.angularVelocity = Vector3.zero;
        sim.vDynamics.ResetStepAccumulators();
        
        for (int i = 0; i < 4; i++)
        {
            if (sim.corners[i] != null) sim.corners[i].tire.Initialize(); 
        }

        Vector3 currentPos = sim.rb.position;
        sim.rb.position = new Vector3(currentPos.x, currentPos.y + 1.5f, currentPos.z);
        
        Vector3 euler = sim.rb.rotation.eulerAngles;
        sim.rb.rotation = Quaternion.Euler(0f, euler.y, 0f);
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

        if (autoDetectDevice && controls.Driving.Steering.activeControl != null)
        {
            var activeDevice = controls.Driving.Steering.activeControl.device;
            if (activeDevice is Keyboard && applyInputFiltering) currentDeviceMode = InputDeviceMode.Keyboard;
            else if (activeDevice is Gamepad && applyInputFiltering) currentDeviceMode = InputDeviceMode.Gamepad;
            else currentDeviceMode = InputDeviceMode.Raw; 
        }

        float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        float maxAllowedSteer = speedSteerLimit.Evaluate(speedKmh);

        switch (currentDeviceMode)
        {
            case InputDeviceMode.Raw:
                Steering = rawSteer;
                Throttle = rawThrottle;
                Brake = rawBrake;
                break;

            case InputDeviceMode.Gamepad:
                float curvedSteer = Mathf.Sign(rawSteer) * Mathf.Pow(Mathf.Abs(rawSteer), steeringGamma);
                float targetGamepadSteer = curvedSteer * maxAllowedSteer;
                Steering = Mathf.Lerp(Steering, targetGamepadSteer, gamepadDampingSpeed * dt);
                Throttle = rawThrottle;
                Brake = rawBrake;
                break;

            case InputDeviceMode.Keyboard:
                float targetKeyboardSteer = rawSteer * maxAllowedSteer;
                if (Mathf.Abs(rawSteer) > 0.01f)
                    Steering = Mathf.MoveTowards(Steering, targetKeyboardSteer, steerTurnSpeed * dt);
                else
                    Steering = Mathf.MoveTowards(Steering, 0f, steerReturnSpeed * dt);

                Throttle = Mathf.MoveTowards(Throttle, rawThrottle, throttleSmoothSpeed * dt);
                Brake = Mathf.MoveTowards(Brake, rawBrake, brakeSmoothSpeed * dt);
                break;
        }
    }
}