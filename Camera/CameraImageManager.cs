using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CamParam
{
    public int width;
    public int height;
    public bool bShowLog;
}

public class CameraImageManager : MonoBehaviour
{
    public Text mText;
    public ARCameraManager cameraManager;
    public PoseManager poseManager;

    CamParam param;
    string dirPath, filename;

    [HideInInspector]
    public int mnBufferSize = 0;
    [HideInInspector]
    public int mnFrame = 1;

    int mnSkipFrame = 4;
    public Mat rgbaMat;

    bool bInit;
    XRCpuImage.ConversionParams conversionParams;
    XRCameraIntrinsics cameraIntrinsics;
    

    bool WantsToQuit()
    {
        File.WriteAllText(filename, JsonUtility.ToJson(param));
        return true;
    }

    void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }
    
    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)  
    {
        try {
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                mnBufferSize = 0;
                return;
            }
            if (!mbStart)
                return;

            if (!bInit)
            {
                if (!cameraManager.TryGetIntrinsics(out cameraIntrinsics))
                {
                    return;
                }
                //event
                float fx = cameraIntrinsics.focalLength.x;
                float fy = cameraIntrinsics.focalLength.y;
                float cx = cameraIntrinsics.principalPoint.x;
                float cy = cameraIntrinsics.principalPoint.y;

                Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
                camMatrix.put(0, 0, fx);
                camMatrix.put(0, 1, 0);
                camMatrix.put(0, 2, cx);
                camMatrix.put(1, 0, 0);
                camMatrix.put(1, 1, fy);
                camMatrix.put(1, 2, cy);
                camMatrix.put(2, 0, 0);
                camMatrix.put(2, 1, 0);
                camMatrix.put(2, 2, 1.0f);

                double invfx = 1 / fx;
                double invfy = 1 / fy;
                double invcx = -cx / fx;
                double invcy = -cy / fy;
                Mat invCamMatrix = new Mat(3, 3, CvType.CV_64FC1);
                invCamMatrix.put(0, 0, invfx);
                invCamMatrix.put(0, 1, 0);
                invCamMatrix.put(0, 2, invcx);
                invCamMatrix.put(1, 0, 0);
                invCamMatrix.put(1, 1, invfy);
                invCamMatrix.put(1, 2, invcy);
                invCamMatrix.put(2, 0, 0);
                invCamMatrix.put(2, 1, 0);
                invCamMatrix.put(2, 2, 1.0f);
                //invCamMatrix = camMatrix.inv();

                Mat distCoeffs = new MatOfDouble(0, 0, 0, 0);
                float widthScale = ((float)Screen.width) / width;
                float heightScale = ((float)Screen.height) / height;
                float cropped = (height - (Screen.height / widthScale)) / 2f;//터치 영역 보정

                CameraInitEvent.RunEvent(new CameraInitEventArgs(camMatrix, invCamMatrix, distCoeffs, (int)width, (int)height, widthScale, heightScale, cropped));
                bInit = true;

                if(param.bShowLog)
                    mText.text = image.width+" "+image.height+"||"+Screen.width+" "+Screen.height+"= "+widthScale+" "+heightScale+" "+cropped;
            }


            // See how many bytes you need to store the final image.
            int size = image.GetConvertedDataSize(conversionParams);

            // Extract the image data
            var length = (int)rgbaMat.total() * (int)rgbaMat.elemSize();
            IntPtr addr = (IntPtr)rgbaMat.dataAddr();
            image.Convert(conversionParams, addr, length);
            image.Dispose();

            //Utils.fastMatToTexture2D(rgbaMat, m_Texture);
            mnBufferSize = length;
            if (!poseManager.mbRunMode) {
                poseManager.AddPose(mnFrame, Camera.main.transform);
            }
            ImageCatchEvent.RunEvent(new ImageCatchEventArgs(ref rgbaMat, mnFrame++));
            //mText.text = cameraIntrinsics.focalLength.ToString() + " " + cameraIntrinsics.principalPoint.ToString() + " " + cameraIntrinsics.resolution.ToString();
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
        
    }
    ScreenOrientation screenOrientation;
    int height;
    int width;
    bool mbStart = false;

    void Awake()
    {
        //파라메터 로드
        dirPath = Application.persistentDataPath + "/data/Param";
        filename = dirPath + "/CameraParam.json";
        try
        {
            string strAddData = File.ReadAllText(filename);
            param = JsonUtility.FromJson<CamParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            param = new CamParam();
            param.bShowLog = false;
            param.width = 640;
            param.height = 480;
        }
        //파라메터 로드

        //카메라 회전에 따라서 변화시키기
        bInit = false;
        Application.wantsToQuit += WantsToQuit;
        
        try
        {
            screenOrientation = Screen.orientation;
            if (screenOrientation == ScreenOrientation.LandscapeLeft || screenOrientation == ScreenOrientation.LandscapeRight)
            {
                height = param.height;
                width = param.width;
            }else if(screenOrientation == ScreenOrientation.Portrait)
            {
                height = param.width;
                width = param.height;
            }

            conversionParams = new XRCpuImage.ConversionParams
            {
                // Get the entire image.
                inputRect = new RectInt(0, 0, width, height),

                // Downsample by 2.
                //outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                outputDimensions = new Vector2Int(width, height),
                // Choose RGBA format.
                outputFormat = TextureFormat.RGBA32,

                // Flip across the vertical axis (mirror image).
                transformation = XRCpuImage.Transformation.None //mirrorx

            };
            rgbaMat = new Mat(height, width, CvType.CV_8UC4);
            mbStart = true;
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }

    void Start()
    {

    }
    
}
