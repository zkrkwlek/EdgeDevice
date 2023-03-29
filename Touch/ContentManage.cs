using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Content
{
    public int mnContentID;
    public GameObject obj;
    public Vector3 s, e;
    public int nTTL;
    public bool visible;
    public Content(int id, GameObject model, float fScale, Vector3 _s, Vector3 _e, int _TTL, Text mtext) {

        //var obj2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //obj2.transform.position = _s;// + new Vector3(0f,0.7f,0f);
        //obj2.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
        //obj2.transform.parent = obj.transform;
        //obj2.name = "walking"; 
        //obj2.GetComponent<Renderer>().material.color = new Color(0f, 0f, 1f, 0.3f);
        ////obj2.GetComponent<Renderer>().material.shader = 
        s = _s;
        e = _e;
        try {
            mnContentID = id;
            obj = GameObject.Instantiate(model, _s, Quaternion.identity);
            obj.transform.localScale = new Vector3(fScale, fScale, fScale);
            obj.transform.tag = "Path";

            //mtext.text = "new contetnt";
            var objPath = obj.AddComponent<ObjectPath>();
            objPath.Init(id, _s, _e, mtext);

            //var col = obj.AddComponent<MeshCollider>();
            //col.isTrigger = true;
        }
        catch (Exception e)
        {
            mtext.text = e.ToString();
        }
        
        
        nTTL = _TTL;
        visible = true;
    }

    public Content(int id, GameObject model, float fScale, Vector3 _s, int _TTL, Text mtext)
    {
        s = _s;
        try
        {
            mnContentID = id;
            obj = GameObject.Instantiate(model, _s, Quaternion.identity);
            obj.transform.localScale = new Vector3(fScale, fScale, fScale);
            obj.transform.tag = "VO";
            var objPath = obj.AddComponent<ARObject>();
            objPath.Init(id);
        }
        catch (Exception e)
        {
            mtext.text = e.ToString();
        }

        nTTL = _TTL;
        visible = true;
    }

}

public class ContentManage : MonoBehaviour
{
    public Text mText;
    public GameObject pathObjPrefab;
    public GameObject tempObjPrefab;
    public Dictionary<int, Content> ContentDictionary;
    public ParameterManager mParamManager;
    ObjectParam mObjParam;
    
    // Start is called before the first frame update
    public bool CheckContent(int id)
    {
        return ContentDictionary.ContainsKey(id);
    }
    int ttl = 10;
    void RegistContent(int id, Vector3 s, Vector3 e, GameObject model)
    {
        try {
            var newContent = new Content(id, model, mObjParam.fWalkingObjScale, s, e, ttl,mText);
            ContentDictionary.Add(id, newContent);
            //mText.text = "reg success";
        }
        catch(Exception ex)
        {
            mText.text = "reg error " + ex.ToString();
        }
    }
    void RegistContent(int id, Vector3 s, GameObject model)
    {
        try
        {
            var newContent = new Content(id, model, mObjParam.fTempObjScale, s, ttl, mText);
            ContentDictionary.Add(id, newContent);
            //mText.text = "reg success";
        }
        catch (Exception ex)
        {
            mText.text = "reg error " + ex.ToString();
        }
    }


    void Awake()
    {
        ContentDictionary = new Dictionary<int, Content>();
        mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];
    }
    void Start()
    {
        
    }
    public void Move(int id)
    {
        if (ContentDictionary.ContainsKey(id))
        {
            var content = ContentDictionary[id];
            content.obj.GetComponent<ObjectPath>().MoveStart();
            //mText.text = "move ~" + content.obj.transform.position.ToString();
        }
    }

    void UpdateContent(int id, Vector3 pos)
    {
        UpdateContent(id);
        var c = ContentDictionary[id];
        //c.obj.transform.position = Vector3.Lerp(c.obj.transform.position, pos, Time.deltaTime);
        c.obj.transform.position = pos;
    }
    void UpdateContent(int id)
    {
        var c = ContentDictionary[id];
        c.nTTL = ttl;
        c.obj.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        foreach (int id in ContentDictionary.Keys)
        {

            ContentDictionary[id].nTTL--;
            if (ContentDictionary[id].nTTL <= 0)
            {
                ContentDictionary[id].obj.SetActive(false);
            }
        }
    }
    public void PathProcess(int contentid, float sx, float sy, float sz, float ex, float ey, float ez)
    {
        try {
            //mText.text = "??? " + contentid;
            //일단 이렇게 해놓기
            Vector3 s = new Vector3(sx, -sy, sz);
            Vector3 e = new Vector3(ex, -ey, ez);
            if (CheckContent(contentid))
            {
                //갱신
                UpdateContent(contentid);
            }
            else
            {
                //추가
                RegistContent(contentid, s, e, pathObjPrefab);
            }
        }
        catch(Exception exc)
        {
            mText.text = exc.ToString();
        }
        
    }
    public void Process(int cid, float x, float y, float z)
    {
        try {
            Vector3 s = new Vector3(x, y, z);
            if (CheckContent(cid))
            {
                UpdateContent(cid, s);
            }
            else
            {
                RegistContent(cid, s, tempObjPrefab);
            }
        }catch(Exception e)
        {
            mText.text = e.ToString();
        }
    }
}
