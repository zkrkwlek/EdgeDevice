using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

enum SegLabel { NONE = 0, FLOOR=4, WALL=1, CEIL=6 };

public class PointCloudReceivedEventArgs : EventArgs
{
    public PointCloudReceivedEventArgs(int _fid, int _n, float[] _fdata)
    {
        fid = _fid;
        fdata = _fdata;
        N = _n;
    }
    public int fid { get; set; }
    public float[] fdata { get; set; }
    public int N { get; set; }
}

class PointCloudReceivedEvent
{
    public static event EventHandler<PointCloudReceivedEventArgs> pointCloudReceived;
    public static void RunEvent(PointCloudReceivedEventArgs e)
    {
        if (pointCloudReceived != null)
        {
            pointCloudReceived(null, e);
        }
    }
}
[Serializable]
public class PointCloudManagerParam : Param
{
    public bool bVisualization;
    public int nMode = 0;
}

public class PointCloudProcess : MonoBehaviour
{
    public ParticleSystem m_ParticleSystem;
    public ParameterManager mParamManager;
    public Text mText;

    PointCloudManagerParam mPointParam;
    TrackerParam mTrackerParam;
    string dirPath, filename;

    bool bTracking;
    Color[] labelColors;

    bool WantsToQuit()
    {
        //파라메터 저장
        File.WriteAllText(filename, JsonUtility.ToJson(mPointParam));
        return true;
    }

    void Awake()
    {
        //파라메터 로드
        dirPath = Application.persistentDataPath + "/data/Param";
        filename = dirPath + "/PointCloudManager.json";
        try
        {
            string strAddData = File.ReadAllText(filename);
            mPointParam = JsonUtility.FromJson<PointCloudManagerParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            mPointParam = new PointCloudManagerParam();
            mPointParam.nMode = 1;
        }
        if (!mPointParam.bVisualization)
        {
            enabled = false;
            return;
        }

        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        bTracking = mTrackerParam.bTracking;

        //파라메터 로드
        m_NumParticles = 0;
        Application.wantsToQuit += WantsToQuit;
        
        //seg color
        labelColors = new Color[4];
        labelColors[0] = new Color(0f, 0f, 0f, 0.7f);
        labelColors[1] = new Color(1f, 0f, 0f, 0.7f);
        labelColors[2] = new Color(0f, 0f, 1f, 0.7f);
        labelColors[3] = new Color(0f, 1f, 0f, 0.7f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        if(mPointParam.bVisualization)
            PointCloudReceivedEvent.pointCloudReceived += OnPointCloudReceived;
    }

    void OnDisable()
    {
        if (mPointParam.bVisualization)
            PointCloudReceivedEvent.pointCloudReceived -= OnPointCloudReceived;
    }

    int m_NumParticles;
    ParticleSystem.Particle[] m_Particles;
    Matrix4x4 invertYMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
    void OnPointCloudReceived(object sender, PointCloudReceivedEventArgs eventArgs) {
        //데이터 만들고 파티클 생성하기
        try {

            int N = eventArgs.N;

            ////포즈 생성
            Matrix4x4 ARM = Matrix4x4.identity;
            if (!bTracking)
            {
                Matrix3x3 R = new Matrix3x3(
                    eventArgs.fdata[1], eventArgs.fdata[2], eventArgs.fdata[3],
                    eventArgs.fdata[4], eventArgs.fdata[5], eventArgs.fdata[6],
                    eventArgs.fdata[7], eventArgs.fdata[8], eventArgs.fdata[9]
                );
                PoseData data = new PoseData();
                data.rot = R.GetQuaternion();
                data.pos = new Vector3(eventArgs.fdata[10], eventArgs.fdata[11], eventArgs.fdata[12]);
                ARM = invertYMatrix * Matrix4x4.TRS(data.pos, data.rot, Vector3.one) * invertYMatrix;
            }

            int dataIdx = 13;

            int numParticles = N;
            if (m_Particles == null || m_Particles.Length < numParticles) { 
                m_Particles = new ParticleSystem.Particle[numParticles];
                m_NumParticles = numParticles;
            }
            //var color = m_ParticleSystem.main.startColor.color;
            var size = m_ParticleSystem.main.startSize.constant;

            for (int i = 0; i < N; i++)
            {
                dataIdx += 4;
                int mid = (int)eventArgs.fdata[dataIdx++];
                int label = (int)eventArgs.fdata[dataIdx++];
                float x = eventArgs.fdata[dataIdx++];
                float y = eventArgs.fdata[dataIdx++];
                float z = eventArgs.fdata[dataIdx++];

                if(mPointParam.nMode == 1)
                {
                    y *= -1f;
                }

                Vector3 pt = new Vector3(x, y, z);
                if (!bTracking)
                {
                    var T = Camera.main.transform.localToWorldMatrix * ARM;
                    pt = T.MultiplyPoint(pt);
                }

                var color = labelColors[0];
                if(label == (int)SegLabel.FLOOR)
                {
                    color = labelColors[1];
                }
                if (label == (int)SegLabel.WALL)
                {
                    color = labelColors[2];
                }
                if (label == (int)SegLabel.CEIL)
                {
                    color = labelColors[3];
                }

                //트래킹 모드가 아니면 변환이 필요함.
                m_Particles[i].startColor = color;
                m_Particles[i].startSize = size;
                m_Particles[i].position = pt;
                m_Particles[i].remainingLifetime = 1f;
            }
            for (int i = numParticles; i < m_NumParticles; ++i)
            {
                m_Particles[i].remainingLifetime = -1f;
            }
            m_ParticleSystem.SetParticles(m_Particles, numParticles);

        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }
}
