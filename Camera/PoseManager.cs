using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseManager : MonoBehaviour
{
    public bool mbRunMode; //true이면 UVR, false이면 ARCore 추후 스위치로 변경
    Dictionary<int, Transform> dictCamPose;
    // Start is called before the first frame update
    void Awake()
    {
        dictCamPose = new Dictionary<int, Transform>();
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
}
