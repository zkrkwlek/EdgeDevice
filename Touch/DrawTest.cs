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
    Mat camMat;
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
            camMat = e.camMat;
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
    bool bInit = true;
    int touchID = 0;
    List<Vector3> touchPoints;
    Vector3 prevPoint;
    // Update is called once per frame
    void Update()
    {
        try {
            //if (bInit)
            //{
            //    var obj = new GameObject();
            //    var spos = new Vector3(1, 0, 0);
            //    var epos = new Vector3(0, 10, 0);
            //    obj.transform.position = spos;
            //    var q = Quaternion.LookRotation(new Vector3(0f, 0f, 1f));
            //    obj.transform.rotation = q;
            //    var lineRenderer = obj.AddComponent<LineRenderer>();
            //    lineRenderer.material.color = Color.red;
            //    lineRenderer.startWidth = 1f;
            //    lineRenderer.endWidth = 1f;
            //    lineRenderer.SetPosition(0, spos);
            //    lineRenderer.SetPosition(1, epos);
            //    lineRenderer.alignment = LineAlignment.TransformZ;
            //    bInit = false;
            //}

            int nTouchCount = Input.touchCount;
            if (nTouchCount == 0)
                return;

            Touch touch = Input.GetTouch(0);
            var phase = touch.phase;

            var ray = mPlaneManager.CreateRay(touch.position, widthScale, heightScale, invCamMat);
            float dist;
            Plane p;
            int pid;
            //bool bRay = p.Raycast(ray, out dist);
            bool bRay = mPlaneManager.FindNearestPlane(ray, out pid, out p, out dist);
            if (bRay)
            {
                touchID++;
                Vector3 point = ray.origin + ray.direction * dist;
                //touchPoints.Add(point);

                //var temp = Camera.main.transform.worldToLocalMatrix.MultiplyPoint(point);
                //Mat pos = new Mat(3, 1, CvType.CV_64FC1);
                //pos.put(0, 0, temp.x);
                //pos.put(1, 0, temp.y);
                //pos.put(2, 0, temp.z);
                //var proj = camMat* pos;
                //proj = proj / temp.z;
                //float newx = (float)proj.get(0, 0)[0];
                //float newy = (float)proj.get(1, 0)[0];
                //float sx = touch.position.x / widthScale;
                //float sy = touch.position.y / heightScale;
                //mText.text = pid +"=="+newx + " " + newy + "\\" + sx + " " + sy;

                //처음이 아니면 전송
                if (phase != TouchPhase.Began)
                {
                    //전송
                    float[] tempData = new float[7];
                    tempData[0] = point.x;
                    tempData[1] = point.y;
                    tempData[2] = point.z;
                    tempData[3] = prevPoint.x;
                    tempData[4] = prevPoint.y;
                    tempData[5] = prevPoint.z;
                    tempData[6] = (float)pid;

                    byte[] bdata = new byte[tempData.Length * 4];
                    Buffer.BlockCopy(tempData, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                    UdpData mdata = new UdpData("VO.DRAW", mSystemManager.User.UserName, touchID, bdata, 1.0);
                    StartCoroutine(mSender.SendData(mdata));
                }
                prevPoint = point;
            }
        }
        catch(Exception e){
            mText.text = e.ToString();
        }
    }
}
