﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using static RingSensor;
using static RewardArena;

[DisallowMultipleComponent]
public class BallController : MonoBehaviour
{
    //
    //SerialPort _serialPort;
    wrmhl ball = new wrmhl();
    public static BallController Ball;


    public float pitch;
    public float roll;
    [HideInInspector] public float yaw;


    [HideInInspector] public float motorYaw;
    [HideInInspector] public float motorRoll;

    [HideInInspector] public float deltaX;
    [HideInInspector] public float deltaZ;
    [HideInInspector] public float deltaYaw;

    [HideInInspector] public float ballDeltaX;
    [HideInInspector] public float ballDeltaZ;
    [HideInInspector] public float ballDeltaYaw;

    private float kb_vert;
    private float kb_hor;
    public float xVel;
    public float zVel;
    public float yawVel;
    public float yawVelRing;

    [HideInInspector] public float vert;
    [HideInInspector] public float hor;
    private string data;
    private float initTime;
    private float initDelay;
    public bool IsConnected = true;
    public bool mcActive;


    public string portName = "COM11";
    public int baudRate = 2000000;
    public int ReadTimeout = 5000;
    public int QueueLength = 1;
    // Start is called before the first frame update

    private System.Random rand;
    private float maxSpeed;
    private float maxAcc;
    [ShowOnly] public int seed;

    private float lastFrameRingRead;
    private float currentFrameRingRead;

    private bool keyboard;
    // replay vars
    bool isReplay;
    string replayPath;
    readonly List<float> replayX = new List<float>();
    readonly List<float> replayZ = new List<float>();
    readonly List<float> replayYaw = new List<float>();
    int replayIdx = 0;
    int replayMaxIdx;

    float Zscale;
    float Xscale;
    float Yawscale;

    float V_t1 = 0f;
    float V_t2 = 0f;
    float V_t3 = 0f;
    float V_t4 = 0f;




    float Apply_momentum(float V_t0)
    {

        float result = 0f;

        if (!(V_t1 == 0f && V_t2 == 0f && V_t3 == 0f && V_t4 == 0f))
        {
            //result =  V_t0 * 0.5f + V_t1 * 0.2f + V_t2 * 0.1f + V_t3 * 0.1f + V_t4 * 0.1f;

            result = (V_t0 + V_t1 + V_t2 + V_t3 + V_t4) / 5f;
            V_t1 = V_t0;
            V_t2 = V_t1;
            V_t3 = V_t2;
            V_t4 = V_t3;
            return result;

        }
        V_t1 = V_t0;
        V_t2 = V_t1;
        V_t3 = V_t2;
        V_t4 = V_t3;
        return V_t0;
    }

    void Start()
    {
        keyboard = (int)PlayerPrefs.GetFloat("IsKeyboard") == 1;
        motorYaw = PlayerPrefs.GetFloat("motorYaw"); // 0 = 1D (F/B), 1 = 2D (L/R/F/B), 2 - yaw rot
        motorRoll = PlayerPrefs.GetFloat("motorRoll"); // 0 = 1D (F/B), 1 = 2D (L/R/F/B), 2 - yaw rot

        Ball = this;
        ball.set(portName, baudRate, ReadTimeout, QueueLength);

        //if MC not active, connect directly. if MC active, connect after 15 seconds
        //if (!keyboard)
        //{
        //    ball.connect();
        //}
        ball.connect();
        

        mcActive = (int)PlayerPrefs.GetFloat("Enable MC") == 1;
        if(mcActive)
        {
            initDelay = 20;
        } else
        {
            initDelay = 1;
        }
        

        initTime = Time.time;

        isReplay = (int)PlayerPrefs.GetFloat("IsReplay") == 1;
        //print(string.Format("is replay {0}, is keyboard {1}", isReplay, keyboard));

        // calibration
        Zscale = PlayerPrefs.GetFloat("ZScale") == 0 ? 1 : PlayerPrefs.GetFloat("ZScale");
        Xscale = PlayerPrefs.GetFloat("XScale") == 0 ? 1 : PlayerPrefs.GetFloat("XScale");
        Yawscale = PlayerPrefs.GetFloat("YawScale") == 0 ? 1 : PlayerPrefs.GetFloat("YawScale");


        if (isReplay)
        {
            replayPath = PlayerPrefs.GetString("ReplayPath");
            //print(replayPath);
            StreamReader sr = new StreamReader(replayPath);

            string[] split;
            while (!sr.EndOfStream)
            {
                split = sr.ReadLine().Split(',');
                replayX.Add(float.Parse(split[0]));
                replayZ.Add(float.Parse(split[1]));
                replayYaw.Add(float.Parse(split[2]));
            }

            replayMaxIdx = replayX.Count;
        }

        maxSpeed = 0.2f/60.0f; //not used
        maxAcc = 0.2f/3600.0f; //not used
        lastFrameRingRead = ringSensor.dir;
        currentFrameRingRead = ringSensor.dir;

    }

    public async void Update()
    {
        try
        {
            

            float t = Time.time;
            // if mc strat 60s after start
            if (t - initTime > initDelay)
            {



                if (!keyboard)
                {
                    string ball_input = ball.readQueue();
                    string[] line = ball_input.Split(',');
                    // print(ball_input);


                    pitch = float.Parse(line[0]);
                    roll = float.Parse(line[1]);
                    yaw = float.Parse(line[2]);


                }



                // calibrate once a week
                //ballDeltaZ = pitch * -0.0085384834f * 3f;
                //ballDeltaX = roll * -0.0093862942f * 2.5f;
                //ballDeltaYaw = yaw * -0.1713298528f * 4;
                // squeze 360 deg into 30
                //deltaYaw = deltaYaw * 12;
                float ball_circumference = 0.937608f;
                //float full_rotation_to_vr_z_coord = 19.9055f;
                //float full_rotation_to_vr_z_coord = 18.38f;
                float full_rotation_to_vr_z_coord = 18.936f;
                float full_rotation_to_vr_x_coord = 18.936f;
                float full_rotation_to_vr_degrees = 237.34f;
                float pitch_gain = ball_circumference / full_rotation_to_vr_z_coord;
                float roll_gain = ball_circumference / full_rotation_to_vr_x_coord;
                float yaw_gain = 360.0f / full_rotation_to_vr_degrees;

                ballDeltaZ = pitch * pitch_gain;
                ballDeltaX = roll * roll_gain;
                ballDeltaYaw = yaw * yaw_gain;

                lastFrameRingRead = currentFrameRingRead;
                currentFrameRingRead = ringSensor.dir;




                if (isReplay & replayIdx < replayMaxIdx)
                {
                    //print(string.Format("x {0}, yaw {1}, z {2}", replayX[replayIdx], replayYaw[replayIdx], replayZ[replayIdx]));


                    deltaZ = replayZ[replayIdx];
                    deltaX = replayX[replayIdx];
                    deltaYaw = replayYaw[replayIdx];


                    //if (deltaZ > 0.015f)
                    //{
                    //    deltaZ = deltaZ / 5;
                    //}else if (deltaZ < 0.015f && deltaZ > 0.01f)
                    //{
                    //    deltaZ = deltaZ / 2;
                    //}

                    zVel = deltaZ / Time.deltaTime;
                    xVel = deltaX / Time.deltaTime;
                    yawVel = deltaYaw / Time.deltaTime;

                    Apply_momentum(zVel);
                    //zVel = zVel / 3f;
                    //player.transform.position += new Vector3(replayX[replayIdx], p_height, replayZ[replayIdx]);
                    //player.transform.rotation = Quaternion.Euler(0f, replayYaw[replayIdx], 0f);

                    //// ignore yaw vel that is too small. 6 degree/s is the threshold for the yaw motor to respond
                    if (yawVel > -6.0257f && yawVel < 6.0257f)
                    {
                        yawVel = 0;

                    }

                    replayIdx++;

                }
                // keyboard: acceleration

                else if (keyboard)
                {

                    kb_vert = Input.GetAxis("Vertical");
                    //     1 / 0.2 second / 100 = 50 cm/s
                    //zVel = kb_vert / Time.deltaTime / 100 * 0.5f;
                    zVel = kb_vert / Time.deltaTime / 100 / 2.5f;


                    kb_hor = Input.GetAxis("Horizontal");
                    //xVel = kb_hor / Time.deltaTime / 20 / 5;
                    xVel = kb_hor / Time.deltaTime / 100 / 2.5f;

                    //print("ball script 2222222222222, " + kb_hor);

                    // 1 = 50 deg / s
                    // roll / Time.deltaTime = 1 / 0.2 = 50 deg / s
                    yawVel = kb_hor / Time.deltaTime;

                    //// ignore yaw vel that is too small. 6 degree/s is the threshold for the yaw motor to respond
                    if (yawVel > -6.0257f && yawVel < 6.0257f) //deadband = 0.075V
                    //if (yawVel > -4.01715f && yawVel < 4.01715f) //deadband = 0.05V
                    {
                        yawVel = 0;

                    }

                    //if (Math.Abs(yawVel) < 120f)
                    //{
                    //    yawVel += kb_hor / Time.deltaTime / 8;
                    //}


                    //if (kb_hor == 0)
                    //{
                    //    if (yawVel > 0)
                    //    {
                    //        yawVel -= 1 / Time.deltaTime / 8;
                    //    }
                    //    else if (yawVel < 0)
                    //    {
                    //        yawVel += 1 / Time.deltaTime / 8;
                    //    }
                    //    else
                    //    {
                    //        yawVel = 0;
                    //    }

                    //}

                    //------------------------------    implementing ring MC   ------------------------------

                    var deltaYawRing = currentFrameRingRead - lastFrameRingRead;


                    // clw
                    var playerAngle = SharedReward.player.transform.eulerAngles[1];

                    if (deltaYawRing < -180 && playerAngle != 0f)
                    {
                        //print("---------------------clockwise rotation ");
                        deltaYawRing =+ 360;
                    } else if (deltaYawRing > 180 && playerAngle != 0f)
                    {
                        //print("---------------------counter clockwise rotation ");
                        deltaYawRing = -360;
                    }
                    
                    if (Math.Abs(deltaYawRing) > 4)
                    {
                        deltaYawRing = 0;
                    }

            
                    var error = (currentFrameRingRead - playerAngle);
                    error += (error > 180) ? -360 : (error < -180) ? 360 : 0;
                    yawVelRing = deltaYawRing / Time.deltaTime;

                    //print("Ring yaw angle : " + ringSensor.dir);
                    //print("Player yaw angle : " + deltaYawRing);

                    //------------------------------    implementing ring MC   ------------------------------ 

                }



                // keyboard: constant speed

                //else if (keyboard)
                //{

                //    kb_vert = Input.GetAxis("Vertical");
                //    //     1 / 0.2 second / 100 = 50 cm/s
                //    zVel = kb_vert / Time.deltaTime / 100 / 2f;



                //    kb_hor = Input.GetAxis("Horizontal");
                //    xVel = kb_hor / Time.deltaTime / 20 / 5;


                //    // 1 = 50 deg/s
                //    // roll / Time.deltaTime = 1/0.2 = 50 deg/s
                //    //yawVel = kb_hor / Time.deltaTime * 1.5f;
                //    yawVel = kb_hor / Time.deltaTime;
                //}

                else
                {

                    if (motorYaw > 0)
                    {

                        // yaw rotation is not probably detected, so disable yaw when runing at angle
                        //if (Math.Abs(pitch) > 0.05)
                        //{
                        //    ballDeltaYaw = 0;
                        //}
                        yawVel = ballDeltaYaw / Time.deltaTime;
                    }


                    zVel = ballDeltaZ / Time.deltaTime * Zscale;
                    xVel = ballDeltaX / Time.deltaTime * Xscale;

                    

                    if (motorRoll > 0)
                    {
                        yawVel = 0;

                        // rotate when mice run at angle| tan45 = 1; tan60 = 1.73; tan75 = 3.73 (30 deg forward run area)
                        //if (Math.Abs(zVel) / Math.Abs(xVel) < 2.73 & Math.Abs(xVel) > 0.015f)
                        if (Math.Abs(zVel) / Math.Abs(xVel) < 0.57735026919f) //that number is tan(30 degrees)
                            //if (Math.Abs(roll) > 0.03f)
                            {
                            //yawVel = (float)Math.Sqrt(zVel * zVel + xVel * xVel) * 300;
                            // roll and pitch combination is not symmetric, so that running at angle might be different for various angles
                            if (xVel > 0)
                            {
                                // roll or deltaX is not symmetric, not sure why;
                                //yawVel = xVel * 800;
                                // if symmetric
                                yawVel = xVel * 400;
                            }
                            else
                            {
                                yawVel = xVel * 400;
                            }


                            zVel = 0;
                            xVel = 0;

                        }
                    }


                }

                //// ignore yaw vel that is too small. 6 degree/s is the threshold for the yaw motor to respond
                if (yawVel > -6.0257f && yawVel < 6.0257f) //deadband = 0.075V
                //if (yawVel > -4.01715f && yawVel < 4.01715f) //deadband = 0.05V
                {
                    yawVel = 0;

                }
            }



        }
        catch (Exception e)
        {
            // It's gonna be the same exception everytime, but I'm purposely doing this.
            // It's just that this code will read serial in faster than it's actually
            // coming in so there'll be an error saying there's no object or something
            // like that.
        }

        await new WaitForUpdate();
    }

    //void OnEnable()
    //{
    //    Ball = this;

    //    _serialPort = new SerialPort();

    //    // Change com port
    //    _serialPort.PortName = "COM8";
    //    // Change baud rate
    //    _serialPort.BaudRate = 2000000;
    //    //_serialPort.ReadTimeout = 1;
    //    _serialPort.DtrEnable = true;
    //    _serialPort.RtsEnable = true;
    //    // Timeout after 0.5 seconds.
    //    _serialPort.ReadTimeout = 5;
    //    try
    //    {
    //        _serialPort.Open();
    //        _serialPort.DiscardInBuffer();
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogError(e);
    //        IsConnected = false;
    //    }
    //}

    // Update is called once per frame

    //void FixedUpdate()
    // {
    //     //pitch = Input.GetAxis("Vertical") * -1;
    //     //roll = 0.0f;
    //     //yaw = 0.0f;
    //     try
    //     {
    //         float t = Time.time;
    //         if (t - initTime > 0.4f)
    //         {
    //             string[] line = _serialPort.ReadLine().Split(',');

    //             // float.Parse returns int sometimes eg 0.1 -> 1
    //             pitch = float.Parse(line[0]);
    //             roll = float.Parse(line[1]);
    //             yaw = float.Parse(line[2]);

    //             // this leads to a partial loss of msg eg. instead of 0.15 -> 15
    //             //_serialPort.DiscardInBuffer();

    //             //print(pitch);

    //         } else
    //         {
    //             _serialPort.DiscardInBuffer();
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         // It's gonna be the same exception everytime, but I'm purposely doing this.
    //         // It's just that this code will read serial in faster than it's actually
    //         // coming in so there'll be an error saying there's no object or something
    //         // like that.
    //     }

    //     //await new WaitForSeconds(0.0f);
    // }

    public async void MakeProfile(float delay, float duration, float sigma, float amplitude)
    {
        int size = Mathf.RoundToInt(duration / Time.fixedDeltaTime);
        float[] x = new float[size];

        for (int i = 0; i < size; i++)
        {
            x[i] = i * (duration / Time.fixedDeltaTime);
        }

        var a = 1.0f / (sigma * Mathf.Sqrt(2 * Mathf.PI));

        await new WaitForSeconds(delay);

        for (int i = 0; i < size; i++)
        {
            await new WaitForFixedUpdate();

            pitch += amplitude * a * Mathf.Exp(-Mathf.Pow(x[i], 2.0f) / (2.0f * Mathf.Pow(sigma, 2.0f)));
        }
    }

    public float BoxMullerGaussianSample()
    {
        float u1, u2, S;
        do
        {
            u1 = 2.0f * (float)rand.NextDouble() - 1.0f;
            u2 = 2.0f * (float)rand.NextDouble() - 1.0f;
            S = u1 * u1 + u2 * u2;
        }
        while (S >= 1.0f);
        return u1 * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
    }

    private void OnDisable()
    {
        //_serialPort.Close();
        ball.close();
    }

    private void OnApplicationQuit()
    {
        //if (_serialPort.IsOpen)
        //{
        //    _serialPort.Close();
        //}
        ball.close();
    }
}
