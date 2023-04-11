using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전반적인 작업은 터치 매니저와 동일함.
/// 다만 입력에 제한을 두지 않음.
/// 패스 이런거 고려 안함.
/// </summary>

public class DrawManager : MonoBehaviour
{
    public SystemManager mSystemManager;
    public CameraManager mCamManager;
    public DataCommunicator mSender;
    public GameObject UVR;
    public Text mText;

    Plane p;
    Mat invCamMat;
    int width;
    int height;
    float widthScale;
    float heightScale;

    void OnPlaneDetection(object sender, PlaneEventArgs e)
    {
        //mText.text = "plane event test~~ "+e.plane.ToString();
        p = e.plane;
    }
    void OnCameraInitialization(object Sender, CameraInitEventArgs e)
    {
        try
        {
            invCamMat = e.invCamMat;
            width = e.width;
            height = e.height;
            widthScale = e.widthScale;
            heightScale = e.heightScale;
        }
        catch (Exception ex)
        {
            ex.ToString();
        }
    }

    void OnEnable()
    {
        CameraInitEvent.camInitialized += OnCameraInitialization;
        PlaneDetectionEvent.planeDetected += OnPlaneDetection;
    }
    void OnDisable()
    {
        CameraInitEvent.camInitialized -= OnCameraInitialization;
        PlaneDetectionEvent.planeDetected -= OnPlaneDetection;
    }

    // Start is called before the first frame update
    void Awake()
    {

    }

    int dID = 1;

    // Update is called once per frame
    void Update()
    {
        try
        {
            bool bTouch = false;
            Vector2 touchPos = Vector2.zero;
            int nTouchCount = Input.touchCount;
            string keyword = "VO.Generation.Draw";
            int sendID = mCamManager.mnFrame;

            if (nTouchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                Ray raycast = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit raycastHit;
                GameObject obj = null;
                bool bHit = Physics.Raycast(raycast, out raycastHit);

                ////3차원 복원
                float x = touch.position.x / widthScale;
                float y = height - touch.position.y / heightScale;
                Mat pos = new Mat(3, 1, CvType.CV_64FC1);
                pos.put(0, 0, x);
                pos.put(1, 0, y);
                pos.put(2, 0, 1f);
                Mat temp = invCamMat * pos;
                var ptCam = new Vector3((float)temp.get(0, 0)[0], (float)temp.get(1, 0)[0], (float)temp.get(2, 0)[0]);

                var toPoint = UVR.transform.localToWorldMatrix.MultiplyPoint(ptCam);
                var dir = toPoint - UVR.transform.position;
                dir = dir.normalized;
                Ray ray = new Ray(UVR.transform.position, dir);
                float dist = 0f;
                bool bRay = p.Raycast(ray, out dist);
                float[] fdata = new float[5];
                Vector3 newPos = ray.origin + ray.direction * dist;
                fdata[0] = x;
                fdata[1] = y;
                fdata[2] = newPos.x;
                fdata[3] = newPos.y;
                fdata[4] = newPos.z;

                byte[] bdata = new byte[fdata.Length * 4];
                Buffer.BlockCopy(fdata, 0, bdata, 0, bdata.Length); //전체 실수형 데이터 수
                UdpData mdata = new UdpData(keyword, mSystemManager.User.UserName, dID++, bdata, 1.0);
                StartCoroutine(mSender.SendData(mdata));
            }

        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }
}
