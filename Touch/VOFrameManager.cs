using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualFrame
{
    public int fid;
    public bool bEnqueue;
    List<Content> ContentList;
    List<UVR_Plane> PlaneList;
    public VirtualFrame(int id)
    {
        fid = id;
        bEnqueue = false;
        ContentList = new List<Content>();
        PlaneList = new List<UVR_Plane>();
    }
    public void AddContent(Content c)
    {
        //if (!ContentList.Contains(c))
        //{

        //}
        ContentList.Add(c);
        c.nObservation++;
    }

    public void AddPlane(UVR_Plane p)
    {
        PlaneList.Add(p);
        p.nObservation++;
    }

    public void RemoveObservation()
    {
        foreach (Content c in ContentList)
        {
            c.nObservation--;
        }
        foreach (UVR_Plane p in PlaneList)
        {
            p.nObservation--;
        }
    }
}

public class VOFrameManager : MonoBehaviour
{
    public Queue<VirtualFrame> VirtualFrameQueue;
    public Dictionary<int, VirtualFrame> DictVOFrame;

    void Awake()
    {
        VirtualFrameQueue = new Queue<VirtualFrame>();
        DictVOFrame = new Dictionary<int, VirtualFrame>();
    }

    bool CheckFrame(int id) {
        return DictVOFrame.ContainsKey(id);
    }
    
    VirtualFrame CreateFrame(int id)
    {
        var kf = new VirtualFrame(id);
        DictVOFrame.Add(id, kf);
        return kf;
    }

    void RemoveFrame()
    {
        var kf = VirtualFrameQueue.Dequeue();
        DictVOFrame.Remove(kf.fid);
        kf.RemoveObservation();
    }

    public void AddFrame(VirtualFrame vf)
    {
        if (!vf.bEnqueue)
        {
            vf.bEnqueue = true;
            VirtualFrameQueue.Enqueue(vf);
            if(VirtualFrameQueue.Count > 5)
            {
                RemoveFrame();
            }
        }
    }

    public VirtualFrame GetFrame(int id)
    {
        if(CheckFrame(id))
        {
            return DictVOFrame[id];
        }
        else
        {
            return CreateFrame(id);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
