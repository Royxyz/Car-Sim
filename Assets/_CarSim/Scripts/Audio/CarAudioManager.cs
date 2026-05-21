using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(SimulationController))]
public class CarAudioManager : MonoBehaviour
{
    [Header("FMOD Events")]
    public EventReference engineEventPath;
    public EventReference inductionEventPath; 
    public EventReference skidEventPath; 

    [Header("Tuning")]
    [Tooltip("Multiplier to dictate when slipping maxes out the audio intensity.")]
    public float skidMaxMultiplier = 2.5f;

    private SimulationController sim;
    
    private EventInstance engineInstance;
    private EventInstance inductionInstance;
    private EventInstance skidInstance;

    void Start()
    {
        sim = GetComponent<SimulationController>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (!engineEventPath.IsNull)
        {
            engineInstance = RuntimeManager.CreateInstance(engineEventPath);
            RuntimeManager.AttachInstanceToGameObject(engineInstance, transform, rb);
            engineInstance.start();
        }

        if (!inductionEventPath.IsNull)
        {
            inductionInstance = RuntimeManager.CreateInstance(inductionEventPath);
            RuntimeManager.AttachInstanceToGameObject(inductionInstance, transform, rb);
            inductionInstance.start();
        }

        if (!skidEventPath.IsNull)
        {
            skidInstance = RuntimeManager.CreateInstance(skidEventPath);
            RuntimeManager.AttachInstanceToGameObject(skidInstance, transform, rb);
            skidInstance.start();
        }
    }

    void Update()
    {
        if (sim == null) return;

        UpdatePowertrainAudio();
        UpdateSkidAudio();
    }

    private void UpdatePowertrainAudio()
    {
        float idleRPM = sim.powerTrain.engine._engineData.idleRPM;
        float redlineRPM = sim.powerTrain.engine._engineData.redlineRPM;
        float currentRPM = sim.powerTrain.engineRPM;
        
        float normRPM = Mathf.Clamp01(Mathf.InverseLerp(idleRPM, redlineRPM, currentRPM));
        float normLoad = sim.powerTrain.engine.currentThrottleBlade; 

        if (engineInstance.isValid())
        {
            engineInstance.setParameterByName("normRPM", normRPM);
            engineInstance.setParameterByName("normLoad", normLoad);
        }

        if (inductionInstance.isValid())
        {
            float boostIntensity = 0f;
            var induction = sim.powerTrain.engine._engineData.induction;
            
            if (induction != null && induction.type != InductionType.NaturallyAspirated)
            {
                float currentBoost = sim.powerTrain.engine.currentManifoldPressure;
                float maxBoost = induction.maxPressureBar;
                
                boostIntensity = Mathf.InverseLerp(1.0f, maxBoost, currentBoost);
            }

            inductionInstance.setParameterByName("boostIntensity", boostIntensity);
            inductionInstance.setParameterByName("normRPM", normRPM);
            inductionInstance.setParameterByName("normLoad", normLoad);
            //Debug.Log(boostIntensity);
        }
    }

    private void UpdateSkidAudio()
    {
        if (!skidInstance.isValid()) 
        {
            return;
        }

        float maxSkid = 0f;

        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            if (corner == null || !corner.contact.isGrounded) continue;

            float optimalLong = 1f / corner.tire.tireData.longB;
            float optimalLat = (1f / corner.tire.tireData.latB) * Mathf.Rad2Deg;

            float currentLong = Mathf.Abs(corner.wheel.longitudinalSlip);
            float currentLat = Mathf.Abs(corner.wheel.slipAngle * Mathf.Rad2Deg);

            float longSkid = Mathf.InverseLerp(optimalLong, optimalLong * skidMaxMultiplier, currentLong);
            float latSkid = Mathf.InverseLerp(optimalLat, optimalLat * skidMaxMultiplier, currentLat);

            float wheelSkid = Mathf.Max(longSkid, latSkid);

            if (Mathf.Abs(corner.wheel.angularVelocity) < 1f && Mathf.Abs(corner.wheel.forwardSpeed) > 2f)
            {
                wheelSkid = 1f;
            }

            if (wheelSkid > maxSkid) maxSkid = wheelSkid;
        }

        skidInstance.setParameterByName("skidIntensity", maxSkid);

    }

    void OnDestroy()
    {
        if (engineInstance.isValid())
        {
            engineInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            engineInstance.release();
        }

        if (inductionInstance.isValid())
        {
            inductionInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            inductionInstance.release();
        }

        if (skidInstance.isValid())
        {
            skidInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            skidInstance.release();
        }
    }
}