using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ScaleAdjuster : MonoBehaviour
{
    ARPointCloud m_PointCloud;
    public ParticleSystem m_ParticleSystem;
    int m_NumParticles;
    ParticleSystem.Particle[] m_Particles;

    public PoseManager mPoseManager;
    public ParameterManager mParamManager;
    TrackerParam mTrackerParam;
    public Text mText;

    int numPointClouds;
    Mat invK, K, invertYMat;
    int width, height;

    //lock 확인
    List<Mat> pointClouds1, pointClouds2;
    Dictionary<Vector2Int, float> mDepth1, mDepth2;

    bool WantsToQuit()
    {
        return true;
    }
    void OnEnable()
    {
        m_PointCloud.updated += OnPointCloudChanged;
        PointCloudReceivedEvent.pointCloudReceived += OnPointCloudReceived;
        CameraInitEvent.camInitialized += OnCameraInitialization;
    }
    void OnDisable()
    {
        m_PointCloud.updated -= OnPointCloudChanged;
        PointCloudReceivedEvent.pointCloudReceived -= OnPointCloudReceived;
        CameraInitEvent.camInitialized -= OnCameraInitialization;
    }
    void Awake()
    {
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        if (mTrackerParam.bTracking)
        {
            enabled = false;
            return;
        }
        m_PointCloud = GetComponent<ARPointCloud>();
        invertYMat = Mat.eye(3, 3, CvType.CV_32FC1);
        invertYMat.put(1, 1, -1f);
        mDepth1 = new Dictionary<Vector2Int, float>();
        mDepth2 = new Dictionary<Vector2Int, float>();
        pointClouds1 = new List<Mat>();
        pointClouds2 = new List<Mat>();
        Application.wantsToQuit += WantsToQuit;
    }
    void OnCameraInitialization(object Sender, CameraInitEventArgs e)
    {
        K = e.camMat;
        invK = e.invCamMat;
        K.convertTo(K, CvType.CV_32FC1);
        invK.convertTo(invK, CvType.CV_32FC1);
        width = e.width;
        height = e.height;
    }
    unsafe void OnPointCloudChanged(ARPointCloudUpdatedEventArgs eventArgs)
    {
        try
        {
            if (m_PointCloud.positions.HasValue)
            {
                pointClouds2.Clear();
                var tempPoints = new List<Vector3>();

                foreach (var point in m_PointCloud.positions.Value)
                {
                    tempPoints.Add(point);
                    Mat temp = new Mat(3, 1, CvType.CV_32FC1);
                    temp.put(0, 0, point.x);
                    temp.put(1, 0, point.y);
                    temp.put(2, 0, point.z);
                    pointClouds2.Add(temp);
                }

                int numParticles = tempPoints.Count;
                if (m_Particles == null || m_Particles.Length < numParticles)
                {
                    m_Particles = new ParticleSystem.Particle[numParticles];
                    m_NumParticles = numParticles;
                }
                var size = m_ParticleSystem.main.startSize.constant;
                var color = m_ParticleSystem.main.startColor.color;
                            
                for(int i = 0; i < numParticles; i++)
                {
                    m_Particles[i].startColor = color;
                    m_Particles[i].startSize = size;
                    m_Particles[i].position = tempPoints[i];
                    m_Particles[i].remainingLifetime = 1f;
                }
                
                for (int i = numParticles; i < m_NumParticles; ++i)
                {
                    m_Particles[i].remainingLifetime = -1f;
                }
                m_ParticleSystem.SetParticles(m_Particles, numParticles);
                
            }//if
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }

    }
    void OnPointCloudReceived(object sender, PointCloudReceivedEventArgs eventArgs)
    {
        try {
            pointClouds1.Clear();
            mDepth1.Clear();
            int N = eventArgs.N;
            int fid = eventArgs.fid;

            Mat R = new Mat(3, 3, CvType.CV_32FC1);
            R.put(0, 0, eventArgs.fdata[1]);
            R.put(0, 1, eventArgs.fdata[2]);
            R.put(0, 2, eventArgs.fdata[3]);

            R.put(1, 0, eventArgs.fdata[4]);
            R.put(1, 1, eventArgs.fdata[5]);
            R.put(1, 2, eventArgs.fdata[6]);

            R.put(2, 0, eventArgs.fdata[7]);
            R.put(2, 1, eventArgs.fdata[8]);
            R.put(2, 2, eventArgs.fdata[9]);

            Mat t = new Mat(3, 1, CvType.CV_32FC1);
            t.put(0, 0, eventArgs.fdata[10]);
            t.put(1, 0, eventArgs.fdata[11]);
            t.put(2, 0, eventArgs.fdata[12]);

            R = invertYMat * R * invertYMat;
            t = invertYMat * t;
            
            int dataIdx = 13;
            for (int i = 0; i < N; i++)
            {
                dataIdx += 6;
                float x = eventArgs.fdata[dataIdx++];
                float y = -eventArgs.fdata[dataIdx++];
                float z = eventArgs.fdata[dataIdx++];

                Mat temp = new Mat(3, 1, CvType.CV_32FC1);
                temp.put(0, 0, x);
                temp.put(1, 0, y);
                temp.put(2, 0, z);

                //Mat proj = new Mat(3, 1, CvType.CV_32FC1);
                var proj = K * (R * temp + t);
                //mText.text = R.ToString()+t.ToString()+temp.ToString()+K.ToString();
                //continue;
                float depth = (float)proj.get(2, 0)[0];
                if (depth <= 0f)
                {
                    continue;
                }//if
                float px = (float)proj.get(0, 0)[0]; px /= depth;
                float py = (float)proj.get(1, 0)[0]; py /= depth;
                int ix = (int)px;
                int iy = (int)py;

                if (ix < 0 || iy < 0 || ix >= width || iy >= height)
                {
                    continue;
                }
                var pt = new Vector2Int(ix, iy);
                if (!mDepth1.ContainsKey(pt))
                {
                    pointClouds1.Add(temp);
                    mDepth1.Add(pt, depth);
                }
            }
            //mText.text = "asdf" + mDepth1.Count+" "+pointClouds1.Count;

            //스케일 보정
            CalculateScale(fid);
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
    }

    void CalculateScale(int fid)
    {
        try {
            int count = 0;
            int count2 = 0;
            var trans = mPoseManager.GetPose(fid);
            var m00 = trans.worldToLocalMatrix.m00;
            var m01 = trans.worldToLocalMatrix.m01;
            var m02 = trans.worldToLocalMatrix.m02;
            var m10 = trans.worldToLocalMatrix.m10;
            var m11 = trans.worldToLocalMatrix.m11;
            var m12 = trans.worldToLocalMatrix.m12;
            var m20 = trans.worldToLocalMatrix.m20;
            var m21 = trans.worldToLocalMatrix.m21;
            var m22 = trans.worldToLocalMatrix.m22;
            var t1 = trans.worldToLocalMatrix.m03;
            var t2 = trans.worldToLocalMatrix.m13;
            var t3 = trans.worldToLocalMatrix.m23;

            Mat R = new Mat(3, 3, CvType.CV_32FC1);
            R.put(0, 0, m00);
            R.put(0, 1, m01);
            R.put(0, 2, m02);

            R.put(1, 0, m10);
            R.put(1, 1, m11);
            R.put(1, 2, m12);

            R.put(2, 0, m20);
            R.put(2, 1, m21);
            R.put(2, 2, m22);

            Mat t = new Mat(3, 1, CvType.CV_32FC1);
            t.put(0, 0, t1);
            t.put(1, 0, t2);
            t.put(2, 0, t3);

            mDepth2.Clear();
            foreach (var Xw in pointClouds2)
            {
                //var proj = new Mat(3, 1, CvType.CV_32FC1);
                var proj = K * (R * Xw + t);
                //depth
                //id
                float depth = (float)proj.get(2, 0)[0];

                

                if (depth <= 0f)
                {
                    continue;
                }//if
                float px = (float)proj.get(0, 0)[0]; px /= depth;
                float py = (float)proj.get(1, 0)[0]; py /= depth;
                int ix = (int)px;
                int iy = (int)py;

                var pt = new Vector2Int(ix, iy);
                if (!mDepth2.ContainsKey(pt))
                {
                    mDepth2.Add(pt, depth);
                }

                if (ix < 0 || iy < 0 || ix >= width || iy >= height)
                {
                    continue;
                }
                
                
                if (mDepth1.ContainsKey(pt))
                {
                    count++;
                }
                count2++;
                
            }//foreach

            //string dirPath = Application.persistentDataPath + "/data/";
            //string fileName1 = dirPath+"proj1.txt";
            //string[] temp1 = new string[mDepth1.Count];
            //int idx = 0;
            //foreach (KeyValuePair<Vector2Int, float> item in mDepth1)
            //{
            //    var key = item.Key;
            //    var fdepth = item.Value;
            //    temp1[idx] = key.x + " " + key.y + " " + fdepth;
            //    idx++;
            //}
            //File.WriteAllLines(fileName1, temp1);

            //string fileName2 = dirPath+"proj2.txt";
            //string[] temp2 = new string[mDepth2.Count];
            //int idx2 = 0;
            //foreach (KeyValuePair<Vector2Int, float> item in mDepth2)
            //{
            //    var key = item.Key;
            //    var fdepth = item.Value;
            //    temp2[idx2] = key.x + " " + key.y + " " + fdepth;
            //    idx2++;
            //}
            //File.WriteAllLines(fileName2, temp2);

            //mText.text = "overlap test = " + count+", "+count2 + "="+ mDepth1.Count+" "+pointClouds2.Count;
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
    }

    void UpdateVisibility()
    {
        var visible =
            enabled &&
            (m_PointCloud.trackingState != TrackingState.None);

        SetVisible(visible);
    }

    void SetVisible(bool visible)
    {
        if (m_ParticleSystem == null)
            return;

        var renderer = m_ParticleSystem.GetComponent<Renderer>();
        if (renderer != null) { 
            renderer.enabled = visible;
        }
    }
    void Update()
    {
        UpdateVisibility();
    }
    //전송받은 포인트 클라우드와 비교
}

//public class Pose
//{
//    public Pose(int id = 0) {
//        R = new Matrix3x3();
//        t = Vector3.zero;
//    }
//    //기기의 자세로부터 생성시 : axis = Rwc, _t는 center임.
//    public Pose(int id, ref Vector3 axis, ref Vector3 _c) {
//        mnID = id;
//        R = Matrix3x3.EXP(axis).Transpose();
//        c = new Vector3(_c.x, _c.y, _c.z);
//        t = -(R * c);
//    }

//    //서버에서 전송받을 때 : Rs, ts임.
//    public Pose(int id, ref float[] fdata)
//    {
//        mnID = id;
//        R = new Matrix3x3(fdata[0], fdata[1], fdata[2], fdata[3], fdata[4], fdata[5], fdata[6], fdata[7], fdata[8]);
//        t = new Vector3(fdata[9], fdata[10], fdata[11]);
//        c = -(R.Transpose() * t);
//    }
//    public void Update(ref Vector3 axis, ref Vector3 _c)
//    {
//        R = Matrix3x3.EXP(axis).Transpose();
//        c = new Vector3(_c.x, _c.y, _c.z);
//        t = -(R * c);
//    }

//    public Pose Copy() {
//        Pose P = new Pose();
//        P.mnID = this.mnID;
//        P.R = new Matrix3x3(this.R.m00, this.R.m01, this.R.m02, this.R.m10, this.R.m11, this.R.m12, this.R.m20, this.R.m21, this.R.m22);
//        P.t = new Vector3(this.t.x, this.t.y, this.t.z);
//        P.c  = new Vector3(this.c.x, this.c.y, this.c.z);
//        return P;
//    }
//    public string ToString() {
//        return "ID = "+mnID+" "+R.ToString() + t.ToString();
//    }
//    public int mnID;
//    public Matrix3x3 R; //rotation
//    public Vector3 t; //translation
//    public Vector3 c; //center
//}

//public class ScaleAdjuster : MonoBehaviour
//{

//    public Text mText;

//    ////기기에서 추정한 자세
//    //Pose prevDevicePose = new Pose();
//    //Pose currDevicePose = new Pose();

//    ////서버로부터 전송받은 자세
//    //Pose prevServerPose = new Pose();
//    //Pose currServerPose = new Pose();

//    int currID, prevID;
//    Dictionary<int, Pose> ServerPoseDictionary = new Dictionary<int, Pose>();
//    Dictionary<int, Pose> DevicePoseDictionary = new Dictionary<int, Pose>();

//    Matrix3x3 Ty = new Matrix3x3(1f, 0f, 0f, 0f, -1f, 0f, 0f, 0f, 1f);
//    public Pose Tlg = new Pose();


//    //스케일
//    KalmanFilter filter = new KalmanFilter();
//    public float mfScale = 1.0f;
//    public bool mbScaleAdjustment = false;

//    // Start is called before the first frame update
//    void Start()
//    {
//        enabled = false;
//        currID = 0;
//        prevID = 0;
//    }

//    // Update is called once per frame
//    void Update()
//    {
//    }

//    ///axis = normalized*angle
//    public void SetDevicePose(int id, ref Vector3 _axis, ref Vector3 _t)
//    {
//        try
//        {

//            var pose = new Pose(id, ref _axis, ref _t);
//            DevicePoseDictionary.Add(id, pose);
//            //if (!mbScaleAdjustment)
//            //{
//            ////prevDevicePose = currDevicePose.Copy();
//            ////currDevicePose = new Pose(id, ref _axis, ref _t);    
//            //}
//        }
//        catch (Exception e)
//        {
//            mText.text = e.ToString();
//        }
//    }

//    public void SetServerPose(int id, ref float[] fdata)
//    {
//        try
//        {
//            var pose = new Pose(id, ref fdata);
//            ServerPoseDictionary.Add(id, pose);
//            currID = id;

//            //if (!mbScaleAdjustment)
//            //{
//            //    //prevServerPose = currServerPose.Copy();
//            //    //currServerPose = new Pose(id, ref fdata);
//            //}
//        }
//        catch (Exception e) 
//        {
//            mText.text = e.ToString();
//        }

//    }
//    public void CalculateScale(int id)
//    {
//        Pose currDevicePose = DevicePoseDictionary[id];
//        Pose currServerPose = ServerPoseDictionary[id];
//        if (mbScaleAdjustment) {

//            Matrix3x3 R = mfScale * (currServerPose.R);
//            Vector3 t = mfScale * currServerPose.t;
//            //Matrix3x3 R = (currServerPose.R);
//            //Vector3 t = currServerPose.t;

//            ////y축 변환
//            R = Ty * R;
//            t.y = -t.y;

//            ////현재 기기의 좌표계로
//            Tlg.R = currDevicePose.R.Transpose() * R;
//            Tlg.t = currDevicePose.R.Transpose() * t + currDevicePose.c;
//            Tlg.c = -(Tlg.R.Transpose() * Tlg.t);
//            //mText.text = "Cetner = " +id +" = "+ Tlg.c + " " + currDevicePose.c + " " + currServerPose.c + "\n" + "Scale = " + string.Format("{0:0.000} ", mfScale);
//            return;
//        }


//        float max_dist = 0f;
//        foreach (int tid in DevicePoseDictionary.Keys)
//        {
//            if (tid == id)
//                continue;
//            Pose prevDevicePose = DevicePoseDictionary[tid];
//            Pose prevServerPose = ServerPoseDictionary[tid];

//            float dist_device = (currDevicePose.c - prevDevicePose.c).magnitude;
//            float dist_server = (currServerPose.c - prevServerPose.c).magnitude;

//            if(dist_device > max_dist)
//            {
//                max_dist = dist_device;
//            }

//            if (dist_device > 0.2)
//            {
//                mfScale =   / dist_server;
//                ////스케일 보정
//                Matrix3x3 R = mfScale * (currServerPose.R);
//                Vector3 t = mfScale * currServerPose.t;
//                //Matrix3x3 R = (currServerPose.R);
//                //Vector3 t = currServerPose.t;

//                ////y축 변환
//                R = Ty * R;
//                t.y = -t.y;

//                ////현재 기기의 좌표계로
//                Tlg.R = currDevicePose.R.Transpose() * R;
//                Tlg.t = currDevicePose.R.Transpose() * t + currDevicePose.c;
//                Tlg.c = -(Tlg.R.Transpose() * Tlg.t);
//                //mText.text = "Tlg = " + Tlg.t + " Td = " + currDevicePose.t + " Ts = " + currServerPose.t + "\n" + "Cetner = " + Tlg.c + " " + currDevicePose.c + " " + currServerPose.c + "\n" + "Scale = " + string.Format("{0:0.000} ", mfScale) + " || " + string.Format("{0:0.000} ", dist_device) + " " + string.Format("{0:0.000} ", dist_server);
//                //mText.text = "Scale = " + mfScale;
//                mbScaleAdjustment = true;
//            }
//            if (mbScaleAdjustment)
//                break;
//        }

//        if (!mbScaleAdjustment)
//            mText.text = "Scale Test = " + max_dist;
//    }
//    public void CalculateScale() {
//        //smText.text = "ScaleAdjuster::Calculate="+prevDevicePose.mnID + " " + currDevicePose.mnID + " " + prevServerPose.mnID + " " + currServerPose.mnID;

//        //if(prevDevicePose.mnID > 0)
//        //{
//        //    float d_device = (currDevicePose.c - prevDevicePose.c).magnitude;
//        //    float d_server = (currServerPose.c - prevServerPose.c).magnitude;
//        //    mfScale = filter.Update(d_device / d_server);
//        //    //mfScale = currDevicePose.t.magnitude / currServerPose.t.magnitude;
//        //    mbScaleAdjustment = true;

//        //    //////스케일 보정
//        //    //Matrix3x3 R = mfScale * (currServerPose.R.Transpose());
//        //    //Vector3 t = mfScale*currServerPose.c;

//        //    //////y축 변환
//        //    //R = Ty * R;
//        //    //t.y = -t.y;

//        //    //////현재 기기의 좌표계로
//        //    //Tlg.R = currDevicePose.R * R;
//        //    //Tlg.t = currDevicePose.R * t + currDevicePose.t;
//        //    //Tlg.c = -(Tlg.R.Transpose() * Tlg.t);
//        //    //mText.text = "Tlg = " + Tlg.t + " Td = " + currDevicePose.t+" Ts = "+currServerPose.t +"\n"+"Cetner = "+Tlg.c+" "+currDevicePose.c+" "+currServerPose.c;
//        //    ////mText.text = "Scale = " + mfScale;

//        //    ////스케일 보정
//        //    //Matrix3x3 R = mfScale * (currServerPose.R);
//        //    //Vector3 t = mfScale * currServerPose.t;
//        //    Matrix3x3 R = (currServerPose.R);
//        //    Vector3 t = currServerPose.t;

//        //    ////y축 변환
//        //    R = Ty * R;
//        //    t.y = -t.y;

//        //    ////현재 기기의 좌표계로
//        //    Tlg.R = currDevicePose.R.Transpose() * R;
//        //    Tlg.t = currDevicePose.R.Transpose() * t + currDevicePose.c;
//        //    Tlg.c = -(Tlg.R.Transpose() * Tlg.t);
//        //    mText.text = "Tlg = " + Tlg.t + " Td = " + currDevicePose.t + " Ts = " + currServerPose.t + "\n" + "Cetner = " + Tlg.c + " " + currDevicePose.c + " " + currServerPose.c+"\n"+"Scale = "+ string.Format("{0:0.000} ", mfScale) + " || "+ string.Format("{0:0.000} ", d_device) + " "+ string.Format("{0:0.000} ", d_server);
//        //    //mText.text = "Scale = " + mfScale;
//        //}
//    }



//}

