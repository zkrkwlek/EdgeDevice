using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoseManager : MonoBehaviour
{
    public bool mbRunMode; //true이면 UVR, false이면 ARCore 추후 스위치로 변경
    Dictionary<int, Transform> dictCamPose;
    Dictionary<int, bool> bTrackingResults;

    // Start is called before the first frame update
    void Awake()
    {
        dictCamPose = new Dictionary<int, Transform>();
        bTrackingResults = new Dictionary<int, bool>();
        mbRunMode = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public bool CheckPose(int fid)
    {
        return dictCamPose.ContainsKey(fid);
    }
    public Transform GetPose(int fid)
    {
        return dictCamPose[fid];
    }

    public void AddPose(int fid, Transform trans)
    {
        dictCamPose.Add(fid, trans);
    }

    public Transform GetPose(int fid, out bool bTracking)
    {
        bTracking = bTrackingResults[fid];
        return dictCamPose[fid];
    }

    public void AddPose(int fid, Transform trans, bool bTracking)
    {
        bTrackingResults.Add(fid, bTracking);
        dictCamPose.Add(fid, trans);
    }
}
