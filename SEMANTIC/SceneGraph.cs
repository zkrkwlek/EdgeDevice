using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SceneGraphAttr { None = 0, On = 1 }
public enum SceneGraphType { None = 0, Plane = 1, Object = 2 }


public class SceneGraphNode
{
    public SceneGraphNode(int _src, int _dst, SceneGraphAttr _attr, SceneGraphType _type)
    {
        srcID = _src;
        dstID = _dst;
        attr = _attr;
        type = _type;
        dstObj = null;
    }
    public bool isEqual(SceneGraphNode node)
    {
        return (this.attr == node.attr) && (this.dstID == node.dstID);
    }
    public GameObject dstObj;
    public int srcID;
    public int dstID;
    public SceneGraphAttr attr;
    public SceneGraphType type;
}
