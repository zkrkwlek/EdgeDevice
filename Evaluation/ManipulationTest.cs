using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManipulationTest : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataCommunicator mSender;
    public ParameterManager mParamManager;
    public PlaneManager mPlaneManager;
    public Text mText;
    ExperimentParam mTestParam;
    TrackerParam mTrackerParam;
    public GameObject prefabObj;

    public RawImage rawImage;
    VirtualObjectManipulationState voState;
    Plane p;
    Mat invCamMat;
    int width;
    int height;
    float widthScale;
    float heightScale;

    string[] logString;

    void Awake()
    {
        mTestParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];

        if (!mTestParam.bManipulationTest || !mTrackerParam.bTracking)
        {
            enabled = false;
            return;
        }
        voState = VirtualObjectManipulationState.None;
        p = new Plane(Vector3.zero, 0f);
        logString = new string[2];
        logString[0] = "Registration";
        logString[1] = "Manipulation";
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
    void OnEnable()
    {
        CameraInitEvent.camInitialized += OnCameraInitialization;
        PlaneDetectionEvent.planeDetected += OnPlaneDetection;
    }
    void OnDisable()
    {
        CameraInitEvent.camInitialized -= OnCameraInitialization;
        PlaneDetectionEvent.planeDetected -= OnPlaneDetection;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    IEnumerator ChangeColor(Vector4 c)
    {
        rawImage.color = c;
        yield return new WaitForSecondsRealtime(0.3f);
        rawImage.color = new Vector4(0f, 0f, 0f, 0f);
    }

    int touchID = 0;
    GameObject touchObject = null;
    void Update()
    {
        int nTouchCount = Input.touchCount;

        if (nTouchCount == 1)
        {
            try {
                
                touchID++;
                Touch touch = Input.GetTouch(0);
                var phase = touch.phase;

                Ray raycast = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit raycastHit;

                bool bHit = Physics.Raycast(raycast, out raycastHit);
                if (voState == VirtualObjectManipulationState.None && phase == TouchPhase.Began)
                {
                    if (bHit)
                    {
                        touchObject = raycastHit.collider.gameObject;
                        voState = VirtualObjectManipulationState.Update;
                    }
                    else
                    {
                        voState = VirtualObjectManipulationState.Registration;
                    }
                }
                var ray = mPlaneManager.CreateRay(touch.position, widthScale, heightScale, invCamMat);
                float dist;
                Plane p;
                int pid;
                //bool bRay = p.Raycast(ray, out dist);
                bool bRay = mPlaneManager.FindNearestPlane(ray, out pid, out p, out dist);

                if (mTestParam.bShowLog)
                    mText.text = pid +" = "+p.ToString()+" = "+dist;

                if (bRay)
                {
                    float[] fdata = new float[5];
                    Vector3 newPos = ray.origin + ray.direction * dist;
                    //fdata[0] = x;
                    //fdata[1] = y;
                    fdata[2] = newPos.x;
                    fdata[3] = newPos.y;
                    fdata[4] = newPos.z;

                    string keyword = "";
                    int sendID = 0;
                    bool bSend = false;
                    if (voState == VirtualObjectManipulationState.Update)
                    {
                        bSend = true;
                        keyword = "VO.MANIPULATE";
                        sendID = touchObject.GetComponentInParent<ARObject>().contentID;
                        if (mTestParam.bShowLog)
                            mText.text = logString[1];
                    }
                    if (voState == VirtualObjectManipulationState.Registration && phase == TouchPhase.Ended)
                    {
                        bSend = true;
                        keyword = "VO.CREATE";
                        sendID = touchID;
                        if (mTestParam.bShowLog)
                            mText.text = logString[0];
                    }
                    if (bSend)
                    {
                        if(voState==VirtualObjectManipulationState.Registration)
                            StartCoroutine(ChangeColor(new Vector4(1f, 0f, 0f, 0.3f)));
                        byte[] bdata = new byte[fdata.Length * 4];
                        Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                        UdpData mdata = new UdpData(keyword, mSystemManager.User.UserName, sendID, bdata, 1.0);
                        StartCoroutine(mSender.SendData(mdata));

                    }
                    if (phase == TouchPhase.Ended)
                    {
                        touchObject = null;
                        voState = VirtualObjectManipulationState.None;
                    }
                }//bray
            }
            catch(Exception ex)
            {
                mText.text = ex.ToString();
            }
        }
        else
        {
            touchObject = null;
            voState = VirtualObjectManipulationState.None;
        }//if count
    }

    
}
