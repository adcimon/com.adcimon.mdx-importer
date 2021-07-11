using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using MdxLib.Animator;
using MdxLib.Model;
using MdxLib.ModelFormats;
using MdxLib.Primitives;

public class MdxModel
{
    public GameObject gameObject { get; private set; }
    public Mesh mesh { get; private set; }
    public List<Material> materials { get; private set; }
    public List<AnimationClip> clips { get; private set; }

    private string path;
    private MdxImportSettings settings;

    private CModel cmodel;
    private GameObject skeleton;
    private SortedDictionary<int, GameObject> bones = new SortedDictionary<int, GameObject>();

    public MdxModel()
    {
        materials = new List<Material>();
        clips = new List<AnimationClip>();
    }

    public void Import( string path, MdxImportSettings settings )
    {
        this.path = path;
        this.settings = settings;

        ReadFile();

        // Import the mesh.
        ImportMesh();
        gameObject = new GameObject();
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        SkinnedMeshRenderer renderer = gameObject.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = mesh;

        // Import the materials.
        if( settings.importMaterials )
        {
            ImportMaterials(renderer);
        }

        // Import the skeleton.
        ImportSkeleton(renderer);

        // Import the animations.
        if( settings.importAnimations )
        {
            ImportAnimations();
        }

        // Import the attachments.
        if( settings.importAttachments )
        {
            ImportAttachments();
        }

        // Import the events.
        if( settings.importEvents )
        {
            ImportEvents();
        }

        // Import the particles.
        if( settings.importParticles )
        {
            ImportParticles();
        }
    }

    private void ReadFile()
    {
        try
        {
            cmodel = new CModel();
            using( FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read) )
            {
                string extension = Path.GetExtension(path);
                if( extension.Equals(".mdx") )
                {
                    CMdx cmdx = new CMdx();
                    cmdx.Load(path, stream, cmodel);
                }
                else if( extension.Equals(".mdl") )
                {
                    CMdl cmdl = new CMdl();
                    cmdl.Load(path, stream, cmodel);
                }
                else
                {
                    throw new IOException("Invalid file extension.");
                }
            }
        }
        catch( Exception e )
        {
            cmodel = null;
            Debug.LogError(e.Message);
        }
    }

    private void ImportMesh()
    {
        mesh = new Mesh();
        mesh.name = cmodel.Name;

        // Set the bounding box.
        Bounds bounds = new Bounds();
        bounds.min = cmodel.Extent.Min.ToVector3().SwapYZ();
        bounds.max = cmodel.Extent.Max.ToVector3().SwapYZ();
        mesh.bounds = bounds;

        // For each geoset.
        List<CombineInstance> combines = new List<CombineInstance>();
        for( int i = 0; i < cmodel.Geosets.Count; i++ )
        {
            CGeoset cgeoset = cmodel.Geosets.Get(i);
            if( settings.excludeGeosets.Contains(i) || cgeoset.ContainsTextures(settings.excludeByTexture) )
            {
                continue;
            }

            CombineInstance combine = new CombineInstance();
            Mesh submesh = new Mesh();

            // Vertices.
            List<Vector3> vertices = new List<Vector3>();
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL up axis is Z.
                // Unity up axis is Y.
                CGeosetVertex cvertex = cgeoset.Vertices.Get(j);
                Vector3 vertex = new Vector3(cvertex.Position.X, cvertex.Position.Z, cvertex.Position.Y);
                vertices.Add(vertex);
            }

            // Triangles.
            List<int> triangles = new List<int>();
            for( int j = 0; j < cgeoset.Faces.Count; j++ )
            {
                // MDX/MDL coordinate system is anti-clockwise.
                // Unity coordinate system is clockwise.
                CGeosetFace cface = cgeoset.Faces.Get(j);
                triangles.Add(cface.Vertex1.ObjectId);
                triangles.Add(cface.Vertex3.ObjectId); // Swap the order of the vertex 2 and 3.
                triangles.Add(cface.Vertex2.ObjectId);
            }

            // Normals.
            List<Vector3> normals = new List<Vector3>();
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL up axis is Z.
                // Unity up axis is Y.
                CGeosetVertex cvertex = cgeoset.Vertices.Get(j);
                Vector3 normal = new Vector3(cvertex.Normal.X, cvertex.Normal.Z, cvertex.Normal.Y);
                normals.Add(normal);
            }

            // UVs.
            List<Vector2> uvs = new List<Vector2>();
            for( int j = 0; j < cgeoset.Vertices.Count; j++ )
            {
                // MDX/MDL texture coordinate origin is at top left.
                // Unity texture coordinate origin is at bottom left.
                CGeosetVertex cvertex = cgeoset.Vertices.Get(j);
                Vector2 uv = new Vector2(cvertex.TexturePosition.X, Mathf.Abs(cvertex.TexturePosition.Y - 1)); // Vunity = abs(Vmdx - 1)
                uvs.Add(uv);
            }

            submesh.vertices = vertices.ToArray();
            submesh.triangles = triangles.ToArray();
            submesh.normals = normals.ToArray();
            submesh.uv = uvs.ToArray();

            combine.mesh = submesh;
            combine.transform = Matrix4x4.identity;
            combines.Add(combine);
        }

        // Combine the submeshes.
        mesh.CombineMeshes(combines.ToArray(), false);
    }

    private void ImportMaterials( SkinnedMeshRenderer renderer )
    {
        // For each material.
        for( int i = 0; i < cmodel.Materials.Count; i++ )
        {
            CMaterial cmaterial = cmodel.Materials.Get(i);
            if( cmaterial.ContainsTextures(settings.excludeByTexture) )
            {
                continue;
            }

            Material material = new Material(Shader.Find("MDX/Standard"));
            material.name = i.ToString();

            // For each layer.
            int blendMode = 1; // Cutout.
            bool twoSided = false;
            for( int j = 0; j < cmaterial.Layers.Count; j++ )
            {
                CMaterialLayer clayer = cmaterial.Layers[j];

                // Two Sided.
                if( clayer.TwoSided )
                {
                    twoSided = true;
                }

                // Team color.
                if( clayer?.Texture?.Object.ReplaceableId > 0 )
                {
                    blendMode = 0; // Opaque.
                }
            }

            material.SetFloat("_Cutoff", 0.5f);
            material.SetInt("_Cull", (twoSided) ? (int)UnityEngine.Rendering.CullMode.Off : (int)UnityEngine.Rendering.CullMode.Back);
            material.SetFloat("_Mode", blendMode);

            materials.Add(material);
        }

        // Add the materials to the renderer in order.
        List<Material> rendererMaterials = new List<Material>();
        for( int i = 0; i < cmodel.Geosets.Count; i++ )
        {
            CGeoset cgeoset = cmodel.Geosets.Get(i);
            if( cgeoset.ContainsTextures(settings.excludeByTexture) )
            {
                continue;
            }

            for( int j = 0; j < cmodel.Materials.Count; j++ )
            {
                CMaterial cmaterial = cmodel.Materials.Get(j);

                if( cgeoset.Material.Object.ObjectId == cmaterial.ObjectId )
                {
                    rendererMaterials.Add(materials[j]);
                }
            }
        }

        renderer.materials = rendererMaterials.ToArray();
    }

    private void ImportSkeleton( SkinnedMeshRenderer renderer )
    {
        skeleton = new GameObject("Skeleton");
        skeleton.transform.SetParent(gameObject.transform);

        // Create the bones.
        // Bones reference geosets with geoanims, if there aren't geoanims, then bones will act as helpers.
        CObjectContainer<CBone> cbones = cmodel.Bones;
        for( int i = 0; i < cbones.Count; i++ )
        {
            CBone cbone = cbones.Get(i);
            GameObject bone = new GameObject(cbone.Name);

            // Pivot points are the positions of each object.
            CVector3 cpivot = cbone.PivotPoint;

            // Set the bone position.
            // MDX/MDL up axis is Z.
            // Unity up axis is Y.
            bone.transform.position = new Vector3(cpivot.X, cpivot.Z, cpivot.Y);

            bones[cbone.NodeId] = bone;
        }

        // Create the helpers.
        // Helpers are only used for doing transformations to their children.
        CObjectContainer<CHelper> chelpers = cmodel.Helpers;
        for( int i = 0; i < chelpers.Count; i++ )
        {
            CHelper chelper = chelpers.Get(i);
            GameObject helper = new GameObject(chelper.Name);

            // Pivot points are the positions of each object.
            CVector3 cpivot = chelper.PivotPoint;

            // Set the helper position.
            // MDX/MDL up axis is Z.
            // Unity up axis is Y.
            helper.transform.position = new Vector3(cpivot.X, cpivot.Z, cpivot.Y);

            bones[chelper.NodeId] = helper;
        }

        // Add the root bone.
        bones.Add(bones.Keys.Max() + 1, skeleton);

        // Set the bones' parents.
        for( int i = 0; i < cbones.Count; i++ )
        {
            CBone cbone = cbones.Get(i);

            GameObject bone = bones[cbone.NodeId];
            if( bones.ContainsKey(cbone.Parent.NodeId) )
            {
                GameObject parent = bones[cbone.Parent.NodeId];
                bone.transform.SetParent(parent.transform);
            }
            else
            {
                bone.transform.SetParent(skeleton.transform);
            }
        }

        // Set the helpers' parents.
        for( int i = 0; i < chelpers.Count; i++ )
        {
            CHelper chelper = chelpers.Get(i);

            GameObject helper = bones[chelper.NodeId];
            if( bones.ContainsKey(chelper.Parent.NodeId) )
            {
                GameObject parent = bones[chelper.Parent.NodeId];
                helper.transform.SetParent(parent.transform);
            }
            else
            {
                helper.transform.SetParent(skeleton.transform);
            }
        }

        // Calculate the bind poses.
        // The bind pose is the inverse of the transformation matrix of the bone when the bone is in the bind pose.
        mesh.bindposes = bones.Values.Select(go => go.transform.worldToLocalMatrix * gameObject.transform.localToWorldMatrix).ToArray();

        // Set the bones to the skinned mesh renderer.
        renderer.bones = bones.Values.ToArray().Select(go => go.transform).ToArray();
        renderer.rootBone = skeleton.transform;

        ImportWeights();
    }

    private void ImportWeights()
    {
        // Calculate the bone weights.
        // For each geoset.
        List<BoneWeight> weights = new List<BoneWeight>();
        for( int i = 0; i < cmodel.Geosets.Count; i++ )
        {
            CGeoset cgeoset = cmodel.Geosets.Get(i);
            if( settings.excludeGeosets.Contains(i) || cgeoset.ContainsTextures(settings.excludeByTexture) )
            {
                continue;
            }

            // For each vertex.
            CObjectContainer<CGeosetVertex> cvertices = cgeoset.Vertices;
            for( int j = 0; j < cvertices.Count; j++ )
            {
                CGeosetVertex cvertex = cvertices.Get(j);
                BoneWeight weight = new BoneWeight();

                // Group.
                // A vertex group reference a group (of matrices).
                CGeosetGroup cgroup = cvertex.Group.Object; // Vertex group reference.

                // Matrices.
                // A matrix reference an object. The bone weights are evenly distributed, each weight is 1/N.
                CObjectContainer<CGeosetGroupNode> cmatrices = cgroup.Nodes;
                for( int k = 0; k < cmatrices.Count; k++ )
                {
                    CGeosetGroupNode cmatrix = cmatrices.Get(k);
                    switch( k )
                    {
                        case 0:
                        {
                            weight.boneIndex0 = cmatrix.Node.NodeId;
                            weight.weight0 = 1f / cmatrices.Count;
                            break;
                        }
                        case 1:
                        {
                            weight.boneIndex1 = cmatrix.Node.NodeId;
                            weight.weight1 = 1f / cmatrices.Count;
                            break;
                        }
                        case 2:
                        {
                            weight.boneIndex2 = cmatrix.Node.NodeId;
                            weight.weight2 = 1f / cmatrices.Count;
                            break;
                        }
                        case 3:
                        {
                            weight.boneIndex3 = cmatrix.Node.NodeId;
                            weight.weight3 = 1f / cmatrices.Count;
                            break;
                        }
                        default:
                        {
                            throw new Exception("Invalid number of bones " + k + " when skining.");
                        }
                    }
                }

                weights.Add(weight);
            }
        }

        mesh.boneWeights = weights.ToArray();
    }

    private void ImportAnimations()
    {
        // For each sequence.
        for( int i = 0; i < cmodel.Sequences.Count; i++ )
        {
            CSequence csequence = cmodel.Sequences.Get(i);
            if( settings.excludeAnimations.Contains(csequence.Name) )
            {
                continue;
            }

            AnimationClip clip = new AnimationClip();
            clip.name = csequence.Name;

            // Set the loop mode.
            if( !csequence.NonLooping )
            {
                clip.wrapMode = WrapMode.Loop;
            }

            // For each bone.
            for( int j = 0; j < cmodel.Bones.Count; j++ )
            {
                CBone cbone = cmodel.Bones.Get(j);
                GameObject bone = bones[cbone.NodeId];
                string path = GetPath(bone);

                // Translation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> ctranslations = cbone.Translation;
                    for( int k = 0; k < ctranslations.Count; k++ )
                    {
                        CAnimatorNode<CVector3> node = ctranslations.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;
                            Vector3 position = bone.transform.localPosition + node.Value.ToVector3().SwapYZ();

                            Keyframe keyX = new Keyframe(time / settings.frameRate, position.x);
                            if( settings.importTangents )
                            {
                                keyX.inTangent = node.InTangent.X;
                                keyX.outTangent = node.OutTangent.X;
                            }
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / settings.frameRate, position.y);
                            if( settings.importTangents )
                            {
                                keyY.inTangent = node.InTangent.Z;
                                keyY.outTangent = node.OutTangent.Z;
                            }
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / settings.frameRate, position.z);
                            if( settings.importTangents )
                            {
                                keyZ.inTangent = node.InTangent.Y;
                                keyZ.outTangent = node.OutTangent.Y;
                            }
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.x", curveX);
                    }
                    if( curveY.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.y", curveY);
                    }
                    if( curveZ.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.z", curveZ);
                    }
                }

                // Rotation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();
                    AnimationCurve curveW = new AnimationCurve();

                    CAnimator<CVector4> crotations = cbone.Rotation;
                    for( int k = 0; k < crotations.Count; k++ )
                    {
                        CAnimatorNode<CVector4> node = crotations.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;
                            Quaternion rotation = node.Value.ToQuaternion();

                            Keyframe keyX = new Keyframe(time / settings.frameRate, rotation.x);
                            if( settings.importTangents )
                            {
                                keyX.inTangent = node.InTangent.X;
                                keyX.outTangent = node.OutTangent.X;
                            }
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / settings.frameRate, rotation.z);
                            if( settings.importTangents )
                            {
                                keyY.inTangent = node.InTangent.Z;
                                keyY.outTangent = node.OutTangent.Z;
                            }
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / settings.frameRate, rotation.y);
                            if( settings.importTangents )
                            {
                                keyZ.inTangent = node.InTangent.Y;
                                keyZ.outTangent = node.OutTangent.Y;
                            }
                            curveZ.AddKey(keyZ);

                            Keyframe keyW = new Keyframe(time / settings.frameRate, -rotation.w);
                            if( settings.importTangents )
                            {
                                keyW.inTangent = node.InTangent.W;
                                keyW.outTangent = node.OutTangent.W;
                            }
                            curveW.AddKey(keyW);
                        }
                    }

                    if( curveX.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
                    }
                    if( curveY.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
                    }
                    if( curveZ.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
                    }
                    if( curveW.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
                    }
                }

                // Scaling.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> cscalings = cbone.Scaling;
                    for( int k = 0; k < cscalings.Count; k++ )
                    {
                        CAnimatorNode<CVector3> node = cscalings.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / settings.frameRate, node.Value.X);
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / settings.frameRate, node.Value.Z);
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / settings.frameRate, node.Value.Y);
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localScale.x", curveX);
                    }
                    if( curveY.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localScale.y", curveY);
                    }
                    if( curveZ.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localScale.z", curveZ);
                    }
                }
            }

            // For each helper.
            for( int j = 0; j < cmodel.Helpers.Count; j++ )
            {
                CHelper chelper = cmodel.Helpers.Get(j);
                GameObject bone = bones[chelper.NodeId];
                string path = GetPath(bone);

                // Translation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> ctranslations = chelper.Translation;
                    for( int k = 0; k < ctranslations.Count; k++ )
                    {
                        CAnimatorNode<CVector3> node = ctranslations.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;
                            Vector3 position = bone.transform.localPosition + node.Value.ToVector3().SwapYZ();

                            Keyframe keyX = new Keyframe(time / settings.frameRate, position.x);
                            if( settings.importTangents )
                            {
                                keyX.inTangent = node.InTangent.X;
                                keyX.outTangent = node.OutTangent.X;
                            }
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / settings.frameRate, position.y);
                            if( settings.importTangents )
                            {
                                keyY.inTangent = node.InTangent.Z;
                                keyY.outTangent = node.OutTangent.Z;
                            }
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / settings.frameRate, position.z);
                            if( settings.importTangents )
                            {
                                keyZ.inTangent = node.InTangent.Y;
                                keyZ.outTangent = node.OutTangent.Y;
                            }
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.x", curveX);
                    }
                    if( curveY.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.y", curveY);
                    }
                    if( curveZ.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.z", curveZ);
                    }
                }

                // Rotation.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();
                    AnimationCurve curveW = new AnimationCurve();

                    CAnimator<CVector4> crotations = chelper.Rotation;
                    for( int k = 0; k < crotations.Count; k++ )
                    {
                        CAnimatorNode<CVector4> node = crotations.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;
                            Quaternion rotation = node.Value.ToQuaternion();

                            Keyframe keyX = new Keyframe(time / settings.frameRate, rotation.x);
                            if( settings.importTangents )
                            {
                                keyX.inTangent = node.InTangent.X;
                                keyX.outTangent = node.OutTangent.X;
                            }
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / settings.frameRate, rotation.z);
                            if( settings.importTangents )
                            {
                                keyY.inTangent = node.InTangent.Z;
                                keyY.outTangent = node.OutTangent.Z;
                            }
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / settings.frameRate, rotation.y);
                            if( settings.importTangents )
                            {
                                keyZ.inTangent = node.InTangent.Y;
                                keyZ.outTangent = node.OutTangent.Y;
                            }
                            curveZ.AddKey(keyZ);

                            Keyframe keyW = new Keyframe(time / settings.frameRate, -rotation.w);
                            if( settings.importTangents )
                            {
                                keyW.inTangent = node.InTangent.W;
                                keyW.outTangent = node.OutTangent.W;
                            }
                            curveW.AddKey(keyW);
                        }
                    }

                    if( curveX.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.x", curveX);
                    }
                    if( curveY.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.y", curveY);
                    }
                    if( curveZ.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.z", curveZ);
                    }
                    if( curveW.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localRotation.w", curveW);
                    }
                }

                // Scaling.
                {
                    AnimationCurve curveX = new AnimationCurve();
                    AnimationCurve curveY = new AnimationCurve();
                    AnimationCurve curveZ = new AnimationCurve();

                    CAnimator<CVector3> cscalings = chelper.Scaling;
                    for( int k = 0; k < cscalings.Count; k++ )
                    {
                        CAnimatorNode<CVector3> node = cscalings.Get(k);
                        if( csequence.IntervalStart <= node.Time && node.Time <= csequence.IntervalEnd )
                        {
                            float time = node.Time - csequence.IntervalStart;

                            Keyframe keyX = new Keyframe(time / settings.frameRate, node.Value.X);
                            curveX.AddKey(keyX);

                            Keyframe keyY = new Keyframe(time / settings.frameRate, node.Value.Z);
                            curveY.AddKey(keyY);

                            Keyframe keyZ = new Keyframe(time / settings.frameRate, node.Value.Y);
                            curveZ.AddKey(keyZ);
                        }
                    }

                    if( curveX.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localScale.x", curveX);
                    }
                    if( curveY.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localScale.y", curveY);
                    }
                    if( curveZ.length > 0 )
                    {
                        clip.SetCurve(path, typeof(Transform), "localScale.z", curveZ);
                    }
                }
            }

            // Realigns quaternion keys to ensure shortest interpolation paths and avoid rotation glitches.
            clip.EnsureQuaternionContinuity();

            clips.Add(clip);
        }
    }

    private void ImportAttachments()
    {
        CObjectContainer<CAttachment> cattachments = cmodel.Attachments;
        for( int i = 0; i < cattachments.Count; i++ )
        {
            CAttachment cattachment = cattachments.Get(i);
            CreateGameObject(cattachment.Name, cattachment.PivotPoint, cattachment.Parent.NodeId);
        }
    }

    private void ImportEvents()
    {
        CObjectContainer<CEvent> cevents = cmodel.Events;
        for( int i = 0; i < cevents.Count; i++ )
        {
            CEvent cevent = cevents.Get(i);
            CreateGameObject(cevent.Name, cevent.PivotPoint, cevent.Parent.NodeId);
        }
    }

    private void ImportParticles()
    {
        CObjectContainer<CParticleEmitter> cparticles = cmodel.ParticleEmitters;
        for( int i = 0; i < cparticles.Count; i++ )
        {
            CParticleEmitter cparticle = cparticles.Get(i);
            CreateGameObject(cparticle.Name, cparticle.PivotPoint, cparticle.Parent.NodeId);
        }

        CObjectContainer<CParticleEmitter2> cparticles2 = cmodel.ParticleEmitters2;
        for( int i = 0; i < cparticles2.Count; i++ )
        {
            CParticleEmitter2 cparticle2 = cparticles2.Get(i);
            CreateGameObject(cparticle2.Name, cparticle2.PivotPoint, cparticle2.Parent.NodeId);
        }
    }

    private void CreateGameObject( string name, CVector3 pivot, int parentId )
    {
        GameObject gameObject = new GameObject(name);

        // Set the position.
        // MDX/MDL up axis is Z.
        // Unity up axis is Y.
        gameObject.transform.position = new Vector3(pivot.X, pivot.Z, pivot.Y);

        // Set the parent.
        if( bones.ContainsKey(parentId) )
        {
            GameObject parent = bones[parentId];
            gameObject.transform.SetParent(parent.transform);
        }
        else
        {
            gameObject.transform.SetParent(skeleton.transform);
        }
    }

    private string GetPath( GameObject bone )
    {
        if( !bone )
        {
            return "";
        }

        string path = bone.name;
        while( bone.transform.parent != skeleton.transform && bone.transform.parent != null )
        {
            bone = bone.transform.parent.gameObject;
            path = bone.name + "/" + path;
        }

        path = skeleton.name + "/" + path;

        return path;
    }
}