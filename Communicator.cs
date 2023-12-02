using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static SystemManager; //삭제 예저ㅏㅇ
using System.IO; //삭제 예저ㅏㅇ
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class Communicator : MonoBehaviour
{
#if UNITY_EDITOR_WIN
    [DllImport("UnityLibrary")]
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4, int nfeature, int nlevel, float fscale, int nskip, int nKFs);
    [DllImport("UnityLibrary")]
    private static extern void SetUserName(char[] src, int len);
    [DllImport("UnityLibrary")]
    private static extern void ConnectDevice();
    [DllImport("UnityLibrary")]
    private static extern void DisconnectDevice();
    [DllImport("UnityLibrary")]
    private static extern void SetPath(byte[] name, int len);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern void SetInit(int w, int h, float fx, float fy, float cx, float cy, float d1, float d2, float d3, float d4, int nfeature, int nlevel, float fscale, int nskip, int nKFs);    
    [DllImport("edgeslam")]
    private static extern void SetUserName(char[] src, int len);    
    [DllImport("edgeslam")]
    private static extern void ConnectDevice();
    [DllImport("edgeslam")]
    private static extern void DisconnectDevice();
    [DllImport("edgeslam")]
    private static extern void SetPath(char[] path);
#endif

    public DataCommunicator sender;
    public SystemManager mSystemManager;
    public ParameterManager mParamManager;
    public ExperimentSetup mExperimentSetup;
    public Text mText;

    //void OnApplicationQuit() {
    bool WantsToQuit() {
        //Application.CancelQuit();
        ////Device & Map store
        string addr2 = mSystemManager.AppData.Address + "/Store?keyword=DeviceDisconnect&id=0&src=" + mSystemManager.User.UserName;
        string msg2 = mSystemManager.User.UserName + "," + mSystemManager.User.MapName;
        byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg2);

        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
        uH.contentType = "application/json";
        request.uploadHandler = uH;
        request.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res = request.SendWebRequest();

        string addr3 = mSystemManager.AppData.Address + "/Disconnect?src=" + mSystemManager.User.UserName + "&type=device";
        UnityWebRequest request3 = new UnityWebRequest(addr3);
        request3.method = "POST";
        //UploadHandlerRaw uH3 = new UploadHandlerRaw(bdata3);
        //uH3.contentType = "application/json";
        //request3.uploadHandler = uH3;
        request3.downloadHandler = new DownloadHandlerBuffer();
        UnityWebRequestAsyncOperation res3 = request3.SendWebRequest();

        DisconnectDevice();

        while (!request.downloadHandler.isDone)//&& !request3.downloadHandler.isDone
        {
            continue;
        }

#if !UNITY_EDITOR
        System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        return true;
    }

    void OnEnable()
    {
        CameraInitEvent.camInitialized += OnCameraInitialization;
    }
    void OnDisable()
    {
        CameraInitEvent.camInitialized -= OnCameraInitialization;
    }

    bool bCamInit = false;
    void OnCameraInitialization(object Sender, CameraInitEventArgs e)
    {

#if (UNITY_EDITOR_WIN)
        byte[] b = System.Text.Encoding.ASCII.GetBytes(Application.persistentDataPath);
        SetPath(b, b.Length);
        //SystemManager.Instance.strBytes, SystemManager.Instance.strBytes.Length, 
#elif (UNITY_ANDROID)
        SetPath(Application.persistentDataPath.ToCharArray());    
#endif

        try
        {
            TrackerParam mTrackParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
            ExperimentParam mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
            mSystemManager.User.ModeMapping = mTrackParam.bMapping;
            mSystemManager.User.ModeTracking = mTrackParam.bTracking;
            mSystemManager.User.ModeAsyncQualityTest = mTrackParam.bPlaneOptimization;
            mSystemManager.User.bSaveTrajectory = mTrackParam.bARReset;
            mSystemManager.User.UseGyro = mTrackParam.bIMU;

            mSystemManager.AppData.numFeatures = mTrackParam.nFeatures;
            mSystemManager.AppData.numPyramids = mTrackParam.nPyramids;
            mSystemManager.AppData.numSkipFrames = mTrackParam.nSkipFrames;
            mSystemManager.AppData.numLocalKeyFrames = mTrackParam.nLocalKeyFrames;
            mSystemManager.AppData.JpegQuality = mTrackParam.nJpegQuality;


            mSystemManager.CamParam.fx = (float)e.camMat.get(0, 0)[0];
            mSystemManager.CamParam.fy = (float)e.camMat.get(1, 1)[0];
            mSystemManager.CamParam.cx = (float)e.camMat.get(0, 2)[0];
            mSystemManager.CamParam.cy = (float)e.camMat.get(1, 2)[0];
            mSystemManager.CamParam.w = e.width;
            mSystemManager.CamParam.h = e.height;
            bCamInit = true;

            mSystemManager.CamParam.d1 = (float)e.distCoeffs.get(0, 0)[0];
            mSystemManager.CamParam.d2 = (float)e.distCoeffs.get(1, 0)[0];
            mSystemManager.CamParam.d3 = (float)e.distCoeffs.get(2, 0)[0];
            mSystemManager.CamParam.d4 = (float)e.distCoeffs.get(3, 0)[0];

            //기기의 NDK에 데이터 전송
            SetInit((int)mSystemManager.CamParam.w,
                (int)mSystemManager.CamParam.h,
                mSystemManager.CamParam.fx,
                mSystemManager.CamParam.fy,
                mSystemManager.CamParam.cx,
                mSystemManager.CamParam.cy,
                mSystemManager.CamParam.d1,
                mSystemManager.CamParam.d2,
                mSystemManager.CamParam.d3,
                mSystemManager.CamParam.d4,
                mSystemManager.AppData.numFeatures,
                mSystemManager.AppData.numPyramids,
                1.2f,
                mSystemManager.AppData.numSkipFrames,
                mSystemManager.AppData.numLocalKeyFrames
            );
            SetUserName(
                mSystemManager.User.UserName.ToCharArray(),
                mSystemManager.User.UserName.Length
            );

            ConnectDevice();

            //플롯데이터

            int nFloat = 20;
            int nByte = 10;

            int nidx = 0;
            float[] IntrinsicData = new float[nFloat];
            IntrinsicData[nidx++] = (float)mSystemManager.CamParam.w;
            IntrinsicData[nidx++] = (float)mSystemManager.CamParam.h;
            IntrinsicData[nidx++] = mSystemManager.CamParam.fx;
            IntrinsicData[nidx++] = mSystemManager.CamParam.fy;
            IntrinsicData[nidx++] = mSystemManager.CamParam.cx;
            IntrinsicData[nidx++] = mSystemManager.CamParam.cy;
            IntrinsicData[nidx++] = 0f;
            IntrinsicData[nidx++] = 0f;
            IntrinsicData[nidx++] = 0f;
            IntrinsicData[nidx++] = 0f;
            IntrinsicData[nidx++] = 0f;
            IntrinsicData[nidx++] = mSystemManager.AppData.JpegQuality;
            IntrinsicData[nidx++] = mSystemManager.AppData.numSkipFrames;
            IntrinsicData[nidx++] = mSystemManager.AppData.numContentKFs;

            ////알람 서버에 등록
            ApplicationData appData = mSystemManager.AppData;
            UdpAsyncHandler.Instance.UdpSocketBegin(appData.UdpAddres, appData.UdpPort, appData.LocalPort);
            if (mExperimentSetup.rKeywords.Length > 0)
                mSystemManager.User.ReceiveKeywords += mExperimentSetup.rKeywords;
            string[] keywords = mSystemManager.User.ReceiveKeywords.Split(',');
            for (int i = 0; i < keywords.Length; i += 2)
            {
                UdpAsyncHandler.Instance.Send(mSystemManager.User.UserName, keywords[i], "connect", keywords[i + 1]);
            }
            
            ////데이터 서버에 등록
            if (mExperimentSetup.sKeywords.Length>0)
                mSystemManager.User.SendKeywords += mExperimentSetup.sKeywords;
            InitConnectData data = mSystemManager.GetConnectData();
            string msg = JsonUtility.ToJson(data);
            byte[] bdata = System.Text.Encoding.UTF8.GetBytes(msg);

            UnityWebRequest request = new UnityWebRequest(mSystemManager.AppData.Address + "/Connect?port=40003");
            request.method = "POST";
            UploadHandlerRaw uH = new UploadHandlerRaw(bdata);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
            request.downloadHandler = new DownloadHandlerBuffer();
            UnityWebRequestAsyncOperation res = request.SendWebRequest();

            while (!request.downloadHandler.isDone)
            {
                continue;
            }

            ////Device & Map store, 슬램 서버에 접속
            var timeSpan = DateTime.UtcNow - mSystemManager.StartTime;
            double ts = timeSpan.TotalMilliseconds;

            //string addr2 = SystemManager.
            //ce.AppData.Address + "/Store?keyword=DeviceConnect&id=0&src=" + SystemManager.Instance.User.UserName;
            string msg2 = mSystemManager.User.UserName + "," + mSystemManager.User.MapName;
            byte[] bdatab = System.Text.Encoding.UTF8.GetBytes(msg2);
            float[] fdataa = IntrinsicData;// mSystemManager.IntrinsicData;
            
            int nbFlagIdx = fdataa.Length * 4;
            byte[] bdata2 = new byte[nByte + nbFlagIdx + bdatab.Length];
            bdata2[nbFlagIdx] = mSystemManager.User.ModeMapping ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 1] = mSystemManager.User.ModeTracking ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 2] = mSystemManager.User.UseGyro ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 3] = mSystemManager.User.bSaveTrajectory ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 4] = mSystemManager.User.ModeAsyncQualityTest ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 5] = mExParam.bEdgeBase ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 6] = mExParam.bCreateKFMethod ? (byte)1 : (byte)0;
            bdata2[nbFlagIdx + 7] = mExParam.bCommuTest ? (byte)1 : (byte)0;

            //인트린직 파라메터
            Buffer.BlockCopy(fdataa, 0, bdata2, 0, nbFlagIdx);
            //플래그 데이터
            Buffer.BlockCopy(bdatab, 0, bdata2, nbFlagIdx + nByte, bdatab.Length);

            UdpData deviceConnectData = new UdpData("DeviceConnect", mSystemManager.User.UserName, 0, bdata2, ts);
            StartCoroutine(sender.SendData(deviceConnectData));
            //mText.text = "success connect!!! " + mSystemManager.FocalLengthX+" "+mSystemManager.FocalLengthY;
        }
        catch (Exception ex)
        {
            mText.text = ex.ToString();
        }
        mSystemManager.bConnect = true;
        mSystemManager.bStart = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        //종료 이벤트 등록
        Application.wantsToQuit += WantsToQuit;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

}
