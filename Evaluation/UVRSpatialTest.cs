using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UVRSpatialTest : MonoBehaviour
{
    public SystemManager mSystemManager;
    public DataSender mSender;
    public ParameterManager mParamManager;
    public PlaneManager mPlaneManager;
    public PoseManager mPoseManager;
    public Text mText;
    ExperimentParam mTestParam;
    TrackerParam mTrackerParam;
    ObjectParam mObjParam;
    public GameObject prefabObj;
    [HideInInspector]
    public Dictionary<int, GameObject> mAnchorObjects;

    //ī�޶� ����
    float scale;
    bool mbCamInit = false;
    Mat camMatrix;
    Mat invCamMatrix;

    StreamWriter writer_spatial;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AttachObject(int id, float[] fdata)
    {
        Vector3 pos = new Vector3(fdata[0], fdata[1], fdata[2]);
        Vector3 axis = new Vector3(fdata[3], fdata[4], fdata[5]);
        float angle = axis.magnitude;
        axis = axis.normalized;
        var rot = Quaternion.AngleAxis(angle,axis);
        var obj = Instantiate(prefabObj, pos,rot);
        obj.GetComponent<Renderer>().material.color = Color.blue;
        obj.transform.localScale = new Vector3(scale, scale, scale);
        mAnchorObjects.Add(id, obj);
    }

    void AttachObject(int id, Transform trans)
    {
        try
        {
            var obj = Instantiate(prefabObj, trans);
            if(mTestParam.bHost)
                obj.GetComponent<Renderer>().material.color = Color.red;
            else
                obj.GetComponent<Renderer>().material.color = Color.blue;
            obj.transform.localScale = new Vector3(scale, scale, scale);
            //obj.transform.localScale = new Vector3()
            mAnchorObjects.Add(id, obj);
        }
        catch (Exception e)
        {
            mText.text = "AttachObject:Err=" + e.ToString();
        }
    }
    bool isResolved(int id)
    {
        return mAnchorObjects.ContainsKey(id);
    }

    void Awake()
    {
        try
        {
            mTestParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
            mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
            
            if (!mTestParam.bRegistrationTest || !mTrackerParam.bTracking)
            {
                enabled = false;
                return;
            }
            mAnchorObjects = new Dictionary<int, GameObject>();
            mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];
            scale = mObjParam.fTempObjScale;

            Application.wantsToQuit += WantsToQuit;

            ////csv ���� ����
            string dirPath = Application.persistentDataPath + "/data";
            string filePath = "";
            if (mTestParam.bEdgeBase)
            {
                filePath = dirPath + "/err_uvr_base.csv";
            }
            else {
                int quality = mTrackerParam.nJpegQuality;
                int nSkip = mTrackerParam.nSkipFrames;
                filePath = dirPath + "/err_uvr_" + quality + "_" + nSkip + ".csv";
            }
            writer_spatial = new StreamWriter(filePath, true);

        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }

    bool WantsToQuit()
    {
        writer_spatial.Close();
        return true;
    }

    void OnEnable()
    {
        MarkerDetectEvent.markerDetected += OnMarkerInteraction;
        CameraInitEvent.camInitialized += OnCameraInitialization;
    }
    void OnDisable()
    {
        MarkerDetectEvent.markerDetected -= OnMarkerInteraction;
        CameraInitEvent.camInitialized -= OnCameraInitialization;
    }

    //�����׿��� ��Ŀ ���
    //ȣ���ÿ��� ��Ŀ ����

    ArucoMarker marker = null;
    bool bReqMarker = true;
    void OnMarkerInteraction(object sender, MarkerDetectEventArgs me)
    {
        try {
            marker = me.marker;
            int id = me.marker.id;
            //��ϵǾ�����

            //��� �� ��
            if (!isResolved(id))
            {
                //ȣ��Ʈ�̸鼭 ��Ŀ ��ü�� �����Ǿ��� �� ��Ͽ�û�� ���� ���ߴٸ�
                if (mTestParam.bHost && marker.mbCreate && bReqMarker)
                {
                    if (mTestParam.bShowLog)
                        mText.text = "Marker Object Registration";
                    AttachObject(id, marker.gameobject.transform);

                    var pos = marker.gameobject.transform.position;
                    var rot = marker.gameobject.transform.rotation;
                    float angle = 0f;
                    Vector3 axis = Vector3.zero;
                    rot.ToAngleAxis(out angle, out axis);
                    //axis = angle * Mathf.Deg2Rad * axis;
                    axis = angle * axis;
                    float[] fdata = new float[6];
                    fdata[0] = pos.x;
                    fdata[1] = pos.y;
                    fdata[2] = pos.z;
                    fdata[3] = axis.x;
                    fdata[4] = axis.y;
                    fdata[5] = axis.z;
                    byte[] bdata = new byte[fdata.Length * 4];
                    Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //��ü �Ǽ��� ������ ��
                    UdpData mdata = new UdpData("VO.MARKER.CREATE", mSystemManager.User.UserName, id, bdata, 1.0);
                    StartCoroutine(mSender.SendData(mdata));
                    bReqMarker = false;
                }
                //ȣ��Ʈ�� �ƴϸ鼭 ���� ��Ŀ ��û�� ������ ��
                if (!mTestParam.bHost && bReqMarker)
                {
                    float[] fdata = new float[6];
                    byte[] bdata = new byte[fdata.Length * 4];
                    Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //��ü �Ǽ��� ������ ��
                    UdpData mdata = new UdpData("VO.MARKER.CREATE", mSystemManager.User.UserName, id, bdata, 1.0);
                    StartCoroutine(mSender.SendData(mdata));
                    bReqMarker = false;
                }
            }
            else {
                int fid = me.marker.frameId;

                if (mPoseManager.CheckPose(fid))
                {
                    bool bTrackRes;
                    var trans = mPoseManager.GetPose(fid, out bTrackRes);
                    var anchorObj = mAnchorObjects[id];
                    float azi = 0f;
                    float ele = 0f;
                    float dist = 0f;
                    Vector2 temp = Vector2.zero;
                    marker.CalculateAziAndEleAndDist(trans.position, out azi, out ele, out dist);
                    float err = marker.Calculate(trans.worldToLocalMatrix, camMatrix, anchorObj.transform.position, marker.corners[0], out temp, false);
                    //if (err < 1000f)
                    writer_spatial.WriteLine(marker.frameId+","+dist + "," + azi + "," + ele + "," + err + ","+ bTrackRes);
                    if (mTestParam.bShowLog)
                    {
                        mText.text = temp.ToString() + marker.corners[0].ToString() + " == " + err;
                    }
                }
                else
                {
                    mText.text = fid+"??????????????pose error";
                }
            }
        }
        catch(Exception e)
        {
            if(mTestParam.bShowLog)
                mText.text = e.ToString();
        }
    }
    void OnCameraInitialization(object sender, CameraInitEventArgs e)
    {
        mbCamInit = true;
        camMatrix = e.camMat;
        invCamMatrix = e.invCamMat;
    }
}
