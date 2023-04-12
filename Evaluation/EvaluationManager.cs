using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EvaluationManager : MonoBehaviour
{
    public ParameterManager mParamManager;
    TrackerParam mTrackerParam;
    EvaluationParam mEvalParam;

    public StreamWriter writer_server_localization;
    public StreamWriter writer_device_localization;
    public StreamWriter writer_network_traffic;
    public StreamWriter writer_latency;
    public StreamWriter writer_consistency;

    bool WantsToQuit()
    {
        if(mEvalParam.bServerLocalization)
            writer_server_localization.Close();
        if (mEvalParam.bDeviceLocalization)
            writer_device_localization.Close();
        if (mEvalParam.bNetworkTraffic)
            writer_network_traffic.Close();
        if (mEvalParam.bLatency)
            writer_latency.Close();
        if (mEvalParam.bConsistency)
            writer_consistency.Close();
        return true;
    }

    void Awake()
    {
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mEvalParam = (EvaluationParam)mParamManager.DictionaryParam["Evaluation"];

        string dirPath = Application.persistentDataPath + "/data/Evaluation";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        string filePath = "";
        if (mEvalParam.bServerLocalization)
        {
            filePath = dirPath + "/eval_server_localization.csv";
            writer_server_localization = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bDeviceLocalization)
        {
            filePath = dirPath + "/eval_device_localization.csv";
            writer_device_localization = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bNetworkTraffic)
        {
            filePath = dirPath + "/eval_network_traffic.csv";
            writer_network_traffic = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bLatency)
        {
            filePath = dirPath + "/eval_latency.csv";
            writer_latency = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bConsistency)
        {
            filePath = dirPath + "/eval_consistency.csv";
            writer_consistency = new StreamWriter(filePath, true);
        }
        Application.wantsToQuit += WantsToQuit;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
