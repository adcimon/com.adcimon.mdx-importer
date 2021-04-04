using System.Collections.Generic;

public class MdxImportSettings
{
    public List<string> discardTextures = new List<string>();
    public bool importMaterials = true;
    public bool importAnimations = true;
    public bool importTangents = true;
    public float frameRate = 960;
}