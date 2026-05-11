using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

public static class DragonClawRigGenerator
{
    private const string DragonPrefabPath = "Assets/Resources/Prefabs/Mono/Boss/Dragon/dragon.prefab";
    private const string ClawLeftPath = "Assets/Resources/Arts/dragonClawL.png";
    private const string ClawRightPath = "Assets/Resources/Arts/dragonClawR.png";

    private static readonly ClawRigSetup LeftSetup = new ClawRigSetup(
        "dragonClawL",
        ClawLeftPath,
        new[]
        {
            new Vector2(0.58f, 0.12f),
            new Vector2(0.60f, 0.31f),
            new Vector2(0.55f, 0.58f),
            new Vector2(0.45f, 0.88f)
        });

    private static readonly ClawRigSetup RightSetup = new ClawRigSetup(
        "dragonClawR",
        ClawRightPath,
        new[]
        {
            new Vector2(0.42f, 0.12f),
            new Vector2(0.40f, 0.31f),
            new Vector2(0.45f, 0.58f),
            new Vector2(0.55f, 0.88f)
        });

    [MenuItem("Tools/NeonPulse/Dragon/Rig Dragon Claws")]
    public static void RigDragonClaws()
    {
        if (!PrepareClawSprites())
            return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(DragonPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"Dragon claw rig failed: missing prefab at {DragonPrefabPath}");
            return;
        }

        try
        {
            RigClaw(prefabRoot.transform, LeftSetup);
            RigClaw(prefabRoot.transform, RightSetup);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, DragonPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dragon claws rigged on dragon prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool PrepareClawSprites()
    {
        return PrepareClawSprite(LeftSetup) && PrepareClawSprite(RightSetup);
    }

    private static bool PrepareClawSprite(ClawRigSetup setup)
    {
        TextureImporter importer = AssetImporter.GetAtPath(setup.AssetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Dragon claw rig failed: missing texture importer at {setup.AssetPath}");
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();

        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            Debug.LogError($"Dragon claw rig failed: sprite data provider unavailable for {setup.AssetPath}");
            return false;
        }

        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
        if (spriteRects == null || spriteRects.Length == 0)
        {
            Debug.LogError($"Dragon claw rig failed: no sprite rect found at {setup.AssetPath}");
            return false;
        }

        SpriteRect rect = spriteRects[0];
        GUID spriteId = rect.spriteID;
        Vector2 spriteSize = rect.rect.size;

        ISpriteBoneDataProvider boneProvider = dataProvider.GetDataProvider<ISpriteBoneDataProvider>();
        ISpriteMeshDataProvider meshProvider = dataProvider.GetDataProvider<ISpriteMeshDataProvider>();
        if (boneProvider == null || meshProvider == null)
        {
            Debug.LogError($"Dragon claw rig failed: mesh or bone data provider unavailable for {setup.AssetPath}");
            return false;
        }

        Vector2[] controlPoints = BuildAbsolutePoints(setup.NormalizedPoints, spriteSize);
        boneProvider.SetBones(spriteId, CreateBones(setup.NodeName, controlPoints));

        CreateMesh(controlPoints, spriteSize, out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges);
        meshProvider.SetVertices(spriteId, vertices);
        meshProvider.SetIndices(spriteId, indices);
        meshProvider.SetEdges(spriteId, edges);

        dataProvider.Apply();
        AssetDatabase.ImportAsset(setup.AssetPath, ImportAssetOptions.ForceUpdate);
        return true;
    }

    private static void RigClaw(Transform root, ClawRigSetup setup)
    {
        Transform claw = FindChildRecursive(root, setup.NodeName);
        if (claw == null)
        {
            Debug.LogError($"Dragon claw rig failed: node {setup.NodeName} not found in dragon prefab.");
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(setup.AssetPath);
        if (sprite == null)
        {
            Debug.LogError($"Dragon claw rig failed: sprite not found at {setup.AssetPath}");
            return;
        }

        SpriteRenderer renderer = claw.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = claw.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        RemoveGeneratedBones(claw, setup.NodeName + "_Bone_");

        Transform[] bones = CreateBoneHierarchy(claw, sprite.GetBones());
        SpriteSkin spriteSkin = claw.GetComponent<SpriteSkin>();
        if (spriteSkin == null)
            spriteSkin = claw.gameObject.AddComponent<SpriteSkin>();
        BindSpriteSkin(spriteSkin, bones);
    }

    private static Transform FindChildRecursive(Transform root, string nodeName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == nodeName)
                return child;
        }
        return null;
    }

    private static void RemoveGeneratedBones(Transform parent, string prefix)
    {
        List<GameObject> toDelete = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(prefix, StringComparison.Ordinal))
                toDelete.Add(child.gameObject);
        }

        foreach (GameObject go in toDelete)
            UnityEngine.Object.DestroyImmediate(go);
    }

    private static Vector2[] BuildAbsolutePoints(Vector2[] normalizedPoints, Vector2 spriteSize)
    {
        Vector2[] points = new Vector2[normalizedPoints.Length];
        for (int i = 0; i < normalizedPoints.Length; i++)
            points[i] = new Vector2(normalizedPoints[i].x * spriteSize.x, normalizedPoints[i].y * spriteSize.y);
        return points;
    }

    private static List<SpriteBone> CreateBones(string nodeName, Vector2[] points)
    {
        List<SpriteBone> bones = new List<SpriteBone>(points.Length);
        float[] worldAngles = new float[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 direction;
            if (i < points.Length - 1)
                direction = points[i + 1] - points[i];
            else
                direction = points[i] - points[i - 1];

            worldAngles[i] = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        for (int i = 0; i < points.Length; i++)
        {
            float length = i < points.Length - 1
                ? Vector2.Distance(points[i], points[i + 1])
                : Vector2.Distance(points[i - 1], points[i]) * 0.45f;

            Vector2 localPosition;
            Quaternion localRotation;

            if (i == 0)
            {
                localPosition = points[i];
                localRotation = Quaternion.Euler(0f, 0f, worldAngles[i]);
            }
            else
            {
                Quaternion parentWorldRotation = Quaternion.Euler(0f, 0f, worldAngles[i - 1]);
                localPosition = Quaternion.Inverse(parentWorldRotation) * (points[i] - points[i - 1]);
                localRotation = Quaternion.Euler(0f, 0f, worldAngles[i] - worldAngles[i - 1]);
            }

            bones.Add(new SpriteBone
            {
                name = $"{nodeName}_Bone_{i:00}",
                guid = GUID.Generate().ToString(),
                position = new Vector3(localPosition.x, localPosition.y, 0f),
                rotation = localRotation,
                length = length,
                parentId = i - 1,
                color = new Color(0.3f, 0.95f, 1f, 1f)
            });
        }

        return bones;
    }

    private static void CreateMesh(Vector2[] controlPoints, Vector2 spriteSize, out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges)
    {
        const int columns = 7;
        const int rows = 18;

        List<Vertex2DMetaData> vertexList = new List<Vertex2DMetaData>(columns * rows);
        for (int y = 0; y < rows; y++)
        {
            float v = y / (float)(rows - 1);
            float py = Mathf.Lerp(0f, spriteSize.y, v);
            for (int x = 0; x < columns; x++)
            {
                float u = x / (float)(columns - 1);
                Vector2 position = new Vector2(Mathf.Lerp(0f, spriteSize.x, u), py);
                float chainPosition = ProjectToChain(position, controlPoints);
                int boneA = Mathf.Clamp(Mathf.FloorToInt(chainPosition), 0, controlPoints.Length - 1);
                int boneB = Mathf.Clamp(boneA + 1, 0, controlPoints.Length - 1);
                float blend = Mathf.Clamp01(chainPosition - boneA);

                vertexList.Add(new Vertex2DMetaData
                {
                    position = position,
                    boneWeight = CreateWeight(boneA, boneB, 1f - blend, blend)
                });
            }
        }

        List<int> indexList = new List<int>();
        for (int y = 0; y < rows - 1; y++)
        {
            for (int x = 0; x < columns - 1; x++)
            {
                int i0 = y * columns + x;
                int i1 = i0 + 1;
                int i2 = i0 + columns;
                int i3 = i2 + 1;

                indexList.Add(i0);
                indexList.Add(i2);
                indexList.Add(i1);
                indexList.Add(i1);
                indexList.Add(i2);
                indexList.Add(i3);
            }
        }

        List<Vector2Int> edgeList = new List<Vector2Int>();
        for (int y = 0; y < rows; y++)
        {
            int rowStart = y * columns;
            for (int x = 0; x < columns - 1; x++)
                edgeList.Add(new Vector2Int(rowStart + x, rowStart + x + 1));
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows - 1; y++)
                edgeList.Add(new Vector2Int(y * columns + x, (y + 1) * columns + x));
        }

        vertices = vertexList.ToArray();
        indices = indexList.ToArray();
        edges = edgeList.ToArray();
    }

    private static float ProjectToChain(Vector2 vertex, Vector2[] points)
    {
        float bestDistance = float.MaxValue;
        float bestPosition = 0f;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            float t = lengthSq <= Mathf.Epsilon ? 0f : Mathf.Clamp01(Vector2.Dot(vertex - a, ab) / lengthSq);
            Vector2 projected = a + ab * t;
            float distanceSq = (vertex - projected).sqrMagnitude;
            if (distanceSq < bestDistance)
            {
                bestDistance = distanceSq;
                bestPosition = i + t;
            }
        }

        return bestPosition;
    }

    private static BoneWeight CreateWeight(int boneA, int boneB, float weightA, float weightB)
    {
        BoneWeight weight = new BoneWeight
        {
            boneIndex0 = boneA,
            weight0 = weightA
        };

        if (boneB != boneA && weightB > 0.0001f)
        {
            weight.boneIndex1 = boneB;
            weight.weight1 = weightB;
        }

        return weight;
    }

    private static Transform[] CreateBoneHierarchy(Transform spriteRoot, SpriteBone[] spriteBones)
    {
        Transform[] transforms = new Transform[spriteBones.Length];
        for (int i = 0; i < spriteBones.Length; i++)
            CreateBoneTransform(i, spriteBones, transforms, spriteRoot);
        return transforms;
    }

    private static void CreateBoneTransform(int index, SpriteBone[] spriteBones, Transform[] transforms, Transform spriteRoot)
    {
        if (transforms[index] != null)
            return;

        SpriteBone bone = spriteBones[index];
        Transform parent = spriteRoot;
        if (bone.parentId >= 0)
        {
            CreateBoneTransform(bone.parentId, spriteBones, transforms, spriteRoot);
            parent = transforms[bone.parentId];
        }

        GameObject boneObject = new GameObject(bone.name);
        Transform boneTransform = boneObject.transform;
        boneTransform.SetParent(parent, false);
        boneTransform.localPosition = bone.position;
        boneTransform.localRotation = bone.rotation;
        boneTransform.localScale = Vector3.one;
        transforms[index] = boneTransform;
    }

    private static void BindSpriteSkin(SpriteSkin spriteSkin, Transform[] bones)
    {
        SerializedObject serializedSkin = new SerializedObject(spriteSkin);
        serializedSkin.FindProperty("m_RootBone").objectReferenceValue = bones.Length > 0 ? bones[0] : null;

        SerializedProperty boneTransforms = serializedSkin.FindProperty("m_BoneTransforms");
        boneTransforms.arraySize = bones.Length;
        for (int i = 0; i < bones.Length; i++)
            boneTransforms.GetArrayElementAtIndex(i).objectReferenceValue = bones[i];

        serializedSkin.FindProperty("m_AlwaysUpdate").boolValue = true;
        serializedSkin.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct ClawRigSetup
    {
        public readonly string NodeName;
        public readonly string AssetPath;
        public readonly Vector2[] NormalizedPoints;

        public ClawRigSetup(string nodeName, string assetPath, Vector2[] normalizedPoints)
        {
            NodeName = nodeName;
            AssetPath = assetPath;
            NormalizedPoints = normalizedPoints;
        }
    }
}
