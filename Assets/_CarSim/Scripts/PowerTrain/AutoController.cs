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
    private const float COOLDOWN_DURATION = 0.5f; // Half-second breathing room

    public void Initialize(PowerTrain pt, AutoControllerLogicData data)
    {
        powerTrain = pt;
        logicData = data;
        shiftTimer = 0f;
        isShifting = false;
        pendingGearChange = 0;
        shiftCooldownTimer = 0f;
    }

    public void UpdateController(float dt)
    {
        if (powerTrain == null || logicData == null) return;

        HandleShiftingLogic(dt);
        HandleClutchLogic(dt); 
    }

    private void HandleShiftingLogic(float dt)
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
        int maxGear = trans.transmissionData.forwardGears.Length;
        float currentRPM = powerTrain.engineRPM;

        if (currentGear == 0)
        {
            StartShift(1);
            return; 
        }

        if (shiftCooldownTimer <= 0f)
        {
            // UPSHIFT LOGIC
            if (currentGear > 0 && currentGear < maxGear && currentRPM > logicData.upshiftRPM)
            {
                float currentRatio = trans.transmissionData.forwardGears[currentGear - 1];
                float nextRatio = trans.transmissionData.forwardGears[currentGear]; // Index of next gear
                
                // Gear ratio math: Predict what the RPM will be in the higher gear
                float expectedRPM = currentRPM * (nextRatio / currentRatio);

                // Only upshift if the drop won't instantly trigger a downshift
                if (expectedRPM > (logicData.downshiftRPM + 200f))
                {
                    StartShift(1);
                }
            }
            // DOWNSHIFT LOGIC
            else if (currentGear > 1 && currentRPM < logicData.downshiftRPM)
            {
                float currentRatio = trans.transmissionData.forwardGears[currentGear - 1];
                float prevRatio = trans.transmissionData.forwardGears[currentGear - 2]; 
                
                float expectedRPM = currentRPM * (prevRatio / currentRatio);

                // Only downshift if it won't blow up the engine past redline
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

    private void HandleClutchLogic(float dt)
    {
        if (isShifting)
        {
            // Smoothly disengage during a shift to prevent violent torque snaps
            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, 0f, dt * (1f / logicData.shiftDuration));
            return;
        }

        int currentGear = powerTrain.transmission.currentGear;

        if (currentGear == 1 || currentGear == -1)
        {
            // Launch control logic
            float targetEngagement = Mathf.InverseLerp(logicData.biteRPM, logicData.lockRPM, powerTrain.engineRPM);
            
            // Override: If the car is rolling fast enough, force full engagement
            if (Mathf.Abs(powerTrain.transmissionInputRPM) > logicData.lockRPM) 
            {
                targetEngagement = 1f;
            }

            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, targetEngagement, dt * 5f);
        }
        else if (currentGear > 1)
        {
            // Rapidly dump the clutch back to 100% after a 2nd+ gear shift
            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, 1f, dt * 15f);
        }
        else
        {
            powerTrain.clutch.engagement = 0f;
        }
    }
}