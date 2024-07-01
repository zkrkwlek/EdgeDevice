using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IMUProcessor : MonoBehaviour
{
    public DataCommunicator mSender;
    public SystemManager mSystemManager;
    public ParameterManager mParamManager;
    ExperimentParam mExParam;
    public Text mText;

    DateTime startTime;
    Vector3 gyro, acc, mag;
    // Start is called before the first frame update
    void Start()
    {
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        if (!mExParam.bIMU) { 
            enabled = false;
            Input.gyro.enabled = false;
            Input.compass.enabled = false;
        }
        else
        {
            startTime = DateTime.Now;
            Input.gyro.enabled = true;
            Input.compass.enabled = true;
        }
        //enable
        //parameter
    }

    int nSensorID = 0;
    // Update is called once per frame
    void Update()
    {
        //capture sensor data
        mag  = Input.compass.rawVector;
        gyro = Input.gyro.rotationRateUnbiased;
        acc  = GetAccelerometerValue();
        
        float[] farray = new float[9];
        farray[0] = gyro.x;
        farray[1] = gyro.y;
        farray[2] = gyro.z;

        farray[3] = acc.x;
        farray[4] = acc.y;
        farray[5] = acc.z;

        farray[6] = mag.x;
        farray[7] = mag.y;
        farray[8] = mag.z;

        //send
        byte[] bdata = new byte[36];
        Buffer.BlockCopy(farray, 0, bdata, 0, 36);

        var timeSpan = DateTime.Now - startTime;
        double ts = timeSpan.TotalMilliseconds;

        UdpData pdata = new UdpData("IMUrawdata", mSystemManager.User.UserName, ++nSensorID, bdata, ts);
        StartCoroutine(mSender.SendData(pdata));
        //IMU processing

    }
    Vector3 GetAccelerometerValue()
    {
        Vector3 acc = Vector3.zero;
        float period = 0.0f;

        foreach (AccelerationEvent evnt in Input.accelerationEvents)
        {
            acc += evnt.acceleration * evnt.deltaTime;
            period += evnt.deltaTime;
        }
        if (period > 0)
        {
            acc *= 1.0f / period;
        }
        return acc;
    }
}
