using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

[Serializable]
public class opentera_join_room_data
{
    public string data;
    public string name;
    public string password;
    public string room;
    public int protocolVersion = 2;
}

[Serializable]
public class opentera_join_room
{
    [JsonProperty("event")]
    public string @event = "join-room";
    public opentera_join_room_data data;
}

[Serializable]
public class opentera_join_room_answer
{
    [JsonProperty("event")]
    public string @event;
    public string data;
}

[Serializable]
public class SessionDescription
{
    public string type;
    public string sdp;
}

[Serializable]
public class peer_call_recieved
{
    [JsonProperty("event")]
    public string @event;
    public SessionDescription_Offer data;
}

[Serializable]
public class peer_call_answer
{
    [JsonProperty("event")]
    public string @event {get; set;} = "make-peer-call-answer";
    public SessionDescription_Answer data;
}

[Serializable]
public class SessionDescription_Offer
{
    public SessionDescription offer;
    public string toId;
    public string fromId;
}

[Serializable]
public class SessionDescription_Answer
{
    public SessionDescription answer;
    public string toId;

}

[Serializable]
public class CandidateInit
{
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;
}

[Serializable]
public class Candidate_Recv
{
    public CandidateInit candidate;
    public string toId;
    public string fromId;
}

[Serializable]
public class Candidate_Send
{
    public CandidateInit candidate;
    public string toId;
}

[Serializable]
public class recieve_ICE_candidate
{
    [JsonProperty("event")]
    public string @event;
    public Candidate_Recv data;
}


[Serializable]
public class send_ICE_candidate
{
    [JsonProperty("event")]
    public string @event {get; set;} = "send-ice-candidate";
    public Candidate_Send data;
}