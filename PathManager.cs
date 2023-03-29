using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ObjectState
{ None, Manipulation, Moving, OnPath} //moving은 애니메이션 트리거에 의해 움직이는 거, onpath는 경로 위에 있는 상태. update는 사용자가 조작하는 상태

public class PathManager : MonoBehaviour
{
    public int contentID;
    //public int startMarkerID;
    public int endContentID;
    public int currPathID;
    public bool mbPath; //패스가 생성되었는지, 해당 객체가 시작 객체가 되고, 끝나는 위치의 객체가 연결되어야 함.
    public ObjectState mObjState;
    //public bool mbMoving; //움직이는지
    //public bool mbOnPath; //패스 위에 있는지. 태그로 패스 확인.
    public ContentManager mContentManager;
    public DataSender mSender;
    public GameObject endPathObject; //이게 경로일 때 처음은 이 클래스를 생성한 오브젝트임. 따라서 마지막 오브젝트만 알면 됨.
    public string log;
    float speed;

    string userName;
    string[] logString = new string[4];

    /// <summary>
    /// 패스에 올랐을 때 설정되는 것.
    /// 자동으로 다음 패스가 활성화 된 패스이면 갱신되어야 함.
    /// 이것으로 패스의 시작과 끝을 설정하지는 않음.
    /// 왜냐하면, 패스의 경우 계속 변경이 가능학 때문ㅇ
    /// 가상 객체는 움직이지 않는동안에만 계속 위치를 잡아줌. 움직이는 동안에는 일단 놔둬보자.
    /// </summary>
    float startTime;
    public GameObject startObject;
    public GameObject endObject;
    LineRenderer lineRenderer;

    float journeyLength;

    [HideInInspector]
    Text mText;

    // Start is called before the first frame update
    void Awake()
    {
        speed = 0.5f;
        log = "";
        mObjState = ObjectState.None;
        mbPath = false;
        var UVR = GameObject.Find("UVR");
        mContentManager = UVR.GetComponent<ContentManager>();
        mSender = UVR.GetComponent<DataSender>();
        userName = UVR.GetComponent<SystemManager>().User.UserName;
        mText = GameObject.Find("StatusText").GetComponent<Text>();

        logString[0] = "NONE";
        logString[1] = "Manipulation";
        logString[2] = "Moving";
        logString[3] = "OnPath";
    }

    // Update is called once per frame
    void Update()
    {
        if (mbPath)
        {
            lineRenderer.SetPosition(0, gameObject.transform.position);
            lineRenderer.SetPosition(1, endPathObject.transform.position);
        }

        Move();

        //무브이면

        //충돌시 다음 경로 이동을 위해 브로드 캐스트

        //패스가 아니거나
        //패스여도 움직이지 않을 때는 현재 위치로.
        //패스인 경우는 다음 위치로.

        //엔드 객체 충돌시, 다음 패스로 넘어가야함. 다음 패스 없으면 패스 종료.
    }
    public bool CheckPath(bool flag)
    {
        return gameObject.tag == "Path" && mbPath == flag;
    }
    
    public bool isOnPath() {
        return gameObject.tag == "VO" && mObjState == ObjectState.OnPath;
    }

    public void CreatePath(GameObject _next)
    {
        mbPath = true;
        endPathObject = _next;
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material.color = new Color(0f, 1f, 1f, 0.2f);
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth   = 0.01f;
    }
    
    public void MoveStart()
    {
        //move를 받으면 시작
        mObjState = ObjectState.Moving;
        startTime = Time.time;
    }
    public void MoveEnd()
    {
        mObjState = ObjectState.None;
    }

    public void Move()
    {
        if (mObjState == ObjectState.Moving)
        {
            float distCovered = (Time.time - startTime) * speed;
            float fractionOfJourney = distCovered / journeyLength;
            transform.position = Vector3.Lerp(startObject.transform.position, endObject.transform.position, fractionOfJourney);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        var obj = other.gameObject;
        var path = obj.GetComponent<PathManager>();
        if (path.CheckPath(true))
        {
            mObjState = ObjectState.OnPath;
            //패스 설정.

            startObject = obj;
            endObject = path.endPathObject;

            journeyLength = Vector3.Distance(startObject.transform.position, endObject.transform.position);

            log = "on path object =" + contentID + " is on path " + path.currPathID + logString[(int)mObjState];
            //log = "OnTriggerEnter = col = " + other.gameObject.tag + ", this = " + gameObject.tag;
            
            transform.position = startObject.transform.position;
            gameObject.GetComponent<Renderer>().material.color = Color.magenta;
        }
        else if (path.CheckPath(false))
        {
            mObjState = ObjectState.None;
            gameObject.GetComponent<Renderer>().material.color = Color.blue;
            log = "on end path object =" + contentID + " is on path " + path.currPathID + "= dist = " + journeyLength;
        }
        else
            log = obj.tag + " " + path.contentID + "/ " + contentID;

        //log = "OnTriggerEnter2 = col = " + other.gameObject.tag + ", this = " + gameObject.tag;
        //mText.text = log;

        //{
        //    string keyword = "VO.SELECTION";
        //    float[] fdata = new float[5];
        //    fdata[0] = 0f;
        //    fdata[1] = 0f;
        //    fdata[2] = transform.position.x;
        //    fdata[3] = transform.position.y;
        //    fdata[4] = transform.position.z;
        //    byte[] bdata = new byte[fdata.Length * 4];
        //    Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
        //    UdpData mdata = new UdpData(keyword, userName, contentID, bdata, 1.0);
        //    StartCoroutine(mSender.SendData(mdata));
        //}

    }
    private void OnTriggerStay(Collider other)
    {
        var obj = other.gameObject;
        var path = obj.GetComponent<PathManager>();
        log = "OnTriggerStay " + contentID +", "+ logString[(int)mObjState] + " = " + obj.tag+" "+path.currPathID;
        //mText.text = log;
    }

    void OnTriggerExit(Collider other)
    {
        var obj = other.gameObject;
        var path = obj.GetComponent<PathManager>();
        //if (path.CheckPath(true))
        //{
        //    mObjState = ObjectState.OnPath;
        //    //패스 설정.

        //    startObject = obj;
        //    endObject = path.endPathObject;

        //    journeyLength = Vector3.Distance(startObject.transform.position, endObject.transform.position);

        //    log = "on path object =" + contentID + " is on path " + path.currPathID + "= dist = " + journeyLength;
        //    //log = "OnTriggerEnter = col = " + other.gameObject.tag + ", this = " + gameObject.tag;

        //    transform.position = startObject.transform.position;
        //    gameObject.GetComponent<Renderer>().material.color = Color.magenta;
        //}
        //else if (path.CheckPath(false))
        //{
        //    mObjState = ObjectState.None;
        //    gameObject.GetComponent<Renderer>().material.color = Color.blue;
        //    log = "on end path object =" + contentID + " is on path " + path.currPathID + "= dist = " + journeyLength;
        //}
        //else
        //    log = obj.tag + " " + path.contentID + "/ " + contentID;

        //충돌은 VO
        //other는 Path
        log = "OnTriggerExit " + contentID + ", " + logString[(int)mObjState]+" = "+other.gameObject.tag+" "+path.currPathID;
        //mText.text = log;
    }
    void OnCollisionEnter(Collision collision)
    {
        //var obj = collision.gameObject;
        //var path = obj.GetComponent<PathManager>(); 
        //if (path.CheckPath(true))
        //{
        //    mObjState = ObjectState.OnPath;
        //    //패스 설정.
        //    startObject = obj;
        //    endObject = path.endPathObject;

        //    journeyLength = Vector3.Distance(startObject.transform.position, endObject.transform.position);

        //    log = "object =" + contentID + " is on path " + path.currPathID + "= dist = "+journeyLength;

        //    transform.position = startObject.transform.position;
        //    gameObject.GetComponent<Renderer>().material.color = Color.magenta;
        //}else if (path.CheckPath(false))
        //{
        //    mObjState = ObjectState.None;
        //    gameObject.GetComponent<Renderer>().material.color = Color.blue;
        //}
        //else 
        //    log = obj.tag+" "+path.contentID + "/ "+contentID;

        //if(obj == endPathObject)
        //{
        //    log = "check end path objecct";
        //}

        //경로가 끝났을 때
        //1)다음 경로가 존재하는 경우
        //다음 경로 시작 요청.
        //경로 갱신
        //2) 다음 경로가 존재하지 않는 경우
        //움직임 멈춤.
        //패스 해제


        //내 알고리즘에선 현재 선택 된 객체가 어디에 충돌 했는지를 알려줌.
        //모든 객체는 생서이 패스매니져를 추가함.
        //패스에 오를려면 1) 충돌한 객체의 태그가 path이고, 2) 해당 객체의 패스가 생성되었어야 함.


        ////Check for a match with the specified name on any GameObject that collides with your GameObject
        //if (collision.gameObject.name == "MyGameObjectName")
        //{
        //    //If the GameObject's name matches the one you suggest, output this message in the console
        //    Debug.Log("Do something here");
        //}

        ////Check for a match with the specific tag on any GameObject that collides with your GameObject
        //if (collision.gameObject.tag == "MyGameObjectTag")
        //{
        //    //If the GameObject has the same tag as specified, output this message in the console
        //    Debug.Log("Do something else here");
        //}
        
    }

}
