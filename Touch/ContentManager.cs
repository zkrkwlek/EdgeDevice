using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointContent : Content
{
    public ParticleSystem.Particle particle;
    public PointContent(int id, int type, Vector3 pos) : base(id, type, pos)
    {
        this.particle.startColor = Color.black;
        this.particle.startSize = 0.04f;
        this.particle.remainingLifetime = 50f;
        this.particle.position = pos;
        obj = null;
    }
    public PointContent(int id, int type, Vector3 pos,float r, float g, float b, float a, float size) : base(id, type, pos)
    {
        this.particle.startColor = new Color(r, g, b, a);
        this.particle.startSize = size;
        this.particle.remainingLifetime = 10f;
        this.particle.position = pos;
        obj = null;
    }
}

public class PathContent: Content
{
    public PathContent(int id, int type, GameObject model, Vector3 _s, Vector3 _e, float fScale, Text mtext) : base(id, type, _s) {
        e = _e;
        try
        {

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
    }
}

public class Content
{
    public int mnContentID;
    public GameObject obj;
    public Vector3 s, e;
    public int type;
    public int nObservation;
    public bool visible;
    public Content() { }
    public Content(int id, int _type, Vector3 _s)
    {
        mnContentID = id;
        type = _type;
        s = _s;
        nObservation = 0;
        visible = true;
    }
    
    public Content(int id, int _type, GameObject model, Vector3 _s, float fScale, Color c, Text mtext) : this(id, _type, _s)
    {
        try
        {
            obj = GameObject.Instantiate(model, _s, Quaternion.identity);
            obj.transform.localScale = new Vector3(fScale, fScale, fScale);
            obj.transform.tag = "VO";
            var objPath = obj.AddComponent<ARObject>();
            obj.GetComponent<Renderer>().material.color = c;
            objPath.Init(id);
        }
        catch (Exception e)
        {
            mtext.text = e.ToString();
        }
    }

}

public class DrawContentManager : ContentManager
{
    //��ƼŬ �ý��� �߰��ϱ�
    ParticleSystem.Particle[] m_Particles;
    public DrawPaintParam mDrawParam; //���� ����� �޴� �׸� �Ӽ���. ���� ���� ��ο� �׽�Ʈ������.
    public DrawContentManager():base()
    {

    }
    void UpdateContent(int id)
    {

    }
    public Content Process(int id, int type, float x, float y, float z, Text mText)
    {
        try
        {
            Vector3 pos = new Vector3(x, y, z);
            if (CheckContent(id))
            {
                UpdateContent(id);
            }
            else
            {
                var newContent = new PointContent(id, type, pos,mDrawParam.r, mDrawParam.g, mDrawParam.b, mDrawParam.a, mDrawParam.size);
                RegistContent(id, newContent);
            }
            //mText.text = "draw = " + ContentDictionary.Count;
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        return ContentDictionary[id];
    }
    public ParticleSystem.Particle[] Update(out int len)
    {
        if (m_Particles == null || m_Particles.Length < ContentDictionary.Count)
            m_Particles = new ParticleSystem.Particle[ContentDictionary.Count];
        int idx = 0;
        foreach (int id in ContentDictionary.Keys)
        {
            var content = (PointContent)ContentDictionary[id];
            if (content.nObservation <= 0)
            {
                content.particle.remainingLifetime = -1f;
            }
            else
            {
                //m_Particles[idx].startColor = Color.cyan;
                //m_Particles[idx].startSize = 0.05f;
                //m_Particles[idx].remainingLifetime = 10f;
                //m_Particles[idx++].position = content.s;
                m_Particles[idx++] = content.particle;
            }
        }
        for (int i = idx; i < m_Particles.Length; ++i)
        {
            m_Particles[i].remainingLifetime = -1f;
        }
        len = idx;
        return m_Particles;
        //mText.text = "draw test = " + idx + " " + ContentDictionary.Count;
    }
}
public class PathContentManager : ContentManager
{
    public PathContentManager() : base()
    {

    }
    void UpdateContent(int id)
    {
        var c = ContentDictionary[id];
        c.obj.SetActive(true);
    }
    public Content Process(int cid, int type, GameObject prefab, float sx, float sy, float sz, float ex, float ey, float ez, float s, Text mText) {
        Vector3 sPos = new Vector3(sx, -sy, sz);
        Vector3 ePos = new Vector3(ex, -ey, ez);
        if (CheckContent(cid))
        {
            //����
            UpdateContent(cid);
        }
        else
        {
            //�߰�
            var newContent = new PathContent(cid, type, prefab, sPos, ePos, s, mText);
            RegistContent(cid, newContent);
        }
        return ContentDictionary[cid];
    }
    public void Move(int id)
    {
        if (ContentDictionary.ContainsKey(id))
        {
            var content = ContentDictionary[id];
            content.obj.GetComponent<ObjectPath>().MoveStart();
        }
    }
}
public class ContentManager
{
    public Dictionary<int, Content> ContentDictionary;
    public ContentManager()
    {
        ContentDictionary = new Dictionary<int, Content>();
    }
    public bool CheckContent(int id)
    {
        return ContentDictionary.ContainsKey(id);
    }
    public void RegistContent(int id, Content c)
    {
        ContentDictionary.Add(id, c);
    }
    
    void UpdateContent(int id, Vector3 pos)
    {
        var c = ContentDictionary[id];
        c.obj.SetActive(true);
        c.obj.transform.position = pos;
    }
    public Content Process(int cid, int type, GameObject prefab, Color c, float x, float y, float z, float s, Text mText)
    {
        Vector3 pos = new Vector3(x, y, z);
        if (CheckContent(cid))
        {
            UpdateContent(cid, pos);
        }
        else
        {
            //create
            var newContent = new Content(cid, type, prefab, pos, s, c, mText);
            RegistContent(cid, newContent);
        }
        return ContentDictionary[cid];
    }
    public void Update()
    {
        foreach (int id in ContentDictionary.Keys)
        {
            var content = ContentDictionary[id];
            if (content.nObservation <= 0)
            {
                content.obj.SetActive(false);
            }
        }
    }
}