using System.Collections.Generic;

public class MdxImportSettings
{
    // Geosets.
    public List<string> discardTextures = new List<string>();

    // Materials.
    public bool importMaterials = true;

    // Animations.
    public bool importAnimations = true;
    public bool importTangents = true;
    public float frameRate = 960;
    public List<string> discardAnimations = new List<string>();
}