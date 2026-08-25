// using WebSocketSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Newtonsoft.Json;

using NativeWebSocket;

public class WebRTC_ROS_Data : MonoBehaviour {

    [Header("Signalling Server Information")]
    [SerializeField] private string address;
    [SerializeField] private ushort port;
    [SerializeField] private string room;
    [SerializeField] private string user;
    [SerializeField] private string password;

    [Header("Turn Server Information")]
    [SerializeField] private string T_address;
    [SerializeField] private ushort T_port;
    [SerializeField] private string T_user;
    [SerializeField] private string T_password;
    [SerializeField] private bool use_turn_server=false;
    [SerializeField] private double timeout_time=13.0;
    
    [Header("ROS_INPUTS")]
    [SerializeField] private Transform centerEyeAnchor = null;
    [SerializeField] private Transform leftHandAnchor = null;
    [SerializeField] private Transform rightHandAnchor = null;

    [SerializeField] private double message_rate;



    // private RawImage receiveImage;
    private RTCPeerConnection connection;
    private RTCDataChannel receiverDataChannel;

    private WebSocket ws;
    private bool ws_connected = false;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private string caller_id;

    private bool ice_recieved = false;
    private bool ice_sent = false;
    private bool ice_made = false;
    private send_ICE_candidate stored_ice_candidate;
    
    private float timeElapsed = 0, ros_time_rate=0;

    private bool last_button_state = false;

    private custom_teleop_msgs_XRMod last_message;

    public static Vector3 ConvertVecFromROS(Vector3 v)
    {
        return new Vector3(-v.y, v.z, v.x);
    }

    public static Vector3 ConvertVecToROS(Vector3 v)
    {
        return new Vector3(v.z, -v.x, v.y);
    }

    public static Quaternion ConvertQuatFromROS(Quaternion q)
    {
        return new Quaternion(q.y, -q.z, q.x, -q.w);
    }

    public static Quaternion ConvertQuatToROS(Quaternion q)
    {
        return new Quaternion(-q.z, q.x, -q.y, q.w);
    }

    private void start_rtc()
    {
        RTCConfiguration config = default;
        if (use_turn_server)
        {
            config.iceServers = new[] {
                new RTCIceServer {
                    urls = new[] {
                        "stun:stun.l.google.com:19302"
                    }
                },
                new RTCIceServer {
                    urls = new[] {
                        "turn:"+T_address+":"+T_port
                    },
                    credentialType = RTCIceCredentialType.Password,
                    username = T_user,
                    credential = T_password
                }
            };
        }
        else
        {
            config.iceServers = new[] {
                new RTCIceServer {
                    urls = new[] {
                        "stun:stun.l.google.com:19302"
                    }
                }
            }; 
        }

        connection = new RTCPeerConnection(ref config);
        connection.OnIceCandidate = candidate => {
            stored_ice_candidate = new send_ICE_candidate();
            stored_ice_candidate.data = new Candidate_Send();
            
            stored_ice_candidate.data.candidate = new CandidateInit() {
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                candidate = candidate.Candidate
            };
            ice_made = true;
        };
        connection.OnIceConnectionChange = state => {
            Debug.Log(state);
        };

        connection.OnDataChannel = channel => {
            receiverDataChannel = channel;
            receiverDataChannel.OnMessage = bytes => {
                var message = Encoding.UTF8.GetString(bytes);
                Debug.Log("Receiver received: " + message);
            };
        };
        
    }

    void join_room()
    {
        opentera_join_room join_room = new opentera_join_room();
        join_room.data = new opentera_join_room_data();
        join_room.data.name = user;
        join_room.data.password = password;
        join_room.data.room = room;
        ws.SendText(JsonConvert.SerializeObject(join_room));
    }

    private void Start() {
        Debug.Log("Start Data!");
        StartCoroutine(WebRTC.Update());
        Start_System();
    }

    private void Start_System()
    {
        ws = new WebSocket("ws://"+address+":"+Convert.ToString((int)port)+"/signaling");
        
        Debug.Log("Starting Data");

        ws.OnOpen += () => {
            ws_connected = true;
            // STUN server config
            join_room();
            Debug.Log("ws_opened");
        };

        ws.OnMessage += (bytes) => {
            var data = System.Text.Encoding.UTF8.GetString(bytes);
            // Debug.Log(data);
            // Debug.Log(data);
            if (data.Contains("join-room-answer"))
            {
                opentera_join_room_answer answer = JsonConvert.DeserializeObject<opentera_join_room_answer>(data);
                if (answer.data.Length == 0)
                {
                    Debug.LogError("Unable to join room!");
                }
                else
                {
                    Debug.Log("Room Get!");
                }
            }
            else if (data.Contains("peer-call-received"))
            {
                Debug.Log("peer-call-received");
                peer_call_recieved message = JsonConvert.DeserializeObject<peer_call_recieved>(data);
                receivedOfferSessionDescTemp = message.data.offer;
                caller_id = message.data.fromId;
                // Debug.Log(JsonConvert.SerializeObject(message));
                // Debug.Log(caller_id);
                hasReceivedOffer = true;
            }
            else if (data.Contains("ice-candidate-received") && !ice_recieved)
            {
                ice_recieved = true;

                Debug.Log("ice-candidate-received");

                recieve_ICE_candidate ice_recv = JsonConvert.DeserializeObject<recieve_ICE_candidate>(data);
                
                caller_id = ice_recv.data.fromId;

                // generate candidate data
                RTCIceCandidateInit init = new RTCIceCandidateInit();
                init.sdpMid = ice_recv.data.candidate.sdpMid;
                init.sdpMLineIndex = ice_recv.data.candidate.sdpMLineIndex;
                init.candidate = ice_recv.data.candidate.candidate;
                RTCIceCandidate candidate = new RTCIceCandidate(init);

                // add candidate to this connection
                connection.AddIceCandidate(candidate);
                
            }

        };

        ws.Connect();
        start_rtc();
    }

    private void Update() {
        
    	if (!ws_connected || ws.State != WebSocketState.Open)
    	{
            return;
    	}

        #if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
        #endif
        
        if (hasReceivedOffer) {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
        if (ice_recieved && ice_made && !ice_sent)
        {
            ice_sent = true;
            stored_ice_candidate.data.toId = caller_id;
            ws.SendText(JsonConvert.SerializeObject(stored_ice_candidate));
            ws.SendText(JsonConvert.SerializeObject(stored_ice_candidate));
            Debug.Log("Data ice sent!");
        }

        if (ice_sent && (connection.ConnectionState != RTCPeerConnectionState.Connected))
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed > timeout_time)
            {
                close_webrtc();
                Start_System();
            }            
        }

        if (receiverDataChannel != null)
        {
            ros_time_rate += Time.deltaTime;
            if (message_rate != 0.0 || ros_time_rate > 1.0/message_rate)
            {
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

                custom_teleop_msgs_XRMod message = new custom_teleop_msgs_XRMod() {
                    left_controller_pose = new geometry_msgs_Pose() {
                        position = new geometry_msgs_Point() {
                            x = leftPostitionVal.x,
                            y = leftPostitionVal.y,
                            z = leftPostitionVal.z
                        },
                        orientation = new geometry_msgs_Quaternion() {
                            x = leftRotationVal.x,
                            y = leftRotationVal.y,
                            z = leftRotationVal.z,
                            w = leftRotationVal.w,
                            
                        }
                    },
                    right_controller_pose = new geometry_msgs_Pose() {
                        position = new geometry_msgs_Point() {
                            x = rightPostitionVal.x,
                            y = rightPostitionVal.y,
                            z = rightPostitionVal.z
                        },
                        orientation = new geometry_msgs_Quaternion() {
                            x = rightRotationVal.x,
                            y = rightRotationVal.y,
                            z = rightRotationVal.z,
                            w = rightRotationVal.w,
                            
                        }
                    },
                    hmd_pose = new geometry_msgs_Pose() {
                        position = new geometry_msgs_Point() {
                            x = hmdPositionVal.x,
                            y = hmdPositionVal.y,
                            z = hmdPositionVal.z
                        },
                        orientation = new geometry_msgs_Quaternion() {
                            x = hmdRotationVal.x,
                            y = hmdRotationVal.y,
                            z = hmdRotationVal.z,
                            w = hmdRotationVal.w,
                            
                        }
                    },
                    left_squeeze = leftTrigger, 
                    right_squeeze = rightTrigger
                };

                last_message = message;

                geometry_msgs_Twist base_message = new geometry_msgs_Twist() {
                    linear = new geometry_msgs_Point() {
                            x = left_up,
                            y = left_lr*-1.0f,
                            z = 0
                        },
                    angular = new geometry_msgs_Point() {
                            x = 0,
                            y = 0,
                            z = right_lr*-1.0f
                        }
                };



                if (leftX && !last_button_state)
                {
                    movo_gp_cmd mode_msg = new movo_gp_cmd(){
                        header = new std_msgs_header(),
                        gp_cmd = "GENERAL_PURPOSE_CMD_SET_OPERATIONAL_MODE",
                        gp_param = 5};
                    send_message("/movo/gp_command","movo_msgs/ConfigCmd",JsonConvert.SerializeObject(mode_msg));
                    //receiverDataChannel.Send("/movo/gp_command!movo_msgs/ConfigCmd!"+JsonConvert.SerializeObject(mode_msg));
                }
                send_message("/xr_input","custom_teleop_messages/XRMod",JsonConvert.SerializeObject(message));
                send_message("/cmd_vel","geometry_msgs/Twist",JsonConvert.SerializeObject(base_message));
                //receiverDataChannel.Send("/xr_input!custom_teleop_messages/XRMod!"+JsonConvert.SerializeObject(message));
                //receiverDataChannel.Send("/cmd_vel!geometry_msgs/Twist!"+JsonConvert.SerializeObject(base_message));
                
                last_button_state = leftX;
                ros_time_rate = 0;
            }
            // if (send_Test)
            // {
            //     receiverDataChannel.Send(Test);
            // }
            
        }
    }

    public void send_message(string topic, string msg_type, string json_data)
    {
        if (receiverDataChannel != null)
        {
            receiverDataChannel.Send($"{topic}!{msg_type}!{json_data}");
        }
    }

    private void close_webrtc()
    {
        if (receiverDataChannel != null) {
            receiverDataChannel.Close();
            receiverDataChannel = null;
        }
        if (connection != null) {
            connection.Close();
        }
        connection.Close();
        ws.Close();
        ice_sent = false;
        ice_made = false;
        ice_recieved = false;
        hasReceivedOffer=false;
        ws_connected=false;
        timeElapsed = 0;
    }
    private void OnDestroy() {
        close_webrtc();
    }

    private IEnumerator CreateAnswer() {
        
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        peer_call_answer call_answer = new peer_call_answer();
        call_answer.data = new SessionDescription_Answer();
        call_answer.data.answer = new SessionDescription() {
            type = "answer",
            sdp = answerDesc.sdp
        };
        call_answer.data.toId = caller_id;
        ws.SendText(JsonConvert.SerializeObject(call_answer));
        // ws.SendText(JsonConvert.SerializeObject(call_answer));
    }
}
