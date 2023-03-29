using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public enum VirtualObjectManipulationState { None, Registration, Update,Trigger}

public class TouchManager : MonoBehaviour
{
    string[] logString = new string[4];

    public GameObject UVR;
    public SystemManager mSystemManager;
    public CameraManager mCamManager;
    public DataSender mSender;
    VirtualObjectManipulationState voState;
    //public ParticleSystem mParticleSystem;
    public Text mText;
    // Start is called before the first frame update
    void Awake()
    {
        voState = VirtualObjectManipulationState.None;
        p = new Plane(Vector3.zero, 0f);
        //mParticleSystem = GetComponent<ParticleSystem>();
        //var renderer = mParticleSystem.GetComponent<Renderer>();
        //if (renderer != null)
        //    renderer.enabled = true;

        logString[0] = "NONE";
        logString[1] = "Registration";
        logString[2] = "Manipulation";
        logString[3] = "Trigger";
    }
    Plane p;
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
    Mat invCamMat;
    int width;
    int height;
    float widthScale;
    float heightScale;

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
    [HideInInspector]
    public GameObject touchObject = null;

    // Update is called once per frame
    void Update()
    {

        try {
            bool bTouch = false;
            Vector2 touchPos = Vector2.zero;

            int nTouchCount = Input.touchCount;

            if (nTouchCount > 0)
            {

                if (nTouchCount == 1)
                {
                    Touch touch = Input.GetTouch(0);
                    var phase = touch.phase;

                    Ray raycast = Camera.main.ScreenPointToRay(touch.position);
                    RaycastHit raycastHit;
                    GameObject obj = null;
                    bool bHit = Physics.Raycast(raycast, out raycastHit);

                    ////조작 상태 설정
                    if (voState == VirtualObjectManipulationState.None && !bHit && phase == TouchPhase.Began)
                    {
                        voState = VirtualObjectManipulationState.Registration;
                    }
                    if (voState == VirtualObjectManipulationState.None && bHit && phase == TouchPhase.Began)
                    {
                        ////선택 객체가 경로 위에 있으면 트리거
                        ////그냥 객체면 조작
                        
                        touchObject = raycastHit.collider.gameObject;
                        var pathManager = touchObject.GetComponent<PathManager>();
                        if (pathManager.isOnPath()) {
                            voState = VirtualObjectManipulationState.Trigger;
                        }else if(pathManager.mObjState == ObjectState.None) { 
                            voState = VirtualObjectManipulationState.Update;
                            pathManager.mObjState = ObjectState.Manipulation;
                            touchObject.GetComponent<Renderer>().material.color = Color.yellow;
                        }
                    }
                    //////중간에 움직여서 패스에 올렸을 때
                    //if (voState == VirtualObjectManipulationState.Update) {
                    //    touchObject = raycastHit.collider.gameObject;
                    //    var pathManager = touchObject.GetComponent<PathManager>();
                    //    if (pathManager.mObjState == ObjectState.OnPath) { 
                    //        //여기서 조작 멈추게 해야 함.
                    //    }
                    //}

                    ////3차원 복원
                    float x = touch.position.x / widthScale;
                    float y = height - touch.position.y / heightScale;
                    Mat pos = new Mat(3, 1, CvType.CV_64FC1);
                    pos.put(0, 0, x);
                    pos.put(1, 0, y);
                    pos.put(2, 0, 1f);
                    Mat temp = invCamMat * pos;
                    var ptCam = new Vector3((float)temp.get(0, 0)[0], (float)temp.get(1, 0)[0], (float)temp.get(2, 0)[0]);

                    var toPoint = UVR.transform.localToWorldMatrix.MultiplyPoint(ptCam);
                    var dir = toPoint - UVR.transform.position;
                    dir = dir.normalized;
                    Ray ray = new Ray(UVR.transform.position, dir);
                    float dist = 0f;
                    bool bRay = p.Raycast(ray, out dist);
                    float[] fdata = new float[5];

                    if (bRay && voState != VirtualObjectManipulationState.Trigger)
                    {
                        ////트리거이면 객체 위치를 기록 안함.
                        Vector3 newPos = ray.origin + ray.direction * dist;
                        fdata[0] = x;
                        fdata[1] = y;
                        fdata[2] = newPos.x;
                        fdata[3] = newPos.y;
                        fdata[4] = newPos.z;
                        //byte[] bdata = new byte[fdata.Length * 4];
                        //Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                        //UdpData mdata = new UdpData("VO.SELECTION", mSystemManager.User.UserName, cid, bdata, 1.0);
                        //StartCoroutine(mSender.SendData(mdata));
                    }

                    string keyword = "";
                    int sendID = 0;
                    bool bSend = false;
                    if (voState == VirtualObjectManipulationState.Update)
                    {
                        bSend = true;
                        keyword = "VO.SELECTION";
                        var pathManager = touchObject.GetComponent<PathManager>();
                        sendID = pathManager.contentID;
                    }
                    if (voState == VirtualObjectManipulationState.Registration && phase == TouchPhase.Ended)
                    {
                        bSend = true;
                        keyword = "ContentGeneration";
                        sendID = mCamManager.mnFrame;
                    }
                    if (voState == VirtualObjectManipulationState.Trigger && phase == TouchPhase.Ended)
                    {
                        bSend = true;
                        keyword = "VO.REQMOVE";
                        var pathManager = touchObject.GetComponent<PathManager>();
                        sendID = pathManager.contentID;
                    }
                    if (bSend)
                    {
                        byte[] bdata = new byte[fdata.Length * 4];
                        Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                        UdpData mdata = new UdpData(keyword, mSystemManager.User.UserName, sendID, bdata, 1.0);
                        StartCoroutine(mSender.SendData(mdata));
                    }

                    //var path = touchObject.GetComponent<PathManager>();
                    //if(touchObject)
                    //    mText.text = touchObject.tag + ", " + path.mbPath +" "+path.currPathID+ " " +" "+" "+path.contentID + " =" + path.log;
                    ////mText.text = logString[(int)voState] + " " + keyword + " " + sendID + "=" + fdata[0] + " " + fdata[1] + " " + fdata[2];

                    if (phase == TouchPhase.Ended)
                    {
                        voState = VirtualObjectManipulationState.None;
                        if (touchObject)
                        {
                            var path = touchObject.GetComponent<PathManager>();
                            if(path.mObjState == ObjectState.Manipulation)
                            {
                                path.mObjState = ObjectState.None;
                                touchObject.GetComponent<Renderer>().material.color = Color.blue;
                            }
                        }
                        touchObject = null;
                    }
                }
                else {
                    voState = VirtualObjectManipulationState.None;
                    if (touchObject)
                    {
                        var path = touchObject.GetComponent<PathManager>();
                        if (path.mObjState == ObjectState.Manipulation)
                        {
                            path.mObjState = ObjectState.None;
                            touchObject.GetComponent<Renderer>().material.color = Color.blue;
                        }
                    }
                    touchObject = null;
                }
                
                //Ray raycast = Camera.main.ScreenPointToRay(touch.position);
                //RaycastHit raycastHit;
                //if (Physics.Raycast(raycast, out raycastHit))
                //{
                //    var obj = raycastHit.collider.gameObject;

                

                //    var pathManager = obj.GetComponent<PathManager>();
                //    int cid = pathManager.contentID;
                //    float x = touch.position.x / widthScale;
                //    float y = height - touch.position.y / heightScale;
                //    Mat pos = new Mat(3, 1, CvType.CV_64FC1);
                //    pos.put(0, 0, x);
                //    pos.put(1, 0, y);
                //    pos.put(2, 0, 1f);
                //    Mat temp = invCamMat * pos;
                //    var ptCam = new Vector3((float)temp.get(0, 0)[0], (float)temp.get(1, 0)[0], (float)temp.get(2, 0)[0]);

                //    var toPoint = UVR.transform.localToWorldMatrix.MultiplyPoint(ptCam);
                //    var dir = toPoint - UVR.transform.position;
                //    dir = dir.normalized;
                //    Ray ray = new Ray(UVR.transform.position, dir);
                //    float dist = 0f;
                //    bool bRay = p.Raycast(ray, out dist);

                //    if (bRay)
                //    {
                //        Vector3 newPos = ray.origin + ray.direction * dist;
                //        float[] fdata = new float[1000];
                //        fdata[0] = x;
                //        fdata[1] = y;
                //        fdata[2] = newPos.x;
                //        fdata[3] = newPos.y;
                //        fdata[4] = newPos.z;
                //        byte[] bdata = new byte[fdata.Length * 4];
                //        Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                //        UdpData mdata = new UdpData("VO.SELECTION", mSystemManager.User.UserName, cid, bdata, 1.0);
                //        StartCoroutine(mSender.SendData(mdata));
                //    }

                //    //pathManager.Move();
                //    //mText.text = obj.name+" "+ ;

                //    //Debug.Log("Something Hit " + raycastHit.collider.name);



                //}
                //else                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
                //{
                //    ////화면 끝 처리 필요
                //    if (touch.phase != TouchPhase.Ended)
                //        return;
                    
                //    float x = touch.position.x / widthScale;
                //    float y = height - touch.position.y / heightScale;
                //    Mat pos = new Mat(3, 1, CvType.CV_64FC1);
                //    pos.put(0, 0, x);
                //    pos.put(1, 0, y);
                //    pos.put(2, 0, 1f);
                //    Mat temp = invCamMat * pos;
                //    var ptCam = new Vector3((float)temp.get(0, 0)[0], (float)temp.get(1, 0)[0], (float)temp.get(2, 0)[0]);

                //    var toPoint = UVR.transform.localToWorldMatrix.MultiplyPoint(ptCam);
                //    var dir = toPoint - UVR.transform.position;
                //    dir = dir.normalized;
                //    Ray ray = new Ray(UVR.transform.position, dir);
                //    float dist = 0f;
                //    bool bRay = p.Raycast(ray, out dist);

                //    if (bRay)
                //    {
                //        Vector3 newPos = ray.origin + ray.direction * dist;
                //        float[] fdata = new float[1000];
                //        fdata[0] = x;
                //        fdata[1] = y;
                //        fdata[2] = newPos.x;
                //        fdata[3] = newPos.y;
                //        fdata[4] = newPos.z;
                //        byte[] bdata = new byte[fdata.Length * 4];
                //        Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                //        UdpData mdata = new UdpData("ContentGeneration", mSystemManager.User.UserName, mCamManager.mnFrame, bdata, 1.0);
                //        StartCoroutine(mSender.SendData(mdata));
                //    }
                //}
            }
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }
}
