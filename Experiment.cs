using OpenCVForUnity.CoreModule;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ExperimentData
{
    public string name;
    public int nTotal;
    public float fSum;
    public float fSum_2;
    public float fAvg;
    public float fStddev;

    public ExperimentData(string _name)
    {
        name = _name;
        nTotal = 0;
        fSum = 0.0f;
        fSum_2 = 0.0f;
        fAvg = 0.0f;
        fStddev = 0.0f;
    }

    public void Update(float ts)
    {
        nTotal++;
        fSum += ts;
        fSum_2 += (ts * ts);
    }

    public void Calculate()
    {
        if (nTotal > 2)
        {
            int N = nTotal - 1;
            fAvg = fSum / nTotal;
            fStddev = Mathf.Sqrt(fSum_2 / N - fAvg * fSum / N);
        }
    }
    
}

public class Experiment : MonoBehaviour
{
    
    ExperimentData data;
    string dirPath;
    bool WantsToQuit()
    {
        data.Calculate();
        File.WriteAllText(dirPath + "/Latency.json", JsonUtility.ToJson(data));
        return true;
    }
     
    void Start()
    {
        dirPath = Application.persistentDataPath + "/data";
        enabled = false;

        
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        try
        {
            string strIntrinsics = File.ReadAllText(dirPath + "/Latency.json");
            data = JsonUtility.FromJson<ExperimentData>(strIntrinsics);
        }
        catch (Exception)
        {
            data = new ExperimentData("latency");
            File.WriteAllText(dirPath + "/Latency.json", JsonUtility.ToJson(data));
        }

        Application.wantsToQuit += WantsToQuit;
    }
    void Update()
    {

    }

    public void Calculate(float ts)
    {
        data.Update(ts);
    }
}