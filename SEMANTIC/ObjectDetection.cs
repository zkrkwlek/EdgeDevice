using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectDetection : MonoBehaviour
{
    public ParameterManager mParamManager;
    public Text mText;
    ExperimentParam param;

    int width;
    int height;
    float widthScale;
    float heightScale;
    string[] ObjectLabel;

    bool WantsToQuit()
    {
        return true;
    }
    void Awake()
    {
        param = (ExperimentParam)mParamManager.DictionaryParam["Experiment"];
        if(!param.bObjectDetection)
        {
            enabled = false;
            return;
        }
        string strYoloObjectLabel = "person,bicycle,car,motorcycle,airplane,bus,train,truck,boat,traffic light,fire hydrant,stop sign,parking meter,bench,bird,cat,dog,horse,sheep,cow,elephant,bear,zebra,giraffe,backpack,umbrella,handbag,tie,suitcase,frisbee,skis,snowboard,sports ball,kite,baseball bat,baseball glove,skateboard,surfboard,tennis racket,bottle,wine glass,cup,fork,knife,spoon,bowl,banana,apple,sandwich,orange,broccoli,carrot,hot dog,pizza,donut,cake,chair,couch,potted plant,bed,dining table,toilet,tv,laptop,mouse,remote,keyboard,cell phone,microwave,oven,toaster,sink,refrigerator,book,clock,vase,scissors,teddy bear,hair drier,toothbrush";
        ObjectLabel = strYoloObjectLabel.Split(',');
    }
    void OnEnable()
    {
        CameraInitEvent.camInitialized += OnCameraInitialization;
    }
    void OnDisable()
    {
        CameraInitEvent.camInitialized -= OnCameraInitialization;
    }

    void OnCameraInitialization(object sender, CameraInitEventArgs e)
    {
        width = e.width;
        height = e.height;
        widthScale = e.widthScale;
        heightScale = e.heightScale;
    }

    public void Receive(float[] fdata)
    {
        int N = fdata.Length / 6;

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
