using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UVR_Plane
{
    public Plane plane;
    public int mnTTL;
    public int mnId;
    public UVR_Plane(int id, float x, float y, float z, float d, int _skip = 4)
    {
        mnId = id;
        Vector3 normal = new Vector3(x, y, z);
        plane = new Plane(normal, d);
        mnTTL = _skip;
    }
    public Vector3 CreatePoint(Vector3 origin, Vector3 dir) {
        float a = Vector3.Dot(plane.normal, -dir);
        float u = (Vector3.Dot(plane.normal, origin)+plane.distance)/a;
        return origin + dir * u;

        //cv::Mat x3D = cv::Mat::ones(1, 3, CV_32FC1);
        //x3D.at<float>(0) = x;
        //x3D.at<float>(1) = y;

        //cv::Mat R, t;
        //pCameraPose->GetPose(R, t);

        //cv::Mat Xw = pCamera->Kinv * x3D.t();
        //Xw.push_back(cv::Mat::ones(1, 1, CV_32FC1)); //3x1->4x1
        //Xw = pCameraPose->GetInversePose() * Xw; // 4x4 x 4 x 1
        //float testaaasdf = Xw.at<float>(3);
        //Xw = Xw.rowRange(0, 3) / Xw.at<float>(3); // 4x1 -> 3x1
        //cv::Mat Ow = pCameraPose->GetCenter(); // 3x1
        //cv::Mat dir = Xw - Ow; //3x1

        //bool bres = false;
        //auto planes = LocalMapPlanes.Get();
        //float min_val = 10000.0;
        //cv::Mat min_param;
        //for (auto iter = planes.begin(), iend = planes.end(); iter != iend; iter++)
        //{
        //    cv::Mat param = iter->second; //4x1
        //    cv::Mat normal = param.rowRange(0, 3); //3x1
        //    float dist = param.at<float>(3);
        //    float a = normal.dot(-dir);
        //    if (std::abs(a) < 0.000001)
        //        continue;
        //    float u = (normal.dot(Ow) + dist) / a;
        //    if (u > 0.0 && u < min_val)
        //    {
        //        min_val = u;
        //        min_param = param;
        //    }

        //}
        //if (min_val < 10000.0)
        //{
        //    _pos = Ow + dir * min_val;
        //    bres = true;
        //}

    }
}

public class PlaneEventArgs : EventArgs
{
    public PlaneEventArgs(Plane p)
    {
        plane = p;
    }
    public Plane plane { get; set; }
}

class PlaneDetectionEvent
{
    public static event EventHandler<PlaneEventArgs> planeDetected;
    public static void RunEvent(PlaneEventArgs e)
    {
        if (planeDetected != null)
        {
            planeDetected(null, e);
        }
    }
}

public class PlaneManager : MonoBehaviour
{
    /// <summary>
    /// 실험용 임시
    /// </summary>
    public Text mText;
    //Dictionary<int, UVR_Plane> Planes;
    Dictionary<int, UVR_Plane> Planes;

    // Start is called before the first frame update
    void Awake()
    {
        //Planes = new Dictionary<int, UVR_Plane>();
        Planes = new Dictionary<int, UVR_Plane>();
        //Pfloor = new UVR_Plane(0, 0f,0f,0f,0f);
    }

    // Update is called once per frame
    void Update()
    {
        //var planes = Planes.Values;
        ////TTL 관리
        List<int> removePlaneIDs = new List<int>(); 
        foreach(UVR_Plane p in Planes.Values)
        {
            p.mnTTL--;
            if(p.mnTTL <= 0)
            {
                //삭제 및 종료
                if(p.mnId != 0 && p.mnId == 1)
                    removePlaneIDs.Add(p.mnId);
            }
        }
        for (int i = 0; i < removePlaneIDs[i]; i++)
            Planes.Remove(i);
    }

    public void AddPlane(int id, float x, float y, float z, float d, int _skip = 5) {
        var p = new UVR_Plane(id, x, y, z, d, _skip);
        Planes.Add(id, p);
        //Planes.Add(p);
    }
    public bool CheckPlane(int id)
    {
        return Planes.ContainsKey(id);
    }
    public void UpdatePlane(int id, float x, float y, float z, float d)
    {
        y = -y;
        if (!CheckPlane(id))
        {
            AddPlane(id, x, y, z,d);
        }
        else
        {
            var p = Planes[id];
            p.mnTTL += 5;
            p.plane.normal = new Vector3(x, y, z);
            p.plane.distance = d;
        }
        ////Pfloor.plane.normal = new Vector3(x, y, z);
        ////Pfloor.plane.distance = d;

        //////PlaneEventArgs args = new PlaneEventArgs();
        //////args.plane = Pfloor.plane;
        ////PlaneDetectionEvent.RunEvent(new PlaneEventArgs(Pfloor.plane));
        //////mText.text = "plane detection event";
    }

    public bool FindNearestPlane(Ray ray, out int pid, out Plane plane, out float min_dist)
    {
        bool bRay = false;
        min_dist = 1000f;
        plane = new Plane();
        pid = -1;
        foreach (UVR_Plane p in Planes.Values)
        {
            if (p.mnTTL < 0)
                continue;
            float dist = 10000f;
            p.plane.Raycast(ray, out dist);
            if(dist > 0 && dist < min_dist)
            {
                min_dist = dist;
                pid = p.mnId;
                plane = p.plane;
                bRay = true;
            }
        }
        return bRay;
    }

    
    ////가장 작은 애 찾기

    //추후 업데이트 필요함
}
