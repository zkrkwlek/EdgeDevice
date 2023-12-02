using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class ContentProcessor : MonoBehaviour
{
    public Text mText;
    public GameObject pathObjPrefab;
    public GameObject tempObjPrefab;
    public ParameterManager mParamManager;
    public EvaluationManager mEvalManager;
    public PlaneManager mPlaneManager;
    public VOFrameManager mVOFrameManager;
    public ObjectManager mObjManager;
    ObjectParam mObjParam;
    DrawPaintParam mDrawParam;
    Dictionary<int, int> ConnectedGraph;

    ContentManager mContentManager;
    //PathContentManager mPathManager;
    //DrawContentManager mDrawManager;

    void Awake()
    {
        mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];
        mDrawParam = (DrawPaintParam)mParamManager.DictionaryParam["DrawPaint"];
        mContentManager = new ContentManager(mEvalManager,tempObjPrefab,pathObjPrefab);
        //mPathManager = new PathContentManager();
        //mDrawManager = new DrawContentManager();
        //mDrawManager.mDrawParam = mDrawParam;

        //파싱 과정에서 관계 추가.
        //실제 객체도 추가해야 하는데 이건 어떻게?
        //오브젝트 매니저의 리얼 오브젝트도 이용해야함.
        ConnectedGraph = new Dictionary<int, int>();
    }
    void Start()
    {
        
    }

    public Content Process(ref float[] fdata, int idx, DateTime startTime, bool _b, Text mText)
    {
        try
        {
            int len = (int)fdata[idx++];
            int id = (int)fdata[idx++];
            int type = (int)fdata[idx++];
            
            if ((ContentType)type == ContentType.Object)
            {
                Vector3 pos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                Vector3 rot = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                Color c = new Color(fdata[idx++], fdata[idx++], fdata[idx++]);
                float scale = fdata[idx++];
                int sgSrcID = (int)fdata[idx++];
                SceneGraphAttr sgAttr = (SceneGraphAttr)fdata[idx++];
                SceneGraphType sgType = (SceneGraphType)fdata[idx++];
                int sgDstID = (int)fdata[idx];
                SceneGraphNode node = new SceneGraphNode(sgSrcID, sgDstID, sgAttr, sgType);
                if(sgType == SceneGraphType.Object)
                {
                    bool bObj = mObjManager.RealObjDict.ContainsKey(sgDstID);
                    if (bObj)
                    {
                        node.dstObj = mObjManager.RealObjDict[sgDstID];
                    }
                }
                //mText.text = id+" == "+sgDstID + " " + sgType+" "+ sgAttr + " " + sgSrcID+" ";
                return mContentManager.Process(node, id, type, tempObjPrefab, c, pos, scale, startTime, _b, mText).GetComponent<Content>();
            }
            if ((ContentType)type == ContentType.Draw)
            {
                int pid = (int)fdata[idx++];

                if (mPlaneManager.CheckPlane(pid))
                {
                    var plane = mPlaneManager.GetPlane(pid);
                    var normal = plane.plane.normal * -1f;
                    Vector3 spos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                    Vector3 epos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                    Color c = new Color(fdata[idx++], fdata[idx++], fdata[idx++]);
                    float size = fdata[idx++];
                    return mContentManager.DrawProcess(id, type, spos, epos, normal, c, size, mText);
                }
                else return null;
            }
            if((ContentType)type == ContentType.Path)
            {
                Vector3 spos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                Vector3 epos = new Vector3(fdata[idx++], fdata[idx++], fdata[idx++]);
                return mContentManager.PathProcess(id, type, pathObjPrefab, spos, epos, mObjParam.fWalkingObjScale, mText);
            }
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }

        return null;
    }

    // Update is called once per frame
    void Update()
    {
        try {
            mContentManager.Update();
            //mPathManager.Update();
            //mDrawManager.Update();
            //int len = 0;
            //var particles = mDrawManager.Update(out len);
            //if(len > 0)
            //    m_ParticleSystem.SetParticles(particles, len);
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
    }

    public void Move(int id)
    {
        mContentManager.Move(id);
    }

    public void UpdateVirtualFrame(int fid, ref float[] fdata, DateTime startTime, bool _b)
    {

        try {
            //var newVF = new VirtualFrame(fid);
            var newVF = mVOFrameManager.GetFrame(fid);

            Color color = Color.white;
            int N = (int)fdata[0];
            int idx = 1;
            //mText.text = "update object start~~ " + N;
            for (int j = 0; j < N; j++)
            {
                int len = (int)fdata[idx];
                Content content = null;
                content = Process(ref fdata, idx, startTime, _b, mText);
                idx += len;

                if (content != null)
                {
                    //여기에 제대로 들어가는가?
                    newVF.AddContent(content);
                }
            }

            mVOFrameManager.AddFrame(newVF);
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }
}
