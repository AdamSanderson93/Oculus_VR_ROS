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
                ros = ROSConnection.GetOrCreateInstance();
                ros.RegisterPublisher<Float64MultiArrayMsg>("/facial_input");
                ros.RegisterPublisher<Float64MultiArrayMsg>("/full_facial_input");
                ros.RegisterPublisher<TransformMsg>("/left_eye_transform");
                ros.RegisterPublisher<TransformMsg>("/right_eye_transform");
                ros.RegisterPublisher<BoolMsg>("/left_eye_open");
                ros.RegisterPublisher<BoolMsg>("/right_eye_open");
                ros.Subscribe<StringMsg>("/emotion_string", ReceiveMessage);
                Debug.Log("ROS Publisher & Subscriber Registered!");

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

                Float64MultiArrayMsg array = new()
                {
                    layout = new MultiArrayLayoutMsg(),
                    data = new double[] {Inner_brow_raiser,
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
                    }
                };
                

                string formattedString = "[";
                for (int i = 0; i < array.data.Length; i++)
                {
                    formattedString += string.Format("{0:0.000}", array.data[i]);
                    if (i < array.data.Length - 1)
                    {
                        formattedString += ", ";
                    }
                }
                formattedString += "]";

                FAU_Text.text = "FAUs: " + formattedString;

                //double[] weightsBuffer = new double[faceExpressions.Count];
                //float[] copyBuffer = new float[faceExpressions.Count];

                //faceExpressions.CopyTo(copyBuffer);

                //for (int i = 0; i < faceExpressions.Count; i++)
                //{
                //    weightsBuffer[i] = copyBuffer[i];
                //}

                Float64MultiArrayMsg new_array = new()
                {
                    layout = new MultiArrayLayoutMsg(),
                    data = faceExpressions.Select(x => (double)x).ToArray()
                };

                ros.Publish("/facial_input", array);
                ros.Publish("/full_facial_input", new_array);
                ros.Publish("/left_eye_open", new BoolMsg(faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedL] < 0.8));
                ros.Publish("/right_eye_open", new BoolMsg(faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedR] < 0.8));
                time_elapsed = 0f;
                Debug.Log("Published Valid Face!");
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

                ros.Publish("/left_eye_transform", left_trans);
                ros.Publish("/right_eye_transform", right_trans);
            }
        }

        if (msg_recv)
        {
            Response_Text.text = to_do_response;
            msg_recv = false;
        }
    }

    void ReceiveMessage(StringMsg MsgData)
    {

        to_do_response = MsgData.data;
        msg_recv = true;
    }

}
