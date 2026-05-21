using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineBasicMultiChannelPerlin))]
public class DynamicCamera : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drop the car's main Rigidbody here.")]
    public Rigidbody carRb;
    
    private CinemachineCamera vcam;
    private CinemachineBasicMultiChannelPerlin noise;

    [Header("Speed Dynamics (FOV)")]
    public float baseFOV = 60f;
    [Tooltip("The FOV when the car is at max speed. High numbers = tunnel vision / high speed sensation.")]
    public float maxFOV = 95f;
    [Tooltip("The speed in KM/H where FOV and Shake hit their absolute maximum.")]
    public float topSpeedReferenceKmh = 240f;
    public float fovDamping = 4.0f;

    [Header("Violent Speed Shake")]
    [Tooltip("Shake amplitude at top speed.")]
    public float maxShakeAmplitude = 1.5f;
    [Tooltip("Shake frequency at top speed.")]
    public float maxShakeFrequency = 15.0f;
    [Tooltip("The speed in KM/H where the camera actually begins to shake.")]
    public float shakeThresholdKmh = 120f;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    private void Update()
    {
        if (carRb == null || vcam == null) return;

        float speedKmh = carRb.linearVelocity.magnitude * 3.6f;
        float speedRatio = Mathf.Clamp01(speedKmh / topSpeedReferenceKmh);

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio);
  
        LensSettings lens = vcam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFOV, Time.deltaTime * fovDamping);
        vcam.Lens = lens;

        if (noise != null)
        {
            if (speedKmh > shakeThresholdKmh)
            {
                float shakeRatio = Mathf.Clamp01((speedKmh - shakeThresholdKmh) / (topSpeedReferenceKmh - shakeThresholdKmh));
                
                // CM3 dropped the "m_" prefixes for properties
                noise.AmplitudeGain = Mathf.Lerp(0f, maxShakeAmplitude, shakeRatio);
                noise.FrequencyGain = Mathf.Lerp(0f, maxShakeFrequency, shakeRatio);
            }
            else
            {
                noise.AmplitudeGain = 0f;
                noise.FrequencyGain = 0f;
            }
        }
    }
}