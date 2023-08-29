using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class NeeddKeyFrameEventArgs : EventArgs
{
    public NeeddKeyFrameEventArgs(int _id)
    {
        mFrameID = _id;
    }
    public int mFrameID { get; set; }
}

class NeedKeyFrameEvent
{
    public static event EventHandler<int> needNewKeyFrame;
    public static void RunEvent(int e)
    {
        if (needNewKeyFrame != null)
        {
            needNewKeyFrame(null, e);
        }
    }
}

class ExpRobustTracking{
    int nTotal_our;
    int nTotal_base;
    int nSuccess_our;
    int nSuccess_base;
}

public class Tracker : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)

    [DllImport("UnityLibrary")]
    private static extern bool Localization(IntPtr texdata, IntPtr posedata, int id, double ts, int nQuality, bool bNotBase, bool bTracking, bool bVisualization);
    [DllImport("UnityLibrary")]
    private static extern bool NeedNewKeyFrame(int fid);
    [DllImport("UnityLibrary")]
    private static extern void NeedNewKeyFrame2(int fid);
    [DllImport("UnityLibrary")]
    private static extern int CreateReferenceFrame(int id, bool bNotBase, IntPtr data);
    [DllImport("UnityLibrary")]
    private static extern int CreateReferenceFrame2(int id, IntPtr data);
    [DllImport("UnityLibrary")]
    private static extern void UpdateLocalMap(int id, int n, IntPtr data);
    [DllImport("edgeslam")]
    private static extern void ConvertImage(int id, IntPtr texdata);
    [DllImport("edgeslam")]
    private static extern int DynamicObjectTracking(int id, IntPtr posedata);
    [DllImport("edgeslam")]
    private static extern void ConvertCoordinateObjectToWorld();
    [DllImport("edgeslam")]
    private static extern void UpdateDynamicObjectPoints(IntPtr addr, int size);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetIMUAddress(IntPtr addr, bool bIMU);
    [DllImport("edgeslam")]
    private static extern void ConvertImage(int id, IntPtr texdata);
    [DllImport("edgeslam")]
    private static extern int DynamicObjectTracking(int id, IntPtr posedata);
    [DllImport("edgeslam")]
    private static extern void ConvertCoordinateObjectToWorld();
    [DllImport("edgeslam")]
    private static extern void UpdateDynamicObjectPoints(IntPtr addr, int size);
    [DllImport("edgeslam")]
    private static extern bool Localization(IntPtr texdata, IntPtr posedata, int id, double ts, int nQuality, bool bNotBase, bool bTracking, bool bVisualization);
    [DllImport("edgeslam")]
    private static extern bool NeedNewKeyFrame(int fid);
    [DllImport("edgeslam")]
    private static extern void NeedNewKeyFrame2(int fid);
    [DllImport("edgeslam")]
    private static extern int CreateReferenceFrame(int id, bool bNotBase, IntPtr data);    
    [DllImport("edgeslam")]
    private static extern int CreateReferenceFrame2(int id, IntPtr data);    
    [DllImport("edgeslam")]
    private static extern void UpdateLocalMap(int id, int n, IntPtr data);    
#endif
    public SystemManager mManager;
    public ParameterManager mParamManager;
    public EvaluationManager mEvalManager;
    public PoseManager mPoseManager;
    public PointCloudProcess mPointCloud;
    public Text mText;

    string dirPath, filename;
    TrackerParam mTrackParam;
    ExperimentParam mExParam;
    bool bNotBase;

    float[] poseData;
    GCHandle poseHandle;
    IntPtr posePtr;

    float[] fIMUPose;
    GCHandle imuHandle;
    IntPtr imuPtr;

    float[] oposeData;
    GCHandle oposeHandle;
    IntPtr oposePtr;

    public bool mbSuccessInit;
    public Camera uvrCam;

    bool WantsToQuit()
    {
        //File.WriteAllText(filename, JsonUtility.ToJson(param));
        return true;
    }

    void OnEnable()
    {
        if (mTrackParam.bTracking) { 
            ImageCatchEvent.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (mTrackParam.bTracking)
            ImageCatchEvent.frameReceived -= OnCameraFrameReceived;
    }
    void Awake()
    {
        ////파라메터 로드
        //dirPath = Application.persistentDataPath + "/data/Param";
        //filename = dirPath + "/Tracker.json";
        //try
        //{
        //    string strAddData = File.ReadAllText(filename);
        //    param = JsonUtility.FromJson<TrackerParam>(strAddData);
        //    //mText.text = "success load " + load_scripts;
        //}
        //catch (Exception e)
        //{
        //    param = new TrackerParam();
        //    param.bTracking = false;
        //    param.bVisualization = false;
        //}
        ////파라메터 로드

        mTrackParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        bNotBase = !mExParam.bEdgeBase;
        mbSuccessInit = false;

        try {

            if(!mTrackParam.bTracking)
            {
                enabled = false;
                return;
            }

            if (mTrackParam.bTracking)
            {
                SwitchTrackingMode();

                ////pose ptr
                poseData = new float[12];
                poseHandle = GCHandle.Alloc(poseData, GCHandleType.Pinned);
                posePtr = poseHandle.AddrOfPinnedObject();
                ////pose ptr

                ////imu ptr
                fIMUPose = new float[12];
                imuHandle = GCHandle.Alloc(fIMUPose, GCHandleType.Pinned);
                imuPtr = imuHandle.AddrOfPinnedObject();
                ////imu ptr
                
                ////object pose ptr 다중 추정용으로 변경 필요
                oposeData = new float[12];
                oposeHandle = GCHandle.Alloc(oposeData, GCHandleType.Pinned);
                oposePtr = oposeHandle.AddrOfPinnedObject();
                ////pose ptr

            }
            Application.wantsToQuit += WantsToQuit;
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

    public void CreateKeyFrame(int id, IntPtr ptr)
    {
        

        if (mExParam.bCreateKFMethod)
        {
            int N = CreateReferenceFrame(id, bNotBase, ptr);
            bool bRes = true;
            if (N < 30)
            {
                bRes = false;
            }
            if (!mbSuccessInit && bNotBase && bRes)
            {
                mbSuccessInit = true;
            }
            if (mEvalManager.bServerLocalization)
            {
                string res;
                if (mExParam.bEdgeBase)
                {
                    res = "base," + id + ",-1,-1," + bRes + "," + mbSuccessInit;
                }
                else {
                    res = "our," + id + "," + mTrackParam.nJpegQuality + "," + mTrackParam.nSkipFrames + "," + bRes;
                }
                mEvalManager.mServerLocalizationTask.AddMessage(res);
            }
            
        }
        else
        {
            CreateReferenceFrame2(id, ptr);
        }
    }

    public void UpdateData(int id, int n, IntPtr ptr)
    {
        if (!mbSuccessInit && mExParam.bEdgeBase)
        {
            mbSuccessInit = true;
        }
        UpdateLocalMap(id, n, ptr);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
    Matrix4x4 invertYMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));

    Matrix4x4 mode3(PoseData data)
    {
        Matrix4x4 ARM = invertYMatrix * Matrix4x4.TRS(data.pos, data.rot, Vector3.one) * invertYMatrix;
        return ARM;
    }

    void OnCameraFrameReceived(object sender, ImageCatchEventArgs e)
    {
        try
        {
            var rgbMat = e.rgbMat.clone();
            var frameID = e.mnFrameID;
            var timeSpan = DateTime.UtcNow - mManager.StartTime;
            double ts = timeSpan.TotalMilliseconds;
            IntPtr addr = (IntPtr)rgbMat.dataAddr();
            var sTime = DateTime.UtcNow;
            ConvertImage(frameID, addr);
            bool bSuccessTracking = Localization(addr, posePtr, frameID, ts, mManager.AppData.JpegQuality, bNotBase, mTrackParam.bTracking, mTrackParam.bVisualization);
            int nObj = DynamicObjectTracking(frameID, oposePtr);
            if (nObj > 10) {
                //ConvertCoordinateObjectToWorld();
                Vector3[] array = new Vector3[nObj];
                GCHandle ahandle = GCHandle.Alloc(array, GCHandleType.Pinned);
                IntPtr arrayptr = ahandle.AddrOfPinnedObject();
                UpdateDynamicObjectPoints(arrayptr, nObj);
                mPointCloud.GetWorldPoints(array, oposeData);
                mText.text = "Object test = " +frameID+" = "+nObj+" "+ array[0].ToString();
                ahandle.Free();
            }
            if (bSuccessTracking)
            {
                if (mEvalManager.bProcess)
                {
                    var timeSpan2 = DateTime.UtcNow - sTime;
                    string res = "";
                    if (mExParam.bEdgeBase)
                    {
                        res = "base,localization";
                    }
                    else
                    {
                        res = "our,localization";
                    }
                    res +=timeSpan2.TotalMilliseconds;
                    mEvalManager.mProcessTask.AddMessage(res);
                }
                if (mExParam.bEdgeBase)
                {
                    bool bNeedNewKF = NeedNewKeyFrame(frameID);
                    if (bNeedNewKF)
                    {
                        NeedKeyFrameEvent.RunEvent(frameID);
                    }
                }
                //R.t(), C 생성하고 아무것도 변화안함.
                Matrix3x3 R = new Matrix3x3(poseData[0], poseData[1], poseData[2],
                            poseData[3], poseData[4], poseData[5],
                            poseData[6], poseData[7], poseData[8]);

                PoseData data = new PoseData();
                data.rot = R.Transpose().GetQuaternion();
                data.pos = new Vector3(poseData[9], poseData[10], poseData[11]);

                Matrix4x4 ARM = Matrix4x4.identity;
                ARM = mode3(data);
                Camera.main.transform.position = ARUtils.ExtractTranslationFromMatrix(ref ARM);
                Camera.main.transform.rotation = ARUtils.ExtractRotationFromMatrix(ref ARM);
            }
            else
            {
                //리로컬 역할도 함
                if (mExParam.bEdgeBase && frameID % mTrackParam.nSkipFrames == 0) {
                    NeedNewKeyFrame2(frameID);
                    NeedKeyFrameEvent.RunEvent(frameID);
                }
            }
            if (mPoseManager.CheckPose(frameID))
            {
                //mText.text = "????????????????????????????????";
            }
            else {
                mPoseManager.AddPose(frameID, Camera.main.transform, bSuccessTracking);
                if (mEvalManager.bDeviceLocalization && mbSuccessInit)
                {
                    if (mExParam.bEdgeBase)
                    {
                        string res = "base,"+frameID + ",-1,-1,"+bSuccessTracking;
                        mEvalManager.mDeviceLocalizationTask.AddMessage(res);
                    }
                    else {
                        string res = "our,"+frameID + "," + mTrackParam.nJpegQuality + "," + mTrackParam.nSkipFrames + "," + bSuccessTracking;
                        mEvalManager.mDeviceLocalizationTask.AddMessage(res);
                    }
                }
            }
            //if (mTrackParam.bShowLog) { 
            //    mText.text = "localization = " + bSuccessTracking + " == "+ timeSpan2.TotalMilliseconds;
            //}
        }
        catch (Exception ex)
        {
            mText.text = ex.ToString();
        }
    }
    void SwitchTrackingMode()
    {
        GameObject.Find("AR Camera").GetComponent<ARPoseDriver>().enabled = false;
        mPoseManager.mbRunMode = true;
        //Camera.main.enabled = false;
        //uvrCam.enabled = true;
    }
}
