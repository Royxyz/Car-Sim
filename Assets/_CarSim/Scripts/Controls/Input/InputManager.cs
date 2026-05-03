using UnityEngine;

public class InputManager : MonoBehaviour, IVehicleInput
{
    private CarController controls;
    public float steeringInput { get; private set; }
    public float throttleInput { get; private set; }
    public float brakeInput { get; private set; }
    public float clutchInput { get; private set; }
    
    public bool shiftUpTriggered { get; private set; }
    public bool shiftDownTriggered { get; private set; }


    public float Steering => steeringInput;
    public float Throttle => throttleInput;
    public float Brake => brakeInput;
    public float Clutch => clutchInput;
    public bool ShiftUp => shiftUpTriggered;
    public bool ShiftDown => shiftDownTriggered;


    private void Awake()
    {
        controls = new CarController();
    }

    private void OnEnable()
    {
        controls.Driving.Enable();
    }

    private void OnDisable()
    {
        controls.Driving.Disable();
    }

    private void Update()
    {
        steeringInput = controls.Driving.Steering.ReadValue<float>();
        throttleInput = controls.Driving.Throttle.ReadValue<float>();
        brakeInput = controls.Driving.Brake.ReadValue<float>();
        clutchInput = controls.Driving.Clutch.ReadValue<float>();

        shiftUpTriggered = controls.Driving.ShiftUp.WasPressedThisFrame();
        shiftDownTriggered = controls.Driving.ShiftDown.WasPressedThisFrame();
    }
}