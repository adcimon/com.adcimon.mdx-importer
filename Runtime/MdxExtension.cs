using UnityEngine;
using MdxLib.Primitives;

public static class MdxExtension
{
    public static Vector3 ToVector3( this CVector3 cvector3 )
    {
        return new Vector3(cvector3.X, cvector3.Y, cvector3.Z);
    }
}