using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class ManipulationTest : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataCommunicator mSender;
    public ParameterManager mParamManager;
    public PlaneManager mPlaneManager;
    public EvaluationManager mEvalManager;
    public Text mText;
    public GameObject prefabObj;

    ExperimentParam mTestParam;
    TrackerParam mTrackerParam;
    ObjectParam mObjParam;

    public RawImage rawImage;
    VirtualObjectManipulationState voState;
    Plane p;
    Mat invCamMat;
    int width;
    int height;
    float widthScale;
    float heightScale;
    float heightCropped;
    string[] logString;

    void Awake()
    {
        mTestParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];

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
            heightCropped = e.fHeightCropped;
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

    DateTime startTime;
    int touchID = 0;
    GameObject touchObject = null;
    void Update()
    {
        int nTouchCount = Input.touchCount;

        if (nTouchCount == 1)
        {
            try {
                startTime = DateTime.UtcNow;
                touchID++;
                Touch touch = Input.GetTouch(0);
                var phase = touch.phase;

                //평면 또는 객체. 충돌시 객체, 충돌 안하면 평면임.
                //연속된 입력을 방지하기 위해 상호작용을 위한 키워드는 처음 클릭시 선택함.
                Ray raycast = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit raycastHit;
                GameObject currObject = null;

                bool bObjectHit = Physics.Raycast(raycast, out raycastHit);
                if (voState == VirtualObjectManipulationState.None && phase == TouchPhase.Began)
                {
                    if (bObjectHit)
                    {
                        touchObject = raycastHit.collider.gameObject;
                        voState = VirtualObjectManipulationState.Update;
                    }
                    else
                    {
                        voState = VirtualObjectManipulationState.Registration;
                    }
                }
                if (bObjectHit)
                {
                    currObject = raycastHit.collider.gameObject;
                }
                
                //통신을 위한 정보
                string keyword = "";
                int sendID = 0;
                bool bSend = false;
                Vector3 newPos = Vector3.zero;
                Vector3 axis = Vector3.zero;

                //항상 객체가 붙는 것을 알려줘야 함.
                //평면인지 객체인지
                //객체가 선택되었을 때                

                //가상 객체의 연결 부위 파악.
                //신그래프용
                bool bRealObjHit = false;
                bool bPlaneHit = false;
                int sgSrcId = 0;
                int sgDstId = 0;
                SceneGraphType sgtype = SceneGraphType.None;
                SceneGraphAttr sgattr = SceneGraphAttr.On;
                if (voState == VirtualObjectManipulationState.Update && bObjectHit && currObject.transform.tag == "RO" && touchObject.transform.tag == "VO")
                {
                    //vo on real object
                    sgtype = SceneGraphType.Object;
                    bRealObjHit = true;
                }
                else
                {
                    sgtype = SceneGraphType.Plane;
                    bPlaneHit = true;
                    
                }//bRealObjecthit
                if (bRealObjHit)
                {
                    //가상 객체의 위치는 실제 객체의 오리진으로 설정함.
                    newPos = currObject.transform.position;
                    bSend = true;
                    keyword = "VO.MANIPULATE";
                    float angle;
                    var rot = currObject.transform.rotation;
                    rot.ToAngleAxis(out angle, out axis);
                    axis = angle * axis;
                    sendID = touchObject.GetComponentInParent<Content>().mnContentID;
                    sgSrcId = sendID;
                    sgDstId = currObject.GetComponent<RealObject>().mnObjID;
                    
                }
                if (bPlaneHit)
                {
                    var ray = mPlaneManager.CreateRay(touch.position, widthScale, heightCropped, invCamMat);//heightScale
                    float dist;
                    Plane p;
                    int pid;

                    //bool bRay = p.Raycast(ray, out dist);
                    bool bRay = mPlaneManager.FindNearestPlane(ray, out pid, out p, out dist);

                    //if (mTestParam.bShowLog)
                    //    mText.text = pid +" = "+p.ToString()+" = "+dist;

                    if (bRay)
                    {
                        float angle;
                        var rot = Quaternion.LookRotation(p.normal);
                        rot.ToAngleAxis(out angle, out axis);
                        axis = angle * axis;
                        newPos = ray.origin + ray.direction * dist;

                        if (voState == VirtualObjectManipulationState.Update)
                        {
                            bSend = true;
                            keyword = "VO.MANIPULATE";
                            sendID = touchObject.GetComponentInParent<Content>().mnContentID;
                            sgSrcId = sendID;
                            sgDstId = pid;
                            //if (mTestParam.bShowLog)
                            //    mText.text = logString[1];
                        }
                        if (voState == VirtualObjectManipulationState.Registration && phase == TouchPhase.Ended)
                        {
                            bSend = true;
                            keyword = "VO.CREATE";
                            sendID = touchID;
                            sgSrcId = sendID;
                            sgDstId = pid;
                        }

                    }//bray
                }
                
                if (bSend)
                {
                    if (voState == VirtualObjectManipulationState.Registration)
                        StartCoroutine(ChangeColor(new Vector4(1f, 0f, 0f, 0.3f)));
                    //byte[] bdata = new byte[fdata.Length * 4];
                    //Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수

                    //신그래프 : 가상, 특성, 구조물
                    //자기 자신 포함 : length+id+type +3xvector3+scale
                    byte[] bdata2 = ContentData.Generate(17f, sendID, (float)ContentType.Object, newPos.x, newPos.y, newPos.z, axis.x, axis.y, axis.z, mObjParam.objColor.r, mObjParam.objColor.g, mObjParam.objColor.b, mObjParam.fTempObjScale, (float)sgSrcId, (float)sgattr, (float)sgtype, (float)sgDstId);
                    UdpData mdata = new UdpData(keyword, mSystemManager.User.UserName, sendID, bdata2, 1.0);
                    StartCoroutine(mSender.SendData(mdata));

                    //byte[] bdata = ContentData.Generate(13f, sendID, (float)ContentType.Object, newPos.x, newPos.y, newPos.z, axis.x, axis.y, axis.z, mObjParam.objColor.r, mObjParam.objColor.g, mObjParam.objColor.b, mObjParam.fTempObjScale);
                    //GCHandle handle = GCHandle.Alloc(bdata, GCHandleType.Pinned);
                    //IntPtr addr = handle.AddrOfPinnedObject();
                    //UdpData idata = new UdpData(keyword, mSystemManager.User.UserName, sendID, addr, bdata.Length, 0f);
                    //StartCoroutine(mSender.SendDataWithNDK(idata));
                    //handle.Free();
                    if (mEvalManager.bProcess)
                    {
                        var timeSpan2 = DateTime.UtcNow - startTime;
                        string res = "";
                        if (mTestParam.bEdgeBase)
                        {
                            res = "base,Hosting," + keyword + ",";
                        }
                        else
                        {
                            res = "our,Hosting," + keyword + ",";
                        }
                        res += timeSpan2.TotalMilliseconds;
                        mEvalManager.mProcessTask.AddMessage(res);
                    }
                }
                if (phase == TouchPhase.Ended || bRealObjHit)
                {
                    touchObject = null;
                    voState = VirtualObjectManipulationState.None;
                }
            }
            catch(Exception ex)
            {
                touchObject = null;
                voState = VirtualObjectManipulationState.None;
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
