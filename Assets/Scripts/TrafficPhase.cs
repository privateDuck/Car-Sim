using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficPhase : MonoBehaviour {
    
    public string Name;
    [Tooltip("Red light time")]
    public float PhaseInactiveTime = 2.0f;
    [Tooltip("Green light time")]
    public float PhaseActiveTime = 10.0f;
    [Tooltip("Amber light time")]
    public float PhaseEndTime = 2.0f;
    public List<MeshRenderer> mrs;
    public LightState state = LightState.None;    
    private Material material;
    private readonly int stateID = Shader.PropertyToID("_State");
    private readonly int powerID = Shader.PropertyToID("_Power");
    private void Start() {
        Material tmp = Resources.Load<Material>("traffic_temp");
        material = new Material(tmp);
        Resources.UnloadAsset(tmp);

        foreach (var mr in mrs) {
            mr.material = material;
        }
    }

    public void ChangeLightState(LightState toState) {
        state = toState;

        ChangeLights();
        SetLights(true);
        if (state == LightState.Caution || state == LightState.Malfunction) {
            StartCoroutine(BlinkerProgram());
        }
    }

    private IEnumerator BlinkerProgram() {
        while(true) {
            if (state != LightState.Caution || state == LightState.Malfunction)
            {
                ChangeLights();
                yield break;
            }

            SetLights(true);
            yield return new WaitForSeconds(1.0f);
            SetLights(false);
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void ChangeLights() {
        
        float stateValue = 0.0f;

        switch (state)
        {
            case LightState.None:
                Debug.LogError("Do not set state to None");
                break;
            case LightState.Malfunction:
            case LightState.Red:
                stateValue = 0.0f;
                break;
            case LightState.ChangingToGreen:
                stateValue = 0.25f;
                break;
            case LightState.Green:
                stateValue = 0.5f;
                break;
            case LightState.ChangingToRed:
            case LightState.Caution:
                stateValue = 0.75f;
                break;
        }

        material.SetFloat(stateID, stateValue);
    }

    private void SetLights(bool isTurnedOn){
        material.SetFloat(powerID, isTurnedOn ? 1.0f : 0.0f);
    }

    public enum LightState {
        None, Red, ChangingToGreen, Green, ChangingToRed, Caution, Malfunction
    }
}