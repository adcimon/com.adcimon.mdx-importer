using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MdxLib.Model;
using MdxLib.Primitives;

public static class MdxExtension
{
	public static Vector3 ToVector3(this CVector3 cvector3)
	{
		return new Vector3(cvector3.X, cvector3.Y, cvector3.Z);
	}

	public static Vector3 SwapYZ(this Vector3 vector3)
	{
		return new Vector3(vector3.x, vector3.z, vector3.y);
	}

	public static Quaternion ToQuaternion(this CVector4 cvector4)
	{
		return new Quaternion(cvector4.X, cvector4.Y, cvector4.Z, cvector4.W);
	}

	public static bool ContainsTexture(this CMaterial cmaterial, string filename)
	{
		for (int i = 0; i < cmaterial.Layers.Count; i++)
		{
			CMaterialLayer clayer = cmaterial.Layers.Get(i);
			if (Path.GetFileName(clayer.Texture.Object.FileName).Equals(filename))
			{
				return true;
			}
		}

		return false;
	}

	public static bool ContainsTextures(this CMaterial cmaterial, List<string> filenames)
	{
		foreach (string filename in filenames)
		{
			if (cmaterial.ContainsTexture(filename))
			{
				return true;
			}
		}

		return false;
	}

	public static bool ContainsTexture(this CGeoset cgeoset, string filename)
	{
		return cgeoset.Material.Object.ContainsTexture(filename);
	}

	public static bool ContainsTextures(this CGeoset cgeoset, List<string> filenames)
	{
		foreach (string filename in filenames)
		{
			if (cgeoset.ContainsTexture(filename))
			{
				return true;
			}
		}

		return false;
	}
}