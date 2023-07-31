using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OXRTest : MonoBehaviour
{

    public DataCommunicator mSender;
    public SystemManager mSystemManager;
    public ParameterManager mParamManager;
    TrackerParam mTrackParam;
    ExperimentParam mExParam;

    bool WantsToQuit()
    {
        return true;
    }

    void OnEnable()
    {
        ImageCatchEvent.frameReceived += OnCameraFrameReceived;
    }
    void OnDisable()
    {
        ImageCatchEvent.frameReceived -= OnCameraFrameReceived;
    }

    int mnSkipFrame;
    MatOfInt param;
    MatOfByte data;
    bool bSendImage = false;
    int prevID = -1;
    void OnCameraFrameReceived(object sender, ImageCatchEventArgs e)
    {
        try
        {
            var frameID = e.mnFrameID;
            if (frameID % mnSkipFrame == 0)
            {
                //이미지 압축
                Imgcodecs.imencode(".jpg", e.rgbMat.clone(), data, param);//jpg
                byte[] bImgData = data.toArray();
                var timeSpan = DateTime.UtcNow - mSystemManager.StartTime;
                double ts = timeSpan.TotalMilliseconds;
                UdpData idata = new UdpData("OXR::IMAGE", mSystemManager.User.UserName, frameID, bImgData, ts);
                StartCoroutine(mSender.SendData(idata));
            }

        }
        catch (Exception ex)
        {
            //mText.text = ex.ToString();
        }
    }
    void Awake()
    {
        mTrackParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];

        if (!mExParam.bOXRTest)
        {
            enabled = false;
            return;
        }

        data = new MatOfByte();
        int[] temp = new int[2];
        temp[0] = Imgcodecs.IMWRITE_JPEG_QUALITY; //JPEG_QUALITY
        temp[1] = mTrackParam.nJpegQuality;
        param = new MatOfInt(temp);
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
