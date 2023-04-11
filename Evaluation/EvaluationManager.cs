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

    bool WantsToQuit()
    {
        if(mEvalParam.bServerLocalization)
            writer_server_localization.Close();
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
