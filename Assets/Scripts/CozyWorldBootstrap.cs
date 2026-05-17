using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[DisallowMultipleComponent]
public sealed class CozyWorldBootstrap : MonoBehaviour
{
    private const string GeneratedRootName = "Generated Cozy Game Runtime";

    private static readonly Color GrassColor = new Color32(113, 169, 106, 255);
    private static readonly Color MeadowColor = new Color32(139, 189, 123, 255);
    private static readonly Color PathColor = new Color32(197, 166, 112, 255);
    private static readonly Color PlazaColor = new Color32(186, 181, 151, 255);
    private static readonly Color WaterColor = new Color32(77, 151, 183, 255);
    private static readonly Color ShoreColor = new Color32(180, 172, 126, 255);
    private static readonly Color FenceColor = new Color32(137, 100, 68, 255);

    [Header("Startup")]
    [SerializeField] private bool buildOnAwake;
    [SerializeField] private Vector2 playerSpawn = new Vector2(-8f, -2f);
    [SerializeField] private float playerMoveSpeed = 4.25f;
    [SerializeField] private float cameraZoom = 7.5f;

    private string authenticatedPlayerName = "Player";
    private static Sprite squareSprite;
    private static Sprite treeSprite;
    private static Sprite playerSprite;
    private static Sprite flowerSprite;

    private void Awake()
    {
        if (buildOnAwake && Application.isPlaying)
        {
            RebuildWorld();
        }
    }

    [ContextMenu("Rebuild Starter Map")]
    public void RebuildWorld()
    {
        ClearGeneratedWorld();

        Transform generatedRoot = new GameObject(GeneratedRootName).transform;
        generatedRoot.SetParent(transform, false);

        BuildGround(generatedRoot);
        BuildWater(generatedRoot);
        BuildTown(generatedRoot);
        BuildForest(generatedRoot);
        BuildDetails(generatedRoot);
        BuildWorldBoundaries(generatedRoot);

        GameObject player = BuildPlayer(generatedRoot);
        ConfigureCamera(generatedRoot, player.transform);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    public void SetAuthenticatedPlayer(string playerName)
    {
        authenticatedPlayerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName.Trim();
    }

    public void ClearWorld()
    {
        ClearGeneratedWorld();
    }

    private void ClearGeneratedWorld()
    {
        Transform existingRoot = transform.Find(GeneratedRootName);

        if (existingRoot != null)
        {
            DestroyGeneratedObject(existingRoot.gameObject);
        }
    }

    private void BuildGround(Transform parent)
    {
        CreateRect("Meadow Ground", parent, Vector2.zero, new Vector2(48f, 32f), GrassColor, -100, false);
        CreateRect("Soft Meadow Patch", parent, new Vector2(-9f, 6f), new Vector2(18f, 10f), MeadowColor, -98, false);
        CreateRect("Town Plaza", parent, new Vector2(-5.5f, -1.5f), new Vector2(8.5f, 5.5f), PlazaColor, -90, false);
        CreateRect("West Forest Trail", parent, new Vector2(-15f, -2f), new Vector2(17f, 2.25f), PathColor, -88, false);
        CreateRect("East Forest Trail", parent, new Vector2(9.5f, -2f), new Vector2(18f, 2.25f), PathColor, -88, false);
        CreateRect("North Road", parent, new Vector2(-5.5f, 7f), new Vector2(3.25f, 17f), PathColor, -87, false);
        CreateRect("Garden Footpath", parent, new Vector2(1f, 5f), new Vector2(10f, 1.25f), PathColor, -87, false);
    }

    private void BuildWater(Transform parent)
    {
        CreateRect("Pond Shore", parent, new Vector2(12.5f, -8f), new Vector2(9f, 4.75f), ShoreColor, -86, false);
        CreateRect("Quiet Pond", parent, new Vector2(12.5f, -8f), new Vector2(7.5f, 3.35f), WaterColor, -84, false);
        CreateRect("Creek Bend", parent, new Vector2(6.5f, -7f), new Vector2(5f, 1.15f), WaterColor, -84, false);
    }

    private void BuildTown(Transform parent)
    {
        CreateHouse(parent, "Blue Roof Cottage", new Vector2(-9.5f, 2.7f), new Color32(222, 205, 163, 255), new Color32(82, 124, 158, 255));
        CreateHouse(parent, "Berry Roof Cottage", new Vector2(-2.2f, 3.3f), new Color32(231, 214, 177, 255), new Color32(164, 89, 94, 255));
        CreateHouse(parent, "Gardener Cabin", new Vector2(3.4f, -4.8f), new Color32(211, 184, 137, 255), new Color32(92, 126, 87, 255));

        CreateRect("Notice Board", parent, new Vector2(-2.1f, 0.75f), new Vector2(1.2f, 0.8f), new Color32(114, 80, 51, 255), 3, true);
        CreateRect("Notice Board Paper", parent, new Vector2(-2.1f, 0.85f), new Vector2(0.75f, 0.42f), new Color32(233, 220, 172, 255), 4, false);
        CreateRect("Well Base", parent, new Vector2(-7.7f, -4.3f), new Vector2(1.45f, 1.15f), new Color32(142, 142, 130, 255), 3, true);
        CreateRect("Well Water", parent, new Vector2(-7.7f, -4.18f), new Vector2(0.85f, 0.45f), WaterColor, 4, false);
    }

    private void BuildForest(Transform parent)
    {
        for (float x = -22f; x <= 22f; x += 2f)
        {
            if (x < -7.5f || x > -3.5f)
            {
                CreateTree(parent, new Vector2(x, 14.2f), 1.2f);
            }

            if (x < -18f || x > -12f)
            {
                CreateTree(parent, new Vector2(x, -14.3f), 1.15f);
            }
        }

        for (float y = -12f; y <= 12f; y += 2f)
        {
            if (y < -4f || y > 0f)
            {
                CreateTree(parent, new Vector2(-22f, y), 1.2f);
                CreateTree(parent, new Vector2(22f, y), 1.2f);
            }
        }

        Vector2[] treeClusters =
        {
            new Vector2(-15f, 8f), new Vector2(-13.6f, 10.2f), new Vector2(-17.6f, 5.9f),
            new Vector2(6.5f, 10.5f), new Vector2(8.8f, 12f), new Vector2(11.1f, 9.4f),
            new Vector2(16.2f, 3.4f), new Vector2(18.6f, 5.3f), new Vector2(14.1f, 6.1f),
            new Vector2(-15.7f, -8.6f), new Vector2(-18.2f, -6.5f), new Vector2(-12.7f, -10.2f),
            new Vector2(5.4f, -12.2f), new Vector2(8.2f, -11.4f), new Vector2(17.4f, -11.8f)
        };

        for (int i = 0; i < treeClusters.Length; i++)
        {
            float scale = i % 3 == 0 ? 1.35f : 1.05f;
            CreateTree(parent, treeClusters[i], scale);
        }
    }

    private void BuildDetails(Transform parent)
    {
        Vector2[] flowers =
        {
            new Vector2(-12.2f, 4.2f), new Vector2(-10.8f, 5.5f), new Vector2(-7.7f, 6.1f),
            new Vector2(-3.2f, 6.2f), new Vector2(0.5f, 6.7f), new Vector2(4.6f, 7.1f),
            new Vector2(6.8f, 2.2f), new Vector2(9.7f, 1.3f), new Vector2(14.8f, -4.2f),
            new Vector2(16.2f, -6.2f), new Vector2(9.4f, -5.7f), new Vector2(-11.7f, -5.2f),
            new Vector2(-14.6f, -4.4f), new Vector2(-18.1f, -1.1f), new Vector2(-1.3f, -6.7f)
        };

        for (int i = 0; i < flowers.Length; i++)
        {
            CreateFlower(parent, flowers[i], i % 2 == 0 ? 0.55f : 0.42f);
        }

        CreateFence(parent, new Vector2(-11.4f, -5.7f), 5);
        CreateFence(parent, new Vector2(0.7f, -7.1f), 4);
        CreateRect("North Sign", parent, new Vector2(-4.1f, 10.2f), new Vector2(1.4f, 0.65f), new Color32(128, 88, 54, 255), 2, true);
        CreateRect("North Sign Post", parent, new Vector2(-4.1f, 9.55f), new Vector2(0.25f, 0.8f), FenceColor, 1, true);
        CreateRect("East Sign", parent, new Vector2(15.3f, -0.3f), new Vector2(1.4f, 0.65f), new Color32(128, 88, 54, 255), 2, true);
        CreateRect("East Sign Post", parent, new Vector2(15.3f, -0.95f), new Vector2(0.25f, 0.8f), FenceColor, 1, true);
    }

    private void BuildWorldBoundaries(Transform parent)
    {
        CreateCollider("North Boundary", parent, new Vector2(0f, 16.1f), new Vector2(48f, 1f));
        CreateCollider("South Boundary", parent, new Vector2(0f, -16.1f), new Vector2(48f, 1f));
        CreateCollider("West Boundary", parent, new Vector2(-24.1f, 0f), new Vector2(1f, 32f));
        CreateCollider("East Boundary", parent, new Vector2(24.1f, 0f), new Vector2(1f, 32f));
    }

    private GameObject BuildPlayer(Transform parent)
    {
        GameObject player = new GameObject($"Player - {authenticatedPlayerName}");
        player.transform.SetParent(parent, false);
        player.transform.localPosition = playerSpawn;

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = PlayerSprite;
        renderer.sortingOrder = 50;

        Rigidbody2D body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = new Vector2(0.58f, 0.9f);
        collider.offset = new Vector2(0f, 0.45f);

        PlayerMovement2D movement = player.AddComponent<PlayerMovement2D>();
        movement.Configure(playerMoveSpeed);

        return player;
    }

    private void ConfigureCamera(Transform parent, Transform player)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, -10f);
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = cameraZoom;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(128, 176, 197, 255);

        AudioListener listener = cameraObject.AddComponent<AudioListener>();
        listener.enabled = true;

        CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
        follow.SetTarget(player);
        follow.SetBounds(new Vector2(-16f, -10.5f), new Vector2(16f, 10.5f));
    }

    private void CreateHouse(Transform parent, string houseName, Vector2 position, Color wallColor, Color roofColor)
    {
        GameObject house = new GameObject(houseName);
        house.transform.SetParent(parent, false);
        house.transform.localPosition = position;

        CreateRect("Walls", house.transform, new Vector2(0f, 0f), new Vector2(3.4f, 2.25f), wallColor, 5, false);
        CreateRect("Roof", house.transform, new Vector2(0f, 1.25f), new Vector2(3.8f, 0.9f), roofColor, 6, false);
        CreateRect("Door", house.transform, new Vector2(-0.55f, -0.58f), new Vector2(0.65f, 1.1f), new Color32(116, 80, 55, 255), 7, false);
        CreateRect("Window Left", house.transform, new Vector2(0.45f, 0.2f), new Vector2(0.55f, 0.55f), new Color32(246, 222, 135, 255), 7, false);
        CreateRect("Window Right", house.transform, new Vector2(1.15f, 0.2f), new Vector2(0.55f, 0.55f), new Color32(246, 222, 135, 255), 7, false);

        BoxCollider2D collider = house.AddComponent<BoxCollider2D>();
        collider.offset = new Vector2(0f, 0.25f);
        collider.size = new Vector2(3.65f, 2.85f);
    }

    private void CreateTree(Transform parent, Vector2 position, float scale)
    {
        GameObject tree = new GameObject("Forest Tree");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = position;
        tree.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = TreeSprite;
        renderer.sortingOrder = 10;

        CircleCollider2D collider = tree.AddComponent<CircleCollider2D>();
        collider.offset = new Vector2(0f, 0.35f);
        collider.radius = 0.42f;
    }

    private void CreateFlower(Transform parent, Vector2 position, float scale)
    {
        GameObject flower = new GameObject("Wildflower");
        flower.transform.SetParent(parent, false);
        flower.transform.localPosition = position;
        flower.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = flower.AddComponent<SpriteRenderer>();
        renderer.sprite = FlowerSprite;
        renderer.sortingOrder = 1;
    }

    private void CreateFence(Transform parent, Vector2 startPosition, int posts)
    {
        for (int i = 0; i < posts; i++)
        {
            Vector2 postPosition = startPosition + new Vector2(i * 0.75f, 0f);
            CreateRect("Fence Post", parent, postPosition, new Vector2(0.22f, 0.75f), FenceColor, 2, true);
        }

        CreateRect("Fence Rail", parent, startPosition + new Vector2((posts - 1) * 0.375f, 0.18f), new Vector2(posts * 0.75f, 0.18f), FenceColor, 2, true);
    }

    private GameObject CreateRect(string objectName, Transform parent, Vector2 position, Vector2 size, Color color, int sortingOrder, bool hasCollider)
    {
        GameObject rect = new GameObject(objectName);
        rect.transform.SetParent(parent, false);
        rect.transform.localPosition = position;
        rect.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = rect.AddComponent<SpriteRenderer>();
        renderer.sprite = SquareSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        if (hasCollider)
        {
            rect.AddComponent<BoxCollider2D>();
        }

        return rect;
    }

    private void CreateCollider(string objectName, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject colliderObject = new GameObject(objectName);
        colliderObject.transform.SetParent(parent, false);
        colliderObject.transform.localPosition = position;

        BoxCollider2D collider = colliderObject.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static void DestroyGeneratedObject(Object target)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(target);
            return;
        }
#endif

        Destroy(target);
    }

    private static Sprite SquareSprite
    {
        get
        {
            if (squareSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply();
                texture.hideFlags = HideFlags.HideAndDontSave;

                squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                squareSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return squareSprite;
        }
    }

    private static Sprite TreeSprite
    {
        get
        {
            if (treeSprite == null)
            {
                Texture2D texture = CreateTransparentTexture(24, 32);
                FillRect(texture, 10, 0, 4, 11, new Color32(108, 73, 42, 255));
                FillEllipse(texture, 12, 14, 8, 8, new Color32(52, 113, 74, 255));
                FillEllipse(texture, 8, 18, 7, 8, new Color32(66, 139, 85, 255));
                FillEllipse(texture, 16, 18, 7, 8, new Color32(63, 132, 83, 255));
                FillEllipse(texture, 12, 24, 8, 7, new Color32(78, 154, 90, 255));
                texture.Apply();

                treeSprite = Sprite.Create(texture, new Rect(0f, 0f, 24f, 32f), new Vector2(0.5f, 0f), 16f);
                treeSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return treeSprite;
        }
    }

    private static Sprite PlayerSprite
    {
        get
        {
            if (playerSprite == null)
            {
                Texture2D texture = CreateTransparentTexture(16, 24);
                FillRect(texture, 5, 1, 2, 4, new Color32(69, 75, 87, 255));
                FillRect(texture, 9, 1, 2, 4, new Color32(69, 75, 87, 255));
                FillRect(texture, 4, 5, 8, 8, new Color32(99, 148, 129, 255));
                FillRect(texture, 3, 8, 10, 4, new Color32(111, 165, 143, 255));
                FillRect(texture, 5, 13, 6, 5, new Color32(229, 178, 137, 255));
                FillRect(texture, 4, 17, 8, 4, new Color32(91, 61, 49, 255));
                FillRect(texture, 6, 15, 1, 1, new Color32(48, 47, 45, 255));
                FillRect(texture, 10, 15, 1, 1, new Color32(48, 47, 45, 255));
                FillRect(texture, 7, 13, 2, 1, new Color32(170, 89, 87, 255));
                FillRect(texture, 2, 6, 2, 5, new Color32(229, 178, 137, 255));
                FillRect(texture, 12, 6, 2, 5, new Color32(229, 178, 137, 255));
                texture.Apply();

                playerSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 24f), new Vector2(0.5f, 0.05f), 16f);
                playerSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return playerSprite;
        }
    }

    private static Sprite FlowerSprite
    {
        get
        {
            if (flowerSprite == null)
            {
                Texture2D texture = CreateTransparentTexture(8, 8);
                FillRect(texture, 3, 0, 2, 5, new Color32(57, 127, 72, 255));
                FillRect(texture, 1, 4, 2, 2, new Color32(224, 137, 169, 255));
                FillRect(texture, 5, 4, 2, 2, new Color32(224, 137, 169, 255));
                FillRect(texture, 3, 5, 2, 2, new Color32(245, 219, 104, 255));
                texture.Apply();

                flowerSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0f), 16f);
                flowerSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return flowerSprite;
        }
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        texture.SetPixels(pixels);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private static void FillRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
    {
        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        for (int y = -radiusY; y <= radiusY; y++)
        {
            for (int x = -radiusX; x <= radiusX; x++)
            {
                float normalizedX = x / (float)radiusX;
                float normalizedY = y / (float)radiusY;

                if (normalizedX * normalizedX + normalizedY * normalizedY <= 1f)
                {
                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    if (pixelX >= 0 && pixelX < texture.width && pixelY >= 0 && pixelY < texture.height)
                    {
                        texture.SetPixel(pixelX, pixelY, color);
                    }
                }
            }
        }
    }
}
