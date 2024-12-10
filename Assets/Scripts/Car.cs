using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Car : MonoBehaviour {
    [SerializeField] private TransComponent trans;
    [SerializeField] private EngineComponent engine;
    [SerializeField] private WheelCollider fl,fr,bl,br;
    [SerializeField] private Transform fl_tire,fr_tire,bl_tire,br_tire;
    [SerializeField] private float maxSteeringAngle = 42f;
    [SerializeField] private AnimationCurve brakingCurve;
    [SerializeField] private bool isAWD = false;
    [SerializeField] private bool autoCenterSteering = false;

    [Header("Debug")]
    [SerializeField] private TextMeshPro text;
    [SerializeField] private TMPro.TMP_Text speedoText;
    private List<WheelCollider> allWheels;    
    private float throttlePos = 0.0f;
    private float brakePos = 0.0f;
    private float currentSteering = 0.0f;
    private float steeringLock;
    private Rigidbody rb;
    private void Start() {
        trans.Gear = 0;
        engine.Init();
        trans.SetClutch(0.0f);
        allWheels = new()
        {
            fl,
            bl,
            fr,
            br
        };

        allWheels.ForEach(w => w.brakeTorque = 0.0f);
        rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        float throttleInp = Mathf.Clamp01(Input.GetAxis("Vertical"));
        float brakeInp = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0);
        
        throttlePos += throttleInp * Time.deltaTime;
        if (throttleInp < 0.05f){
            throttlePos -= Time.deltaTime * 2.0f;
        }

        brakePos -= brakeInp * Time.deltaTime * 2.0f;
        if (brakeInp > -0.05f){
            brakePos -= Time.deltaTime * 2.0f;
        }

        trans.SetClutch(Input.GetKey(KeyCode.C) ? 1.0f : 0.0f);
        if(Input.GetKeyDown(KeyCode.LeftShift)) trans.Gear++;
        if (Input.GetKeyDown(KeyCode.LeftControl)) trans.Gear--;

        throttlePos = Mathf.Clamp01(throttlePos);
        brakePos = Mathf.Clamp01(brakePos);

        engine.Throttle = throttlePos;
        engine.UpdateEngine();

        float steerInput = Input.GetAxis("Horizontal");
        currentSteering += steerInput * Time.deltaTime * 5.0f;
        currentSteering = Mathf.Clamp(currentSteering, -1, 1);
        if (autoCenterSteering && Mathf.Abs(currentSteering) > 0.001f && Mathf.Abs(steerInput) < 0.01f){
            currentSteering = Mathf.Lerp(currentSteering, 0, Time.deltaTime * 5.0f);
        }
        
        float wheelBase = Vector3.Distance(fl_tire.position, br_tire.position);
        float trackWidth = Vector3.Distance(fl_tire.position, fr_tire.position);
        float turningRadius = wheelBase / Mathf.Tan(maxSteeringAngle * currentSteering * Mathf.Deg2Rad);

        float innerAckAngle = Mathf.Atan(wheelBase / (turningRadius - trackWidth * 0.5f));
        float outerAckAngle = Mathf.Atan(wheelBase / (turningRadius + trackWidth * 0.5f));

        Quaternion leftTire, rightTire;
        float leftAngle, rightAngle;
        // if currentSteering > 0: turning right
        // if currentSteering < 0: turning left
        if (currentSteering > 0){
            leftTire = Quaternion.Euler(new Vector3(0, Mathf.Rad2Deg * outerAckAngle, 0)); // left front wheel
            leftAngle = Mathf.Rad2Deg * outerAckAngle;
            rightTire = Quaternion.Euler(new Vector3(0, Mathf.Rad2Deg * innerAckAngle, 0)); // right front wheel
            rightAngle = Mathf.Rad2Deg * innerAckAngle;
        }
        else{
            leftTire = Quaternion.Euler(new Vector3(0, Mathf.Rad2Deg * innerAckAngle, 0)); // left front wheel
            leftAngle = Mathf.Rad2Deg * innerAckAngle;
            rightTire = Quaternion.Euler(new Vector3(0, Mathf.Rad2Deg * outerAckAngle, 0)); // right front wheel
            rightAngle = Mathf.Rad2Deg * outerAckAngle;
        }

        fl.steerAngle = leftAngle;
        fr.steerAngle = rightAngle;

        fl.GetWorldPose(out Vector3 posfl, out Quaternion quatfl);
        fl_tire.SetPositionAndRotation(posfl, quatfl);
        fr.GetWorldPose(out Vector3 posfr, out Quaternion quatfr);
        fr_tire.SetPositionAndRotation(posfr, quatfr);
        bl.GetWorldPose(out Vector3 posbl, out Quaternion quatbl);
        bl_tire.SetPositionAndRotation(posbl, quatbl);
        br.GetWorldPose(out Vector3 posbr, out Quaternion quatbr);
        br_tire.SetPositionAndRotation(posbr, quatbr);

        speedoText.text = $"{rb.linearVelocity.magnitude * 3.6:##.#} Kmph";
    }
    private void FixedUpdate() {
        float totalDriveTorque = trans.GetDriveTorque(engine);
        bl.GetGroundHit(out WheelHit rlHit);
        br.GetGroundHit(out WheelHit rrHit);

        float tractionL = 1f - Mathf.Clamp01(Mathf.Abs(rlHit.forwardSlip) + Mathf.Abs(rlHit.sidewaysSlip));
        float tractionR = 1f - Mathf.Clamp01(Mathf.Abs(rrHit.forwardSlip) + Mathf.Abs(rrHit.sidewaysSlip));
        float totalTraction = Mathf.Max(tractionL + tractionR, 0.01f);

        //float flt = 0.0f,frt = 0.0f,blt = 0.0f,brt = 0.0f;
        br.motorTorque = totalDriveTorque * tractionL / totalTraction;
        bl.motorTorque = totalDriveTorque * tractionR / totalTraction;

        fl.GetGroundHit(out WheelHit flHit);
        fl.GetGroundHit(out WheelHit frHit);
        float flTraction = 1f - Mathf.Clamp01(Mathf.Abs(flHit.forwardSlip));
        float frTraction = 1f - Mathf.Clamp01(Mathf.Abs(frHit.forwardSlip));

        float commonBreakForce = brakePos * 1000.0f * brakingCurve.Evaluate(flTraction);
        fl.brakeTorque = Mathf.Abs(fl.rpm) < 0.1f && Vector3.Dot(rb.linearVelocity, flHit.forwardDir) > 0.01f ? 0.0f : commonBreakForce;
        fr.brakeTorque = Mathf.Abs(fr.rpm) < 0.1f && Vector3.Dot(rb.linearVelocity, frHit.forwardDir) > 0.01f ? 0.0f : commonBreakForce;

        float avgDiffRPM = (bl.rpm * tractionL + br.rpm * tractionR) / totalTraction;
        engine.RPM = Mathf.Lerp(engine.RPM, trans.DifferentialToEngineRPM(avgDiffRPM), Time.fixedDeltaTime * 5.0f);
        text.SetText($"RPM: {engine.RPM:0000} {rlHit.forwardSlip:0.00} {rlHit.sidewaysSlip:0.00}");
    }
}