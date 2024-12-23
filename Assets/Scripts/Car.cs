using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Car : MonoBehaviour {

    [Header("DriveTrain Physics")]
    [SerializeField] private Drivetrain dt;
    [SerializeField] private WheelCollider fl,fr,bl,br;
    [SerializeField] private Transform fl_tire,fr_tire,bl_tire,br_tire;
    [SerializeField] private Transform centerOfMassTransform;
    [SerializeField] private float antiRollForce;
    [SerializeField] private float brakingPower;
    [SerializeField] private bool isAWD = false;


    [Header("Steering")]
    [SerializeField] private float maxSteeringAngle = 30f;
    [SerializeField] private float steeringLockLimit = 10f;
    [SerializeField] private float steeringLockEngageSpeed = 20f;
    [SerializeField] private float steeringLockMaxSpeed = 30f;
    [SerializeField] private bool autoCenterSteering = false;
    [SerializeField] private float steeringWheelMaxAngle = 180f;
    [SerializeField] private Transform steeringWheel;


    [Header("Debug")]
    [SerializeField] private TMPro.TMP_Text text;
    [SerializeField] private TMPro.TMP_Text gearText;

    #region privateMembers
    private List<WheelCollider> allWheels;    
    private float throttlePos = 0.0f;
    private float brakePos = 0.0f;
    private float currentSteering = 0.0f;
    private float steeringLock;
    private Rigidbody rb;
    private Transform tf;
    #endregion

    private void Start() {
        dt.Init();
        allWheels = new()
        {
            fl,
            bl,
            fr,
            br
        };

        allWheels.ForEach(w => w.brakeTorque = 0.0f);
        rb = GetComponent<Rigidbody>(); 
        tf = transform;
        rb.centerOfMass = tf.InverseTransformPoint(centerOfMassTransform.position);
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

        if (Input.GetKey(KeyCode.C)) dt.clutchPosition = 0.0f; else dt.clutchPosition = 1.0f;
        if(Input.GetKeyDown(KeyCode.LeftShift)) dt.ShiftUp();
        if (Input.GetKeyDown(KeyCode.LeftControl)) dt.ShiftDown();

        throttlePos = Mathf.Clamp01(throttlePos);
        brakePos = Mathf.Clamp01(brakePos);

        dt.throttlePos = throttlePos;

        float steerInput = Input.GetAxis("Horizontal");
        currentSteering += steerInput * Time.deltaTime * 5.0f;
        currentSteering = Mathf.Clamp(currentSteering, -1, 1);
        if (autoCenterSteering && Mathf.Abs(currentSteering) > 0.001f && Mathf.Abs(steerInput) < 0.01f){
            currentSteering = Mathf.Lerp(currentSteering, 0, Time.deltaTime * 5.0f);
        }
        steeringWheel.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, currentSteering * steeringWheelMaxAngle, 0));

        float fwdRatio = Mathf.Max(Mathf.Abs(Vector3.Dot(rb.linearVelocity, tf.forward)) - steeringLockEngageSpeed, 0)/steeringLockMaxSpeed;
        steeringLock = Mathf.Lerp(maxSteeringAngle, steeringLockLimit, fwdRatio);

        float wheelBase = Vector3.Distance(fl_tire.position, br_tire.position);
        float trackWidth = Vector3.Distance(fl_tire.position, fr_tire.position);
        float turningRadius = wheelBase / Mathf.Tan(steeringLock * currentSteering * Mathf.Deg2Rad);

        float innerAckAngle = Mathf.Atan(wheelBase / (turningRadius - trackWidth * 0.5f));
        float outerAckAngle = Mathf.Atan(wheelBase / (turningRadius + trackWidth * 0.5f));

        float leftAngle, rightAngle;
        // if currentSteering > 0: turning right
        // if currentSteering < 0: turning left
        if (currentSteering > 0){
            leftAngle = Mathf.Rad2Deg * outerAckAngle; // left front wheel
            rightAngle = Mathf.Rad2Deg * innerAckAngle; // right front wheel
        }
        else{
            leftAngle = Mathf.Rad2Deg * innerAckAngle; // left front wheel
            rightAngle = Mathf.Rad2Deg * outerAckAngle; // right front wheel
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

        float carSpeedKPH = bl.rpm * Mathf.PI * fl.radius * 3.6f * 0.033333f;
        text.SetText($"RPM: {dt.engineRPM:0000} Speed:{carSpeedKPH:##.0} Kmph");

        if (dt.isShifting) gearText.text = "O";
        else if (dt.Gear == 0) gearText.text = "D";
        else if (dt.Gear == -1) gearText.text = "R";
        else gearText.text = dt.Gear.ToString();
    }
    private void FixedUpdate() {
        bool blGrounded = bl.GetGroundHit(out WheelHit rlHit);
        bool brGrounded = br.GetGroundHit(out WheelHit rrHit);
        bool flGrounded = fl.GetGroundHit(out WheelHit flHit);
        bool frGrounded = fr.GetGroundHit(out WheelHit frHit);
        
        if (rb.linearVelocity.sqrMagnitude > 25.0f){
        
            float travelBL = 1.0f, travelBR = 1.0f;
            float travelFL = 1.0f, travelFR = 1.0f;

            if (blGrounded)
                travelBL = (-bl_tire.InverseTransformPoint(rlHit.point).y - bl.radius)/bl.suspensionDistance;
            if (brGrounded)
                travelBR = (-br_tire.InverseTransformPoint(rrHit.point).y - br.radius)/br.suspensionDistance;

            float antiRollForceBack = (travelBL - travelBR) * antiRollForce;
            
            if (flGrounded)
                travelFL = (-fl_tire.InverseTransformPoint(flHit.point).y - fl.radius)/fl.suspensionDistance;
            if (frGrounded)
                travelFR = (-fr_tire.InverseTransformPoint(frHit.point).y - fr.radius)/fr.suspensionDistance;

            float antiRollForceFront = (travelFL - travelFR) * antiRollForce;
            
            if (blGrounded) rb.AddForceAtPosition(-bl_tire.up * antiRollForceBack, bl_tire.position);
            if (brGrounded) rb.AddForceAtPosition(br_tire.up * antiRollForceBack, br_tire.position);

            if (flGrounded) rb.AddForceAtPosition(-fl_tire.up * antiRollForceFront, fl_tire.position);
            if (frGrounded) rb.AddForceAtPosition(fr_tire.up * antiRollForceFront, fr_tire.position);

        }
        float tractionL = 1f - Mathf.Clamp01(Mathf.Abs(rlHit.forwardSlip) + Mathf.Abs(rlHit.sidewaysSlip));
        float tractionR = 1f - Mathf.Clamp01(Mathf.Abs(rrHit.forwardSlip) + Mathf.Abs(rrHit.sidewaysSlip));
        float totalTraction = Mathf.Max(tractionL + tractionR, 0.01f);
        
        float avgDiffRPM = (bl.rpm + br.rpm) * 0.5f;
        float totalDriveTorque = dt.SimulateAndGetTorque(avgDiffRPM, Time.fixedDeltaTime);

        br.motorTorque = totalDriveTorque * 0.5f;
        bl.motorTorque = totalDriveTorque * 0.5f;

        float commonBreakForce = brakePos * brakingPower;
        fl.brakeTorque = commonBreakForce;
        fr.brakeTorque = commonBreakForce;
    }
}