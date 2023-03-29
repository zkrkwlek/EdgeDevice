//using ARFoundationWithOpenCVForUnity.UnityUtils.Helper;
//using ARFoundationWithOpenCVForUnityExample;
using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

//2D기반
public class MarkerReceiveEventArgs : EventArgs
{
    public MarkerReceiveEventArgs(int _markerid, float[] data)
    {
        mnMarkerID = _markerid;
        marker_data = data;
    }
    public int mnMarkerID { get; set; }
    public float[] marker_data { get; set; }
}

class MarkerReceiveEvent
{
    public static event EventHandler<MarkerReceiveEventArgs> markerReceived;
    public static void RunEvent(MarkerReceiveEventArgs e)
    {
        if (markerReceived != null)
        {
            markerReceived(null, e);
        }
    }
}

//통합 좌표계 3D 정보까지
class MarkerReceiveEvent2
{
    public static event EventHandler<MarkerReceiveEventArgs> markerReceived;
    public static void RunEvent(MarkerReceiveEventArgs e)
    {
        if (markerReceived != null)
        {
            markerReceived(null, e);
        }
    }
}

public class MarkerDetectEventArgs2 : EventArgs
{
    public MarkerDetectEventArgs2(int _frameid,float[] data, Vector3 _pos)
    {
        mnFrameID = _frameid;
        marker_data = data;
        pos = _pos;
    }
    public int mnFrameID { get; set; }
    public float[] marker_data { get; set; }
    public Vector3 pos { get; set; }
}

class MarkerDetectEvent2
{
    public static event EventHandler<MarkerDetectEventArgs2> markerDetected;
    public static void RunEvent(MarkerDetectEventArgs2 e)
    {
        if (markerDetected != null)
        {
            markerDetected(null, e);
        }
    }
}

public class MarkerDetectEventArgs : EventArgs
{
    public MarkerDetectEventArgs(ArucoMarker m) 
    {
        marker = m;
    }
    public ArucoMarker marker { get; set; }
}

//delegate void eventMarkerDetect();
class MarkerDetectEvent
{
    public static event EventHandler<MarkerDetectEventArgs> markerDetected;
    public static void RunEvent(MarkerDetectEventArgs e)
    {
        if (markerDetected != null)
        {
            markerDetected(null, e);
        }
    }
}
public enum ArUcoDictionary
{
    //DICT_4X4_50 = Aruco.DICT_4X4_50,
    //DICT_4X4_100 = Aruco.DICT_4X4_100,
    //DICT_4X4_250 = Aruco.DICT_4X4_250,
    //DICT_4X4_1000 = Aruco.DICT_4X4_1000,
    //DICT_5X5_50 = Aruco.DICT_5X5_50,
    //DICT_5X5_100 = Aruco.DICT_5X5_100,
    //DICT_5X5_250 = Aruco.DICT_5X5_250,
    //DICT_5X5_1000 = Aruco.DICT_5X5_1000,
    //DICT_6X6_50 = Aruco.DICT_6X6_50,
    //DICT_6X6_100 = Aruco.DICT_6X6_100,
    //DICT_6X6_250 = Aruco.DICT_6X6_250,
    //DICT_6X6_1000 = Aruco.DICT_6X6_1000,
    //DICT_7X7_50 = Aruco.DICT_7X7_50,
    //DICT_7X7_100 = Aruco.DICT_7X7_100,
    //DICT_7X7_250 = Aruco.DICT_7X7_250,
    //DICT_7X7_1000 = Aruco.DICT_7X7_1000,
    //DICT_ARUCO_ORIGINAL = Aruco.DICT_ARUCO_ORIGINAL,
}

public class ArucoMarker{
    public int id;
    public int frameId; //마커를 디텍션한 최신 프레임의 아이디
    public List<Vector2> corners; //최신 프레임에서 코너 위치
    public Vector3 origin; //arcore 기준.
    public Vector3 origin2;//내 알고리즘 기준
    public VirtualObject gameobject; //필터링용. 이것은 azi, ele로 위치 측정할 때 이용
    public Matrix4x4 ARM;
    public bool mbCreate;
    public int nUpdated;
    public ArucoMarker(){ 
        corners = new List<Vector2>();
        mbCreate = false;
        nUpdated = 0;
    }
    public ArucoMarker(int _id)
    {
        nUpdated = 0;
        id = _id;
        corners = new List<Vector2>();
        mbCreate = false;
    }
    public float Calculate(Matrix4x4 P, Mat K, Vector3 pos, Vector2 corner, out Vector2 corner2, bool bFlip)
    {
        //유니티 좌표계에서는 y를 카메라 좌표계에서 플립해야 함.
        //opencv 좌표계에서는 해당사항 없음.
        float sign= bFlip ? -1f : 1f;
        var pt = P.MultiplyPoint(pos);
        Mat proj = new Mat(3, 1, CvType.CV_64FC1);
        proj.put(0, 0, pt.x);
        proj.put(1, 0, sign*pt.y);
        proj.put(2, 0, pt.z);
        proj = K * proj;
        double depth = proj.get(2, 0)[0];
        float px = (float)(proj.get(0, 0)[0] / depth);
        float py = (float)(proj.get(1, 0)[0] / depth);
        corner2 = new Vector2(px, py);
        Vector2 proj2D = corner2 - corner;
        return proj2D.sqrMagnitude;
    }
    public void UpdateObject(Vector4 pos, float markerLength, Transform trans)
    {
        Matrix4x4 obj = Matrix4x4.identity;
        obj.SetColumn(3, pos);

        ARM = ARM * obj;
        ARM = trans.localToWorldMatrix * ARM;
        
        if (nUpdated == 0)
        {
            gameobject.transform.position = ARUtils.ExtractTranslationFromMatrix(ref ARM);
            gameobject.transform.rotation = ARUtils.ExtractRotationFromMatrix(ref ARM);

            //var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //obj1.transform.parent = gameobject.transform;
            //obj1.transform.position = gameobject.transform.position;
            //obj1.transform.rotation = gameobject.transform.rotation;
            ////obj1.transform.localPosition = Vector3.zero;
            ////obj1.transform.localRotation = Quaternion.identity;
            //obj1.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            ////var obj1 = Instantiate(PrimitiveType.Cube, Vector3.zero, Quaternion.identity);
            ////obj1.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            gameobject.SetMatrix4x4(ARM);
        }

        nUpdated++;
    }

    public void UpdateObject(Vector4 pos,float markerLength, Matrix4x4 fitARFoundationBackgroundMatrix, Matrix4x4 fitHelpersFlipMatrix, GameObject cam)
    {
        Matrix4x4 obj = Matrix4x4.identity;
        obj.SetColumn(3, pos);
        //Matrix4x4 tempMat = fitARFoundationBackgroundMatrix * ARM * obj;
        //tempMat = fitHelpersFlipMatrix * tempMat;
        //tempMat = cam.transform.localToWorldMatrix * tempMat;

        ARM = fitARFoundationBackgroundMatrix * ARM * obj;
        ARM = fitHelpersFlipMatrix * ARM;
        ARM = cam.transform.localToWorldMatrix * ARM;

        /////이 부분은 삭제 할 것인디
        if (nUpdated == 0)
        {
            //ARUtils.SetTransformFromMatrix(gameobject.transform, ref ARM);
            gameobject.transform.position = ARUtils.ExtractTranslationFromMatrix(ref ARM);
            gameobject.transform.rotation = ARUtils.ExtractRotationFromMatrix(ref ARM);

            //var obj1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //obj1.transform.parent = gameobject.transform;
            //obj1.transform.position = gameobject.transform.position;
            //obj1.transform.rotation = gameobject.transform.rotation;
            ////obj1.transform.localPosition = Vector3.zero;
            ////obj1.transform.localRotation = Quaternion.identity;
            //obj1.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            ////var obj1 = Instantiate(PrimitiveType.Cube, Vector3.zero, Quaternion.identity);
            ////obj1.GetComponent<Renderer>().material.color = Color.red;
        }
        else
        {
            gameobject.SetMatrix4x4(ARM);
        }
        
        nUpdated++;
    }

    public void CreateOrigin(float markerLength, Matrix4x4 fitARFoundationBackgroundMatrix, Matrix4x4 fitHelpersFlipMatrix, GameObject cam)
    {
        //Matrix4x4 obj = Matrix4x4.identity;
        //obj.SetColumn(3, new Vector4(-markerLength / 2, -markerLength / 2, 0.0f, 1.0f));
        //ARM = fitARFoundationBackgroundMatrix * ARM * obj;
        //ARM = fitHelpersFlipMatrix * ARM;
        //ARM = cam.transform.localToWorldMatrix * ARM;

        //this.origin = new Vector3(ARM.m03, ARM.m13, ARM.m23);

        //marker.gameobject.SetMatrix4x4(ARM);
        //this.origin = this.gameobject.transform.position;
    }

    public void CalculateAziAndEleAndDist(Vector3 center, out float azi, out float ele, out float dist) {
        //Matrix4x4 obj = Matrix4x4.identity;
        //obj.SetColumn(3, new Vector4(-markerLength / 2, -markerLength / 2, 0.0f, 1.0f));
        //ARM = fitARFoundationBackgroundMatrix * ARM * obj;
        //ARM = fitHelpersFlipMatrix * ARM;
        //ARM = cam.transform.localToWorldMatrix * ARM;
        //gameobject.SetMatrix4x4(ARM);

        Vector3 dir = center - gameobject.transform.position;
        dir.z *= -1f;
        
        azi = Mathf.Rad2Deg*Mathf.Atan2(dir.z, dir.x);
        if (azi < 0f)
            azi += 360f;
        if (azi > 360f)
            azi -= 360f;

        Vector3 dir2 = dir; dir2.y = 0f;
        ele = Mathf.Rad2Deg * Mathf.Atan2(dir.sqrMagnitude, dir2.sqrMagnitude);
        if (ele < 0f)
            ele += 360f;
        if (ele > 360f)
            ele -= 360f;

        dist = dir.sqrMagnitude;
    }
}



public class ArucoMarkerDetector : MonoBehaviour
{
    ArUcoMarkerParam param;
    TrackerParam mTrackerParam;

    public ParameterManager mParamManager;

    public Camera arCamera;
    public PoseManager mPoseManager;
    public GameObject prefabObj;
    public Text mText;
    [HideInInspector]
    public Dictionary<int, ArucoMarker> mDictMarkers;

    //ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;
    
    public float markerLength;
    Mat rgbMat;
    Mat ids;
    [HideInInspector]
    public List<Mat> corners;
    
    List<Mat> rejectedCorners;
    Mat rvecs;
    Mat tvecs;
    Mat rotMat;
    //DetectorParameters detectorParams;
    //Dictionary dictionary;

    Mat camMatrix;
    Mat invCamMatrix;
    Mat distCoeffs;
    Matrix4x4 fitARFoundationBackgroundMatrix;
    Matrix4x4 fitHelpersFlipMatrix;
    int width;
    int height;
    float widthScale;
    float heightScale;
    int mnFrameID;
    
    bool WantsToQuit()
    {
        rgbMat.Dispose();
        ids.Dispose();
        rvecs.Dispose();
        tvecs.Dispose();
        rotMat.Dispose();
        foreach (var item in corners)
        {
            item.Dispose();
        }
        corners.Clear();
        mDictMarkers.Clear();
        return true;
    }

    void OnEnable()
    {
        ImageCatchEvent.frameReceived += OnCameraFrameReceived;
        CameraInitEvent.camInitialized += OnCameraInitialization;
        if (!mTrackerParam.bTracking) { 
            //이 부분은 내꺼 모듈에서도 테스트 할 수 있기는 함.
            MarkerReceiveEvent.markerReceived += OnMarkerReceived;
        }
        if (mTrackerParam.bTracking)
            MarkerReceiveEvent2.markerReceived += OnMarkerReceived2;
    }

    void OnDisable()
    {
        ImageCatchEvent.frameReceived -= OnCameraFrameReceived;
        CameraInitEvent.camInitialized -= OnCameraInitialization;
        MarkerReceiveEvent.markerReceived -= OnMarkerReceived;
        MarkerReceiveEvent2.markerReceived -= OnMarkerReceived2;
    }

    Matrix4x4 invertYMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));

    Matrix4x4 mode1(PoseData data)
    {
        Matrix4x4 ARM = invertYMatrix * Matrix4x4.TRS(data.pos, data.rot, Vector3.one) * invertYMatrix;
        return ARM;
    }
    Matrix4x4 mode2(PoseData data)
    {
        Matrix4x4 ARM =  Matrix4x4.TRS(data.pos, data.rot, Vector3.one) * invertYMatrix;
        return ARM;
    }
    Matrix4x4 mode3(PoseData data)
    {
        Matrix4x4 ARM = invertYMatrix * Matrix4x4.TRS(data.pos, data.rot, Vector3.one);
        return ARM;
    }
    void OnCameraInitialization(object Sender, CameraInitEventArgs e)
    {
        try {
            camMatrix = e.camMat;
            invCamMatrix = e.invCamMat;
            distCoeffs = e.distCoeffs;
            fitARFoundationBackgroundMatrix = Matrix4x4.identity;
            fitHelpersFlipMatrix = Matrix4x4.identity;
            width = e.width;
            height = e.height;
            widthScale = e.widthScale;
            heightScale = e.heightScale;

            rgbMat = new Mat(height, width, CvType.CV_8UC3);
            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();

            rvecs = new Mat();
            tvecs = new Mat();
            rotMat = new Mat(3, 3, CvType.CV_64FC1);

            //detectorParams = DetectorParameters.create();
            //dictionary = Aruco.getPredefinedDictionary((int)dictionaryId);
            mDictMarkers = new Dictionary<int, ArucoMarker>();
        }
        catch (Exception ex)
        {
            ex.ToString();
        }
    }

    bool bInit = true;
    GameObject tempObj = null;
    void OnMarkerReceived2(object sender, MarkerReceiveEventArgs args)
    {
        try
        {
            int mid = args.mnMarkerID;
            int fid = (int)args.marker_data[0];
            if (fid < 30)
                return;

            if (mPoseManager.CheckPose(fid))
            {

                //R,T 추정
                float a = args.marker_data[2];
                float b = args.marker_data[3];
                float c = args.marker_data[4];

                float r1 = args.marker_data[5];
                float r2 = args.marker_data[6];
                float r3 = args.marker_data[7];
                PoseData data = new PoseData();
                data.rot = Matrix3x3.ConvertRvecToRot(r1, r2, r3);
                data.pos = new Vector3(a, b, c);

                Matrix4x4 ARM = Matrix4x4.identity;
                if (param.nMode == 0)
                    ARM = mode1(data);
                if (param.nMode == 1)
                    ARM = mode2(data);
                if (param.nMode == 2)
                    ARM = mode3(data);
                //R,T 추정

                ////마커 코너
                ArucoMarker marker;
                if (!mDictMarkers.ContainsKey(mid))
                {
                    marker = new ArucoMarker(mid);
                    marker.gameobject = this.gameObject.AddComponent<VirtualObject>();
                    //marker.gameobject.transform.parent = Camera.main.transform;

                    mDictMarkers.Add(mid, marker);
                }
                marker = mDictMarkers[mid];
                marker.frameId = fid;
                marker.corners.Clear();
                //marker.corners.Add(new Vector2(widthScale*args.marker_data[8], Screen.height - heightScale * args.marker_data[9]));
                marker.corners.Add(new Vector2(args.marker_data[8], height - args.marker_data[9]));
                marker.corners.Add(Vector2.zero);
                marker.corners.Add(Vector2.zero);
                marker.corners.Add(Vector2.zero);

                //marker.corners.Clear();
                //marker.corners.Add(Vector2.zero);
                //marker.corners.Add(Vector2.zero);
                //marker.corners.Add(Vector2.zero);
                //marker.corners.Add(Vector2.zero);
                ////마커 코너

                /*
                //코너, invK, R,t로 복원
                float d = args.marker_data[1];
                float x = args.marker_data[8];
                float y = args.marker_data[9];
                //float y = height - args.marker_data[3];
                Mat pos = new Mat(3, 1, CvType.CV_64FC1);
                pos.put(0, 0, x);
                pos.put(1, 0, y);
                pos.put(2, 0, 1f);
                Mat temp = invCamMatrix * pos * d;
                var ptCam = new Vector3((float)temp.get(0, 0)[0], -(float)temp.get(1, 0)[0], (float)temp.get(2, 0)[0]);

                var trans = mPoseManager.GetPose(fid);
                var ptWorld = trans.localToWorldMatrix.MultiplyPoint(ptCam);
                */
                if (bInit)
                {
                    bInit = false;
                    marker.gameobject.transform.position = ARUtils.ExtractTranslationFromMatrix(ref ARM);
                    marker.gameobject.transform.rotation = ARUtils.ExtractRotationFromMatrix(ref ARM);


                    //tempObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //tempObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    //tempObj.GetComponent<Renderer>().material.color = Color.yellow;

                    ////double[] rvec = new double[3];
                    ////rvec[0] = args.marker_data[5];
                    ////rvec[1] = args.marker_data[6];
                    ////rvec[2] = args.marker_data[7];
                    ////double[] tvec = new double[3];
                    ////tvec[0] = args.marker_data[2];
                    ////tvec[1] = args.marker_data[3];
                    ////tvec[2] = args.marker_data[4];
                    ////var ARM = UpdateARObjectTransform(rvec, tvec);



                    ////var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ////Vector3 ptWorld2 = new Vector3(a, -b, c);
                    ////obj2.transform.position = ptWorld2;



                    ////var obj2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ////obj2.transform.position = ARUtils.ExtractTranslationFromMatrix(ref ARM);
                    ////obj2.transform.rotation = ARUtils.ExtractRotationFromMatrix(ref ARM);
                    ////obj2.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    ////obj2.GetComponent<Renderer>().material.color = Color.magenta;

                    //////3차원 정보 갱신
                    //tempObj.transform.position = ARUtils.ExtractTranslationFromMatrix(ref ARM);
                    //tempObj.transform.rotation = ARUtils.ExtractRotationFromMatrix(ref ARM);
                }
                //마커 이벤트
                MarkerDetectEvent.RunEvent(new MarkerDetectEventArgs(marker));

            }
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }
    void OnMarkerReceived(object sender, MarkerReceiveEventArgs args)
    {
        try {
            int mid = args.mnMarkerID;
            int fid = (int)args.marker_data[0];

            if (mPoseManager.CheckPose(fid))
            {
                double[] rvec = new double[3];
                rvec[0] = args.marker_data[1];
                rvec[1] = args.marker_data[2];
                rvec[2] = args.marker_data[3];
                double[] tvec = new double[3];
                tvec[0] = args.marker_data[4];
                tvec[1] = args.marker_data[5];
                tvec[2] = args.marker_data[6];

                ArucoMarker marker;
                if (!mDictMarkers.ContainsKey(mid))
                {
                    marker = new ArucoMarker(mid);
                    marker.gameobject = this.gameObject.AddComponent<VirtualObject>();
                    //marker.gameobject.transform.parent = Camera.main.transform;

                    mDictMarkers.Add(mid, marker);
                }
                marker = mDictMarkers[mid];
                ////마커 객체 생성

                ///코너 갱신
                //코너도 전송하도록 변경해야 함.
                marker.corners.Clear();
                marker.corners.Add(new Vector2(args.marker_data[7], height - args.marker_data[8]));
                marker.corners.Add(Vector2.zero);
                marker.corners.Add(Vector2.zero);
                marker.corners.Add(Vector2.zero);
                //for (int j = 0; j < 4; j++)
                //{
                //    float x = (float)corners[i].get(0, j)[0];
                //    float y = (float)corners[i].get(0, j)[1];
                //    //float x = widthScale * (float)corners[i].get(0, j)[0];
                //    //float y = Screen.height - heightScale * (float)corners[i].get(0, j)[1];
                //    marker.corners.Add(new Vector2(x, y));
                //}
                //마커 프레임 아이디 갱신
                marker.frameId = fid;
                marker.ARM = UpdateARObjectTransform(rvec, tvec);
                var trans = mPoseManager.GetPose(fid);
                marker.UpdateObject(new Vector4(0f, 0f, 0f, 1f), markerLength, trans);
                MarkerDetectEvent.RunEvent(new MarkerDetectEventArgs(marker));
            }
        }
        catch(Exception e)
        {
            mText.text = "MarkerReceived=" + e.ToString();
        }
    }

    void OnCameraFrameReceived(object sender, ImageCatchEventArgs e) {

        try
        {
            mnFrameID = e.mnFrameID;
            Imgproc.cvtColor(e.rgbMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
            //Aruco.detectMarkers(rgbMat, dictionary, corners, ids, detectorParams, rejectedCorners);

            if(ids.total() > 0)
            {
                float[] fdata = new float[ids.total()*3+1];
                fdata[0] = (float)ids.total();
                Aruco.estimatePoseSingleMarkers(corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);
                for (int i = 0; i < ids.total(); i++)
                {
                    ////마커 객체 생성
                    int id = (int)ids.get(i, 0)[0];
                    ArucoMarker marker;
                    if (!mDictMarkers.ContainsKey(id))
                    {
                        marker = new ArucoMarker(id);
                        marker.gameobject = this.gameObject.AddComponent<VirtualObject>();
                        //marker.gameobject.transform.parent = Camera.main.transform;

                        mDictMarkers.Add(id, marker);
                    }
                    marker = mDictMarkers[id];
                    
                    ////마커 객체 생성

                    ///코너 갱신
                    marker.corners.Clear();
                    for (int j = 0; j < 4; j++)
                    {
                        float x = (float)corners[i].get(0, j)[0];
                        float y = (float)corners[i].get(0, j)[1];
                        //float x = widthScale * (float)corners[i].get(0, j)[0];
                        //float y = Screen.height - heightScale * (float)corners[i].get(0, j)[1];
                        marker.corners.Add(new Vector2(x, y));
                    }
                    //마커 프레임 아이디 갱신
                    marker.frameId = mnFrameID;

                    ///마커에서 포즈
                    Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1));
                    Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1));
                    marker.ARM = UpdateARObjectTransform(rvec, tvec);
                    //가상 객체의 변환된 3차원 위치를 필터링으로 기록함.
                    marker.UpdateObject(new Vector4(-markerLength / 2, -markerLength / 2, 0.0f, 1.0f), markerLength, fitARFoundationBackgroundMatrix, fitHelpersFlipMatrix, Camera.main.gameObject);
                    MarkerDetectEvent.RunEvent(new MarkerDetectEventArgs(marker));

                    ////데이터에 기록
                    fdata[i * 3 + 1] = (float)id;
                    fdata[i * 3 + 2] = marker.corners[0].x;
                    fdata[i * 3 + 3] = marker.corners[0].y;

                }//for
                ////마커랑 포즈 모음 이벤트 생성
                {
                    MarkerDetectEvent2.RunEvent(new MarkerDetectEventArgs2(mnFrameID,fdata, Camera.main.transform.position));
                }
            }
        }
        catch (Exception ex)
        {
            ex.ToString();
        }
    }

    private void EstimatePoseCanonicalMarker(Mat rgbMat)
    {
        try
        {
            Aruco.estimatePoseSingleMarkers(corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);

            for (int i = 0; i < ids.total(); i++)
            {
                
                using (Mat rvec = new Mat(rvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                using (Mat tvec = new Mat(tvecs, new OpenCVForUnity.CoreModule.Rect(0, i, 1, 1)))
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    // Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    if (i == 0)
                    {
                        //UpdateARObjectTransform(rvec, tvec, id, ref marker, corners[i]);
                    }
                    var ARM = UpdateARObjectTransform(rvec, tvec);

                    //마커 생성 또는 탐색
                    int id = (int)ids.get(i, 0)[0];
                    //mListIDs.Add(id);
                    ArucoMarker marker;
                    if (!mDictMarkers.ContainsKey(id))
                    {
                        marker = new ArucoMarker(id);
                        marker.gameobject = this.gameObject.AddComponent<VirtualObject>();
                        mDictMarkers.Add(id, marker);
                    }
                    marker = mDictMarkers[id];
                    marker.ARM = ARM;
                    //마커 생성 또는 탐색

                    //마커 3차원 origin 복원. ARCore 성능 검증용
                    Matrix4x4 obj = Matrix4x4.identity;
                    obj.SetColumn(3, new Vector4(-markerLength / 2, -markerLength / 2, 0.0f, 1.0f));
                    ARM = fitARFoundationBackgroundMatrix * ARM * obj;
                    ARM = fitHelpersFlipMatrix * ARM;
                    ARM = arCamera.transform.localToWorldMatrix * ARM;

                    //marker.gameobject.SetMatrix4x4(ARM);
                    ARUtils.SetTransformFromMatrix(marker.gameobject.transform, ref ARM);

                    //marker.origin = new Vector3(ARM.m03, ARM.m13, ARM.m23);
                    marker.origin = marker.gameobject.transform.position;
                    //마커 3차원 origin 복원. ARCore 성능 검증용
                    //마커 코너 저장
                    marker.corners.Clear();
                    for (int j = 0; j < 4; j++)
                    {
                        float x = (float)corners[i].get(0, j)[0];
                        float y = (float)corners[i].get(0, j)[1];
                        //float x = widthScale * (float)corners[i].get(0, j)[0];
                        //float y = Screen.height - heightScale * (float)corners[i].get(0, j)[1];
                        marker.corners.Add(new Vector2(x, y));
                    }
                    marker.frameId = mnFrameID;
                    //마커 코너 저장
                    //마커 이벰ㄴ트
                    MarkerDetectEvent.RunEvent(new MarkerDetectEventArgs(marker));
                }
                
               
                //mDictMarkers[id] = marker;
                //mText.text = marker.origin.ToString()+marker.gameobject.transform.position.ToString();
            }
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }

    }
    private Matrix4x4 UpdateARObjectTransform(double[] rvec, double[] tvec)
    {
        PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvec, tvec);
        var ARM = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
        return ARM;
    }
    //마커와 카메라 사이의 포즈 계산
    private Matrix4x4 UpdateARObjectTransform(Mat rvec, Mat tvec)
    {
        Matrix4x4 ARM;
        // Convert to unity pose data.
        double[] rvecArr = new double[3];
        rvec.get(0, 0, rvecArr);
        double[] tvecArr = new double[3];
        tvec.get(0, 0, tvecArr);
        PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);
        ARM = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
        
        //// Convert to transform matrix.
        //try
        //{
            
            
        //    //// Apply the effect (flipping factors) of the projection matrix applied to the ARCamera by the ARFoundationBackground component to the ARM.
        //    //ARM = fitARFoundationBackgroundMatrix * ARM * obj;

        //    //// When detecting the AR marker from a horizontal inverted image (front facing camera),
        //    //// will need to apply an inverted X matrix to the transform matrix to match the ARFoundationBackground component display.
        //    //ARM = fitHelpersFlipMatrix * ARM;

        //    //ARM = arCamera.transform.localToWorldMatrix * ARM;

        //    //marker.gameobject.SetMatrix4x4(ARM);


        //    //mText.text = fitARFoundationBackgroundMatrix.ToString()+"\n"+fitHelpersFlipMatrix.ToString();

        //    //if (enableLerpFilter)
        //    //{
        //    //    arGameObject.SetMatrix4x4(ARM);
        //    //}
        //    //else
        //    //{
        //    //    ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
        //    //}

        //    //mText.text = poseData.pos.ToString();
        //    mText.text = "markder detection~~";
        //}
        //catch (Exception e)
        //{
        //    mText.text = e.ToString();
        //}
        return ARM;
    }

    void Awake()
    {

        param = (ArUcoMarkerParam)mParamManager.DictionaryParam["Marker"];
        mTrackerParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];

        try
        {
            
        } catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        Application.wantsToQuit += WantsToQuit;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
