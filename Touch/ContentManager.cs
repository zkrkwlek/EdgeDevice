using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointContent : Content
{
    public LineRenderer lineRenderer;

    public PointContent(int id, int type, Vector3 spos, Vector3 epos, float r, float g, float b, float a, float size)// : base(id, type, spos)
    {
        //obj = new GameObject();
        //obj.transform.position = spos;
        //lineRenderer = obj.AddComponent<LineRenderer>();
        //lineRenderer.material.color = new Color(r,g,b,a);
        //lineRenderer.startWidth = size;
        //lineRenderer.endWidth = size;
        //lineRenderer.SetPosition(0, spos);
        //lineRenderer.SetPosition(1, epos);
    }
    public PointContent(int id, int type, Vector3 spos, Vector3 epos, Vector3 normal, float r, float g, float b, float a, float size)// : base(id, type, spos)
    {
        //obj = new GameObject();
        //obj.transform.position = spos;
        //var q = Quaternion.LookRotation(normal);
        //obj.transform.rotation = q;
        //lineRenderer = obj.AddComponent<LineRenderer>();
        //lineRenderer.material.color = new Color(r, g, b, a);
        //lineRenderer.startWidth = size;
        //lineRenderer.endWidth = size;
        //lineRenderer.SetPosition(0, spos);
        //lineRenderer.SetPosition(1, epos);
        //lineRenderer.alignment = LineAlignment.TransformZ;
    }
}

public class PathContent: Content
{
    public PathContent(int id, int type, GameObject model, Vector3 _s, Vector3 _e, float fScale, Text mtext)// : base(id, type, _s)
    {
        e = _e;
        try
        {
            //obj = GameObject.Instantiate(model, _s, Quaternion.identity);
            //obj.transform.localScale = new Vector3(fScale, fScale, fScale);
            //obj.transform.tag = "Path";

            ////mtext.text = "new contetnt";
            //var objPath = obj.AddComponent<ObjectPath>();
            //objPath.Init(id, _s, _e, mtext);

            ////var col = obj.AddComponent<MeshCollider>();
            ////col.isTrigger = true;
        }
        catch (Exception e)
        {
            mtext.text = e.ToString();
        }
    }
}

public class Content : MonoBehaviour
{
    public int mnContentID;
    //public GameObject obj;
    public Vector3 s, e;
    public int type;
    public int nObservation;
    public bool visible;
    public SceneGraphNode node;
    public Content() { }
    public void Init(int id, int _type, Vector3 _s)
    {
        mnContentID = id;
        type = _type;
        s = _s;
        nObservation = 0;
        visible = true;
    }
    
    public void Init(int id, int _type, GameObject model, Vector3 _s, float fScale, Color c, Text mtext)
    {
        Init(id, _type, _s);
        
        //try
        //{
        //    obj = GameObject.Instantiate(model, _s, Quaternion.identity);
        //    obj.transform.localScale = new Vector3(fScale, fScale, fScale);
        //    obj.transform.tag = "VO";
        //    var objPath = obj.AddComponent<ARObject>();
        //    obj.GetComponent<Renderer>().material.color = c;
        //    objPath.Init(id);
        //}
        //catch (Exception e)
        //{
        //    mtext.text = e.ToString();
        //}
    }

}

public enum ContentType
{
    Object=0,Path,Draw
}

//float으로 객체 생성하고 보낼 수 있또록
public class ContentData
{
    static public byte[] Generate(params float[] fdata)
    {
        byte[] bdata = new byte[fdata.Length * 4];
        Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length);
        //byte[] bdata = new byte[5000];
        //Buffer.BlockCopy(fdata, 0, bdata, 0, fdata.Length*4);
        return bdata;
    }
    static public float[] GenerateFloatArray(params float[] fdata)
    {
        return fdata;
    }
}

public class ContentManager
{

    public Dictionary<int, GameObject> ContentDictionary;
    public ObjectManager mObjManager;
    public GameObject pathObjPrefab;
    public GameObject tempObjPrefab;
    EvaluationManager mEvalManager;

    public ContentManager()
    {
        ContentDictionary = new Dictionary<int, GameObject>();
    }
    public ContentManager(EvaluationManager _evalManager,GameObject _obj, GameObject _path):this()
    {
        mEvalManager = _evalManager;
        tempObjPrefab = _obj;
        pathObjPrefab = _path;
    }
    public bool CheckContent(int id)
    {
        return ContentDictionary.ContainsKey(id);
    }
    public void RegistContent(int id, GameObject c)
    {
        ContentDictionary.Add(id, c);
    }

    void UpdateContent(int id, SceneGraphNode node, Vector3 pos)
    {
        var c = ContentDictionary[id];
        c.SetActive(true);
        var content =  c.GetComponent<Content>();
        if (!node.isEqual(content.node))
        {
            //갱신
            if(node.type == SceneGraphType.Object)
            {
                node.dstObj.GetComponent<RealObject>().ConnectedObjs.Add(c);
                pos = node.dstObj.transform.position;
            }
            if(content.node.type == SceneGraphType.Object && node.type == SceneGraphType.Plane)
            {
                content.node.dstObj.GetComponent<RealObject>().ConnectedObjs.Remove(c);
            }
            content.node = node;
        }

        c.transform.position = pos;
    }
    void UpdateContent(int id, Vector3 pos)
    {
        var c = ContentDictionary[id];
        c.SetActive(true);
        c.transform.position = pos;
    }
    void UpdateContent(int id)
    {
        var c = ContentDictionary[id];
        c.SetActive(true);
    }

    public GameObject Process(SceneGraphNode node, int cid, int type, GameObject prefab, Color c, Vector3 pos, float s, DateTime startTime, bool bBase, Text mText)
    {
        try
        {
            string method;
            if (CheckContent(cid))
            {
                UpdateContent(cid, node, pos);
                method = "VO.MANIPULATE,";
            }
            else
            {
                var obj = GameObject.Instantiate(prefab, pos, Quaternion.identity);
                obj.transform.localScale = new Vector3(s, s, s);
                obj.transform.tag = "VO";
                obj.GetComponent<Renderer>().material.color = c;
                var content = obj.AddComponent<Content>();
                content.mnContentID = cid;
                content.node = node;
                //var objPath = obj.AddComponent<ARObject>();
                //objPath.Init(id);

                //var newContent = new Content(cid, type, prefab, pos, s, c, mText);
                RegistContent(cid, obj);
                method = "VO.CREATE,";
            }
            if (mEvalManager.bProcess)
            {
                var timeSpan = DateTime.UtcNow - startTime;
                string edge = "our,resolving,";
                if (bBase)
                    edge = "base,resolving,";
                string res = edge + method + timeSpan.TotalMilliseconds;
                mEvalManager.mProcessTask.AddMessage(res);
            }
            return ContentDictionary[cid];
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
        return null;
    }

    public GameObject Process(int cid, int type, GameObject prefab, Color c, Vector3 pos, float s, DateTime startTime, bool bBase, Text mText)
    {
        try {
            string method;
            if (CheckContent(cid))
            {
                UpdateContent(cid, pos);
                method = "VO.MANIPULATE,";
            }
            else
            {
                var obj = GameObject.Instantiate(prefab, pos, Quaternion.identity);
                obj.transform.localScale = new Vector3(s,s,s);
                obj.transform.tag = "VO";
                obj.GetComponent<Renderer>().material.color = c;
                var content = obj.AddComponent<Content>();
                content.mnContentID = cid;
                //var objPath = obj.AddComponent<ARObject>();
                //objPath.Init(id);

                //var newContent = new Content(cid, type, prefab, pos, s, c, mText);
                RegistContent(cid, obj);
                method = "VO.CREATE,";
            }
            if (mEvalManager.bProcess)
            {
                var timeSpan = DateTime.UtcNow - startTime;
                string edge = "our,resolving,";
                if (bBase)
                    edge = "base,resolving,";
                string res = edge + method + timeSpan.TotalMilliseconds;
                mEvalManager.mProcessTask.AddMessage(res);
            }
            return ContentDictionary[cid];
        }
        catch(Exception e) {
            mText.text = e.ToString();
        }
        return null;   
    }

    public Content DrawProcess(int id, int type, Vector3 spos, Vector3 epos, Vector3 normal, Color c, float size, Text mText)
    {
        //try
        //{
        //    if (CheckContent(id))
        //    {
        //        UpdateContent(id);
        //    }
        //    else
        //    {
        //        var newContent = new PointContent(id, type, spos, epos, normal, c.r, c.g, c.b, c.a, size);
        //        RegistContent(id, newContent);
        //    }
        //}
        //catch (Exception e)
        //{
        //    mText.text = e.ToString();
        //}
        //return ContentDictionary[id];
        return null;
    }

    public Content PathProcess(int cid, int type, GameObject prefab, Vector3 sPos, Vector3 ePos, float s, Text mText)
    {
        //try {
            
        //    if (CheckContent(cid))
        //    {
        //        //갱신
        //        UpdateContent(cid);
        //    }
        //    else
        //    {
        //        //추가
        //        var newContent = new PathContent(cid, type, prefab, sPos, ePos, s, mText);
        //        RegistContent(cid, newContent);
        //    }
        //    return ContentDictionary[cid];
        //}
        //catch(Exception e)
        //{
        //    mText.text = e.ToString();
        //}
        return null;
    }

    public void Update()
    {
        foreach (int id in ContentDictionary.Keys)
        {
            var obj = ContentDictionary[id];
            var content = obj.GetComponent<Content>();
            if (content.nObservation <= 0)
            {
                obj.SetActive(false);
            }
        }
    }

    public void Move(int id)
    {
        if (ContentDictionary.ContainsKey(id))
        {
            var obj = ContentDictionary[id];
            obj.GetComponent<ObjectPath>().MoveStart();
        }
    }
}