using System.Collections.Generic;

public class MdxImportSettings
{
    // General.
    public List<int> excludeGeosets = new List<int>();
    public List<string> excludeTextures = new List<string>();

    // Materials.
    public bool importMaterials = true;

    // Animations.
    public bool importAnimations = true;
    public bool importTangents = true;
    public float frameRate = 960;
    public List<string> excludeAnimations = new List<string>();
}