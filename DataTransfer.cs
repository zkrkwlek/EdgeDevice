using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.VideoModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DataTransferEventArgs : EventArgs
{
    public DataTransferEventArgs(int id, int _n, ref byte[] bdata, ref float[] fdata)
    {
        mnID = id;
        N = _n;
        mat = new Mat();
        prev = new MatOfPoint2f();


        Mat buff = new Mat(1, bdata.Length, CvType.CV_8UC1);
        MatUtils.copyToMat<byte>(bdata, buff);
        mat = Imgcodecs.imdecode(buff, Imgcodecs.IMREAD_COLOR);

        //포인트 위치 복사
        Point[] pts = new Point[_n];
        for (int i = 0; i < N; i++)
        {
            pts[i] = new Point(fdata[2 * i], fdata[2 * i + 1]);
        }
        prev.fromArray(pts);

    }
    public int mnID { get; set; }
    public int N { get; set; }
    public Mat mat { get; set; }
    public MatOfPoint2f prev { get; set; }
}

class DataTransferEvent
{
    public static event EventHandler<DataTransferEventArgs> transferEvent;
    public static void RunEvent(DataTransferEventArgs e)
    {
        if (transferEvent != null)
        {
            transferEvent(null, e);
        }
    }
}

[Serializable]
public class DataTransferParam
{
    public int level;
    public int win_size;
    public bool bShowLog;
}

public class DataTransfer : MonoBehaviour
{
    bool WantsToQuit()
    {
        //파라메터 저장
        File.WriteAllText(filename, JsonUtility.ToJson(param));

        if (matOpFlowPrev != null)
            matOpFlowPrev.Dispose();
        if (mMOP2fptsPrev != null)
            mMOP2fptsPrev.Dispose();
        return true;
    }

    public Text mText;
    bool bInit;
    MatOfPoint2f mMOP2fptsPrev;
    Mat matOpFlowPrev;
    DataTransferParam param;
    string dirPath, filename;

    //test
    //MatOfInt param;
    //MatOfByte data;

    Scalar colorRed = new Scalar(0, 0, 255, 255); //BGR?
    int iLineThickness = 3;
    public int win_size = 7; //10,5가 맞긴한데 이러면 느림.
    public int level = 1;
    Size winSize;
    TermCriteria criteria;

    void Awake()
    {
        //파라메터 로드
        dirPath = Application.persistentDataPath + "/data/Param";
        filename = dirPath + "/DataTransfer.json";
        try
        {
            string strAddData = File.ReadAllText(filename);
            param = JsonUtility.FromJson<DataTransferParam>(strAddData);
            //mText.text = "success load " + load_scripts;
        }
        catch (Exception e)
        {
            param = new DataTransferParam();
            param.win_size = 5;
            param.level = 1;
        }
        this.win_size = param.win_size;
        this.level = param.level;
        //파라메터 로드

        bInit = false;
        Application.wantsToQuit += WantsToQuit;

        //data = new MatOfByte();
        //int[] temp = new int[2];
        //temp[0] = Imgcodecs.IMWRITE_JPEG_QUALITY;
        //temp[1] = 50;
        //param = new MatOfInt(temp);

        winSize = new Size(win_size * 2 + 1, win_size * 2 + 1);
        criteria = new TermCriteria(TermCriteria.MAX_ITER | TermCriteria.EPS, 20, 0.3);
    }

    void OnEnable()
    {
        matOpFlowPrev = new Mat();
        mMOP2fptsPrev = new MatOfPoint2f();

        DataTransferEvent.transferEvent += OnTransferEvent;
        ImageCatchEvent.frameReceived += OnCameraFrameReceived;
        
    }
    void OnDisable()
    {
        

        DataTransferEvent.transferEvent -= OnTransferEvent;
        ImageCatchEvent.frameReceived -= OnCameraFrameReceived;
        
    }
    void OnCameraFrameReceived(object sender, ImageCatchEventArgs e)
    {
        try
        {
            if (!bInit)
                return;

            DateTime t1 = DateTime.Now;
            var frameID = e.mnFrameID;

            //matching
            //시간 체크
            Mat rgbaMat = new Mat();
            Imgproc.cvtColor(e.rgbMat, rgbaMat, Imgproc.COLOR_BGRA2BGR);
            rgbaMat.convertTo(rgbaMat, CvType.CV_8UC3);
            //e.rgbMat.copyTo(rgbaMat);

            MatOfPoint2f mMOP2fptsThis = new MatOfPoint2f();
            MatOfByte mMOBStatus = new MatOfByte();
            MatOfFloat mMOFerr = new MatOfFloat();
            Video.calcOpticalFlowPyrLK(matOpFlowPrev, rgbaMat, mMOP2fptsPrev,mMOP2fptsThis, mMOBStatus, mMOFerr,winSize, 3, criteria);

            var timeSpan = DateTime.Now - t1;
            double ts = timeSpan.TotalMilliseconds;

            if (mMOBStatus.rows() > 0)
            {
                List<Point> cornersPrev = mMOP2fptsPrev.toList();
                List<Point> cornersThis = mMOP2fptsThis.toList();
                List<byte> byteStatus = mMOBStatus.toList();

                int x = 0;
                int y = byteStatus.Count - 1;

                int nMatch = 0;
                for (x = 0; x < y; x++)
                {
                    if (byteStatus[x] == 1)
                    {
                        nMatch++;
                        Point pt = cornersThis[x];
                        Point pt2 = cornersPrev[x];

                        Imgproc.circle(rgbaMat, pt, 5, colorRed, iLineThickness - 1);
                        Imgproc.line(rgbaMat, pt, pt2, colorRed, iLineThickness);
                    }
                }

                if(param.bShowLog)
                    mText.text = "OpticalFlow matching = " + nMatch + ", " + ts;
                
                ////파일 저장
                //Imgcodecs.imencode(".jpg", rgbaMat, data, param);
                //byte[] bImgData = data.toArray();
                //var dirPath = Application.persistentDataPath + "/save";
                //if (!Directory.Exists(dirPath))
                //{
                //    Directory.CreateDirectory(dirPath);
                //}
                //File.WriteAllBytes(dirPath + "/superpoint_" + (e.mnFrameID) + ".png", bImgData);
            }
            else {
                mText.text = "OpticalFlow matching Failed = "+rgbaMat.channels()+" "+matOpFlowPrev.channels() + "==, " + ts;
            }
        }
        catch (Exception ex)
        {
            mText.text = ex.ToString();
        }
    }

    void OnTransferEvent(object sender, DataTransferEventArgs e)
    { 
        //이미지와 포인트 저장
        try{

            //이미지 복사
            //Mat buff = new Mat(1, e.bdata.Length, CvType.CV_8UC1);
            //MatUtils.copyToMat<byte>(e.bdata, buff);
            //matOpFlowPrev = Imgcodecs.imdecode(buff, Imgcodecs.IMREAD_COLOR);
            //buff.Dispose();

            ////포인트 위치 복사
            //Point[] pts = new Point[e.N];
            //for(int i = 0; i < e.N; i++)
            //{
            //    pts[i] = new Point(e.fdata[2 * i], e.fdata[2 * i + 1]);
            //}
            //mMOP2fptsPrev.fromArray(pts);

            matOpFlowPrev = e.mat;
            mMOP2fptsPrev = e.prev;

            //List<Point> cornersPrev = mMOP2fptsPrev.toList();
            //for (int i = 0; i < e.N; i++)
            //{
            //    Point pt = cornersPrev[i];
            //    Imgproc.circle(matOpFlowPrev, pt, 5, colorRed, iLineThickness - 1);
            //}

            //string savePath = Application.persistentDataPath + "/save/ImwriteScreenCaptureExample_output.jpg";
            //Imgcodecs.imwrite(savePath, mMOP2fptsPrev);
            //mText.text = "asdfl;asjf;laskdf "+e.N;

            if (!bInit)
                bInit = true;

        }
        catch (Exception ex)
        {
            mText.text = ex.ToString();
        }

    }
        // Start is called before the first frame update
    void Start()
    {
        
    }
    //이미지와 포인트 획득하기

    // Update is called once per frame
    void Update()
    {
    }
}
