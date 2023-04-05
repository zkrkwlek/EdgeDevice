using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawTest : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataSender mSender;
    public ParameterManager mParamManager;
    public PlaneManager mPlaneManager;
    public Text mText;
    ExperimentParam mTestParam;
    TrackerParam mTrackerParam;

    //프로젝션ㅇ 용
    Plane p;
    Mat invCamMat;
    int width;
    int height;
    float widthScale;
    float heightScale;

    void Awake()
    {
        mTestParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];

        if (!mTestParam.bDrawTest || !mTrackerParam.bTracking)
        {
            enabled = false;
            return;
        }
        //voState = VirtualObjectManipulationState.None;
        //p = new Plane(Vector3.zero, 0f);
        //logString = new string[2];
        //logString[0] = "Registration";
        //logString[1] = "Manipulation";
    }

    private void OnEnable()
    {
        CameraInitEvent.camInitialized += OnCameraInitialization;
        PlaneDetectionEvent.planeDetected += OnPlaneDetection;
    }
    private void OnDisable()
    {
        CameraInitEvent.camInitialized -= OnCameraInitialization;
        PlaneDetectionEvent.planeDetected -= OnPlaneDetection;
    }
    void OnCameraInitialization(object Sender, CameraInitEventArgs e)
    {
        try
        {
            invCamMat = e.invCamMat;
            width = e.width;
            height = e.height;
            widthScale = e.widthScale;
            heightScale = e.heightScale;
        }
        catch (Exception ex)
        {
            ex.ToString();
        }
    }

    void OnPlaneDetection(object sender, PlaneEventArgs e)
    {
        //mText.text = "plane event test~~ "+e.plane.ToString();
        p = e.plane;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    int touchID = 0;
    List<Vector3> touchPoints;
    // Update is called once per frame
    void Update()
    {
        try {

            int nTouchCount = Input.touchCount;
            if (nTouchCount == 0)
                return;

            Touch touch = Input.GetTouch(0);
            var phase = touch.phase;

            if(phase == TouchPhase.Began)
            {
                touchPoints = new List<Vector3>();
            }

            if(phase != TouchPhase.Ended)
            {
                //포인트 복원
                var ray = mPlaneManager.CreateRay(touch.position, widthScale, heightScale, invCamMat);
                float dist;
                Plane p;
                int pid;
                //bool bRay = p.Raycast(ray, out dist);
                bool bRay = mPlaneManager.FindNearestPlane(ray, out pid, out p, out dist);
                if (bRay && touch.phase != TouchPhase.Ended)
                {
                    touchID++;
                    Vector3 point = ray.origin + ray.direction * dist;
                    touchPoints.Add(point);
                }
            }
            

            if(phase == TouchPhase.Ended && touchPoints.Count > 0)
            {
                float[] tempData = new float[touchPoints.Count * 3];
                for (int i = 0; i < touchPoints.Count; i++)
                {
                    var point = touchPoints[i];
                    tempData[3 * i] = point.x;
                    tempData[3 * i + 1] = point.y;
                    tempData[3 * i + 2] = point.z;
                }
                byte[] bdata = new byte[tempData.Length * 4];
                Buffer.BlockCopy(tempData, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                UdpData mdata = new UdpData("VO.DRAW", mSystemManager.User.UserName, touchID, bdata, 1.0);
                StartCoroutine(mSender.SendData(mdata));
            }

        }
        catch(Exception e){
            mText.text = e.ToString();
        }
    }
}
