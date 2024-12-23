using System.Collections.Generic;
using Unity.Rendering.Universal;
using UnityEditor.Rendering.BuiltIn;
using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    [SerializeField] private float phaseDelay = 5f;
    [SerializeField] private List<TrafficPhase> trafficPhases;

    private int currentPhase = 0;

}

