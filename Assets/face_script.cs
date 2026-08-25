using Newtonsoft.Json;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
public class face_script : MonoBehaviour
{

    public TextMeshProUGUI FAU_Text;
    public TextMeshProUGUI Response_Text;

    //readonly string topic_name = "/facial_input";

    ROSConnection ros;
    //float pub_period = 1.0f;
    [SerializeField] private double frequency;
    [SerializeField] private RawImage eyeTrackingImage;

    [SerializeField] private RosControllerPublisher rosControllerPublisher;
    [SerializeField] private WebRTC_ROS_Data webRTC_ROS_Data;

    double pub_period = 0.03333f;

    OVRFaceExpressions faceExpressions;
    OVREyeGaze left_eye, right_eye;

    private float time_elapsed, time_elapsed_eyes;
    private double Inner_brow_raiser;
    private double Outer_brow_raiser;
    private double Brow_lowerer;
    private double Upper_lid_raiser;
    private double Cheek_raiser;
    private double Lid_tightener;
    private double Nose_wrinkler;
    private double Upper_lip_raiser;
    private double Lip_corner_puller;
    private double Dimpler;
    private double Lip_corner_depressor;
    private double Chin_raiser;
    private double Lip_stretcher;
    private double Lip_tightener;
    private double Jaw_drop;
    //private double Lip_suck;
    private double Blink;

    private bool face_found = false;
    private bool msg_recv = false;

    private string to_do_response = "";

    // In Awake or Start, get the component reference
    void Start()
    {
        pub_period = 1.0 / frequency;
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
        if (cameraRig != null)
        {

            faceExpressions = cameraRig.GetComponentInChildren<OVRFaceExpressions>();
            if (faceExpressions != null)
            {
                Debug.Log("Found Face!");
                var eyes = cameraRig.GetComponentsInChildren<OVREyeGaze>();
                if (eyes.Length == 2)
                {
                    left_eye = eyes[0];
                    right_eye = eyes[1];
                    Debug.Log("Found Eyes!");
                }
                else
                {
                    Debug.Log("2 EyeGazes not found in children.");
                }
                if (rosControllerPublisher.isActiveAndEnabled)
                {
                    ros = ROSConnection.GetOrCreateInstance();
                    ros.RegisterPublisher<Float64MultiArrayMsg>("/facial_input");
                    ros.RegisterPublisher<Float64MultiArrayMsg>("/full_facial_input");
                    ros.RegisterPublisher<TransformMsg>("/left_eye_transform");
                    ros.RegisterPublisher<TransformMsg>("/right_eye_transform");
                    ros.RegisterPublisher<BoolMsg>("/left_eye_open");
                    ros.RegisterPublisher<BoolMsg>("/right_eye_open");
                    ros.RegisterPublisher<Int32MultiArrayMsg>("/right_eye_pixel");
                    ros.RegisterPublisher<Int32MultiArrayMsg>("/left_eye_pixel");

                    ros.Subscribe<StringMsg>("/emotion_string", ReceiveMessage);
                    Debug.Log("ROS Publisher & Subscriber Registered!");
                }
                else if (webRTC_ROS_Data.isActiveAndEnabled)
                {

                }
                
                FAU_Text.text = "Facial Data Not Found";

                Response_Text.text = "No Response Yet";
            }
            else
            {
                Debug.Log("No OVRFaceExpressions found in children.");
            }
        }
        else
        {
            Debug.Log("No OVRCameraRig Found!");
        }
    }

    // In Update, retrieve the data every frame
    void Update()
    {
        time_elapsed += Time.deltaTime;
        if (faceExpressions != null && faceExpressions.ValidExpressions)
        {
            if (!face_found)
            {
                Debug.Log("Got the face!");
                face_found = true;
            }

            if (time_elapsed > pub_period)
            {
                Inner_brow_raiser = (faceExpressions[OVRFaceExpressions.FaceExpression.InnerBrowRaiserL] + faceExpressions[OVRFaceExpressions.FaceExpression.InnerBrowRaiserR]) / 2.0;
                Outer_brow_raiser = (faceExpressions[OVRFaceExpressions.FaceExpression.OuterBrowRaiserL] + faceExpressions[OVRFaceExpressions.FaceExpression.OuterBrowRaiserR]) / 2.0;
                Brow_lowerer = (faceExpressions[OVRFaceExpressions.FaceExpression.BrowLowererL] + faceExpressions[OVRFaceExpressions.FaceExpression.BrowLowererR]) / 2.0;
                Upper_lid_raiser = (faceExpressions[OVRFaceExpressions.FaceExpression.UpperLidRaiserL] + faceExpressions[OVRFaceExpressions.FaceExpression.UpperLidRaiserR]) / 2.0;
                Cheek_raiser = (faceExpressions[OVRFaceExpressions.FaceExpression.CheekRaiserL] + faceExpressions[OVRFaceExpressions.FaceExpression.CheekRaiserR]) / 2.0;
                Lid_tightener = (faceExpressions[OVRFaceExpressions.FaceExpression.LidTightenerL] + faceExpressions[OVRFaceExpressions.FaceExpression.LidTightenerR]) / 2.0;
                Nose_wrinkler = (faceExpressions[OVRFaceExpressions.FaceExpression.NoseWrinklerL] + faceExpressions[OVRFaceExpressions.FaceExpression.NoseWrinklerR]) / 2.0;
                Upper_lip_raiser = (faceExpressions[OVRFaceExpressions.FaceExpression.UpperLipRaiserL] + faceExpressions[OVRFaceExpressions.FaceExpression.UpperLipRaiserR]) / 2.0;
                Lip_corner_puller = (faceExpressions[OVRFaceExpressions.FaceExpression.LipCornerPullerL] + faceExpressions[OVRFaceExpressions.FaceExpression.LipCornerPullerR]) / 2.0;
                Dimpler = (faceExpressions[OVRFaceExpressions.FaceExpression.DimplerL] + faceExpressions[OVRFaceExpressions.FaceExpression.DimplerR]) / 2.0;
                Lip_corner_depressor = (faceExpressions[OVRFaceExpressions.FaceExpression.LipCornerDepressorL] + faceExpressions[OVRFaceExpressions.FaceExpression.LipCornerDepressorR]) / 2.0;
                Chin_raiser = (faceExpressions[OVRFaceExpressions.FaceExpression.ChinRaiserB] + faceExpressions[OVRFaceExpressions.FaceExpression.ChinRaiserT]) / 2.0;
                Lip_stretcher = (faceExpressions[OVRFaceExpressions.FaceExpression.LipStretcherL] + faceExpressions[OVRFaceExpressions.FaceExpression.LipStretcherR]) / 2.0;
                Lip_tightener = (faceExpressions[OVRFaceExpressions.FaceExpression.LipTightenerL] + faceExpressions[OVRFaceExpressions.FaceExpression.LipTightenerR]) / 2.0;
                Jaw_drop = faceExpressions[OVRFaceExpressions.FaceExpression.JawDrop];
                //Lip_suck = (faceExpressions[OVRFaceExpressions.FaceExpression.LipSuckLB] + faceExpressions[OVRFaceExpressions.FaceExpression.LipSuckLT] +
                //    faceExpressions[OVRFaceExpressions.FaceExpression.LipSuckRB] + faceExpressions[OVRFaceExpressions.FaceExpression.LipSuckRT]) / 4.0;
                Blink = (faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedL] + faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedR]) / 2.0;

                double[] data = new double[]
                {
                            Outer_brow_raiser,
                            Brow_lowerer,
                            Upper_lid_raiser,
                            Cheek_raiser,
                            Lid_tightener,
                            Nose_wrinkler,
                            Upper_lip_raiser,
                            Lip_corner_puller,
                            Dimpler,
                            Lip_corner_depressor,
                            Chin_raiser,
                            Lip_stretcher,
                            Lip_tightener,
                            Jaw_drop,
                            //Lip_suck,
                            Blink
                };

                if (rosControllerPublisher.isActiveAndEnabled)
                {
                    Float64MultiArrayMsg array = new()
                    {
                        layout = new MultiArrayLayoutMsg(),
                        data = new double[] {Inner_brow_raiser,
                        }
                    };

                    Float64MultiArrayMsg new_array = new()
                    {
                        layout = new MultiArrayLayoutMsg(),
                        data = faceExpressions.Select(x => (double)x).ToArray()
                    };

                    ros.Publish("/facial_input", array);
                    ros.Publish("/full_facial_data", new_array);
                    ros.Publish("/left_eye_open", new BoolMsg(faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedL] < 0.8));
                    ros.Publish("/right_eye_open", new BoolMsg(faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedR] < 0.8));
                    Debug.Log("Published Valid Face!");
                }
                else if (webRTC_ROS_Data.isActiveAndEnabled)
                {
                    std_msgs_ArbitraryMultiArray<double> array = new()
                    {
                        data = data,
                        layout = new std_msgs_MultiArrayLayout()
                    };

                    std_msgs_ArbitraryMultiArray<double> new_array = new()
                    {
                        layout = new std_msgs_MultiArrayLayout(),
                        data = faceExpressions.Select(x => (double)x).ToArray()
                    };

                    webRTC_ROS_Data.send_message("/facial_input", "std_msgs/Float64MultiArray", JsonConvert.SerializeObject(array));
                    webRTC_ROS_Data.send_message("/full_facial_input", "std_msgs/Float64MultiArray", JsonConvert.SerializeObject(new_array));
                    webRTC_ROS_Data.send_message("/left_eye_open", "std_msgs/Bool", JsonConvert.SerializeObject(new std_msgs_bool() {data = faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedL] < 0.8 }));
                    webRTC_ROS_Data.send_message("/right_eye_open", "std_msgs/Bool", JsonConvert.SerializeObject(new std_msgs_bool() { data = faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedR] < 0.8 }));
                }

                string formattedString = "[";
                for (int i = 0; i < data.Length; i++)
                {
                    formattedString += string.Format("{0:0.000}", data[i]);
                    if (i < data.Length - 1)
                    {
                        formattedString += ", ";
                    }
                }
                formattedString += "]";

                FAU_Text.text = "FAUs: " + formattedString;

                time_elapsed = 0f;
            }
        }
        else
        {
            if (time_elapsed > pub_period)
            {
                time_elapsed = 0f;
                if (face_found)
                {
                    Debug.Log("Face Lost!");
                    FAU_Text.text = "Face Lost!";
                    face_found = false;
                }
            }
        }

        if (left_eye != null && left_eye.isActiveAndEnabled && right_eye != null & right_eye.isActiveAndEnabled)
        {
            time_elapsed_eyes += Time.deltaTime;

            if (time_elapsed_eyes > pub_period)
            {
                var left_eye_trans = left_eye.transform;
                var right_eye_trans = right_eye.transform;

                TransformMsg left_trans = new()
                {
                    rotation = new QuaternionMsg
                    {
                        x = left_eye_trans.rotation.x,
                        y = left_eye_trans.rotation.y,
                        z = left_eye_trans.rotation.z,
                        w = left_eye_trans.rotation.w,
                    },
                    translation = new Vector3Msg
                    {
                        x = left_eye_trans.position.x,
                        y = left_eye_trans.position.y,
                        z = left_eye_trans.position.z
                    }
                };

                TransformMsg right_trans = new()
                {
                    rotation = new QuaternionMsg
                    {
                        x = right_eye_trans.rotation.x,
                        y = right_eye_trans.rotation.y,
                        z = right_eye_trans.rotation.z,
                        w = right_eye_trans.rotation.w,
                    },
                    translation = new Vector3Msg
                    {
                        x = right_eye_trans.position.x,
                        y = right_eye_trans.position.y,
                        z = right_eye_trans.position.z
                    }
                };

                Vector2? leftPixel = GetGazePixel(left_eye);
                Vector2? rightPixel = GetGazePixel(right_eye);

                if (rosControllerPublisher.isActiveAndEnabled)
                {
                    ros.Publish("/left_eye_transform", left_trans);
                    ros.Publish("/right_eye_transform", right_trans);

                    if (leftPixel.HasValue)
                    {
                        ros.Publish("/left_eye_pixel", new Int32MultiArrayMsg()
                        {
                            data = new Int32[] { (Int32)leftPixel.Value.x, (Int32)leftPixel.Value.y },
                            layout = new MultiArrayLayoutMsg()
                        });
                        Debug.Log($"Left eye looking at pixel: {leftPixel.Value}");
                    }

                    if (rightPixel.HasValue)
                    {
                        ros.Publish("/right_eye_pixel", new Int32MultiArrayMsg()
                        {
                            data = new Int32[] { (Int32)rightPixel.Value.x, (Int32)rightPixel.Value.y },
                            layout = new MultiArrayLayoutMsg()
                        });
                        Debug.Log($"Right eye looking at pixel: {rightPixel.Value}");
                    }

                    if (leftPixel.HasValue && rightPixel.HasValue)
                    {
                        ros.Publish("/eye_pixels", new Int32MultiArrayMsg()
                        {
                            data = new Int32[] { (Int32)leftPixel.Value.x, (Int32)leftPixel.Value.y, (Int32)rightPixel.Value.x, (Int32)rightPixel.Value.y },
                            layout = new MultiArrayLayoutMsg()
                        });
                    }
                }
                else if (webRTC_ROS_Data.isActiveAndEnabled)
                {
                    webRTC_ROS_Data.send_message("/left_eye_transform", "geometry_msgs/Transform", JsonConvert.SerializeObject(new geometry_msgs_Transform()
                    {
                        translation = new geometry_msgs_Point() { x = left_trans.translation.x, y = left_trans.translation.y, z = left_trans.translation.z },
                        rotation = new geometry_msgs_Quaternion() { x = left_trans.rotation.x, y = left_trans.rotation.y, z = left_trans.rotation.z },
                    }));
                    webRTC_ROS_Data.send_message("/right_eye_transform", "geometry_msgs/Transform", JsonConvert.SerializeObject(new geometry_msgs_Transform()
                    {
                        translation = new geometry_msgs_Point() { x = right_trans.translation.x, y = right_trans.translation.y, z = right_trans.translation.z },
                        rotation = new geometry_msgs_Quaternion() { x = right_trans.rotation.x, y = right_trans.rotation.y, z = right_trans.rotation.z },
                    }));
                    if (leftPixel.HasValue)
                    {
                        webRTC_ROS_Data.send_message("/left_eye_pixel", "std_msgs/Int32MultiArray", JsonConvert.SerializeObject(
                            new std_msgs_ArbitraryMultiArray<Int32>()
                            {
                                data = new Int32[] { (Int32)leftPixel.Value.x, (Int32)leftPixel.Value.y },
                                layout = new std_msgs_MultiArrayLayout()
                            }));
                        Debug.Log($"Left eye looking at pixel: {leftPixel.Value}");
                    }

                    if (rightPixel.HasValue)
                    {
                        webRTC_ROS_Data.send_message("/right_eye_pixel", "std_msgs/Int32MultiArray", JsonConvert.SerializeObject(
                            new std_msgs_ArbitraryMultiArray<Int32>()
                            {
                                data = new Int32[] { (Int32)rightPixel.Value.x, (Int32)rightPixel.Value.y },
                                layout = new std_msgs_MultiArrayLayout()
                            }));
                        Debug.Log($"Right eye looking at pixel: {rightPixel.Value}");
                    }

                    if (leftPixel.HasValue && rightPixel.HasValue)
                    {
                        webRTC_ROS_Data.send_message("/eye_pixels", "std_msgs/Int32MultiArray", JsonConvert.SerializeObject(
                            new std_msgs_ArbitraryMultiArray<Int32>()
                            {
                                data = new Int32[] { (Int32)leftPixel.Value.x, (Int32)leftPixel.Value.y, (Int32)rightPixel.Value.x, (Int32)rightPixel.Value.y },
                                layout = new std_msgs_MultiArrayLayout()
                            }));
                    }

                }
                time_elapsed_eyes = 0f;
            }
        }
        if (msg_recv)
        {
            Response_Text.text = to_do_response;
            msg_recv = false;
        }
    }

    private Vector2? GetGazePixel(OVREyeGaze eye)
    {
        if (eye == null || eye.transform == null)
            return null;

        RectTransform rectTransform = eyeTrackingImage.rectTransform;

        // Eye position and gaze direction
        Vector3 rayOrigin = eye.transform.position;
        Vector3 rayDirection = eye.transform.forward;

        // Plane corresponding to the RawImage
        Plane imagePlane = new Plane(
            rectTransform.forward,
            rectTransform.position
        );

        // Find intersection between gaze ray and image plane
        Ray gazeRay = new Ray(rayOrigin, rayDirection);

        if (!imagePlane.Raycast(gazeRay, out float distance))
            return null;

        Vector3 hitPoint = gazeRay.GetPoint(distance);

        // Convert world position into RawImage local coordinates
        Vector3 localPoint = rectTransform.InverseTransformPoint(hitPoint);

        Rect rect = rectTransform.rect;

        // Convert local position to 0-1 coordinates
        float normalizedX = Mathf.InverseLerp(
            rect.xMin,
            rect.xMax,
            localPoint.x
        );

        float normalizedY = Mathf.InverseLerp(
            rect.yMin,
            rect.yMax,
            localPoint.y
        );

        // Outside the RawImage
        if (normalizedX < 0f || normalizedX > 1f ||
            normalizedY < 0f || normalizedY > 1f)
        {
            return null;
        }

        // Convert to pixel coordinates
        int pixelX = Mathf.FloorToInt(
            normalizedX * eyeTrackingImage.texture.width
        );

        int pixelY = Mathf.FloorToInt(
            normalizedY * eyeTrackingImage.texture.height
        );

        return new Vector2(pixelX, pixelY);
    }

    void ReceiveMessage(StringMsg MsgData)
    {

        to_do_response = MsgData.data;
        msg_recv = true;
    }

}
