using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Sensor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;
using RosImage = RosMessageTypes.Sensor.CompressedImageMsg;


public class RosSubscriberScript : MonoBehaviour
{
    // Start is called before the first frame update
    public MeshRenderer meshRenderer;

    public Camera input_camera = null;

    // public TMP_Text text;
    private Texture2D texture2D;
    private byte[] image, data;
    private bool isMessageReceived = false;
    private bool doOnce = false;
    
    ROSConnection ros;

    float publishMessageFrequency = 0.01f;
    private float timeElapsed;

    void Start()
    {
        // texture2D = new Texture2D(2, 2);
        meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        //Initiate ros connection and subscribe to image topic
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<RosImage>("/camera/color/image_raw/compressed", ReceiveMessage);
        ros.Subscribe<RosImage>("/CAMERA_HEAD_L/image_raw/compressed", ReceiveMessage);
        ros.Subscribe<RosImage>("/head_front_camera/color/image_raw/compressed", ReceiveMessage);
        if (input_camera != null)
        {
            ros.RegisterPublisher<RosImage>("/Face_Image");
        }
    }

    // // Update is called once per frame
    void Update()
    {
        if (isMessageReceived) {
            ProcessMessage();
        }
        if (input_camera != null)
        {
            timeElapsed += Time.deltaTime;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (timeElapsed > publishMessageFrequency)
            {

                var oldRT = RenderTexture.active;
                RenderTexture.active = input_camera.targetTexture;
                input_camera.Render();

                // Copy the pixels from the GPU into a texture so we can work with them
                // For more efficiency you should reuse this texture, instead of creating and disposing them every time
                Texture2D camText = new Texture2D(input_camera.targetTexture.width, input_camera.targetTexture.height);
                camText.ReadPixels(new Rect(0, 0, input_camera.targetTexture.width, input_camera.targetTexture.height), 0, 0);
                camText.Apply();
                RenderTexture.active = oldRT;

                RosMessageTypes.Std.HeaderMsg header = new RosMessageTypes.Std.HeaderMsg()
                {
                    frame_id = "base_link",
                    stamp = new TimeMsg
                    {
                        sec = (int)now.ToUnixTimeSeconds(),
                        nanosec = (uint)((now.Ticks % 10000000) * 100)
                    }
                };

                // Encode the texture as an ImageMsg, and send to ROS
                ImageMsg imageMsg = camText.ToImageMsg(header);

                ros.Publish("/Face_Image", imageMsg);
            }
        }
    }

    void ReceiveMessage(RosImage imageData)
    {
        //create texture based on image data
        if(!doOnce){
            texture2D = new Texture2D(2,2);
            doOnce = !doOnce;
        }

        image = imageData.data;
        isMessageReceived = true;
        // UnityEngine.Debug.Log(image.Length.ToString());
    }

    void ProcessMessage()
    {
        //load image to the texture and set the image as the texture
        //This will change if the data used is compressed
        texture2D.LoadImage(image); //for when image is compressed
        //texture2D.LoadRawTextureData(image);
        texture2D.Apply(); //send to gpu?
        meshRenderer.material.SetTexture("_BaseMap", texture2D);
        meshRenderer.material.SetFloat("_Smoothness",0.0f);
        isMessageReceived = false;
    }


}
