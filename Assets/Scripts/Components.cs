using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEditor.Analytics;
using System.Data.Common;

[System.Serializable]
public class CarDynamics{
    
    public float maxRPM;
    public float maxPower;
    public float stallRPM = 600;
    public float idleRPM = 1000;
    public float redLine;
    public TransType type = TransType.Automatic;
    [Range(0,5)] public List<float> gearRatios = new List<float>(new float[] { 2.53f, 2.2f, 1.8f, 1.2f, 1.0f, 0.8f });
    [Range(0,5)] public float reverseGearRatio = 2.8f;
    [Range(0,5)] public float finalDriveRatio = 3.0f;
    [Range(0, 1)] public float efficiency = 0.7f;
    public float shiftUpRPM = 2000;
    public float shiftDownRPM = 2000;
    public float shiftDelay = 1.5f;

    [HideInInspector] public int Gear;
    [HideInInspector] public float clutchPosition;
    [HideInInspector] public float engineRPM;
    [HideInInspector] public float throttlePos;
    [HideInInspector] public float generatedTorque;
    [HideInInspector] public bool isRunning;

    private bool isStalling = false;
    private float shiftTimer = -0.5f;
    public const float RPM2RADS = 2.0f * Mathf.PI / 60;
    public const float HP2W = 745.7f;
    public const float TORQUE_CONST = HP2W / RPM2RADS;


    public void ShiftUp() { if (Gear < gearRatios.Count) {Gear++;}}
    public void ShiftDown() { if (Gear > -1) {Gear--;}}
    
    public void GotoReverse() {Gear = -1;}

    private void StallEngine() {

    }

    public void Init(){
        Gear = 0;
        engineRPM = stallRPM;
    }

    private float OutputTorque(float input){
        if (Gear < 0)
            return -input * reverseGearRatio * finalDriveRatio * efficiency * clutchPosition;
        else if(Gear == 0)
            return 0;
        else
            return input * gearRatios[Gear - 1] * finalDriveRatio * efficiency * clutchPosition;
    }

    public float DifferentialToEngineRPM(float diffRPM){
        if (Gear < 0)
            return -diffRPM * (reverseGearRatio * finalDriveRatio);
        else if(Gear == 0)
            return 0;
        else
            return diffRPM * (gearRatios[Gear - 1] * finalDriveRatio);
    }

    private float GetEngineTorqueAt(float rpm){
        float P1 = maxPower / maxRPM;
        float P2 = maxPower / (maxRPM * maxRPM);
        float P3 = - maxPower / (maxRPM * maxRPM * maxRPM);
        return (P1 + rpm * P2 + rpm * rpm * P3) * TORQUE_CONST * throttlePos;
    }

    public float SimulateAndGetTorque(float currentWheelRPM, float dt) {

        float engineTargetRPM = DifferentialToEngineRPM(currentWheelRPM);

        if ( type == TransType.Automatic ){
            
            if (Gear == 0 && throttlePos > 0){
                ShiftUp();
            }

            if (engineTargetRPM < stallRPM && throttlePos < 0.05f){
                clutchPosition = 0;
                engineRPM = Mathf.Lerp(engineRPM, idleRPM + UnityEngine.Random.Range(-50, 50), dt);
            }

            /* else if (engineTargetRPM > stallRPM && engineTargetRPM < idleRPM) {
                clutchPosition = Mathf.Lerp(0, 1, (engineTargetRPM + UnityEngine.Random.Range(-50, 50))/idleRPM);
                engineRPM = Mathf.Lerp(idleRPM, engineTargetRPM, clutchPosition);
            } */
            else {

                if (shiftTimer > 0.0f) {
                    shiftTimer -= dt;
                }
                else {
                    clutchPosition = 1.0f;
                }

                if (engineTargetRPM < idleRPM)                
                    clutchPosition = Mathf.Clamp(engineTargetRPM, stallRPM, idleRPM) * throttlePos / idleRPM;

                if (engineRPM > shiftUpRPM && shiftTimer <= 0.0f) {
                    ShiftUp();
                    engineTargetRPM = DifferentialToEngineRPM(currentWheelRPM);
                    clutchPosition = 0.0f;
                    shiftTimer = shiftDelay;
                }
                else if (engineRPM < shiftDownRPM && Gear > 1 && shiftTimer <= 0.0f){
                    ShiftDown();
                    engineTargetRPM = DifferentialToEngineRPM(currentWheelRPM);
                    clutchPosition = 0.0f;
                    shiftTimer = shiftDelay;
                }

                engineRPM = Mathf.Lerp(engineRPM, engineTargetRPM * clutchPosition + (1 - clutchPosition) * idleRPM, dt);
            }
        }

        else {
            if (engineTargetRPM < stallRPM || isStalling){
                StallEngine();
                engineRPM = Mathf.Lerp(engineRPM, 0, dt * 1.5f);
                return 0;
            }

            else if (engineTargetRPM > stallRPM && engineTargetRPM < idleRPM) {
                engineRPM = Mathf.Lerp(idleRPM, engineTargetRPM, clutchPosition);
            }

            else {
                engineRPM = Mathf.Lerp(engineRPM, engineTargetRPM, dt);
            }
        }

        float eTorque = GetEngineTorqueAt(engineRPM);
        float torqueAtWheels = OutputTorque(eTorque);
        return torqueAtWheels;
    }

    public enum TransType{
        Manual, Automatic
    }
}