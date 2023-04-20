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

    MarkerDetectEventArgs2 ae = null;

    void OnMarkerInteraction2(object sender, MarkerDetectEventArgs2 e)
    {
        if(e.mnFrameID % mnSkipFrame != 0)
            return;
        ae = e;
        
        //byte[] bdata = new byte[e.marker_data.Length * 4];
        //Buffer.BlockCopy(e.marker_data, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
        //UdpData mdata = new UdpData("MarkerResults", mSystemManager.User.UserName, e.mnFrameID, bdata, 1.0);
        //StartCoroutine(mSender.SendData(mdata));

        //mText.text = "send marker data = " + bdata.Length + " " + e.marker_data[0];
        //2차원 마커 포지션, 카메라 위치, 서버에서 디스턴스 받으면 저장.
        //Camera.main.transform.position;
    }

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
                    //bNeedNewKF = false;
                }
            }

            if (bSendImage)
            {
                //이미지 압축
                Imgcodecs.imencode(".jpg", e.rgbMat.clone(), data, param);//jpg
                //NDK로 전송
                IntPtr addr = (IntPtr)data.dataAddr();
                UdpData idata = new UdpData("Image", mSystemManager.User.UserName, frameID, addr, data.rows(), 0f);
                mSender.SendDataWithNDK(idata);

                bSendImage = false;
                if (bEdgeBase)
                    bNeedNewKF = false;
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

    //AR 파운데이션 정보 전송 + 마커 결과도 전송하는듯? 기억이 안남.ㅏ
    void OnCameraFrameReceived2(object sender, ImageCatchEventArgs e)
    {
        try
        {
            var frameID = e.mnFrameID;
            if (frameID % mnSkipFrame == 0)
            {
                //맵포인트위 위치 전송
                //var changed = PointCloudManager2.Instance.Changed;
                //var nMP = PointCloudManager2.Instance.NumMapPoints;
                var numPoints = mnPoints;
                var mapPoints = points;
                
                Imgcodecs.imencode(".jpg", e.rgbMat, data, param);
                
                byte[] bImgData = data.toArray();//mCamManager.m_Texture.EncodeToJPG(mSystemManager.AppData.JpegQuality);
                var timeSpan = DateTime.UtcNow - mSystemManager.StartTime;
                double ts = timeSpan.TotalMilliseconds;

                ////서버로 전송   
                UdpData idata = new UdpData("Image", mSystemManager.User.UserName, frameID, bImgData, ts);
                StartCoroutine(mSender.SendData(idata));

                if (numPoints > 0)
                //if (changed && nMP > 0)//&& mTestManager.mnFrame % mTestManager.mnSkipFrame == 0
                {
                    ////텍스쳐, 포즈, 이미지를 한번에 전송하기


                    //포즈 갱신
                    float angle = 0.0f;
                    Vector3 _axis = Vector3.zero;
                    Camera.main.transform.rotation.ToAngleAxis(out angle, out _axis);
                    float angle2 = angle * Mathf.Deg2Rad;
                    _axis = angle2 * _axis;
                    Vector3 _c = Camera.main.transform.position;
                    Matrix3x3 R = Matrix3x3.EXP(_axis);
                    Vector3 t = -(R * _c);

                    float[] fposedata = new float[12];
                    R.Copy(ref fposedata, 0);
                    fposedata[9] = t.x;
                    fposedata[10] = t.y;
                    fposedata[11] = t.z;

                    //var numPoints = PointCloudManager2.Instance.NumMapPoints;
                    //var mapPoints = PointCloudManager2.Instance.MapPoints;

                    float[] fmapdata = new float[numPoints * 3];
                    int idx = 0;
                    var mat = Camera.main.transform.worldToLocalMatrix;
                    foreach (var point in mapPoints)
                    {
                        var res = mat.MultiplyPoint3x4(point);
                        //fmapdata[idx++] = point.x;
                        //fmapdata[idx++] = point.y;
                        //fmapdata[idx++] = point.z;
                        fmapdata[idx++] = res.x;
                        fmapdata[idx++] = -res.y;
                        fmapdata[idx++] = res.z;
                    }
                    float[] dataIdx = new float[1];
                    byte[] bmapdata = new byte[(fposedata.Length + fmapdata.Length + dataIdx.Length) * 4 + bImgData.Length]; //이차원 위치 추가

                    int nPoseSize = fposedata.Length * 4;
                    int nMapSize = fmapdata.Length * 4;
                    int nDataSize = dataIdx.Length * 4;

                    dataIdx[0] = (float)(nPoseSize + nMapSize + nDataSize);
                    //dataIdx[1] = tempMarker.id;
                    //dataIdx[2] = tempMarker.corners[0].x;
                    //dataIdx[3] = tempMarker.corners[0].y;

                    Buffer.BlockCopy(dataIdx, 0, bmapdata, 0, nDataSize); //전체 실수형 데이터 수
                    Buffer.BlockCopy(fposedata, 0, bmapdata, nDataSize, nPoseSize); // 포즈 정보, 12개
                    Buffer.BlockCopy(fmapdata, 0, bmapdata, nDataSize + nPoseSize, nMapSize); // 맵포인트 정보
                    Buffer.BlockCopy(bImgData, 0, bmapdata, nDataSize + nPoseSize + nMapSize, bImgData.Length); //이밎 ㅣ정보

                    UdpData mdata = new UdpData("ARFoundationMPs", mSystemManager.User.UserName, frameID, bmapdata, 1.0);
                    StartCoroutine(mSender.SendData(mdata));
                    //mText.text = "\t\t\tPointCloudManager = " + mTestManager.mnFrame + " " + mSystemManager.User.UserName+" "+m_NumParticles;
                    //changed = false;

                    //byte[] bdata = new byte[ae.marker_data.Length * 4];
                    //Buffer.BlockCopy(ae.marker_data, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                    //UdpData madata = new UdpData("MarkerResults", mSystemManager.User.UserName, frameID, bdata, 1.0);
                    //StartCoroutine(mSender.SendData(madata));

                    if (ae != null && ae.mnFrameID == frameID)
                    {
                        byte[] bamdata = new byte[ae.marker_data.Length * 4];
                        Buffer.BlockCopy(ae.marker_data, 0, bamdata, 0, bamdata.Length); //전체 실수형 데이터 수
                        UdpData amdata = new UdpData("MarkerResults", mSystemManager.User.UserName, e.mnFrameID, bamdata, 1.0);
                        StartCoroutine(mSender.SendData(amdata));
                    }

                }
            }
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
