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

#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetIMUAddress(IntPtr addr, bool bIMU);
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
    public DataSender mSender;
    public Text mText;

    string dirPath, filename;
    TrackerParam mTrackParam;
    ExperimentParam mExParam;
    EvaluationParam mEvalParam;
    bool bNotBase;

    float[] poseData;
    GCHandle poseHandle;
    IntPtr posePtr;

    float[] fIMUPose;
    GCHandle imuHandle;
    IntPtr imuPtr;

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
        mEvalParam = (EvaluationParam)mParamManager.DictionaryParam["Evaluation"];
        bNotBase = !mExParam.bEdgeBase;
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
            if (mEvalParam.bServerLocalization)
            {
                //성능 평가시
                bool bRes = true;
                if (N < 30)
                {
                    bRes = false;
                }
                string res = id+","+mTrackParam.nJpegQuality + "," + mTrackParam.nSkipFrames + ","+bRes;
                mEvalManager.writer_server_localization.WriteLine(res);
            }
            
        }
        else
        {
            CreateReferenceFrame2(id, ptr);
        }
    }

    public void UpdateData(int id, int n, IntPtr ptr)
    {
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
            bool bSuccessTracking = Localization(addr, posePtr, frameID, ts, mManager.AppData.JpegQuality, bNotBase, mTrackParam.bTracking, mTrackParam.bVisualization);
            var timeSpan2 = DateTime.UtcNow - sTime;
            if (bSuccessTracking)
            {
                if (mExParam.bEdgeBase)
                {
                    bool bNeedNewKF = NeedNewKeyFrame(frameID);
                    if (bNeedNewKF)
                    {
                        NeedKeyFrameEvent.RunEvent(frameID);
                        //byte[] bdata = new byte[20];
                        ////Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                        //UdpData mdata = new UdpData("ReqUpdateLocalMap", mManager.User.UserName, frameID, bdata, 1.0);
                        //StartCoroutine(mSender.SendData(mdata));
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
            }
            if (mTrackParam.bShowLog) { 
                mText.text = "localization = " + bSuccessTracking + " == "+ timeSpan2.TotalMilliseconds;
            }
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
