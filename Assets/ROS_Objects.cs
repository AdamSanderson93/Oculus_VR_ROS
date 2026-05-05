using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

[Serializable]
public class geometry_msgs_Twist
{
    public geometry_msgs_Point linear;
    public geometry_msgs_Point angular;
}

[Serializable]
public class geometry_msgs_Point
{
    public double x;
    public double y;
    public double z;
}

[Serializable]
public class geometry_msgs_Quaternion
{
    public double x;
    public double y;
    public double z;
    public double w;
}


[Serializable]
public class geometry_msgs_Pose
{
    public geometry_msgs_Point position;
    public geometry_msgs_Quaternion orientation;
}


[Serializable]
public class std_msgs_bool
{
    public bool data;
}

[Serializable]
public class std_msgs_byte
{
    public byte data;
}

[Serializable]
public class custom_teleop_msgs_XRMod
{
    public geometry_msgs_Pose left_controller_pose;
    public geometry_msgs_Pose right_controller_pose;
    public geometry_msgs_Pose hmd_pose;

    public float left_squeeze;
    public float right_squeeze;
}

[Serializable]
public class custom_teleop_msgs_XRPose
{
    public geometry_msgs_Pose left_controller_pose;
    public geometry_msgs_Pose right_controller_pose;
    public geometry_msgs_Pose hmd_pose;
}

[Serializable]
public class std_msgs_header
{
    public uint seq = 0;
    public std_msgs_time stamp;
    public string frame_id = "";
}

[Serializable]
public class std_msgs_time
{
    public int secs = 0;
    public int nsecs = 0;
}

[Serializable]
public class movo_gp_cmd
{
    public std_msgs_header header;
    public string gp_cmd;
    public uint gp_param;
}