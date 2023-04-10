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
    public PlaneManager mPlaneManager;
    public VOFrameManager mVOFrameManager;
    ObjectParam mObjParam;
    ExperimentParam mExParam;
    DrawPaintParam mDrawParam;

    ContentManager mContentManager;
    PathContentManager mPathManager;
    DrawContentManager mDrawManager;
    public ParticleSystem m_ParticleSystem;

    void Awake()
    {
        mObjParam = (ObjectParam)mParamManager.DictionaryParam["VirtualObject"];
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        mDrawParam = (DrawPaintParam)mParamManager.DictionaryParam["DrawPaint"];

        mContentManager = new ContentManager();
        mPathManager = new PathContentManager();
        mDrawManager = new DrawContentManager();
        mDrawManager.mDrawParam = mDrawParam;
    }
    void Start()
    {
        
    }
    
    // Update is called once per frame
    void Update()
    {
        try {
            mContentManager.Update();
            mPathManager.Update();
            mDrawManager.Update();
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
        mPathManager.Move(id);
    }

    public void UpdateVirtualFrame(int fid, float[] fdata)
    {

        try {
            //var newVF = new VirtualFrame(fid);
            var newVF = mVOFrameManager.GetFrame(fid);

            Color color = Color.white;

            int N = (int)fdata[0];
            int idx = 1;
            for (int j = 0; j < N; j++)
            {
                int id = (int)fdata[idx];
                int mid = (int)fdata[idx + 1];
                int type = (int)fdata[idx + 2];
                float x = fdata[idx + 3];
                float y = fdata[idx + 4];
                float z = fdata[idx + 5];
                float ex = fdata[idx + 6];
                float ey = fdata[idx + 7];
                float ez = fdata[idx + 8];

                idx += 9;
                Content content = null;
                if (type == 2)
                {
                    if (mPlaneManager.CheckPlane(mid))
                    {
                        var plane = mPlaneManager.GetPlane(mid);
                        var normal = plane.plane.normal * -1f;
                        Color c = Color.blue;
                        if(mid == 0)
                        {
                            c = Color.red;
                        }else if(mid == 1)
                        {
                            c = Color.green;
                        }
                        
                        content = mDrawManager.Process(id, type, x, y, z, ex, ey, ez, normal, c, mText);
                        //mText.text = mid + " ";
                        //content = mDrawManager.Process(id, type, x, y, z, ex, ey, ez, mText);
                    }
                    else
                    {
                        content = mDrawManager.Process(id, type, x, y, z, ex, ey, ez, mText);
                    }
                    
                }
                else if (type == 1)
                {
                    content = mPathManager.Process(id, type, pathObjPrefab, x, y, z, ex, ey, ez, mObjParam.fWalkingObjScale, mText);
                }
                else
                {
                    if (mExParam.bManipulationTest)
                    {
                        if(id % 3 == 0)
                        {
                            color = Color.red;
                        }
                        if (id % 3 == 1)
                        {
                            color = Color.green;
                        }
                        if (id % 3 == 2)
                        {
                            color = Color.blue;
                        }
                    }
                    content = mContentManager.Process(id, type, tempObjPrefab, color, x, y, z, mObjParam.fTempObjScale, mText);
                }
                if (content != null) { 
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
