using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEditor.Analytics;
using System.Data.Common;

[System.Serializable]
public class Drivetrain{
    
    public float maxRPM;
    public float maxPower;
    public float stallRPM = 600;
    public float idleRPM = 1000;
    public float redLine;
    public TransType type = TransType.Automatic;
    [Range(0,5)] public List<float> gearRatios = new(new float[] { 2.53f, 2.2f, 1.8f, 1.2f, 1.0f, 0.8f });
    [Range(0,5)] public float reverseGearRatio = 2.8f;
    [Range(0,5)] public float finalDriveRatio = 3.0f;
    [Range(0, 1)] public float efficiency = 0.7f;
    public float shiftUpRPM = 2000;
    public float shiftDownRPM = 2000;
    public float shiftDelay = 1.5f;
    public float engineBrakingThreshold = 85f;
    [HideInInspector] public int Gear;
    [HideInInspector] public float clutchPosition;
    [HideInInspector] public float engineRPM;
    [HideInInspector] public float throttlePos;
    [HideInInspector] public float generatedTorque;
    [HideInInspector] public bool isRunning;

    private bool isStalling = false;
    public bool isShifting = false;
    public const float RPM2RADS = 2.0f * Mathf.PI / 60;
    public const float HP2W = 745.7f;
    public const float TORQUE_CONST = HP2W / RPM2RADS;
    private AutoTrans autoTrans;

    public void ShiftUp() { if (Gear < gearRatios.Count) {Gear++;}}
    public void ShiftDown() { if (Gear > -1) {Gear--;}}
    
    public void GotoReverse() {Gear = -1;}

    private void StallEngine() {

    }

    public void Init(){
        Gear = 0;
        engineRPM = stallRPM;
        autoTrans = new AutoTrans();
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
    
    public float DifferentialToEngineRPM(float diffRPM, int targetGear){
        if (targetGear < 0)
            return -diffRPM * (reverseGearRatio * finalDriveRatio);
        else if(targetGear == 0)
            return 0;
        else
            return diffRPM * (gearRatios[targetGear - 1] * finalDriveRatio);
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
            
            /* if (Gear == 0 && throttlePos > 0){
                ShiftUp();
            }

            if (engineTargetRPM < stallRPM && throttlePos < 0.05f){
                clutchPosition = 0;
                engineRPM = Mathf.Lerp(engineRPM, idleRPM + UnityEngine.Random.Range(-50, 50), dt);
            }

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
            }*/
            autoTrans.UpdateTrans(this, currentWheelRPM, dt);
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

    public interface ITransmissionState {
        // void OnEntry(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep);
        void HandleState(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep);
        // void OnExit(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep);
    }

    public class NeutralState : ITransmissionState
    {
        public void HandleState(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep)
        {
            if (dt.Gear == 0 && dt.throttlePos > 0.05f) dt.ShiftUp();

            if (dt.throttlePos > 0.05f) {
                trans.ChangeState(new RunningState());
                return;
            }

            dt.clutchPosition = 0;
            dt.engineRPM = Mathf.Lerp(dt.engineRPM, dt.idleRPM + UnityEngine.Random.Range(-50, 50), timeStep);
        }
    }

    public sealed class ShiftingUpState : ITransmissionState
    {
        private float shiftDelay = 0.5f;
        private readonly int targetGear;
        public ShiftingUpState(float shiftDelay, int targetGear) {
            this.shiftDelay = shiftDelay;
            this.targetGear = targetGear;
        }
        public void HandleState(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep)
        {
            float rpmToMatch = dt.DifferentialToEngineRPM(currentWheelRPM, targetGear);
            float rpmDiff = Mathf.Abs(rpmToMatch - dt.engineRPM);
            dt.clutchPosition = .0f;
            dt.isShifting = true;

            if (shiftDelay > 0.0f) {
                shiftDelay -= timeStep;
                dt.engineRPM = Mathf.Lerp(dt.engineRPM, rpmToMatch, timeStep * dt.shiftDelay);
                return;
            }

            dt.isShifting = false;
            dt.ShiftUp();
            trans.ChangeState(new RunningState());
        }
    }

    public sealed class ShiftingDownState : ITransmissionState {
        private float shiftDelay = 0.5f;
        private readonly int targetGear;
        public ShiftingDownState(float shiftDelay, int targetGear) {
            this.shiftDelay = shiftDelay;
            this.targetGear = targetGear;
        }
        
        public void HandleState(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep)
        {
            float rpmToMatch = dt.DifferentialToEngineRPM(currentWheelRPM, targetGear);
            float rpmDiff = Mathf.Abs(rpmToMatch - dt.engineRPM);
            dt.clutchPosition = .0f;
            dt.isShifting = true;
            if (shiftDelay > 0.0f) {
                shiftDelay -= timeStep;
                dt.engineRPM = Mathf.Lerp(dt.engineRPM, rpmToMatch, timeStep * dt.shiftDelay);
                return;
            }

            dt.isShifting = false;
            dt.ShiftDown();
            trans.ChangeState(new RunningState());
        }
    }

    public sealed class RunningState : ITransmissionState
    {
        public void HandleState(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep)
        {
            float engineTargetRPM = dt.DifferentialToEngineRPM(currentWheelRPM);
            float rand = UnityEngine.Random.Range(-50, 50);
            engineTargetRPM = Mathf.Clamp(engineTargetRPM, dt.idleRPM, dt.redLine + rand);

            if (dt.throttlePos < 0.05f && Mathf.Abs(currentWheelRPM) < 20) {
                dt.Gear = 0;
                trans.ChangeState(new NeutralState());
            }

            // crawl
            // we gradually release the clutch until rpm climbs to idle rpm
            if (engineTargetRPM < dt.idleRPM) {
                dt.Gear = dt.Gear > 0 ? 1 : -1;
                dt.clutchPosition = Mathf.Clamp(engineTargetRPM, dt.stallRPM, dt.idleRPM) * dt.throttlePos / dt.idleRPM;
                dt.engineRPM = Mathf.Lerp(dt.engineRPM, engineTargetRPM * dt.clutchPosition + (1 - dt.clutchPosition) * dt.idleRPM, timeStep);
                return;
            }

            float clutchTarget = currentWheelRPM > dt.engineBrakingThreshold ? 1 : dt.throttlePos;
            dt.clutchPosition = Mathf.Lerp(dt.clutchPosition, clutchTarget, timeStep * 0.25f);
            
            dt.engineRPM = Mathf.Lerp(dt.engineRPM, engineTargetRPM, timeStep);
            
            if (engineTargetRPM > dt.shiftUpRPM && dt.Gear > 0 && dt.Gear < dt.gearRatios.Count) {
                int targetGear = Mathf.Clamp(dt.Gear + 1, 1, dt.gearRatios.Count);
                trans.ChangeState(new ConsiderShift(targetGear, dt.shiftDelay * 0.75f));
            }

            else if (engineTargetRPM < dt.shiftDownRPM && dt.Gear > 1) {
                int targetGear = Mathf.Clamp(dt.Gear - 1, 1, dt.gearRatios.Count);
                trans.ChangeState(new ConsiderShift(targetGear, dt.shiftDelay * 0.75f));
            }

            Debug.Log($"Running: {currentWheelRPM}");
        }
    }

    public sealed class ConsiderShift : ITransmissionState {
        private readonly int targetGear;
        private float considerationTime;
        public ConsiderShift(int targetGear, float waitTime) {
            this.targetGear = targetGear;
            considerationTime = waitTime;
        }
        public void HandleState(AutoTrans trans, Drivetrain dt, float currentWheelRPM, float timeStep)
        {
            float engineTargetRPM = dt.DifferentialToEngineRPM(currentWheelRPM);
            considerationTime -= timeStep;

            dt.engineRPM = Mathf.Lerp(dt.engineRPM, engineTargetRPM, timeStep);
            
            if (dt.Gear < targetGear && engineTargetRPM < dt.shiftUpRPM) {
                trans.ChangeState(new RunningState());
                return;
            }
            
            else if (dt.Gear > targetGear && engineTargetRPM > dt.shiftDownRPM) {
                trans.ChangeState(new RunningState());
                return;
            }
            
            if (considerationTime < 0f){ 
                if (targetGear > dt.Gear) trans.ChangeState(new ShiftingUpState(dt.shiftDelay * 0.25f, targetGear));
                else trans.ChangeState(new ShiftingDownState(dt.shiftDelay * 0.25f, targetGear));
                return;
            }
        }
    }

    public class AutoTrans{
        private ITransmissionState _current;
        public AutoTrans(){
            _current = new NeutralState();
        }

        public void ChangeState(ITransmissionState newState) {
            _current = newState;
        }

        public void UpdateTrans(Drivetrain dt, float currentWheelRPM, float timeStep) {
            _current.HandleState(this, dt, currentWheelRPM, timeStep);
        }
    }

}