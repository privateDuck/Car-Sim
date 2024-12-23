using System.Collections.Generic;
using System.Collections;
using Unity.Rendering.Universal;
using UnityEditor.Rendering.BuiltIn;
using UnityEngine;
using Unity.Collections;

public class TrafficLightManager : MonoBehaviour
{
    [SerializeField] private float phaseDelay = 5f;
    [SerializeField] private List<TrafficPhase> trafficPhases;
    [SerializeField] private OperationMode operationMode = OperationMode.Normal;
    private TrafficPhase currentPhase;
    private int currentPhaseIdx = 0;

    private void Start() {
        SetOperationMode(OperationMode.Normal);
    }

    private void SetOperationMode(OperationMode mode) {
        operationMode = mode;
        StopAllCoroutines();

        switch (operationMode)
        {
            case OperationMode.Normal:
                StartCoroutine(Sequencer());
                break;
            case OperationMode.CautionBlink:
                SetAllPhasesTo(TrafficPhase.LightState.Caution);
                break;
            case OperationMode.MalfunctionBlink:
                SetAllPhasesTo(TrafficPhase.LightState.Malfunction);
                break;
            default:
                StartCoroutine(Sequencer());
                break;
        }
    }

    private IEnumerator Sequencer()
    {
        currentPhaseIdx = 0;
        currentPhase = trafficPhases[0];

        SetAllPhasesTo(TrafficPhase.LightState.Red);

        while (true)
        {
            currentPhase.ChangeLightState(TrafficPhase.LightState.ChangingToGreen);
            yield return new WaitForSeconds(currentPhase.PhaseInactiveTime);

            currentPhase.ChangeLightState(TrafficPhase.LightState.Green);
            yield return new WaitForSeconds(currentPhase.PhaseActiveTime);

            currentPhase.ChangeLightState(TrafficPhase.LightState.ChangingToRed);
            yield return new WaitForSeconds(currentPhase.PhaseEndTime);

            currentPhase.ChangeLightState(TrafficPhase.LightState.Red);
            yield return new WaitForSeconds(phaseDelay);

            currentPhaseIdx++;
            currentPhaseIdx = currentPhaseIdx % trafficPhases.Count;

            currentPhase = trafficPhases[currentPhaseIdx];
        }
    }

    private void SetAllPhasesTo(TrafficPhase.LightState lightState) {
        foreach (var trafficPhase in trafficPhases)
        {
            trafficPhase.ChangeLightState(lightState);
        }
    }

    public enum OperationMode {
        Normal, CautionBlink, MalfunctionBlink
    }
}   

