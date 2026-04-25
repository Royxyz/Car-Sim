using UnityEngine;

[System.Serializable]
public class AutoController
{
    public AutoControllerLogicData logicData;
    public PowerTrain powerTrain;

    private float shiftTimer;
    public bool isShifting { get; private set; }
    private int pendingGearChange;

    public void Initialize(PowerTrain pt, AutoControllerLogicData data)
    {
        powerTrain = pt;
        logicData = data;
        shiftTimer = 0f;
        isShifting = false;
        pendingGearChange = 0;
    }

    public void UpdateController(float dt)
    {
        if (powerTrain == null || logicData == null) return;

        HandleShiftingLogic(dt);
        HandleClutchLogic(dt); 
    }
    private void HandleShiftingLogic(float dt)
    {
        if (isShifting)
        {
            shiftTimer += dt;
            if (shiftTimer >= logicData.shiftDuration)
            {
                CompleteShift();
            }
            return;
        }

        int currentGear = powerTrain.transmission.currentGear;
        int maxGear = powerTrain.transmission.transmissionData.forwardGears.Length;
        float currentRPM = powerTrain.engineRPM;

        if (currentGear > 0 && currentGear < maxGear && currentRPM > logicData.upshiftRPM)
        {
            StartShift(1);
        }
        else if (currentGear > 1 && currentRPM < logicData.downshiftRPM)
        {
            StartShift(-1);
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
        if (pendingGearChange > 0)
        {
            powerTrain.transmission.ShiftUp();
        }
        else if (pendingGearChange < 0)
        {
            powerTrain.transmission.ShiftDown();
        }

        isShifting = false;
        pendingGearChange = 0;
    }

    private void HandleClutchLogic(float dt)
    {
        if (isShifting)
        {
            powerTrain.clutch.engagement = 0f;
            return;
        }

        int currentGear = powerTrain.transmission.currentGear;

        if (currentGear == 1 || currentGear == -1)
        {
            float targetEngagement = Mathf.InverseLerp(logicData.biteRPM, logicData.lockRPM, powerTrain.engineRPM);
            powerTrain.clutch.engagement = targetEngagement;
        }
        else if (currentGear > 1)
        {
            powerTrain.clutch.engagement = Mathf.MoveTowards(powerTrain.clutch.engagement, 1f, dt * 5f);
        }
        else
        {
            powerTrain.clutch.engagement = 0f;
        }
    }
}