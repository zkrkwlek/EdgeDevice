using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UVRSpatialTest : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataSender mSender;
    public ParameterManager mParamManager;
    public PoseManager mPoseManager;
    public Text mText;
    ExperimentParam mTestParam;
    TrackerParam mTrackerParam;
    ObjectParam mObjParam;
    public GameObject prefabObj;
    [HideInInspector]
    public Dictionary<int, GameObject> mAnchorObjects;

    //카메라 정보
    float scale;
    bool mbCamInit = false;
    Mat camMatrix;
    Mat invCamMatrix;

    StreamWriter writer_spatial;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void AttachObject(int id, Transform trans)
    {
        try
        {
            var obj = Instantiate(prefabObj, trans);
            if(mTestParam.bHost)
                obj.GetComponent<Renderer>().material.color = Color.red;
            else
                obj.GetComponent<Renderer>().material.color = Color.blue;
            obj.transform.localScale = new Vector3(scale, scale, scale);
            //obj.transform.localScale = new Vector3()
            mAnchorObjects.Add(id, obj);
        }
        catch (Exception e)
        {
            mText.text = "AttachObject:Err=" + e.ToString();
        }
    }
    bool isResolved(int id)
    {
        return mAnchorObjects.ContainsKey(id);
    }

    void Awake()
    {
        try
        {
            mTestParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
            mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
            
            if (!mTestParam.bRegistrationTest || !mTrackerParam.bTracking)
            {
                enabled = false;
                return;
            }
            mAnchorObjects = new Dictionary<int, GameObject>();
            mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];
            scale = mObjParam.fTempObjScale;

            Application.wantsToQuit += WantsToQuit;

            ////csv 파일 생성
            int quality = mTrackerParam.nJpegQuality;
            int nSkip = mTrackerParam.nSkipFrames;
            string dirPath = Application.persistentDataPath + "/data";
            string filePath = dirPath + "/err_uvr_"+quality+"_"+nSkip+".csv";
            writer_spatial = new StreamWriter(filePath, true);
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }

    bool WantsToQuit()
    {
        writer_spatial.Close();
        return true;
    }

    void OnEnable()
    {
        MarkerDetectEvent.markerDetected += OnMarkerInteraction;
        CameraInitEvent.camInitialized += OnCameraInitialization;
    }
    void OnDisable()
    {
        MarkerDetectEvent.markerDetected -= OnMarkerInteraction;
        CameraInitEvent.camInitialized -= OnCameraInitialization;
    }

    ArucoMarker marker = null;
    void OnMarkerInteraction(object sender, MarkerDetectEventArgs me)
    {
        try {
            marker = me.marker;
            int id = me.marker.id;
            //등록되었는지

            //등록 된 후
            if (!isResolved(id))
            {
                AttachObject(id, marker.gameobject.transform);
            }
            else {
                int fid = me.marker.frameId;
                if (mPoseManager.CheckPose(fid))
                {
                    var trans = mPoseManager.GetPose(fid);
                    var anchorObj = mAnchorObjects[id];
                    float azi = 0f;
                    float ele = 0f;
                    float dist = 0f;
                    Vector2 temp = Vector2.zero;
                    marker.CalculateAziAndEleAndDist(trans.position, out azi, out ele, out dist);
                    float err = marker.Calculate(trans.worldToLocalMatrix, camMatrix, anchorObj.transform.position, marker.corners[0], out temp, false);
                    //if (err < 1000f)
                    writer_spatial.WriteLine(dist + "," + azi + "," + ele + "," + err);
                    if (mTestParam.bShowLog)
                    {
                        mText.text = temp.ToString() + marker.corners[0].ToString() + " == " + err;
                    }
                }
                else
                {
                    mText.text = fid+"??????????????pose error";
                }
            }
        }
        catch(Exception e)
        {
            if(mTestParam.bShowLog)
                mText.text = e.ToString();
        }
    }
    void OnCameraInitialization(object sender, CameraInitEventArgs e)
    {
        mbCamInit = true;
        camMatrix = e.camMat;
        invCamMatrix = e.invCamMat;
    }
}
