using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SensorTracker : MonoBehaviour
{
    public TMP_Text ttlState;
    public TMP_Text headDirectionState;
    public TMP_Text accelerometerState;
    public TMP_Text mcState;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        ttlState.text = string.Format("TTL: {0}", PlayerPrefs.GetString("TTL State"));
        accelerometerState.text = string.Format("Accelerometer: {0}", PlayerPrefs.GetString("Accel State"));
        headDirectionState.text = string.Format("Head Direction: {0}", PlayerPrefs.GetString("Ring State"));
        mcState.text = string.Format("MC State: {0}", PlayerPrefs.GetFloat("Enable MC"));


    }
}
