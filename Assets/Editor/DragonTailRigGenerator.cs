using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

public static class DragonTailRigGenerator
{
    private const string TailHeadPath = "Assets/Resources/Arts/tailHead.png";
    private const string TailEndPath = "Assets/Resources/Arts/tailEnd.png";
    private const string PrefabPath = "Assets/Resources/Prefabs/Mono/Boss/Dragon/DragonTailRig.prefab";

    [MenuItem("Tools/NeonPulse/Dragon/Create Tail Rig")]
    public static void CreateTailRig()
    {
        if (!PrepareTailSprites(out Sprite headSprite, out Sprite endSprite))
            return;

        GameObject root = BuildRigGameObject(headSprite, endSprite);
        try
        {
            EnsureFolder("Assets/Resources/Prefabs/Mono/Boss/Dragon");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            Debug.Log($"Dragon tail rig created: {PrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [MenuItem("Tools/NeonPulse/Dragon/Create Tail Rig In Scene")]
    public static void CreateTailRigInScene()
    {
        if (!PrepareTailSprites(out Sprite headSprite, out Sprite endSprite))
            return;

        GameObject root = BuildRigGameObject(headSprite, endSprite);
        Undo.RegisterCreatedObjectUndo(root, "Create Dragon Tail Rig");

        Transform parent = Selection.activeTransform;
        if (parent != null)
            root.transform.SetParent(parent, false);

        root.transform.localPosition = Vector3.zero;
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("Dragon tail rig instantiated in scene.");
    }

    private static bool PrepareTailSprites(out Sprite headSprite, out Sprite endSprite)
    {
        TailSpriteSetup headSetup = new TailSpriteSetup("TailHead", TailHeadPath, 6, 0.86f, 0.16f);
        TailSpriteSetup endSetup = new TailSpriteSetup("TailEnd", TailEndPath, 11, 0.91f, 0.07f);

        ApplySpriteRig(headSetup);
        ApplySpriteRig(endSetup);

        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(TailHeadPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(TailEndPath, ImportAssetOptions.ForceUpdate);

        headSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TailHeadPath);
        endSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TailEndPath);

        if (headSprite != null && endSprite != null)
            return true;

        Debug.LogError("Dragon tail rig failed: tail sprites were not imported.");
        return false;
    }

    private static GameObject BuildRigGameObject(Sprite headSprite, Sprite endSprite)
    {
        GameObject root = new GameObject("DragonTailRig");

        GameObject head = CreateSkinnedSprite("TailHead", headSprite, root.transform, Vector3.zero, 5);
        Transform[] headBones = CreateBoneHierarchy(head.transform, headSprite.GetBones());
        BindSpriteSkin(head, headBones);

        Transform headTip = headBones.Length > 0 ? headBones[headBones.Length - 1] : head.transform;
        Vector3 endOffset = Vector3.down * GetTopBoneLocalY(endSprite);
        GameObject end = CreateSkinnedSprite("TailEnd", endSprite, headTip, endOffset, 4);
        Transform[] endBones = CreateBoneHierarchy(end.transform, endSprite.GetBones());
        BindSpriteSkin(end, endBones);

        TailBoneSway sway = root.AddComponent<TailBoneSway>();
        sway.SetBones(Combine(headBones, endBones));

        return root;
    }

    private static void ApplySpriteRig(TailSpriteSetup setup)
    {
        TextureImporter importer = AssetImporter.GetAtPath(setup.AssetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Dragon tail rig failed: missing texture importer at {setup.AssetPath}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();

        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
        if (spriteRects == null || spriteRects.Length == 0)
        {
            Debug.LogError($"Dragon tail rig failed: no sprite rect found at {setup.AssetPath}");
            return;
        }

        SpriteRect rect = spriteRects[0];
        GUID spriteId = rect.spriteID;

        ISpriteBoneDataProvider boneProvider = dataProvider.GetDataProvider<ISpriteBoneDataProvider>();
        ISpriteMeshDataProvider meshProvider = dataProvider.GetDataProvider<ISpriteMeshDataProvider>();

        if (boneProvider == null || meshProvider == null)
        {
            Debug.LogError("Dragon tail rig failed: 2D Sprite data providers are unavailable.");
            return;
        }

        boneProvider.SetBones(spriteId, CreateSpriteBones(setup, rect.rect.size));
        CreateMesh(setup, rect.rect.size, out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges);
        meshProvider.SetVertices(spriteId, vertices);
        meshProvider.SetIndices(spriteId, indices);
        meshProvider.SetEdges(spriteId, edges);

        dataProvider.Apply();
        AssetDatabase.ImportAsset(setup.AssetPath, ImportAssetOptions.ForceUpdate);
    }

    private static List<SpriteBone> CreateSpriteBones(TailSpriteSetup setup, Vector2 spriteSize)
    {
        List<SpriteBone> bones = new List<SpriteBone>();
        float centerX = spriteSize.x * 0.5f;
        float topY = spriteSize.y * setup.TopY;
        float bottomY = spriteSize.y * setup.BottomY;
        float step = (topY - bottomY) / Mathf.Max(1, setup.BoneCount - 1);

        for (int i = 0; i < setup.BoneCount; i++)
        {
            bool isRoot = i == 0;
            bones.Add(new SpriteBone
            {
                name = $"{setup.Name}_Bone_{i:00}",
                guid = GUID.Generate().ToString(),
                position = isRoot ? new Vector3(centerX, topY, 0f) : new Vector3(0f, -step, 0f),
                rotation = Quaternion.identity,
                length = step,
                parentId = isRoot ? -1 : i - 1,
                color = Color.cyan
            });
        }

        return bones;
    }

    private static void CreateMesh(TailSpriteSetup setup, Vector2 spriteSize, out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges)
    {
        const int columns = 5;
        int rows = Mathf.Max(8, setup.BoneCount * 3);
        List<Vertex2DMetaData> vertexList = new List<Vertex2DMetaData>(columns * rows);

        for (int y = 0; y < rows; y++)
        {
            float v = y / (float)(rows - 1);
            float pixelY = Mathf.Lerp(0f, spriteSize.y, v);
            float boneT = 1f - v;
            float bonePosition = boneT * (setup.BoneCount - 1);
            int boneA = Mathf.Clamp(Mathf.FloorToInt(bonePosition), 0, setup.BoneCount - 1);
            int boneB = Mathf.Clamp(boneA + 1, 0, setup.BoneCount - 1);
            float blend = Mathf.Clamp01(bonePosition - boneA);

            for (int x = 0; x < columns; x++)
            {
                float u = x / (float)(columns - 1);
                vertexList.Add(new Vertex2DMetaData
                {
                    position = new Vector2(Mathf.Lerp(0f, spriteSize.x, u), pixelY),
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
            {
                edgeList.Add(new Vector2Int(rowStart + x, rowStart + x + 1));
            }
        }

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {
                edgeList.Add(new Vector2Int(y * columns + x, (y + 1) * columns + x));
            }
        }

        vertices = vertexList.ToArray();
        indices = indexList.ToArray();
        edges = edgeList.ToArray();
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

    private static GameObject CreateSkinnedSprite(string name, Sprite sprite, Transform parent, Vector3 localPosition, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;

        go.AddComponent<SpriteSkin>();
        return go;
    }

    private static Transform[] CreateBoneHierarchy(Transform spriteRoot, SpriteBone[] spriteBones)
    {
        Transform[] transforms = new Transform[spriteBones.Length];
        for (int i = 0; i < spriteBones.Length; i++)
        {
            CreateBoneTransform(i, spriteBones, transforms, spriteRoot);
        }

        return transforms;
    }

    private static void CreateBoneTransform(int index, SpriteBone[] spriteBones, Transform[] transforms, Transform spriteRoot)
    {
        if (transforms[index] != null) return;

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

    private static void BindSpriteSkin(GameObject spriteObject, Transform[] bones)
    {
        SpriteSkin spriteSkin = spriteObject.GetComponent<SpriteSkin>();
        SerializedObject serializedSkin = new SerializedObject(spriteSkin);
        serializedSkin.FindProperty("m_RootBone").objectReferenceValue = bones.Length > 0 ? bones[0] : null;

        SerializedProperty boneTransforms = serializedSkin.FindProperty("m_BoneTransforms");
        boneTransforms.arraySize = bones.Length;
        for (int i = 0; i < bones.Length; i++)
        {
            boneTransforms.GetArrayElementAtIndex(i).objectReferenceValue = bones[i];
        }

        serializedSkin.FindProperty("m_AlwaysUpdate").boolValue = true;
        serializedSkin.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform[] Combine(Transform[] headBones, Transform[] endBones)
    {
        Transform[] combined = new Transform[headBones.Length + endBones.Length];
        headBones.CopyTo(combined, 0);
        endBones.CopyTo(combined, headBones.Length);
        return combined;
    }

    private static float GetTopBoneLocalY(Sprite sprite)
    {
        SpriteBone[] bones = sprite.GetBones();
        if (bones == null || bones.Length == 0) return 0f;
        return bones[0].position.y;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private readonly struct TailSpriteSetup
    {
        public readonly string Name;
        public readonly string AssetPath;
        public readonly int BoneCount;
        public readonly float TopY;
        public readonly float BottomY;

        public TailSpriteSetup(string name, string assetPath, int boneCount, float topY, float bottomY)
        {
            Name = name;
            AssetPath = assetPath;
            BoneCount = boneCount;
            TopY = topY;
            BottomY = bottomY;
        }
    }
}
