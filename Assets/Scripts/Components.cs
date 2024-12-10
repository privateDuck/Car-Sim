using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;

[Serializable]
public class DummyClass{
    public float dummy;
}

[Serializable]
public class EngineComponent {
    public float maxRPM;
    public float maxPower;
    public float stallRPM = 1000;
    public float redLine;
    public const float RPM2RADS = 2.0f * Mathf.PI / 60;
    public const float HP2W = 745.7f;
    public const float TORQUE_CONST = HP2W / RPM2RADS;

    public void Init(){
        RPM = stallRPM + 200;
    }

    private float GetEngineTorque(float rpm){
        float P1 = maxPower / maxRPM;
        float P2 = maxPower / (maxRPM * maxRPM);
        float P3 = - maxPower / (maxRPM * maxRPM * maxRPM);
        return (P1 + rpm * P2 + rpm * rpm * P3) * TORQUE_CONST * Throttle;
    }

    public float GetEngineTorque(){
        return GetEngineTorque(RPM);
    }
    
    public void Stall(){

    }

    public void UpdateEngine(){
        generatedTorque = GetEngineTorque(RPM);
    }

    [DoNotSerialize] public float RPM {get; set;}
    [DoNotSerialize] public float Throttle {get;set;}
    private float generatedTorque;
}

[Serializable]
public class TransComponent{
    public Type type = Type.Automatic;
    [Range(0,5)] public List<float> gearRatios = new List<float>(new float[] { 2.53f, 2.2f, 1.8f, 1.2f, 1.0f, 0.8f });
    [Range(0,5)] public float reverseGearRatio = 2.8f;
    [Range(0,5)] public float finalDriveRatio = 3.0f;
    [Range(0, 1)] public float efficiency = 0.7f;
    [DoNotSerialize] public float RPM {get;set;}
    [DoNotSerialize] public int Gear {get;set;}

    /// <param name="input">input torque</param>
    /// <param name="gear">-1: reverse, 0: neutral, 1 - n: gears</param>
    private float OutputTorque(float input, int gear){
        if (gear < 0)
            return -input * reverseGearRatio * finalDriveRatio * efficiency;
        else if(gear == 0)
            return 0;
        else
            return input * gearRatios[gear - 1] * finalDriveRatio * efficiency;
    }

    public float DifferentialToEngineRPM(float diffRPM){
        if (Gear < 0)
            return -diffRPM * (reverseGearRatio * finalDriveRatio);
        else if(Gear == 0)
            return 0;
        else
            return diffRPM * (gearRatios[Gear - 1] * finalDriveRatio);
    }

    public void SetClutch(float value){
        if (type == Type.Manual)
            clutchPosition = 1.0f - value;
    }

    public float GetDriveTorque(EngineComponent engine){
        float engineT = engine.GetEngineTorque();
        return OutputTorque(engineT, Gear) * clutchPosition; // only for manual
    }

    private float clutchPosition;
    public enum Type{
        Manual, Automatic
    }
}