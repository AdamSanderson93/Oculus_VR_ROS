using RosMessageTypes.Std;
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

    private float time_elapsed;
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
                Debug.Log("Found it!");
                ros = ROSConnection.GetOrCreateInstance();
                ros.RegisterPublisher<Float64MultiArrayMsg>("/facial_input");
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

            if (time_elapsed > pub_period)
            {
                
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

                // Option 1: Using string.Format
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

                ros.Publish("/facial_input", array);
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
