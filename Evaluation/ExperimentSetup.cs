using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentSetup : MonoBehaviour
{
    public ParameterManager mParamManager;
    public Text mText;
    ExperimentParam param;

    [HideInInspector]
    public string sKeywords;
    [HideInInspector]
    public string rKeywords;

    bool WantsToQuit()
    {
        return true;
    }
    void Awake()
    {
        try
        {
            param = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
            sKeywords = "";
            rKeywords = "";
            if (param.bPathTest)
            {
                sKeywords += ",VO.REQMOVE";
                rKeywords += ",VO.MOVE,all";
            }
            if (param.bRegistrationTest)
            {
                sKeywords += ",VO.MARKER.CREATE";
                rKeywords += ",VO.MARKER.CREATED,single";
            }
            if (param.bManipulationTest)
            {
                sKeywords += ",VO.CREATE,VO.MANIPULATE";
            }
            if (param.bObjectDetection)
            {
                rKeywords += ",ObjectDetection,single";
            }
            if (param.bDrawTest)
            {
                sKeywords += ",VO.DRAW";
            }
            if (param.bEdgeBase)
            {
                sKeywords += ",ReqUpdateLocalMap";
                rKeywords += ",UpdatedLocalMap,single";
            }
            if (param.bOXRTest)
            {
                sKeywords += ",OXR::IMAGE";
            }
            if (param.bCoordAlign)
            {
                sKeywords += ",DevicePoseForAlign";
                rKeywords += ",GetScaleFactor,single";
            }
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
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
