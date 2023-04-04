using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Param
{
    public bool bShowLog;
    public bool bEnable = true;
}
[Serializable]
public class ObjectParam : Param
{
    public float fWalkingObjScale;
    public float fTempObjScale;
}

[Serializable]
class TrackerParam : Param
{
    public bool bTracking; //true 이면 내꺼, 아니면 ARCore
    public bool bMapping;
    public bool bPlaneOptimization;
    public bool bARReset;
    public bool bIMU;
    public int nFeatures;
    public int nPyramids;
    public int nSkipFrames;
    public int nLocalKeyFrames;
    public int nJpegQuality;
    public bool bVisualization;
}

[Serializable]
public class ArUcoMarkerParam : Param
{
    public int nMode;
}

[Serializable]
class ExperimentParam : Param
{
    public bool bCreateKFMethod; //트루이면 without Desc, false이면 with Desc
    public bool bEdgeBase;
    public bool bHost; //등록 모드 일 때
    public bool bRegistrationTest;
    public bool bManipulationTest;
    public bool bPathTest;
    public bool bObjectDetection;
}

public class ParameterManager : MonoBehaviour
{
    string dirPath;
    public Text mText;
    public Dictionary<string, Param> DictionaryParam;
    TrackerParam mTrackerParam;
    ArUcoMarkerParam mMarkerParam;
    ExperimentParam mExperimentParam;
    ObjectParam mObjParam;
    Param mTimeServerParam;

    bool WantsToQuit()
    {
        File.WriteAllText(dirPath + "/Tracker.json", JsonUtility.ToJson(mTrackerParam));
        File.WriteAllText(dirPath + "/MarkerParam.json", JsonUtility.ToJson(mMarkerParam));
        File.WriteAllText(dirPath + "/ExperimentParam.json", JsonUtility.ToJson(mExperimentParam));
        File.WriteAllText(dirPath + "/ObjectParam.json", JsonUtility.ToJson(mObjParam));
        File.WriteAllText(dirPath + "/TimeServer.json", JsonUtility.ToJson(mTimeServerParam));
        return true;
    }

    void Awake()
    {
        DictionaryParam = new Dictionary<string, Param>();
        dirPath = Application.persistentDataPath + "/data/Param";

        //트래킹 파라메터 로드
        //filename = dirPath + "/Tracker.json";
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/Tracker.json");
            mTrackerParam = JsonUtility.FromJson<TrackerParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mTrackerParam = new TrackerParam();
            mTrackerParam.nFeatures = 800;
            mTrackerParam.nPyramids = 8;
            mTrackerParam.nSkipFrames = 3;
            mTrackerParam.nLocalKeyFrames = 8;
            mTrackerParam.nJpegQuality = 50;
        }
        //트래킹 파라메터 로드

        //마커 파라메터 로드
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/MarkerParam.json");
            mMarkerParam = JsonUtility.FromJson<ArUcoMarkerParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mMarkerParam = new ArUcoMarkerParam();
        }
        //마커 파라메터 로드

        //실험 설정 파라메터 로드
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/ExperimentParam.json");
            mExperimentParam = JsonUtility.FromJson<ExperimentParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mExperimentParam = new ExperimentParam();
        }
        //파라메터 로드

        //실험 설정 파라메터 로드
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/ObjectParam.json");
            mObjParam = JsonUtility.FromJson<ObjectParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mObjParam = new ObjectParam();
            mObjParam.fWalkingObjScale = 0.2f;
        }
        //파라메터 로드

        //타임 서버
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/TimeServer.json");
            mTimeServerParam = JsonUtility.FromJson<Param>(strAddData);
        }
        catch (Exception e)
        {
            mTimeServerParam = new Param();
        }
        //타임 서버

        //딕셔너리에 저장
        try
        {
            DictionaryParam.Add("Tracker", mTrackerParam);
            DictionaryParam.Add("Experiment", mExperimentParam);
            DictionaryParam.Add("Marker", mMarkerParam);
            DictionaryParam.Add("VirtualObject", mObjParam);
            DictionaryParam.Add("TimeServer", mTimeServerParam);
        }
        catch(Exception ex)
        {
            mText.text = ex.ToString();
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
