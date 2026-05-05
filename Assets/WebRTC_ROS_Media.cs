// using WebSocketSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

using NativeWebSocket;

public class WebRTC_ROS_Media : MonoBehaviour {

    [Header("Video Transmission")]
    [SerializeField] private RawImage receiveImage;
    [SerializeField] private RawImage sourceImage;

    [SerializeField] private Camera cameraStream;

    [Header("Audio Transmission")]
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioSource outputAudioSource;

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

    private RTCPeerConnection connection;
    private AudioStreamTrack m_audioTrack;
    private AudioClip m_clipInput;
    
    const int sampleRate = 48000;

    private WebSocket ws;
    private bool ws_connected = false;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private string caller_id;

    private bool ice_recieved = false;
    private bool ice_sent = false;
    private bool ice_made = false;
    private bool video_init = false;
    private send_ICE_candidate stored_ice_candidate;

    private float timeElapsed = 0;

    private void start_rtc()
    {
        // string turn_address = "turn:"+address;
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
            Debug.Log("ICE Connection Change");
            Debug.Log(state);
        };
        
        connection.OnTrack = e => {
            if (e.Track is VideoStreamTrack video) {
                video.OnVideoReceived += tex => {
                    // receiveImage.texture = new Texture2D(2,2);
                    print(tex.width);
                    print(tex.height);
                    receiveImage.texture = tex;
                    print("Video Stream Starting");
                    print(video.Enabled);
                };
            }
            if (e.Track is AudioStreamTrack audio) {
                outputAudioSource.SetTrack(audio);
                outputAudioSource.loop = false;
                outputAudioSource.Play();
                print("Audio Stream Starting");
                print(audio.Enabled);
            }
        };

        print("Start Video Sending");
        // video
        // var videoStreamTrack = cameraStream.CaptureStreamTrack(1920, 1080);
        // connection.AddTrack(videoStreamTrack);

        // var videoStream = cameraStream.CaptureStream(1280, 720);
        // sourceImage.texture = cameraStream.targetTexture;

        // RTCRtpTransceiverInit init = new RTCRtpTransceiverInit()
        // {
        //     direction = RTCRtpTransceiverDirection.SendOnly,
        //     sendEncodings = new RTCRtpEncodingParameters[] {
        //         new RTCRtpEncodingParameters { maxFramerate = 30,  }
        //     },
        // };
        // MediaStreamTrack videoTrack = videoStream.GetTracks().First();
        // RTCRtpTransceiver transceiver = connection.AddTransceiver(videoTrack, init);
        
        print("Start Audio Sending");
        // audio
        // inputAudioSource.loop = true;
        // inputAudioSource.Play();
        // var audioStreamTrack = new AudioStreamTrack(inputAudioSource);
        // audioStreamTrack.Loopback = true;
        // connection.AddTrack(audioStreamTrack);

        m_audioTrack = new AudioStreamTrack(inputAudioSource);
        m_audioTrack.Loopback = false;
        connection.AddTrack(m_audioTrack);

        print("Done!");
    }

    private void join_room()
    {
        opentera_join_room join_room = new opentera_join_room();
        join_room.data = new opentera_join_room_data();
        join_room.data.name = user;
        join_room.data.password = password;
        join_room.data.room = room;
        ws.SendText(JsonConvert.SerializeObject(join_room));
    }



    private void Start() {
        Debug.Log("Start Media!");
        StartCoroutine(WebRTC.Update());
        
        // setup microphone
        int m_lengthSeconds = 1;

        string m_deviceName = Microphone.devices[0];
        Debug.Log("Microphone: " + m_deviceName);
        Microphone.GetDeviceCaps(m_deviceName, out int minFreq, out int maxFreq);
        m_clipInput = Microphone.Start(m_deviceName, true, m_lengthSeconds, sampleRate);

        // set the latency to “0” samples before the audio starts to play.
        while (!(Microphone.GetPosition(m_deviceName) > 0)) { }

        inputAudioSource = GetComponent<AudioSource>();
        inputAudioSource.loop = true;
        inputAudioSource.clip = m_clipInput;

        m_audioTrack = new AudioStreamTrack(inputAudioSource);
        m_audioTrack.Loopback = false;
        
        Start_System();
    }
    private void Start_System()
    {
        ws = new WebSocket("ws://"+address+":"+Convert.ToString((int)port)+"/signaling");
        
        Debug.Log("Starting Media");

        ws.OnOpen += () => {
            ws_connected = true;
            // STUN server config
            join_room();
            Debug.Log("ws_opened");
        };

        ws.OnMessage += (bytes) => {
            var data = System.Text.Encoding.UTF8.GetString(bytes);
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
            // ws.SendText(JsonConvert.SerializeObject(stored_ice_candidate));
            Debug.Log("Media ice sent!");
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
    }

    private void close_webrtc()
    {
        if (connection != null)
        {
            connection.Close();
        }
        ws.Close();
        ice_sent=false;
        ice_made=false;
        ice_recieved=false;
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
        // ws.Send(JsonConvert.SerializeObject(call_answer));
    }
}
