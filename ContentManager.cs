using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Path
{
    public int startID; //컨텐트 아이디를 기록
    public int endID;
    public GameObject pathObj;
    
    public Path(int sid, int eid)
    {
        //GameObject.Instantiate(AnchoredObjectPrefab, worldPos, worldRot)
        startID = sid;
        endID = eid;
    }
}

public class ContentA
{
    public int mnContentID;
    //public Vector3 position;
    public GameObject obj;
    public VirtualObject aobj;
    public Material material;
    public int nTTL;
    public bool visible;
    public PathManager pathManager;


    public ContentA(int id, int mid, Vector3 _pos, int _TTL)
    {
        mnContentID = id;
        //position = new Vector3(x, y, z);
        obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.position = _pos;
        nTTL = _TTL;
        visible = true;

        material = obj.GetComponent<Renderer>().material;
        aobj = obj.AddComponent<VirtualObject>();
        pathManager = obj.AddComponent<PathManager>();
        pathManager.contentID = id;
        
        //pathManager.gameObject.AddComponent<CapsuleCollider>();
        //obj.GetComponent<CapsuleCollider>().isTrigger = true;

        ////충돌을 감지하기 위해서는 최소 움직이는 객체에는 리지드 바디와 콜라이더가 추가되어야함.
        ////키네메틱이 꺼져있는 상태여야 함.

        var col = obj.GetComponent<SphereCollider>();
        col.isTrigger = false; //충돌시 반응하지 않도록 설정.

        if (mid > 0 && mid < 101)
        {
            obj.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            pathManager.currPathID = mid;
            obj.tag = "Path";
            col.isTrigger = true;
            material.color = Color.green;
        }
        else if (mid > 100) {
            obj.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            obj.tag = "VO";
            material.color = Color.black;
        }
        else
        {
            obj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            obj.tag = "VO";
            material.color = Color.blue;

            var body = obj.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        //float fAngle = rot.magnitude * Mathf.Rad2Deg;
        //Quaternion q = Quaternion.AngleAxis(fAngle, rot.normalized);
        
    }
}

public class ContentEventArgs : EventArgs
{
    public ContentEventArgs(ContentA c)
    {
        content = c;
    }
    public ContentA content { get; set; }
}

class ContentRegistrationEvent
{
    public static event EventHandler<ContentEventArgs> contentRegisted;
    public static void RunEvent(ContentEventArgs e)
    {
        if (contentRegisted != null)
        {
            contentRegisted(null, e);
        }
    }
}

public class ContentManager : MonoBehaviour
{
    public GameObject UVR;
    public GameObject pathPrefab;
    public Text mText;
    public Dictionary<int, ContentA> ContentDictionary;
    public Dictionary<int, int> MapContentMarker; //마커와 컨텐트 아이디 저장, 마커 아이디, 컨텐츠 아이디
    public Dictionary<int, Path> PathDictionary; //마커의 시작 아이디로 패스.

    public void UpdateContents(ref float[] tdata, Pose NewPose)
    {
        try
        {
            //int N = (int)tdata[0];
            //int dataIdx = 1;
            //for (int i = 0; i < N; i++)
            //{
            //    int id = (int)tdata[dataIdx++];
            //    int nid = (int)tdata[dataIdx++];
            //    int attr = (int)tdata[dataIdx++];
            //    bool battr = false;
            //    if (attr > 0)
            //        battr = true;
            //    Vector3 spos = new Vector3(tdata[dataIdx++], tdata[dataIdx++], tdata[dataIdx++]);
            //    Vector3 epos = new Vector3(tdata[dataIdx++], tdata[dataIdx++], tdata[dataIdx++]);

            //    string text = "\nContent = "+spos + " ";
            //    spos = NewPose.R * spos + NewPose.t;
            //    epos = NewPose.R * epos + NewPose.t;
            //    text = text + spos;
            //    mText.text = mText.text + text;

            //    if (ContentDictionary.ContainsKey(id))
            //    {
            //        //update
            //        ContentDictionary[id].nTTL += 5;
            //        ContentDictionary[id].position = spos;
            //        ContentDictionary[id].obj.transform.position = spos;
            //    }
            //    else
            //    {
            //        //생성
            //        var newContent = new Content(id, spos.x, spos.y, spos.z, 5);
            //        ContentDictionary.Add(id, newContent);
            //    }
            //}
            ////mText.text = "Content = " + N;
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }

    // Start is called before the first frame update
    void Awake()
    {
        ContentDictionary = new Dictionary<int, ContentA>();
        MapContentMarker = new Dictionary<int, int>();
        PathDictionary = new Dictionary<int, Path>();
        //enabled = false;

        //arSession = FindObjectOfType<ARSession>(); // todo cache or assign reference via Inspector
        //XRSessionSubsystem xrSessionSubsystem = arSession.subsystem;
        //if (xrSessionSubsystem != null)
        //{
        //    trackingState = xrSessionSubsystem.trackingState;
        //}
    }

    //ARSession arSession;
    //XRSessionSubsystem xrSessionSubsystem;
    //TrackingState trackingState;

    // Update is called once per frame
    void Update()
    {
        //arSession = FindObjectOfType<ARSession>(); // todo cache or assign reference via Inspector
        //xrSessionSubsystem = arSession.subsystem;
        //if (xrSessionSubsystem != null)
        //{
        //    trackingState = xrSessionSubsystem.trackingState;
        //    //if (trackingState == TrackingState.Limited)
        //    //    mText.text = "Tracking.Limited";
        //    //if (trackingState == TrackingState.Tracking) {
        //    //    mText.text = "Tracking.Tracking";
        //    //}
        //}

        foreach (int id in ContentDictionary.Keys)
        {

            ContentDictionary[id].nTTL--;
            if(ContentDictionary[id].nTTL <= 0)
            {
                ContentDictionary[id].obj.SetActive(false);
            }
        }
    }
    
    public bool CheckContent(int id)
    {
        return ContentDictionary.ContainsKey(id);
    }
    int ttl = 10;
    void RegistContent(int id, int mid, Vector3 pos)
    {
        var newContent = new ContentA(id, mid, pos, ttl);
        ContentDictionary.Add(id, newContent);
    }
    void UpdateContent(int id, Vector3 pos)
    {
        //딕셔너리에서 빼와서 수정해도 딕셔너리 안에 있는 요소와 연결되어 있음.
        var c = ContentDictionary[id];
        var pathManager = c.obj.GetComponent<PathManager>();
        c.nTTL = ttl;
        if(pathManager.mObjState == ObjectState.None)
            c.obj.transform.position = Vector3.Lerp(c.obj.transform.position, pos, Time.deltaTime);
        c.obj.SetActive(true);

        //mText.text = "update test = " + c.obj.transform.position.x + " " + ContentDictionary[id].obj.transform.position.x;
        //ContentDictionary[id] = c;
    }

    void UpdateContent2(int id, Vector3 pos)
    {
        //딕셔너리에서 빼와서 수정해도 딕셔너리 안에 있는 요소와 연결되어 있음.
        var c = ContentDictionary[id];
        var pathManager = c.obj.GetComponent<PathManager>();
        c.nTTL = ttl;
        c.obj.SetActive(true);
        if (pathManager.mObjState == ObjectState.None)
        {
            pos = UVR.transform.worldToLocalMatrix.MultiplyPoint(pos);
            pos.y *= -1f;
            ////필터링하면 자기 자신에게만 적용 됨. 이것을 다른 애들에게 적용할려면 선택된 것을 알려야 함.
            //Matrix4x4 obj = Matrix4x4.identity;
            //obj.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1.0f));
            //Matrix4x4 mat = Camera.main.transform.localToWorldMatrix * obj;
            //c.aobj.SetMatrix4x4(mat);
            //c.obj.transform.localRotation = c.aobj.transform.localRotation;
            //c.obj.transform.localPosition = c.aobj.transform.localPosition;

            pos = Camera.main.transform.localToWorldMatrix.MultiplyPoint3x4(pos);
            c.obj.transform.position = pos;
        }
        else if (pathManager.mObjState == ObjectState.Manipulation) {
            pos = UVR.transform.worldToLocalMatrix.MultiplyPoint(pos);
            pos.y *= -1f;
            pos = Camera.main.transform.localToWorldMatrix.MultiplyPoint3x4(pos);
            c.obj.transform.position = pos;
        }
        else if(pathManager.mObjState == ObjectState.OnPath)
        {
            c.obj.transform.position = pathManager.startObject.transform.position;
        }
        
        //mText.text = "update test = " + c.obj.transform.position.x + " " + ContentDictionary[id].obj.transform.position.x;
        //ContentDictionary[id] = c;
    }

    public void GeneratePath(int markerID)
    {
        var path = new Path(markerID, markerID+1);
        PathDictionary.Add(markerID, path);
    }

    public void Process(int contentid, int markerid, float x, float y, float z)
    {
        try
        {
            //if (trackingState != TrackingState.Tracking)
            //{
            //    return;
            //}

            //서버에서 전송한 3차원 포즈를 현재 시스템이 좌표계로 변환함.
            var pos = new Vector3(x, y, z);
            
            //pos = Camera.main.transform.localToWorldMatrix.MultiplyPoint(pos);

            ////가상 객체 등록 체크.
            ////마커인지 아닌지도 알려줌

            if (CheckContent(contentid))
            {
                UpdateContent2(contentid, pos);
            }
            else
            {
                DateTime s = DateTime.Now;
                RegistContent(contentid, markerid, pos);

                ////여기가 패스매니저의 스타트보다 빠르기 때문에 awake가 필요할 듯.
                if(markerid > 0 && markerid < 101)
                {
                    MapContentMarker[markerid] = contentid;
                    var currContent = ContentDictionary[contentid];
                    int prevMarkerId = markerid - 1;
                    int nextMarkerId = markerid + 1;
                    
                    if (CheckPath(prevMarkerId)) {
                        int prevContentID = MapContentMarker[prevMarkerId];
                        var prevContent = ContentDictionary[prevContentID];

                        prevContent.material.color = Color.cyan;
                        currContent.material.color = Color.red;

                        //prevContent.pathManager.mbPath = true;
                        prevContent.pathManager.CreatePath(currContent.obj);

                    }
                    if (CheckPath(nextMarkerId)) {
                        int nextContentID = MapContentMarker[nextMarkerId];
                        var nextContent = ContentDictionary[nextContentID];

                        currContent.material.color = Color.black;
                        nextContent.material.color = Color.yellow;

                        //currContent.pathManager.mbPath = true;
                        currContent.pathManager.CreatePath(nextContent.obj);

                    }
                    
                }
                var timeSpan = DateTime.Now - s;
                double ts = timeSpan.TotalMilliseconds;
                //mText.text = "Content generation time = " + ts;
            }
            
            //mText.text = "marker cotent = " + markerid + " " + id;
            //ContentDictionary[contentid].position = new Vector3(x, y, z);
            ContentRegistrationEvent.RunEvent(new ContentEventArgs(ContentDictionary[contentid]));
        }
        catch(Exception e)
        {
            mText.text = "Content Process " + e.ToString();
        }
    }

    public void Move(int id)
    {
        if (ContentDictionary.ContainsKey(id))
        {
            var content = ContentDictionary[id];
            content.pathManager.MoveStart();
            
        }
    }

    bool CheckPath(int mid)
    {
        return MapContentMarker.ContainsKey(mid);
    }
}
