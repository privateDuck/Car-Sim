using Unity.Rendering.Universal;
using UnityEditor.Rendering.BuiltIn;
using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    [SerializeField] private float phaseDelay = 5f;
    [SerializeField] private Transform[] phase1;
    [SerializeField] private Transform[] phase2;
    [SerializeField] private Transform[] phase3;
    [SerializeField] private Transform[] phase4;
    private Material p1, p2, p3, p4;
    private int state;
    private float timer;
    int propID = Shader.PropertyToID("_State");
    void Start()
    {
        Material tmp = Resources.Load<Material>("traffic_temp");

        p1 = new Material(tmp);
        p2 = new Material(tmp);
        p3 = new Material(tmp);
        p4 = new Material(tmp);

        foreach (Transform p in phase1) {
            p.GetComponent<MeshRenderer>().material = p1;
        }
        foreach (Transform p in phase2) {
            p.GetComponent<MeshRenderer>().material = p2;
        }
        foreach (Transform p in phase3) {
            p.GetComponent<MeshRenderer>().material = p3;
        }
        foreach (Transform p in phase4) {
            p.GetComponent<MeshRenderer>().material = p4;
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer < 0.0f) {
            timer = phaseDelay;
            state = (state + 1) % 4;

            float s1 = state * 0.25f;
            float s2 = (state + 1) % 4 * 0.25f;
            float s3 = (state + 2) % 4 * 0.25f;
            float s4 = (state + 3) % 4 * 0.25f;

            p1.SetFloat(propID, s1);
            p2.SetFloat(propID, s2);
            p3.SetFloat(propID, s3);
            p4.SetFloat(propID, s4);
        }
    }

/*     public abstract class SignalPhase {
        public Transform _light1, _light2;
        public Material phaseMaterial;
        public float stateValue;
        public float timer;
        public float phaseDelay;
        public abstract void Simulate(Controller cont, float dt);
    }

    public class Phase1 : SignalPhase
    {
        int propID = Shader.PropertyToID("_state");
        public Phase1(float delay){
            this.phaseDelay = delay;
        }
        public override void Simulate(Controller cont, float dt)
        {
            timer -= dt;
            if (timer < 0f){
                cont.ChangeState(new Phase2(phaseDelay));
                timer = phaseDelay;
                stateValue += 0.25f;
                if (stateValue > 0.76f) stateValue = 0.0f;
                phaseMaterial.SetFloat(propID, stateValue);
                return;
            }
        }
    }
    public class Phase2 : SignalPhase
    {
        int propID = Shader.PropertyToID("_state");
        public Phase2(float delay){
            this.phaseDelay = delay;
        }
        public override void Simulate(Controller cont, float dt)
        {
            timer -= dt;
            if (timer < 0f){
                cont.ChangeState(new Phase3(phaseDelay));
                timer = phaseDelay;
                stateValue += 0.25f;
                if (stateValue > 0.76f) stateValue = 0.0f;
                phaseMaterial.SetFloat(propID, stateValue);
                return;
            }
        }
    }
    public class Phase3 : SignalPhase
    {
        int propID = Shader.PropertyToID("_state");
        public Phase3(float delay){
            this.phaseDelay = delay;
        }
        public override void Simulate(Controller cont, float dt)
        {
            timer -= dt;
            if (timer < 0f){
                cont.ChangeState(new Phase4(phaseDelay));
                timer = phaseDelay;
                stateValue += 0.25f;
                if (stateValue > 0.76f) stateValue = 0.0f;
                phaseMaterial.SetFloat(propID, stateValue);
                return;
            }
        }
    }
    public class Phase4 : SignalPhase
    {
        int propID = Shader.PropertyToID("_state");
        public Phase4(float delay){
            this.phaseDelay = delay;
        }
        public override void Simulate(Controller cont, float dt)
        {
            timer -= dt;
            if (timer < 0f){
                cont.ChangeState(new Phase1(phaseDelay));
                timer = phaseDelay;
                stateValue += 0.25f;
                if (stateValue > 0.76f) stateValue = 0.0f;
                phaseMaterial.SetFloat(propID, stateValue);
                return;
            }
        }
    }

    public class Controller {
        private SignalPhase current;
        private float phaseDelay;
        public Controller(float phaseDelay){
            this.phaseDelay = phaseDelay;
            current = new Phase1(phaseDelay);
        }

        public void ChangeState(SignalPhase newPhase) {
            current = newPhase;
        }

        public void Update(float timeStep) {
            current.Simulate(timeStep);
        }
    } */

}

