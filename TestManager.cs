using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;


public class TestManager : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)
    [DllImport("edgeslam")]
    private static extern void StoreImage(int id, IntPtr addr);
#elif UNITY_ANDROID
[DllImport("edgeslam")]
    private static extern void StoreImage(int id, IntPtr addr);
#endif




    //public CameraManager mCamManager;
    public DataCommunicator mSender;
    public SystemManager mSystemManager;
    public Text mText;
    public RawImage rawImage;

    public ParameterManager mParamManager;
    TrackerParam mTrackParam;
    ExperimentParam mExParam;
    bool bEdgeBase;

    int mnSkipFrame;

    //Mat rgbMat;

    MatOfInt param;
    MatOfByte data;

    bool WantsToQuit()
    {
        //if (rgbMat != null)
        //    rgbMat.Dispose();
        //if (param != null)
        //    param.Dispose();
        //if (data != null)
        //    data.Dispose();
        return true;
    }

    void OnEnable()
    {
        ImageCatchEvent.frameReceived += OnCameraFrameReceived;
        NeedKeyFrameEvent.needNewKeyFrame += OnNeedNewKeyFrame;
        //PointCloudUpdateEvent.pointCloudUpdated += OnPointUpdated;
        //MarkerDetectEvent2.markerDetected += OnMarkerInteraction2;
    }

    void OnDisable()
    {
        ImageCatchEvent.frameReceived -= OnCameraFrameReceived;
        NeedKeyFrameEvent.needNewKeyFrame -= OnNeedNewKeyFrame;
        //PointCloudUpdateEvent.pointCloudUpdated -= OnPointUpdated;
        //MarkerDetectEvent2.markerDetected -= OnMarkerInteraction2;
    }

    List<Vector3> points;
    int mnPoints;

    bool bNeedNewKF = false;
    void OnNeedNewKeyFrame(object sender, int id) {
        bNeedNewKF = true;
    }

    ////마커 전송할 때의 아이디 기록
    bool bSendImage = false;
    int prevID = -1;
    //이미지 전송
    void OnCameraFrameReceived(object sender, ImageCatchEventArgs e) {
        try {
            var frameID = e.mnFrameID;

            if (!bEdgeBase)
            {
                if(frameID % mnSkipFrame == 0)
                {
                    bSendImage = true;
                }
            }else if (bEdgeBase)
            {
                if (bNeedNewKF)
                {
                    bSendImage = true;
                    bNeedNewKF = false;
                }
            }

            if (bSendImage)
            {
                //이미지 압축
                Imgcodecs.imencode(".jpg", e.rgbMat.clone(), data, param);//jpg
                //NDK로 전송
                IntPtr addr = (IntPtr)data.dataAddr();
                UdpData idata = new UdpData("Image", mSystemManager.User.UserName, frameID, addr, data.rows(), 0f);
                StartCoroutine(mSender.SendDataWithNDK(idata));
                bSendImage = false;

                //여기서 이미지를 저장하는게 나을지도 모름.
                IntPtr addr2 = (IntPtr)e.rgbMat.dataAddr();
                StoreImage(frameID, addr2);
                //if (bEdgeBase)
                //    bNeedNewKF = false;
            }
            //if((!bEdgeBase && frameID % mnSkipFrame == 0) || (bEdgeBase && bNeedNewKF))
            //{
            //    //mText.text = "image id = " + frameID+" "+prevID;

            //}

            if (frameID < prevID)
            {
                if(prevID > 0)
                {
                    //색 변화를 1초만 줘야 함.
                    rawImage.color = new Color(1f, 0f, 0f, 0.3f);
                }
            }else
                prevID = frameID;
        }
        catch (Exception ex)
        {
            mText.text = ex.ToString();
        }
    }
        
    void Awake()
    {
        mTrackParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        
        if(mExParam.bOXRTest)
        {
            enabled = false;
            return;
        }
        
        bEdgeBase = mExParam.bEdgeBase;

        data = new MatOfByte();
        int[] temp = new int[2];
        temp[0] = Imgcodecs.IMWRITE_JPEG_QUALITY; //JPEG_QUALITY
        temp[1] = mTrackParam.nJpegQuality;
        param = new MatOfInt(temp);

        points = new List<Vector3>();
        mnPoints = 0;
        mnSkipFrame = mTrackParam.nSkipFrames;
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
