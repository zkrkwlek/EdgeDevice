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

    string keyword;
    public ParameterManager mParamManager;
    TrackerParam mTrackParam;
    ExperimentParam mExParam;
    bool bEdgeBase;
    bool bCoordAlign;

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

    ////��Ŀ ������ ���� ���̵� ���
    bool bSendImage = false;
    int prevID = -1;
    //�̹��� ����
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
            if (bCoordAlign && bSendImage)
            {
                //��� �ڼ� ����
                //Ű���� : DevicePoseForAlign
                var q = Camera.main.transform.rotation;
                var t = Camera.main.transform.position;
                //Matrix4x4 invertYMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
                //Matrix4x4 ARM = invertYMatrix * Matrix4x4.TRS(t,q,Vector3.one) * invertYMatrix;
                var T = Matrix4x4.TRS(t, q, Vector3.one);
                float[] farray = new float[12];
                farray[0] = T.m00;
                farray[1] = T.m01;
                farray[2] = T.m02;

                farray[3] = T.m10;
                farray[4] = T.m11;
                farray[5] = T.m12;

                farray[6] = T.m20;
                farray[7] = T.m21;
                farray[8] = T.m22;

                farray[9] = t.x;
                farray[10] = t.y;
                farray[11] = t.z;
                byte[] bdata = new byte[48];
                Buffer.BlockCopy(farray, 0, bdata, 0, 48);
                UdpData pdata = new UdpData("DevicePoseForAlign", mSystemManager.User.UserName, frameID, bdata, 0f);
                StartCoroutine(mSender.SendData(pdata));
            }
            if (bSendImage)
            {
                //�̹��� ����
                Imgcodecs.imencode(".jpg", e.rgbMat.clone(), data, param);//jpg
                //NDK�� ����
                //IntPtr addr = (IntPtr)data.dataAddr();
                //UdpData idata = new UdpData("Image", mSystemManager.User.UserName, frameID, addr, data.rows(), 0f);
                //StartCoroutine(mSender.SendDataWithNDK(idata));
                byte[] bImgData = data.toArray();
                UdpData idata = new UdpData(keyword, mSystemManager.User.UserName, frameID, bImgData, 0f);
                StartCoroutine(mSender.SendData(idata));
                bSendImage = false;

                //���⼭ �̹����� �����ϴ°� �������� ��.
                IntPtr addr2 = (IntPtr)e.rgbMat.dataAddr();
                StoreImage(frameID, addr2);
                //if (bEdgeBase)
                //    bNeedNewKF = false;

                ////����� �ڼ��� ������ ����.
            }

            //if((!bEdgeBase && frameID % mnSkipFrame == 0) || (bEdgeBase && bNeedNewKF))
            //{
            //    //mText.text = "image id = " + frameID+" "+prevID;

            //}

            if (frameID < prevID)
            {
                if(prevID > 0)
                {
                    //�� ��ȭ�� 1�ʸ� ��� ��.
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
        
        var mCamParam = (CamParam)mParamManager.DictionaryParam["Camera"];
        if (mCamParam.bCaptureDepth)
            keyword = "DImage";
        else
            keyword = "Image";

        bEdgeBase = mExParam.bEdgeBase;
        bCoordAlign = mExParam.bCoordAlign;

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
