using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DataCommunicator : MonoBehaviour
{
#if (UNITY_EDITOR_WIN)
    [DllImport("edgeslam")]
    private static extern void DownloadData(int id, char[] keyword, int len1, char[] src, int len2, IntPtr addr, ref int N, ref float t);
    [DllImport("UnityLibrary")]
    private static extern float UploadData(IntPtr data, int len, int id, char[] keyword, int len1, char[] src, int len2, double ts);
    [DllImport("edgeslam")]
    private static extern int CreateDynamicObjectFrame(int id, IntPtr data, int sidx);
    [DllImport("edgeslam")]
    private static extern int EraseImage(int id);
#elif UNITY_ANDROID
    [DllImport("edgeslam")]
    private static extern float UploadData(IntPtr data, int len, int id, char[] keyword, int len1, char[] src, int len2, double ts);
    [DllImport("edgeslam")]
    private static extern void DownloadData(int id, char[] keyword, int len1, char[] src, int len2, IntPtr addr, ref int N, ref float t);
    [DllImport("edgeslam")]
    private static extern int CreateDynamicObjectFrame(int id, IntPtr data, int sidx);
    [DllImport("edgeslam")]
    private static extern int EraseImage(int id);
#endif

    public Text mText;
    //public ScaleAdjuster mScaleAdjuster;
    public PlaneManager mPlaneManager;
    public ContentProcessor mContentManager;
    public SystemManager mSystemManager;
    public ObjectDetection mObjectDetection;
    public Tracker mTracker;
    public UVRSpatialTest mSpatialTest;

    ExperimentParam mExParam;
    TrackerParam mTrackParam;
    public ParameterManager mParamManager;
    public EvaluationManager mEvalManager;

    ///NDK로 전송하는 데이터 코드
    public IEnumerator SendDataWithNDK(UdpData data)
    {
        data.sendedTime = DateTime.UtcNow;
        var t = UploadData(data.addr, data.length, data.id, data.keyword.ToCharArray(), data.keyword.Length, data.src.ToCharArray(), data.src.Length, data.ts);
        if (mEvalManager.bLatency)
        {
            string newkey = data.keyword + "" + data.id;
            if(!mEvalManager.DictCommuData.ContainsKey(newkey))
                mEvalManager.DictCommuData.Add(newkey, data);
            string res = data.keyword + ",upload," + data.id + "," + mTrackParam.nJpegQuality + "," + data.length + "," + t;
            mEvalManager.mLatencyTask.AddMessage(res);
        }
        yield return null;
    }

    public IEnumerator SendData(UdpData data)
    {
        data.sendedTime = DateTime.UtcNow;
        
        UnityWebRequest req = SetRequest(data.keyword, data.data, data.id, data.ts);
        yield return req.SendWebRequest();
        
        if (mEvalManager.bLatency)
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                string newkey = data.keyword + "" + data.id;
                mEvalManager.DictCommuData.Add(newkey, data);

                var time = DateTime.UtcNow;
                var timeSpan = time - data.sendedTime;
                double ts = timeSpan.TotalMilliseconds;
                string res = data.keyword + ",upload," + data.id + "," + mTrackParam.nJpegQuality + "," + ts;
                mEvalManager.mLatencyTask.AddMessage(res);
            }
            yield return null;
        }

    }

    UnityWebRequest SetRequest(string keyword, byte[] data, int id, double ts)
    {
        string addr2 = mSystemManager.AppData.Address + "/Store?keyword=" + keyword + "&id=" + id + "&ts=" + ts + "&src=" + mSystemManager.User.UserName;
        //string addr2 = strAddr + "/Store?keyword=" + keyword + "&id=" + id + "&ts=" + ts + "&src=" + strUser;
        //if (ts > 0.0)
        //addr2 += "&type2=" + ts;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        if (data.Length > 0)
        {
            UploadHandlerRaw uH = new UploadHandlerRaw(data);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }

    UnityWebRequest SetRequest(string url, byte[] data)
    {
        UnityWebRequest request = new UnityWebRequest(url);
        request.method = "POST";
        if (data.Length > 0)
        {
            UploadHandlerRaw uH = new UploadHandlerRaw(data);
            uH.contentType = "application/json";
            request.uploadHandler = uH;
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }

    UnityWebRequest GetRequest(string keyword, int id)
    {
        string addr2 = mSystemManager.AppData.Address + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + mSystemManager.User.UserName;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }
    UnityWebRequest GetRequest(string keyword, int id, string src)
    {
        string addr2 = mSystemManager.AppData.Address + "/Load?keyword=" + keyword + "&id=" + id + "&src=" + src;
        UnityWebRequest request = new UnityWebRequest(addr2);
        request.method = "POST";
        request.downloadHandler = new DownloadHandlerBuffer();
        //request.SendWebRequest();
        return request;
    }

    void Awake()
    {
        mTrackParam = (TrackerParam)mParamManager.DictionaryParam["Tracker"];
        mExParam = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
    }
    void OnEnable()
    {
        UdpAsyncHandler.Instance.UdpDataReceived += Process;
    }
    void OnDisable()
    {
        UdpAsyncHandler.Instance.UdpDataReceived -= Process;
    }


        // Start is called before the first frame update
    void Start()
    {
        
    }
    void Process(object sender, UdpEventArgs e) {
        try {
            int size = e.bdata.Length;
            string msg = System.Text.Encoding.Default.GetString(e.bdata);
            UdpData data = JsonUtility.FromJson<UdpData>(msg);
            StartCoroutine(MessageParsing(data));
            //StartCoroutine(MessageParsingWithNDK(data));
        }
        catch(Exception ex)
        {
            mText.text = ex.ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    HashSet<int> cids = new HashSet<int>();
    const int nMapPointInfo = 36;
    const int nSizeServerMP = nMapPointInfo + 32; // 3d x y z + desc // info + desc
    IEnumerator MessageParsing(UdpData data) {
        data.receivedTime = DateTime.UtcNow;
        if (data.keyword == "ReferenceFrame")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.UtcNow;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                var recivedTime = DateTime.UtcNow;
                
                ////
                if (!mSystemManager.mbInit)
                {
                    var timeSpan = DateTime.UtcNow - mSystemManager.TestTime;
                    double ts = timeSpan.TotalMilliseconds;
                    mSystemManager.writer.WriteLine(ts);
                    mSystemManager.mbInit = true;
                }
                float[] totalData = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, totalData, 0, req1.downloadHandler.data.Length);
                //float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                
                int Nmp = (int)totalData[1];

                try
                {
                    GCHandle handle = GCHandle.Alloc(totalData, GCHandleType.Pinned);
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    mTracker.CreateKeyFrame(data.id, ptr);

                    int NtrackDataSize = (int)totalData[0];
                    if (NtrackDataSize < totalData.Length)
                    {
                        //오브젝트
                        int NobjDataSize = (int)totalData[NtrackDataSize];
                        int Nobj = (int)totalData[NtrackDataSize + 1];
                        CreateDynamicObjectFrame(data.id, ptr, NtrackDataSize);
                    }
                    EraseImage(data.id);
                    handle.Free();

                    //update까지 끝
                    var processedTime = DateTime.UtcNow;

                    if (mEvalManager.bLatency && !mExParam.bEdgeBase)
                    {
                        string newkey = "Image" + data.id;
                        if (mEvalManager.DictCommuData.ContainsKey(newkey))
                        {
                            var prevData = mEvalManager.DictCommuData[newkey];
                            var latencySpan = data.receivedTime - prevData.sendedTime;
                            double latency = latencySpan.TotalMilliseconds;
                            string res_latency = "localization,latency," + data.id + "," + mTrackParam.nJpegQuality + "," + req1.downloadHandler.data.Length + "," + latency;
                            mEvalManager.mLatencyTask.AddMessage(res_latency);

                            var latencySpan2 = processedTime - prevData.sendedTime;
                            double latency2 = latencySpan2.TotalMilliseconds;
                            string res_latency2 = "updatemap,latency," + data.id + "," + mTrackParam.nJpegQuality + "," + req1.downloadHandler.data.Length + "," + latency2;
                            mEvalManager.mLatencyTask.AddMessage(res_latency2);
                        }
                        var timeSpan1 = recivedTime - data.receivedTime;
                        double ts1 = timeSpan1.TotalMilliseconds;
                        string res = "reference frame,download," + data.id + "," + mTrackParam.nJpegQuality + "," + req1.downloadHandler.data.Length + "," + ts1;
                        mEvalManager.mLatencyTask.AddMessage(res);
                    }
                }
                catch (Exception e)
                {
                    mText.text = "create reference err = " + e.ToString();
                }

                PointCloudReceivedEvent.RunEvent(new PointCloudReceivedEventArgs(data.id, Nmp, totalData));

                

                
            }
        }
        if (data.keyword == "UpdatedLocalMap")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.UtcNow;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                
                int n = req1.downloadHandler.data.Length / nSizeServerMP;
                try
                {
                    GCHandle handle = GCHandle.Alloc(req1.downloadHandler.data, GCHandleType.Pinned);
                    IntPtr ptr = handle.AddrOfPinnedObject();
                    mTracker.UpdateData(data.id, n, ptr);
                    var processedTime = DateTime.UtcNow;
                    string newkey = "Image" + data.id;
                    if (mEvalManager.DictCommuData.ContainsKey(newkey))
                    {
                        var prevData = mEvalManager.DictCommuData[newkey];
                        var latencySpan = data.receivedTime - prevData.sendedTime;
                        double latency = latencySpan.TotalMilliseconds;
                        string res_latency = "localization2,latency," + data.id + "," + mTrackParam.nJpegQuality + "," + req1.downloadHandler.data.Length + "," + latency;
                        mEvalManager.mLatencyTask.AddMessage(res_latency);

                        var latencySpan2 = processedTime - prevData.sendedTime;
                        double latency2 = latencySpan2.TotalMilliseconds;
                        string res_latency2 = "BaseLocalMapUpdate,latency," + data.id + "," + mTrackParam.nJpegQuality + "," + req1.downloadHandler.data.Length + "," + latency2;
                        mEvalManager.mLatencyTask.AddMessage(res_latency2);
                    }

                    handle.Free();
                }
                catch (Exception e)
                {
                    mText.text = "Update Local Map Error = " + e.ToString();
                }

                if (mEvalManager.bLatency && mExParam.bEdgeBase)
                {
                    var time = DateTime.UtcNow;
                    var timeSpan = time - data.receivedTime;
                    double ts = timeSpan.TotalMilliseconds;
                    string res = "base local map,download," + data.id + ",-1," + req1.downloadHandler.data.Length + "," + ts;
                    mEvalManager.mLatencyTask.AddMessage(res);
                }

            }
        }
        if(data.keyword == "ObjectDetection")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] temp = new float[4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, temp, 0, 4);
                mObjectDetection.Receive(temp);
            }
        }
        if (data.keyword == "ShareSemanticInfo")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                if (mEvalManager.bLatency)
                {
                    var time = DateTime.UtcNow;
                    var timeSpan = time - data.receivedTime;
                    double ts = timeSpan.TotalMilliseconds;
                    string res = "virtualobject,download," + data.id + ",-1," + ts;
                    mEvalManager.mLatencyTask.AddMessage(res);
                }
                float[] temp = new float[4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, temp, 0, 4);
                int N = (int)temp[0];
                float[] fdata = new float[2*N];
                int idx = fdata.Length * 4+4;
                Buffer.BlockCopy(req1.downloadHandler.data, 4, fdata, 0, fdata.Length*4);

                int Nimg = req1.downloadHandler.data.Length - idx;
                byte[] bdata = new byte[Nimg];
                Buffer.BlockCopy(req1.downloadHandler.data, idx, bdata, 0,Nimg);

                DataTransferEvent.RunEvent(new DataTransferEventArgs(data.id, N, ref bdata, ref fdata));
                //포인트 개수 읽고
                //포인트
                //이미지 디코딩
            }
        }
        
        if (data.keyword == "PlaneLine") //나중에 키워드 변경
        {

            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();
            //1~12까지가 포즈 정보임.
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                mPlaneManager.UpdateLocalPlane(data.id, fdata);
            }
        }
        if (data.keyword == "LocalContent") //나중에 키워드 변경
        {

            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();   
            
            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                mContentManager.UpdateVirtualFrame(data.id,ref fdata, data.receivedTime, mExParam.bEdgeBase);
            }
        }
        if (data.keyword == "VO.MARKER.CREATED") //나중에 키워드 변경
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();

            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                mSpatialTest.AttachObject(data.id, fdata);
            }
        }
        if (data.keyword == "MarkerRegistrations") //나중에 키워드 변경
        {

            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();

            if (req1.result == UnityWebRequest.Result.Success)
            {
                float[] fdata = new float[req1.downloadHandler.data.Length / 4];
                Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
                //int N = (int)fdata[0];
                //int idx = 1;
                //for (int j = 0; j < N; j++)
                //{
                //    int id = (int)fdata[idx];
                //    float a = fdata[idx + 1];
                //    float b = fdata[idx + 2];
                //    float x = fdata[idx + 3];
                //    float y = fdata[idx + 4];
                //    float z = fdata[idx + 5];
                //    float ex = fdata[idx + 6];
                //    float ey = fdata[idx + 7];
                //    float ez = fdata[idx + 8];

                //    idx += 9;
                //    mContentManager.Process(id, x, y, z);
                //}

            }
        }
        
        if(data.keyword == "GetCloudAnchor")
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            yield return req1.SendWebRequest();

            if (req1.result == UnityWebRequest.Result.Success)
            {
                string str = Encoding.Default.GetString(req1.downloadHandler.data);
                AnchorListenEvent.RunEvent(new AnchorListenArgs(data.id, str));
            }
        }
        if (data.keyword == "VO.MOVE") //나중에 키워드 변경
        {
            UnityWebRequest req1;
            req1 = GetRequest(data.keyword, data.id);
            DateTime t1 = DateTime.Now;
            mContentManager.Move(data.id);
            yield return null;
        }
        
        if (data.keyword == "dr") //나중에 키워드 변경
        {
            DateTime t1 = DateTime.Now;
            DirectLatencyEvent.RunEvent(new LatencyEventArgs(data.id, t1));
            yield return null;
        }
        if (data.keyword == "ir") //나중에 키워드 변경
        {
            DateTime t1 = DateTime.Now;
            IndirectLatencyEvent.RunEvent(new LatencyEventArgs(data.id, t1));
            yield return null;
        }
        
        yield break;
    }
    IEnumerator MessageParsingWithNDK(UdpData data)
    {
        float t = 0f;
        int N = 0;
        int len = data.length;

        float[] fdata = new float[len/4];
        GCHandle handle = GCHandle.Alloc(fdata, GCHandleType.Pinned);
        IntPtr addr = handle.AddrOfPinnedObject();
        DownloadData(data.id, data.keyword.ToCharArray(), data.keyword.Length, data.src.ToCharArray(), data.src.Length, addr, ref N, ref t);
        mText.text = fdata.Length + " " + N;
        if (data.keyword == "ReferenceFrame")
        {
            try
            {
                //GCHandle handle = GCHandle.Alloc(fdata, GCHandleType.Pinned);
                //IntPtr ptr = handle.AddrOfPinnedObject();
                mTracker.CreateKeyFrame(data.id, addr);
                //handle.Free();

                int Nmp = (int)fdata[0];
                PointCloudReceivedEvent.RunEvent(new PointCloudReceivedEventArgs(data.id, Nmp, fdata));
            }
            catch (Exception e)
            {
                mText.text = e.ToString();
            }
            //yield return null;
        }
        if (data.keyword == "LocalContent") //나중에 키워드 변경
        {
            //float[] fdata = new float[req1.downloadHandler.data.Length / 4];
            //Buffer.BlockCopy(req1.downloadHandler.data, 0, fdata, 0, req1.downloadHandler.data.Length);
            mContentManager.UpdateVirtualFrame(data.id, ref fdata, data.receivedTime, mExParam.bEdgeBase);
        }

        handle.Free();
        yield return null;
    }
}