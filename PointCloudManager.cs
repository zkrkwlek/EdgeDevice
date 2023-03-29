using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PointCloudUpdateEventArgs : EventArgs
{
    public PointCloudUpdateEventArgs(int _num, List<Vector3> pts)
    {
        points = pts;
        mnNumPoints = _num;
    }
    public List<Vector3> points { get; set; }
    public int mnNumPoints { get; set; }
}

class PointCloudUpdateEvent
{
    public static event EventHandler<PointCloudUpdateEventArgs> pointCloudUpdated;
    public static void RunEvent(PointCloudUpdateEventArgs e)
    {
        if (pointCloudUpdated != null)
        {
            pointCloudUpdated(null, e);
        }
    }
}

[RequireComponent(typeof(ARPointCloud))]
public class PointCloudManager : MonoBehaviour
{
    
    ARPointCloud m_PointCloud;
    [HideInInspector]
    public int mnNumPoints = 0;
    [HideInInspector]
    public bool mbUpdated;
    public Text mText;

    int m_NumParticles;
    List<Vector3> pointClouds;
    unsafe void OnPointCloudChanged(ARPointCloudUpdatedEventArgs eventArgs)
    {
        try {

            if (m_PointCloud.positions.HasValue)
            {

                var points = pointClouds;//PointCloudManager2.Instance.MapPoints;
                points.Clear();

                if (m_PointCloud.positions.HasValue)
                {
                    foreach (var point in m_PointCloud.positions.Value)
                        points.Add(point);
                }

                //for (int i = 0; i < numParticles; ++i)
                //{
                //    m_Particles[i].startColor = color;
                //    m_Particles[i].startSize = size;
                //    m_Particles[i].position = points[i];
                //    m_Particles[i].remainingLifetime = 1f;
                //}

                //PointCloudManager2.Instance.NumMapPoints = numParticles;
                //if (numParticles > 50) {
                //    PointCloudManager2.Instance.Changed = true;
                //}

                //// Remove any existing particles by setting remainingLifetime
                //// to a negative value.
                //for (int i = numParticles; i < m_NumParticles; ++i)
                //{
                //    m_Particles[i].remainingLifetime = -1f;
                //}

                //m_ParticleSystem.SetParticles(m_Particles, Math.Max(numParticles, m_NumParticles));
                //m_NumParticles = numParticles;
                //mnNumPoints = m_NumParticles;

                //if(numParticles > 30)
                //{
                //    PointCloudUpdateEvent.RunEvent(new PointCloudUpdateEventArgs(mnNumPoints, points));
                //}

                ////if (m_NumParticles > 50)//&& mTestManager.mnFrame % mTestManager.mnSkipFrame == 0
                ////{
                ////    float[] farray = new float[m_NumParticles * 3];
                ////    int idx = 0;

                ////    foreach (var point in m_PointCloud.positions.Value)
                ////    {
                ////        farray[idx++] = point.x;
                ////        farray[idx++] = point.y;
                ////        farray[idx++] = point.z;
                ////    }

                ////    mbUpdated = true;
                ////    byte[] bdata = new byte[farray.Length * 4];
                ////    Buffer.BlockCopy(farray, 0, bdata, 0, bdata.Length);
                ////    string msg2 = mSystemManager.User.UserName + "," + mSystemManager.User.MapName;
                ////    UdpData idata = new UdpData("ARFoundationMPs", mSystemManager.User.UserName, mTestManager.mnFrame, bdata, 1.0);
                ////    StartCoroutine(mSender.SendData(idata));
                ////    //mText.text = "\t\t\tPointCloudManager = " + mTestManager.mnFrame + " " + mSystemManager.User.UserName+" "+m_NumParticles;
                ////}
                ////mText.text = "Particle test = " + m_NumParticles;

            }

            
            ////다른데서 이 클래스의 값을 인식 못함 그래서 여기서 데이터를 전송해야 할듯

        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }

    void Awake()
    {
        m_PointCloud = GetComponent<ARPointCloud>();

        //mText.text = "\t\t\tPointCloudManager Awake";
        mbUpdated = false;
        mnNumPoints = 0;

        pointClouds = new List<Vector3>();
    }

    void OnEnable()
    {
        m_PointCloud.updated += OnPointCloudChanged;
    }

    void OnDisable()
    {
        m_PointCloud.updated -= OnPointCloudChanged;
    }

    void Update()
    {

    }

}
