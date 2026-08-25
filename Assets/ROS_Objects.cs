using Newtonsoft.Json;
using RosMessageTypes.Std;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

[Serializable]
public class geometry_msgs_Transform
{
    public geometry_msgs_Point translation;
    public geometry_msgs_Quaternion rotation;
}

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

[Serializable]
public class std_msgs_MultiArrayLayout
{
    public std_msg_MultiArrayDimension[] dim;
    public UInt32 data_offset;
}

[Serializable]
public class std_msg_MultiArrayDimension
{
    public string label;
    public UInt32 size;
    public UInt32 stride;
}

[Serializable]
public class std_msgs_Float32MultiArray
{
    public std_msgs_MultiArrayLayout layout;
    public float[] data;
}

// From ROS Docs regarding multiarray accessors

//# Accessors should ALWAYS be written in terms of dimension stride
//# and specified outer-most dimension first.
//# 
//# multiarray(i,j,k) = data[data_offset + dim_stride[1]*i + dim_stride[2]*j + k]
//#
//# A standard, 3-channel 640x480 image with interleaved color channels
//# would be specified as:
//#
//# dim[0].label  = "height"
//# dim[0].size   = 480
//# dim[0].stride = 3*640*480 = 921600  (note dim[0] stride is just size of image)
//# dim[1].label  = "width"
//# dim[1].size   = 640
//# dim[1].stride = 3*640 = 1920
//# dim[2].label  = "channel"
//# dim[2].size   = 3
//# dim[2].stride = 3
//#
//# multiarray(i,j,k) refers to the ith row, jth column, and kth channel.

[Serializable]
public class std_msgs_Float64MultiArray
{
    public std_msgs_MultiArrayLayout layout;
    public double[] data;
}

[Serializable]
public class std_msgs_ArbitraryMultiArray<type>
{
    public std_msgs_MultiArrayLayout layout;
    public type[] data;
}

[Serializable]
public class std_msgs_Arbitrary<type>
{
    public type data;
}