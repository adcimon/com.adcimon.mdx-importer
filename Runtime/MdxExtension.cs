using System.Collections.Generic;
using UnityEngine;
using MdxLib.Model;
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

    public static bool HasTexture( this CMaterial cmaterial, string textureName )
    {
        for( int i = 0; i < cmaterial.Layers.Count; i++ )
        {
            CMaterialLayer clayer = cmaterial.Layers.Get(i);
            if( clayer.Texture.Object.FileName.Contains(textureName) )
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasTextures( this CMaterial cmaterial, List<string> textureNames )
    {
        foreach( string textureName in textureNames )
        {
            if( cmaterial.HasTexture(textureName) )
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasTexture( this CGeoset cgeoset, string textureName )
    {
        return cgeoset.Material.Object.HasTexture(textureName);
    }

    public static bool HasTextures( this CGeoset cgeoset, List<string> textureNames )
    {
        foreach( string textureName in textureNames )
        {
            if( cgeoset.HasTexture(textureName) )
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsNamed( this CSequence csequence, List<string> animationNames )
    {
        foreach( string name in animationNames )
        {
            if( csequence.Name == name )
            {
                return true;
            }
        }

        return false;
    }
}