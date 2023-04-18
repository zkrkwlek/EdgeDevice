using Google.XR.ARCoreExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;

[Serializable]
public class VideoParam
{
    public bool bRecorded;
    public bool bPlayed;
    public bool bShowLog;
    [NonSerialized] public string video_path;
    public string filename;
    public List<VideoData> videoLists;
    //public VideoLists List;
}
[Serializable]
public class VideoData
{
    public string file;
    public bool bPlay;
    public VideoData()
    {
        bPlay = false;
    }
    public VideoData(string _file):this()
    {
        file = _file;
    }


}


public class VideoManager : MonoBehaviour
{
    string dirPath, filename;
    VideoParam param;
    public ARSession session;
    ArSession subsession;
    //ARRecordingManager recordingManager;
    //ARPlaybackManager playbackManager;

    ARCoreSessionSubsystem subsystem;
    ArPlaybackStatus playbackStatus;
    ArRecordingStatus recordingStatus;

    bool setPlaybackDataset;

    public Text mText;

    bool WantsToQuit()
    {
        if (param.bRecorded)
            StopRecord();
        else if (param.bPlayed)
            StopPlayVideo();
        File.WriteAllText(filename, JsonUtility.ToJson(param));
        return true;
    }
    void OnEnable()
    {
        
    }
    void OnDisable()
    {
        
    }

    void Awake()
    {
        //VideoParam testParam = new VideoParam();
        //testParam.videoLists = new List<VideoData>();
        //VideoData aaa = new VideoData("ori.mp4");
        //VideoData bbb = new VideoData("asdf.mp4");
        //bbb.bPlay = true;
        //testParam.videoLists.Add(aaa);
        //testParam.videoLists.Add(bbb);
        //Debug.Log(JsonUtility.ToJson(testParam));

        //파라메터 로드
        dirPath = Application.persistentDataPath + "/data/Param";
        filename = dirPath + "/VideoManager.json";
        
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        var dirPath2 = Application.persistentDataPath + "/data/video";
        try
        {
            string strAddData = File.ReadAllText(filename);
            param = JsonUtility.FromJson<VideoParam>(strAddData);
            if(param.bPlayed && !param.bRecorded)
            {
                foreach (VideoData vdata in param.videoLists)
                {
                    if (vdata.bPlay)
                    {
                        param.video_path = dirPath2 + "/" + vdata.file;
                        break;
                    }
                }
            }
            if (param.bRecorded)
            {
                VideoData vdata = new VideoData(param.filename);
                param.video_path = dirPath2 + "/" + vdata.file;
                param.videoLists.Add(vdata);
            }
        }
        catch (Exception e)
        {
            if (!Directory.Exists(dirPath2))
            {
                Directory.CreateDirectory(dirPath2);
            }
            param = new VideoParam();
            param.video_path = dirPath2 + "/video.mp4";
            param.filename = "video.mp4";
            param.videoLists = new List<VideoData>();
            DirectoryInfo di = new DirectoryInfo(dirPath2);
            foreach (FileInfo file in di.GetFiles()) {
                string name = file.Name;
                VideoData temp = new VideoData(name);
                param.videoLists.Add(temp);
            }
        }
        Application.wantsToQuit += WantsToQuit;
        //파라메터 로드
    }

    int GetRotation() => Screen.orientation switch
    {
        ScreenOrientation.Portrait => 0,
        ScreenOrientation.LandscapeLeft => 90,
        ScreenOrientation.PortraitUpsideDown => 180,
        ScreenOrientation.LandscapeRight => 270,
        _ => 0
    };
    bool bStart = false;
    // Start is called before the first frame update
    void Start()
    {
        //try{

        //    if (session.subsystem is ARCoreSessionSubsystem _subsystem)
        //    {
        //        subsystem = _subsystem;
        //        subsession = subsystem.session;
        //        if (subsession == null)
        //        {
        //            if (param.bShowLog)
        //                mText.text = "fail set subsystem";
        //            return;
        //        }
                    
        //        playbackStatus = subsystem.playbackStatus;
        //        recordingStatus = subsystem.recordingStatus;

        //        if (param.bShowLog)
        //            mText.text = "success set subsystem";
        //    }
        //    else
        //    {
        //        if (param.bShowLog)
        //            mText.text = "fail set subsystem";
        //        return;
        //    }
                


        //    if (param.bRecorded)
        //    {
        //        StartRecord();
        //    }else if (param.bPlayed)
        //    {
        //        SetPlayVideo();
        //    }
        //}
        //catch(Exception e)
        //{
        //    mText.text = e.ToString();
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (!bStart  && (param.bPlayed || param.bRecorded) && session.subsystem is ARCoreSessionSubsystem _subsystem)
        {
            subsystem = _subsystem;
            subsession = subsystem.session;
            if (subsession == null)
            {
                if (param.bShowLog)
                    mText.text = "fail set subsystem";
                return;
            }

            playbackStatus = subsystem.playbackStatus;
            recordingStatus = subsystem.recordingStatus;

            if (param.bShowLog)
                mText.text = "success set subsystem";

            bStart = true;
            if (param.bRecorded)
            {
                StartRecord();
            }
            else if (param.bPlayed)
            {
                SetPlayVideo();
            }
        }

    }

    void StartRecord()
    {
        try {
            var config = new ArRecordingConfig(subsession);

            config.SetMp4DatasetFilePath(subsession, param.video_path);
            config.SetRecordingRotation(subsession, GetRotation());
            var status = subsystem.StartRecording(config);

            if (param.bShowLog)
                mText.text = "Start recordning";
        }
        catch(Exception e)
        {
            mText.text = e.ToString();
        }
    }
    void StopRecord()
    {
        subsystem.StopRecording();   
        //recordingManager.StopRecording();
    }
    void SetPlayVideo() {
        try
        {
            subsystem.StartPlayback(param.video_path);
        }
        catch (Exception e)
        {
            mText.text = e.ToString();
        }
    }
    void StopPlayVideo()
    {
        subsystem.StopPlayback();
    }
}
