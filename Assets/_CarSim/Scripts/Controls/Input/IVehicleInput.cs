using UnityEngine;

public interface IVehicleInput
{
    float Steering { get; }
    float Throttle { get; }
    float Brake { get; }
    float Clutch { get; }
    float Handbrake { get; } 
    bool ShiftUp { get; }
    bool ShiftDown { get; }
}