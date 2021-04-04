using UnityEngine;
using MdxLib.Primitives;

public static class MdxExtension
{
    public static Vector3 ToVector3( this CVector3 cvector3 )
    {
        return new Vector3(cvector3.X, cvector3.Y, cvector3.Z);
    }

    public static Vector3 SwapYZ( this Vector3 vector3 )
    {
        return new Vector3(vector3.x, vector3.z, vector3.y);
    }

    public static Quaternion ToQuaternion( this CVector4 cvector4 )
    {
        return new Quaternion(cvector4.X, cvector4.Y, cvector4.Z, cvector4.W);
    }
}