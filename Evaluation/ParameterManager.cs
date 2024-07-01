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
public class CamParam : Param
{
    public int width;
    public int height;
    public float fx;
    public float fy;
    public float cx;
    public float cy;
    public bool bCaptureDepth;
    public bool bShowLog;
}

[Serializable]
public class ObjectParam : Param
{
    public float fWalkingObjScale;
    public float fTempObjScale;
    public Color objColor;
}
[Serializable]
public class DrawPaintParam : Param
{
    public float r;
    public float g;
    public float b;
    public float a;
    public float size;
}
[Serializable]
class TrackerParam : Param
{
    public bool bTracking; //true �̸� ����, �ƴϸ� ARCore
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
    public bool bLocalizationTest;
    public bool bCreateKFMethod; //Ʈ���̸� without Desc, false�̸� with Desc
    public bool bEdgeBase;
    public bool bHost; //��� ��� �� ��
    public bool bRegistrationTest;
    public bool bManipulationTest;
    public bool bPathTest;
    public bool bDrawTest;
    public bool bObjectDetection;
    public bool bCommuTest; //���� ��ü ����� �� true = �׸���, false = Ű������
    public bool bOXRTest;
    public bool bCoordAlign; //���� ��� ���ս�.(ex)Ȧ�η���
    public bool bIMU;
}

[Serializable]
class EvaluationParam : Param
{
    public bool bServerLocalization;
    public bool bDeviceLocalization;
    public bool bNetworkTraffic;
    public bool bLatency; //��Ʈ��ũ �����Ͻ�
    public bool bProcess; //��� ���μ��� Ÿ��
    public bool bConsistency;
}

public class ParameterManager : MonoBehaviour
{
    string dirPath;
    public Text mText;
    public Dictionary<string, Param> DictionaryParam;
    TrackerParam mTrackerParam;
    CamParam mCamParam;
    ArUcoMarkerParam mMarkerParam;
    ExperimentParam mExperimentParam;
    EvaluationParam mEvalParam;
    ObjectParam mObjParam;
    DrawPaintParam mDrawParam;
    Param mTimeServerParam;
    
    bool WantsToQuit()
    {
        if (mExperimentParam.bLocalizationTest && !mExperimentParam.bEdgeBase)
        {
            mTrackerParam.nSkipFrames++;
            if(mTrackerParam.nSkipFrames > 15)
            {
                mTrackerParam.nSkipFrames = 2;
                mTrackerParam.nJpegQuality += 10;
                if(mTrackerParam.nJpegQuality > 100)
                {
                    mTrackerParam.nJpegQuality = 10;
                }
            }
        }
        File.WriteAllText(dirPath + "/CameraParam.json", JsonUtility.ToJson(mCamParam));
        File.WriteAllText(dirPath + "/Tracker.json", JsonUtility.ToJson(mTrackerParam));
        File.WriteAllText(dirPath + "/MarkerParam.json", JsonUtility.ToJson(mMarkerParam));
        File.WriteAllText(dirPath + "/ExperimentParam.json", JsonUtility.ToJson(mExperimentParam));
        File.WriteAllText(dirPath + "/EvaluationParam.json", JsonUtility.ToJson(mEvalParam));
        File.WriteAllText(dirPath + "/ObjectParam.json", JsonUtility.ToJson(mObjParam));
        File.WriteAllText(dirPath + "/DrawPaintParam.json", JsonUtility.ToJson(mDrawParam));
        File.WriteAllText(dirPath + "/TimeServer.json", JsonUtility.ToJson(mTimeServerParam));
        return true;
    }

    void Awake()
    {
        DictionaryParam = new Dictionary<string, Param>();
        dirPath = Application.persistentDataPath + "/data/Param";

        //ī�޶� �Ķ����

        try
        {
            string strAddData = File.ReadAllText(dirPath + "/CameraParam.json");
            mCamParam = JsonUtility.FromJson<CamParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mCamParam = new CamParam();
            mCamParam.bShowLog = false;
            mCamParam.bCaptureDepth = false;
            mCamParam.width = 640;
            mCamParam.height = 480;
        }

        //Ʈ��ŷ �Ķ���� �ε�
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
        //Ʈ��ŷ �Ķ���� �ε�

        //��Ŀ �Ķ���� �ε�
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
        //��Ŀ �Ķ���� �ε�

        //���� ���� �Ķ���� �ε�
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
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/EvaluationParam.json");
            mEvalParam = JsonUtility.FromJson<EvaluationParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mEvalParam = new EvaluationParam();
        }
        //�Ķ���� �ε�

        //���� ���� �Ķ���� �ε�
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
        //�Ķ���� �ε�

        //�׸� ����Ʈ ����
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/DrawPaintParam.json");
            mDrawParam = JsonUtility.FromJson<DrawPaintParam>(strAddData);
        }
        catch (Exception e)
        {
            mDrawParam = new DrawPaintParam();
            mDrawParam.r = 0f;
            mDrawParam.g = 1f;
            mDrawParam.b = 1f;
            mDrawParam.a = 1f;
            mDrawParam.size = 0.05f;
        }
        //�׸� ����Ʈ ����

        //Ÿ�� ����
        try
        {
            string strAddData = File.ReadAllText(dirPath + "/TimeServer.json");
            mTimeServerParam = JsonUtility.FromJson<Param>(strAddData);
        }
        catch (Exception e)
        {
            mTimeServerParam = new Param();
        }
        //Ÿ�� ����

        //��ųʸ��� ����
        try
        {
            DictionaryParam.Add("Camera",mCamParam);
            DictionaryParam.Add("Tracker", mTrackerParam);
            DictionaryParam.Add("Experiment", mExperimentParam);
            DictionaryParam.Add("Evaluation", mEvalParam);
            DictionaryParam.Add("Marker", mMarkerParam);
            DictionaryParam.Add("VirtualObject", mObjParam);
            DictionaryParam.Add("TimeServer", mTimeServerParam);
            DictionaryParam.Add("DrawPaint", mDrawParam);
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
