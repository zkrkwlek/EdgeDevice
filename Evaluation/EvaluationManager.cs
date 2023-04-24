using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class EvaluationTask {
    List<string> messageList;
    StreamWriter writer;
    
    public EvaluationTask(string path)
    {
        messageList = new List<string>();
        writer = new StreamWriter(path, true);
    }
    public void AddMessage(string str)
    {
        messageList.Add(str);
    }
    public void Close()
    {
        foreach (string str in messageList)
        {
            writer.WriteLine(str);
        }
        writer.Close();
    }
    //public bool bEvaluation;
}

public class EvaluationManager : MonoBehaviour
{
    public ParameterManager mParamManager;
    TrackerParam mTrackerParam;
    EvaluationParam mEvalParam;

    public bool bServerLocalization;
    public bool bDeviceLocalization;
    public bool bTraffic; // 가상 객체 다운로드 도 추가
    public bool bLatency;
    public bool bConsistency;
    public bool bProcess; //리졸빙, 호스팅, 로컬라이제이션 시간 기록, 가상 객체 등록, 가상 객체 갱신 시간 등

    public EvaluationTask mServerLocalizationTask;
    public EvaluationTask mDeviceLocalizationTask;
    public EvaluationTask mNetworkTrafficTask;
    public EvaluationTask mLatencyTask;
    public EvaluationTask mConsistencyTask;
    public EvaluationTask mProcessTask;

    //public StreamWriter writer_server_localization;
    //public StreamWriter writer_device_localization;
    //public StreamWriter writer_network_traffic;
    //public StreamWriter writer_latency;
    //public StreamWriter writer_consistency;
    //public StreamWriter writer_process;


    public Dictionary<string, UdpData> DictCommuData;

    bool WantsToQuit()
    {
        if (mEvalParam.bServerLocalization) {
            mServerLocalizationTask.Close();
        }
        if (mEvalParam.bDeviceLocalization)
        {
            mDeviceLocalizationTask.Close();
        }
        if (mEvalParam.bNetworkTraffic) {
            mNetworkTrafficTask.Close();
        }
        if (mEvalParam.bLatency) {
            mLatencyTask.Close();
        }
        if (mEvalParam.bConsistency) {
            mConsistencyTask.Close();
        }
        if (mEvalParam.bProcess)
        {
            mProcessTask.Close();
        }
            
        return true;
    }

    void Awake()
    {
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mEvalParam = (EvaluationParam)mParamManager.DictionaryParam["Evaluation"];

        this.bServerLocalization = mEvalParam.bServerLocalization;
        this.bDeviceLocalization = mEvalParam.bDeviceLocalization;
        this.bTraffic = mEvalParam.bNetworkTraffic;
        this.bLatency = mEvalParam.bLatency;
        this.bConsistency = mEvalParam.bConsistency;
        this.bProcess = mEvalParam.bProcess;

        string dirPath = Application.persistentDataPath + "/data/Evaluation";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        string filePath = "";
        if (mEvalParam.bServerLocalization)
        {
            filePath = dirPath + "/eval_server_localization.csv";
            mServerLocalizationTask = new EvaluationTask(filePath);
            //writer_server_localization = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bDeviceLocalization)
        {
            filePath = dirPath + "/eval_device_localization.csv";
            mDeviceLocalizationTask = new EvaluationTask(filePath);
            //writer_device_localization = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bNetworkTraffic)
        {
            filePath = dirPath + "/eval_network_traffic.csv";
            mNetworkTrafficTask = new EvaluationTask(filePath);
            //writer_network_traffic = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bLatency)
        {
            filePath = dirPath + "/eval_latency.csv";
            mLatencyTask = new EvaluationTask(filePath);
            //writer_latency = new StreamWriter(filePath, true);
            DictCommuData = new Dictionary<string, UdpData>();
        }
        if (mEvalParam.bProcess)
        {
            filePath = dirPath + "/eval_process.csv";
            mProcessTask = new EvaluationTask(filePath);
            //writer_process = new StreamWriter(filePath, true);
        }
        if (mEvalParam.bConsistency)
        {
            filePath = dirPath + "/eval_consistency.csv";
            mConsistencyTask = new EvaluationTask(filePath);
            //writer_consistency = new StreamWriter(filePath, true);
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
