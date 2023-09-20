﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using static BallController;
using static LabJackController;
using static MotionCueingController;
using static RewardArena;
using static RingSensor;
using EasyModbus;


[DisallowMultipleComponent]
public class MotorController : MonoBehaviour
{
    wrmhl motor = new wrmhl();
    public static MotorController motorController;

    //public SerialController serialController;

    float prevValue = 0;
    float value = 2.5f;
    float preCompensateValue = 0;
    float compensateValue_game = 0;
    float compensateValue_yaw = 0;

    public bool IsConnected = true;
    public float deltaYaw = 0.0f;
    public float yawVel = 0.0f;
    public float yawVelMax;
    public float PL_FB_Decimal = 0;
    public float origin_PL_FB_Decimal;

    public SerialPort _serialPort;
    public int ReadTimeout = 5000;
    public int QueueLength = 1;
    public string portName = "COM999";
    public int baudRate = 2000000;
    public List<Dictionary<string, object>> data;

    public double yawAngle = 0.0f;
    public int updatesCounter = 0;

    float roll = 0.0f;
    float yaw = 0.0f;
    int idx = 0;

    ModbusClient modbusClient;

    // Start is called before the first frame update

    void Start()
    {
        motorController = this;
        // this is needed to send normalized values to yawmotor (-1, 1)
        // then set analog gain in kollmorgen as yawvelmax/1.9
        // I couldnt use -2.5 and 2.5 V because of hardware
        yawVelMax = 200.0f;


        //serialController = GameObject.Find("SerialController").GetComponent<SerialController>();
        //_serialPort = new SerialPort();
        //motor.set(portName, baudRate, ReadTimeout, QueueLength);
        //motor.connect();


        //// Change com port
        //_serialPort.PortName = "COM10";
        //// Change baud rate
        //_serialPort.BaudRate = 2000000;
        //_serialPort.DtrEnable = true;
        //_serialPort.RtsEnable = true;
        //// Timeout after 0.5 seconds.
        //_serialPort.ReadTimeout = 2;
        //_serialPort.WriteTimeout = 2;

        //try
        //{
        //    _serialPort.Open();
        //}
        //catch (Exception e)
        //{
        //    Debug.LogError(e);
        //    IsConnected = false;
        //}

        Console.WriteLine("started Modbus TCP connection process");
        modbusClient = new ModbusClient("192.168.0.22", 502);
        modbusClient.Connect();

        int[] readHoldingRegisters = { };

        try
        {
            readHoldingRegisters = modbusClient.ReadHoldingRegisters(588, 4);
            //print(readHoldingRegisters[2] + "," + readHoldingRegisters[3]);
            for (int i = 0; i < readHoldingRegisters.Length; i++)
                Console.WriteLine("Value of HoldingRegister " + (i + 1) + " " + readHoldingRegisters[i].ToString());

            long combinedNumber = 0;
            for (int i = 0; i < 4; i++)
            {
                combinedNumber = (combinedNumber << 16) | (ushort)readHoldingRegisters[i];

            }
            origin_PL_FB_Decimal = (float)combinedNumber / 1000;
            //print("Original Angle: " + origin_PL_FB_Decimal);

        }
        catch (Exception e)
        {
            //print("PL.FB motor reading failed");
        }

    }

    // Update is called once per frame
    public async void Update()
    {
        if ((int)PlayerPrefs.GetFloat("Enable MC") == 1)
        {
            yawVel = (float)motionCueingController.motionCueing.filtered[2][2];

        }
        else
        {
            yawVel = Ball.yawVel;
        }


        // 1.9 = 0 V
        yawVel /= yawVelMax;
        

        // -1, 1 min, max ang vel
        if (yawVel > 1)
        {
            yawVel = 1.0f;

        }
        else if (yawVel < -1)
        {
            yawVel = -1.0f;
        }


        //////////////////////////////////////////////////// alignment ////////////////////////////////////////////////////////////////

        /// the yawEulerAngles is calculated by the accumulation of angle change each frame.
        /// when rotating counter clockwise, this value can be negative
        /// when rotating a full cycle it can be greater than 360. Need to turn it to [0,360] for comparison
        yawAngle += (value - 2.5f) * 80.343f * Time.deltaTime;

        if (yawAngle < 0)
        {
            yawAngle += 360f;
        }
        if (yawAngle > 360)
        {
            
            yawAngle -= 360f;
        }

        //////////////////////////////////////////////////// alignment ////////////////////////////////////////////////////////////////

        int[] readHoldingRegisters = { };

        try
        {
            readHoldingRegisters = modbusClient.ReadHoldingRegisters(588, 4);
            //print(readHoldingRegisters[2] + "," + readHoldingRegisters[3]);
            for (int i = 0; i < readHoldingRegisters.Length; i++)
                Console.WriteLine("Value of HoldingRegister " + (i + 1) + " " + readHoldingRegisters[i].ToString());

            // readHoldingRegisters[2] stores how many borrows made in calculation, 286 is the mechanical offset
            //PL_FB_Decimal = (readHoldingRegisters[2] * 65535 + readHoldingRegisters[3]) / 1000f - 286; //286 is the offset of the yaw motor
            //print((readHoldingRegisters[2] * 65535 + readHoldingRegisters[3]) / 1000f);
            //print(PL_FB_Decimal);


            // This code is used to convert the 2-byte signed value two's complement Big endian style array from readHoldingRegisters to the actual degrees.
            long combinedNumber = 0;
            for (int i = 0; i < 4; i++)
            {
                combinedNumber = (combinedNumber << 16) | (ushort)readHoldingRegisters[i];
            
            }
            PL_FB_Decimal = (float)combinedNumber / 1000 - origin_PL_FB_Decimal;
            //print("Original angle:" + origin_PL_FB_Decimal);
            //print("Combined Number: " + (float)combinedNumber / 1000);
            //print("PL_FB : " + PL_FB_Decimal);

        }
        catch (Exception e)
        {
            //print("PL.FB motor reading failed");
        }
        


        if (PL_FB_Decimal < 0)
        {
            PL_FB_Decimal += 360f;
        }
        if (PL_FB_Decimal > 360)
        {
            PL_FB_Decimal -= 360f;
        }

        //print("PL_FB : " + PL_FB_Decimal);
        //print("Player yaw angle : " + SharedReward.player.transform.eulerAngles[1]);
        //var angleDiff = SharedReward.player.transform.eulerAngles[1] - PL_FB_Decimal;

        //print("angle diff is : " + angleDiff);

        var angleDiff = SharedReward.player.transform.eulerAngles.y - PL_FB_Decimal;

        //print(SharedReward.player.transform.eulerAngles.y);
        print("PL_FB : " + PL_FB_Decimal);
        print("angle diff is : " + angleDiff);


        if (angleDiff < 5 || angleDiff > 300)
        {
            angleDiff = 0;
        }



        if (updatesCounter % 5 == 0)
        {
            updatesCounter = 0;
            compensateValue_yaw = (float)angleDiff / 8.0343f / Time.deltaTime / 6.25f;
            compensateValue_game = (float)angleDiff / 80.343f / Time.deltaTime;

            //// adsjusting the analog signal sending to the motor with fixed values
            value = (yawVel) * 2.5f + 2.5f; //2.5V makes motor stationary and anything below makes it rotate counterclockwise and abov clockwise

            

            //if (angleDiff > 8 && angleDiff < 16)
            //{
            //    value += 0.1f;
            //}
            //else if (angleDiff > 16)
            //{
            //    value += 0.2f;
            //}
            //else if (angleDiff < -8 && angleDiff > -16)
            //{
            //    value -= 0.1f;
            //}
            //else if (angleDiff < -16)
            //{
            //    value -= 0.2f;
            //}


            //float voltageAdjustment = (float)angleDiff / 8;
            //value += voltageAdjustment;


            // we don't need to send to arduino if it's the same value
            if (!(value == prevValue))
            {
                labJackController.ExecuteDACRequest(value);
                //yawAngle += compensateValue_game * 80.343f * Time.deltaTime;
                // yawAngle += angleDiff;
                prevValue = value;


            }
        }




        updatesCounter++;


    }

    //public void SetValue(float input)
    //{
    //    if (!(input.GetType() != typeof(float)))
    //    {
    //        Debug.LogWarning("MotorController: input type not int or float.");
    //        return;
    //    }

    //    var lerp = Mathf.RoundToInt(Mathf.Lerp(0f, 255f, Mathf.InverseLerp(0f, 90f, input)));

    //    if (value == lerp)
    //    {
    //        // we don't need to reset value if it's the same as before
    //        return;
    //    }

    //    if (lerp > 255)
    //    {
    //        value = 255;
    //    }
    //    else if (lerp < 0)
    //    {
    //        value = 0;
    //    }
    //    else
    //    {
    //        value = lerp;
    //    }
    //}

    //public void SetValue(int input)
    //{
    //    if (!(input.GetType() != typeof(int))) 
    //    {
    //        Debug.LogWarning("MotorController: input type not int or float.");
    //        return;
    //    }

    //    if (value == input)
    //    {
    //        // we don't need to reset value if it's the same as before
    //        return;
    //    }

    //    if (input > 255)
    //    {
    //        value = 255;
    //    }
    //    else if (input < 0)
    //    {
    //        value = 0;
    //    }
    //    else
    //    {
    //        value = input;
    //    }
    //}

    private void OnDisable()
    {
        labJackController.ExecuteDACRequest(2.5f);
        //motor.close();
        //if (_serialPort.IsOpen)
        //{
        //    _serialPort.Close();
        //}
    }

    private void OnApplicationQuit()
    {
        labJackController.ExecuteDACRequest(2.5f);
        //if (_serialPort.IsOpen)
        //{
        //    _serialPort.Close();
        //}
        //motor.close();
    }
}
