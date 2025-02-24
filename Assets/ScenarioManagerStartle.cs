using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioManagerStartle : MonoBehaviour
{
    public string currentStimulus;
    public string currentScenario;
    public bool isScenarioActive = false;

    // Start is called before the first frame update
    void Start()
    {
        currentStimulus = "random";
        currentScenario = "random";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
