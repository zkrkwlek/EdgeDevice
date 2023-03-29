using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseManager : MonoBehaviour
{
    public bool mbRunMode; //true�̸� UVR, false�̸� ARCore ���� ����ġ�� ����
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
