using UnityEngine;

[System.Serializable]
public class AutoController
{
    public AutoControllerLogicData logicData;
    public PowerTrain powerTrain;

    private float shiftTimer;
    public bool isShifting { get; private set; }
    private int pendingGearChange;
    
    private float shiftCooldownTimer = 0f;
    private const float COOLDOWN_DURATION = 0.5f; 

    public void Initialize(PowerTrain pt, AutoControllerLogicData data)
    {
        powerTrain = pt;
        logicData = data;
        shiftTimer = 0f;
        isShifting = false;
        pendingGearChange = 0;
        shiftCooldownTimer = 0f;
    }

    public void UpdateController(float throttle, float brake, float dt)
    {
        if (powerTrain == null || logicData == null) return;

        HandleShiftingLogic(throttle, brake, dt);
        HandleClutchLogic(throttle, dt); 
    }

    private void HandleShiftingLogic(float throttle, float brake, float dt)
    {
        if (shiftCooldownTimer > 0f) shiftCooldownTimer -= dt;

        if (isShifting)
        {
            shiftTimer += dt;
            if (shiftTimer >= logicData.shiftDuration)
            {
                CompleteShift();
                shiftCooldownTimer = COOLDOWN_DURATION;
            }
            return;
        }

        Transmission trans = powerTrain.transmission;
        int currentGear = trans.currentGear;
        float currentRPM = powerTrain.engineRPM;

        // Neutral State Escape: Only shift into Drive via throttle. 
        // Reverse is handled manually by the player via the ShiftDown input.
        if (currentGear == 0)
        {
            if (throttle > 0.05f) StartShift(1);       
            return; 
        }

        // Standard Auto-Shifting (Only applies to forward gears)
        if (shiftCooldownTimer <= 0f)
        {
            if (currentGear > 0 && currentGear < trans.transmissionData.forwardGears.Length && currentRPM > logicData.upshiftRPM)
            {
                float currentRatio = trans.transmissionData.forwardGears[currentGear - 1];
                float nextRatio = trans.transmissionData.forwardGears[currentGear]; 
                
                float expectedRPM = currentRPM * (nextRatio / currentRatio);

                if (expectedRPM > (logicData.downshiftRPM + 200f))
                {
                    StartShift(1);
                }
            }
            else if (currentGear > 1 && currentRPM < logicData.downshiftRPM)
            {
                float currentRatio = trans.transmissionData.forwardGears[currentGear - 1];
                float prevRatio = trans.transmissionData.forwardGears[currentGear - 2]; 
                
                float expectedRPM = currentRPM * (prevRatio / currentRatio);

                if (expectedRPM < (powerTrain.engine._engineData.redlineRPM - 200f))
                {
                    StartShift(-1);
                }
            }
        }
    }

    private void StartShift(int direction)
    {
        isShifting = true;
        shiftTimer = 0f;
        pendingGearChange = direction;
    }

    private void CompleteShift()
    {
        if (pendingGearChange > 0) powerTrain.transmission.ShiftUp();
        else if (pendingGearChange < 0) powerTrain.transmission.ShiftDown();

        isShifting = false;
        pendingGearChange = 0;
    }

    private void HandleClutchLogic(float throttle, float dt)
    {
        if (isShifting)
        {
            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, 0f, dt * (1f / logicData.shiftDuration));
            return;
        }

        int currentGear = powerTrain.transmission.currentGear;

        if (currentGear == 0)
        {
            powerTrain.clutch.engagement = 0f;
            return;
        }

        if (currentGear == 1 || currentGear == -1)
        {
            float targetEngagement = 0f;

            if (logicData.hasTorqueConverterCreep) 
            {
                targetEngagement = logicData.idleCreepEngagement;
            }

            if (throttle > 0.01f || logicData.hasTorqueConverterCreep)
            {
                float rpmBite = Mathf.InverseLerp(logicData.biteRPM, logicData.lockRPM, powerTrain.engineRPM);
                targetEngagement = Mathf.Max(targetEngagement, rpmBite);
            }

            if (Mathf.Abs(powerTrain.transmissionInputRPM) > logicData.lockRPM) 
            {
                targetEngagement = 1f;
            }

            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, targetEngagement, dt * 5f);
        }
        else if (currentGear > 1)
        {
            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, 1f, dt * 15f);
        }
    }
}