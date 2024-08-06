using System;
using System.IO.Ports;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class Phototransistor : MonoBehaviour
{

    wrmhl photo = new wrmhl();
    public string portName = "COM7";
    public int baudRate = 2000000;
    public int ReadTimeout = 5000;
    public int QueueLength = 1;

    public static Phototransistor phototransistor;
    //SerialPort _serialPort;
    public float lightLevel;
    public string reading;
    public bool IsConnected = true;
    public string[] line;

    float dt = 0.0f;
    // Start is called before the first frame update
    void Start()
    {
        phototransistor = this;

        photo.set(portName, baudRate, ReadTimeout, QueueLength);
        photo.connect();

    }

    // Update is called once per frame
    public async void Update()
    {
        try
        {
            reading = photo.readQueue();
            
            lightLevel = float.Parse(reading);
            Debug.Log(lightLevel);


        }
        catch (Exception e1)
        {

            //Debug.Log("exception in phototransitor------------------------");
            Debug.LogException(e1, this);

        }
        await new WaitForUpdate();







    }

    private void OnDisable()
    {
        photo.close();
    }

    private void OnApplicationQuit()
    {

        photo.close();
    }
}
