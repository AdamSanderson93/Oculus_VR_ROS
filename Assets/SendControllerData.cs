using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using RosMessageTypes.CustomTeleopMessages;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class SendControllerData : MonoBehaviour
{
    //Getting action references from scene
    [Header("ROS_INPUTS")]
    [SerializeField] private Transform centerEyeAnchor = null;
    [SerializeField] private Transform leftHandAnchor = null;
    [SerializeField] private Transform rightHandAnchor = null;
    
    //ROS related variables
    readonly string topicName = "/xr_input";

    readonly string baseTopicName = "/cmd_vel";


    ROSConnection ros;
    //Tracking elapsed time

    float publishMessageFrequency = 0.01f;
    private float timeElapsed;

    // Start is called before the first frame update
    void Start()
    {
        //Ros Connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<XRModMsg>(topicName);
        ros.RegisterPublisher<TwistMsg>(baseTopicName);
        //Try to send control data at 50 hz
        //StartCoroutine(PublishData(0.02f));
    }


    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {
            var XRMod = new XRModMsg();

            //Read values from action reference
            Vector3 leftPostitionVal = leftHandAnchor.localPosition;
            Quaternion leftRotationVal = leftHandAnchor.localRotation;
            Vector3 rightPostitionVal = rightHandAnchor.localPosition;
            Quaternion rightRotationVal = rightHandAnchor.localRotation;
            Vector3 hmdPositionVal = centerEyeAnchor.localPosition;
            Quaternion hmdRotationVal = centerEyeAnchor.localRotation;

            // --- RIGHT CONTROLLER ---
            // Buttons
            bool rightA = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            bool rightB = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);
            bool rightThumbstickClick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);

            // Thumbstick Position
            Vector2 rightThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            float rightStickX = rightThumbstick.x;
            float rightStickY = rightThumbstick.y;

            // Gripper and Trigger
            float rightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            float rightTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

            // --- LEFT CONTROLLER ---
            // Buttons
            bool leftX = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
            bool leftY = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);
            bool leftThumbstickClick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
            bool leftMenuButton = OVRInput.Get(OVRInput.Button.Start, OVRInput.Controller.LTouch);

            // Thumbstick Position
            Vector2 leftThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
            float leftStickX = leftThumbstick.x;
            float leftStickY = leftThumbstick.y;

            // Gripper and Trigger
            float leftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            float leftTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

            // Scale thumbsticks to reduce speed.
            // Fix rotation and translation directions
            float left_up = leftStickY * 0.25f;
            float left_lr = leftStickX * 0.25f * -1.0f; 
            float right_lr = rightStickX * 0.5f * -1.0f; 

            //Assign it to message and change frame convention from unity to ROS
            XRMod.left_controller_pose = new PoseMsg
            {
                position = leftPostitionVal.To<FLU>(),
                orientation = leftRotationVal.To<FLU>()
            };
            XRMod.right_controller_pose = new PoseMsg
            {
                position = rightPostitionVal.To<FLU>(),
                orientation = rightRotationVal.To<FLU>()
            };
            XRMod.hmd_pose = new PoseMsg
            {
                position = hmdPositionVal.To<FLU>(),
                orientation = hmdRotationVal.To<FLU>()
            };

            XRMod.left_squeeze = leftTrigger;
            XRMod.right_squeeze = rightTrigger;

            var Twist = new TwistMsg();

            Twist.linear.x = left_up;
            Twist.linear.y = left_lr;
            Twist.linear.z = 0;

            Twist.angular.x = 0;
            Twist.angular.y = 0;
            Twist.angular.z = right_lr;

            ros.Publish(topicName, XRMod);
            ros.Publish(baseTopicName, Twist);

            timeElapsed = 0;
        }
    }
}
