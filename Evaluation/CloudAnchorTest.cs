using Google.XR.ARCoreExtensions;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class AnchorListenArgs : EventArgs
{
    public AnchorListenArgs(int _id, string _str)
    {
        id = _id;
        strid = _str;
    }
    public int id { get; set; }
    public string strid{ get; set; }
}

class AnchorListenEvent
{
    public static event EventHandler<AnchorListenArgs> listenEvent;
    public static void RunEvent(AnchorListenArgs e)
    {
        if (listenEvent != null)
        {
            listenEvent(null, e);
        }
    }
}


/// <summary>
/// 마커로 3차원 객체 위치 등록
/// ARCore를 이용하거나
/// 내꺼 서버를 이용하거나
/// </summary>

public class CloudAnchorTest : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataSender mSender;
    public ParameterManager mParamManager;
    public ARRaycastManager mRaycastManager;
    public PoseManager mPoseManager;

    public Text mText;
    ExperimentParam mExperimentParam;
    TrackerParam mTrackerParam;
    ObjectParam mObjParam;
    float scale;

    bool mbCamInit = false;
    Mat camMatrix;
    Mat invCamMatrix;

    public enum Mode { READY, HOST, HOST_PROGRESS, HOST_PENDING, RESOLVE, RESOLVE_PENDING };
    public Mode mode = Mode.READY;
    
    ArucoMarker marker = null;
    AnchorListenArgs anchorInfo = null;

    public ARAnchorManager anchorManager;
    ARAnchor localAnchor;
    private ARCloudAnchor cloudAnchor;
    public GameObject prefabObj;
    

    [HideInInspector]
    public Dictionary<int, string> mResolvedAnchors;
    [HideInInspector]
    public Dictionary<int, GameObject> mAnchorObjects;

    string filePath;
    StreamWriter writer_spatial;

    bool WantsToQuit()
    {
        //파라메터 저장
        //File.WriteAllText(filename, JsonUtility.ToJson(param));
        mResolvedAnchors.Clear();
        mAnchorObjects.Clear();
        writer_spatial.Close();
        return true;
    }

    bool isAttached(int id)
    {
        return mResolvedAnchors.ContainsKey(id);
    }

    bool isResolved(int id)
    {
        return mAnchorObjects.ContainsKey(id);
    }

    void AddMarkerID(int id, string strid)
    {
        mResolvedAnchors.Add(id, strid);
    }

    void AttachObject(int id, Transform trans)
    {
        try {
            var obj = Instantiate(prefabObj, trans);
            obj.GetComponent<Renderer>().material.color = Color.blue;
            obj.transform.localScale = new Vector3(scale, scale, scale);
            mAnchorObjects.Add(id, obj);
        }
        catch(Exception e)
        {
            mText.text = "AttachObject:Err="+e.ToString();
        }
    }

    void Awake()
    {
        ////파라메터 로드
        //dirPath = Application.persistentDataPath + "/data/Param";
        //filename = dirPath + "/CloudAnchorTest.json";
        //try
        //{
        //    string strAddData = File.ReadAllText(filename);
        //    param = JsonUtility.FromJson<AnchorParam>(strAddData);
        //    //mText.text = "success load " + load_scripts;
        //}
        //catch (Exception e)
        //{
        //    param = new AnchorParam();
        //    param.bHost = true;
        //}
        ////파라메터 로드

        mExperimentParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];

        if (mTrackerParam.bTracking || !mExperimentParam.bRegistrationTest)
        {
            enabled = false;
            return;
        }

        ////플레인 매니저, 레이캐스트 매니저, 디폴트 평면 켜기
        try {
            var mSession = GameObject.Find("AR Session Origin");
            if (mSession)
            {
                string scriptName1 = "ARRaycastManager";
                string scriptName2 = "ARPlaneManager";
                string scriptName = "ARAnchorManager";
                (mSession.GetComponent(scriptName) as MonoBehaviour).enabled = true;
                (mSession.GetComponent(scriptName1) as MonoBehaviour).enabled = true;
                (mSession.GetComponent(scriptName2) as MonoBehaviour).enabled = true;
            }
            //var mDefaultPlane = GameObject.Find("AR Default Plane");
            //if (mDefaultPlane)
            //    mDefaultPlane.SetActive(true);
            //else
            //{
            //    mText.text = "find error plane";
            //}
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }

        //플레인 오브젝트 enable
        //레이캐스트 매니저, 플레인 매니저 enable

        if (mExperimentParam.bHost && !mTrackerParam.bTracking)
        {
            mode = Mode.HOST;
        }
        mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];
        scale = mObjParam.fTempObjScale;
        mResolvedAnchors = new Dictionary<int, string>();
        mAnchorObjects = new Dictionary<int, GameObject>();
        Application.wantsToQuit += WantsToQuit;

        ////csv 파일 생성
        string dirPath = Application.persistentDataPath + "/data";
        filePath = dirPath + "/error_spatial_google.csv";
        writer_spatial = new StreamWriter(filePath, true);

    }

    void OnEnable()
    {
        if (!mTrackerParam.bTracking)
        {
            MarkerDetectEvent.markerDetected += OnMarkerInteraction;
            CameraInitEvent.camInitialized += OnCameraInitialization;
            AnchorListenEvent.listenEvent += OnListenAnchor;
        }
    }
    void OnDisable()
    {
        MarkerDetectEvent.markerDetected -= OnMarkerInteraction;
        CameraInitEvent.camInitialized -= OnCameraInitialization;
        AnchorListenEvent.listenEvent -= OnListenAnchor;
    }

    void OnCameraInitialization(object sender, CameraInitEventArgs e)
    {
        mbCamInit = true;
        camMatrix = e.camMat;
        invCamMatrix = e.invCamMat;
        //카메라 정보 받기
        //Kinv = new Mat(3, 3, CvType.CV_64FC1);
        //mCamManager.invCamMatrix.copyTo(Kinv);
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }
    public Pose GetCameraPose()
    {
        return new Pose(Camera.main.transform.position, Camera.main.transform.rotation);
    }

    int mid = 0;
    

    // Update is called once per frame
    void Update()
    {
        if (mExperimentParam.bHost)
        {
            if (mode == Mode.HOST)
            {
                HostProcessing();
            }
            if(mode == Mode.HOST_PROGRESS)
            {
                HostProgressing();
            }
            if (mode == Mode.HOST_PENDING)
            {
                HostPending();
            }
        }
        if (!mExperimentParam.bHost)
        {
            //리졸빙은 다른 곳에서 함
            if(mode == Mode.RESOLVE_PENDING)
            {
                ResolvePending();
            }
        }
    }

    void OnListenAnchor(object sender, AnchorListenArgs args)
    {
        if (!mExperimentParam.bHost && mode == Mode.READY)
        {
            int id = args.id;
            if (isAttached(id))
                return;
            AddMarkerID(args.id, args.strid);
            //리졸빙으로 넘어가는 것은 이걸 이미 받아온적이 있는지 체크한 후
            //딕셔너리로 관리하기
            try {
                cloudAnchor = anchorManager.ResolveCloudAnchorId(args.strid);
                if(cloudAnchor != null)
                {
                    anchorInfo = args;
                    mode = Mode.RESOLVE_PENDING;
                }
            }
            catch(Exception e)
            {
                mText.text = "listen anchor error = "+e.ToString();
            }
        }
    }
    bool bMarkerDetected = false;
    void OnMarkerInteraction(object sender, MarkerDetectEventArgs me)
    {
        try
        {
            marker = me.marker;
            if (mExperimentParam.bHost)
            {
                bMarkerDetected = true;
            }

            int id = me.marker.id;
            //if (!me.marker.mbCreate && param.bHost && mode == Mode.READY)
            //{

            //    mode = Mode.HOST;
            //    marker.mbCreate = true;
                
                
            //    //var obj2 = Instantiate(prefabObj, trans.localPosition, trans.localRotation);

            //    //obj1.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            //    //obj2.GetComponent<Renderer>().material.color = Color.green;
            //    //obj1.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

            //    //마커 생성안했으면 해야 함.
            //    //이전 실행시 마커 생성한게 있으면 생성안해야 함.
            //    //mText.text = "Create Marker~~";
            //}

            //호스트 일 때

            if (!mExperimentParam.bHost)
            {
                //앵커와 거리를 계산.
                if (!isResolved(id))
                {
                    //if (param.bShowLog)
                    //{
                    //    if(mode == H)
                    //    mText.text = "marker " + id + " is not resolved";
                    //}
                    return;
                }
                int fid = me.marker.frameId;
                if (mPoseManager.CheckPose(fid))
                {
                    var trans = mPoseManager.GetPose(fid);
                    var anchorObj = mAnchorObjects[id];
                    float azi = 0f;
                    float ele = 0f;
                    float dist = 0f;
                    Vector2 temp = Vector2.zero;
                    marker.CalculateAziAndEleAndDist(trans.position, out azi, out ele, out dist);
                    float err = marker.Calculate(trans.worldToLocalMatrix, camMatrix, anchorObj.transform.position, marker.corners[0], out temp, false);
                    //if (err < 1000f)
                    writer_spatial.WriteLine(dist + "," + azi + "," + ele + "," + err);
                }
               
            }
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }

    void HostProcessing()
    {
        FeatureMapQuality quality = anchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());
        if (quality == FeatureMapQuality.Sufficient || quality == FeatureMapQuality.Good)
        {
            //Pose pose = new Pose(marker.gameobject.transform.position, marker.gameobject.transform.rotation);
            //localAnchor = anchorManager.AddAnchor(pose);
            //cloudAnchor = anchorManager.HostCloudAnchor(localAnchor, 1);
            
            //if (cloudAnchor != null)
            //{
            //    mode = Mode.HOST_PENDING;
            //}
            mode = Mode.HOST_PROGRESS;

            if (mExperimentParam.bShowLog)
                mText.text = "FeaturMapQuality is " + quality;
        }
        else {
            if (mExperimentParam.bShowLog)
            {
                mText.text = "FeaturMapQuality is " + quality;
            }
                
        }
    }

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void HostProgressing2()
    {
        try
        {
            ////터치 인식하고 마커 인식 해야 함.
            if (Input.touchCount == 0)
                return;

            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began)
                return;
            Vector2 pos = marker.corners[0];
            if (mRaycastManager.Raycast(pos, hits, TrackableType.PlaneWithinPolygon))
            {
                //Pose pose = new Pose(marker.gameobject.transform.position, marker.gameobject.transform.rotation);
                localAnchor = anchorManager.AddAnchor(hits[0].pose);
                cloudAnchor = anchorManager.HostCloudAnchor(localAnchor, 1);
                mid++;

                //var trans = localAnchor.transform;//marker.gameobject.transform;
                //var obj1 = Instantiate(prefabObj, trans.position, trans.rotation);
                //obj1.GetComponent<Renderer>().material.color = Color.red;
            }

            if (cloudAnchor != null)
            {
                mode = Mode.HOST_PENDING;
            }
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }

    }

    int count = 0;
    private void Reset()
    {
        cloudAnchor = null;
        localAnchor = null;
        //mode = Mode.READY;
        mode = Mode.HOST_PROGRESS;
        count = 0;
    }
    void HostProgressing()
    {
        try {
            ////터치 인식하고 마커 인식 해야 함.
            if (Input.touchCount == 0)
                return;
            if (!bMarkerDetected)
                return;

            Pose pose = new Pose(marker.gameobject.transform.position, marker.gameobject.transform.rotation);
            localAnchor = anchorManager.AddAnchor(pose);
            cloudAnchor = anchorManager.HostCloudAnchor(localAnchor, 1);

            mid = marker.id;

            if (cloudAnchor != null)
            {
                mode = Mode.HOST_PENDING;
            }
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }

    }
    void HostPending()
    {
        try {
            
            if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
            {
                //여기서 데이터 전송            
                string strAnchorID = cloudAnchor.cloudAnchorId;
                byte[] bdata = Encoding.UTF8.GetBytes(strAnchorID);
                UdpData mdata = new UdpData("SetCloudAnchor", mSystemManager.User.UserName, mid, bdata, 1.0);
                StartCoroutine(mSender.SendData(mdata));

                var trans = localAnchor.transform;//marker.gameobject.transform;
                var obj1 = Instantiate(prefabObj, trans.position, trans.rotation);
                obj1.GetComponent<Renderer>().material.color = Color.red;
                obj1.transform.localScale = new Vector3(scale, scale, scale);
                //Reset();

                if (mExperimentParam.bShowLog)
                    mText.text = "Success create cloud anchor = " + mid + ", " + strAnchorID;

                //테스트용
                //PlayerPrefs.SetString(""+marker.id, strAnchorID);
            }
            else
            {
                count++;
                if (mExperimentParam.bShowLog)
                    mText.text = $"Host Task in progress ...{cloudAnchor.cloudAnchorState}" + "=="+count;
                if (count < 400)
                    return;
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase != TouchPhase.Began)
                        return;
                    if (mExperimentParam.bShowLog)
                        mText.text = "Reset";
                    Reset();
                }
            }
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }

    void ResolvePending()
    {
        if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
        {
            try {
                AttachObject(anchorInfo.id, cloudAnchor.transform);
                mode = Mode.READY;
                if (mExperimentParam.bShowLog)
                    mText.text = "리졸브 성공 = " + anchorInfo.id+" = "+ Camera.main.transform.position + "==" + cloudAnchor.transform.position;

                anchorInfo = null;
                //cloudAnchor = null;
            }
            catch(Exception e)
            {
                mText.text = e.ToString();
            }
            
        }
        else
        {
            if (mExperimentParam.bShowLog)
                mText.text = $"Resolving Task in progress ...{cloudAnchor.cloudAnchorState}";
        }
    }
    //키프레임으로부터 데이터를 받음.
    //클라우드 앵커 데이터는 여기서 관리해야 함.
    //일단 처음에 접속하면 
}
