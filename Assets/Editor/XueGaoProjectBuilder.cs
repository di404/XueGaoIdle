using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XueGao;

public static class XueGaoProjectBuilder
{
    private const string Root = "Assets/XueGao";
    private const string Art = Root + "/Art";
    private const string Data = Root + "/Data";
    private const string Prefabs = Root + "/Prefabs";
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("XueGao/Build Formal Game")]
    public static void BuildFormalGame()
    {
        EnsureFolders();
        Dictionary<string, Sprite> sprites = CreateSprites();
        List<IceCreamDefinition> definitions = CreateDefinitions(sprites);

        GameObject playAreaPrefab = SavePlayAreaPrefab(sprites);
        GameObject uiPrefab = SaveUIPrefab();
        GameObject rootPrefab = SaveGameRootPrefab(playAreaPrefab, uiPrefab, definitions);
        BuildScene(rootPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("XueGao formal game prefabs, data, placeholder sprites, and scene were built.");
    }

    [MenuItem("XueGao/Validate Formal Game")]
    public static void ValidateFormalGame()
    {
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/GameRoot.prefab") != null, "GameRoot prefab exists");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/GameUI.prefab") != null, "GameUI prefab exists");
        Require(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/IceCreamPlayArea.prefab") != null, "IceCreamPlayArea prefab exists");

        string[] definitionPaths =
        {
            Data + "/OldIce.asset",
            Data + "/CreamIce.asset",
            Data + "/ChocolateIce.asset",
            Data + "/LuxuryIce.asset"
        };

        foreach (string definitionPath in definitionPaths)
        {
            IceCreamDefinition definition = AssetDatabase.LoadAssetAtPath<IceCreamDefinition>(definitionPath);
            Require(definition != null, definitionPath + " definition exists");
            Require(definition.fullSprite != null, definitionPath + " has full sprite");
            Require(definition.stickSprite != null, definitionPath + " has stick sprite");
            Require(definition.prizeMultiplier >= 1, definitionPath + " has multiplier");
        }

        ValidateReadableSprite(Art + "/OldIce.png");
        ValidateReadableSprite(Art + "/CreamIce.png");
        ValidateReadableSprite(Art + "/ChocolateIce.png");
        ValidateReadableSprite(Art + "/LuxuryIce.png");
        ValidateReadableSprite(Art + "/Stick.png");
        ValidateReadableSprite(Art + "/MouthPreview.png");

        GameObject rootPrefab = PrefabUtility.LoadPrefabContents(Prefabs + "/GameRoot.prefab");
        try
        {
            Require(rootPrefab.GetComponent<GameManager>() != null, "GameRoot has GameManager");
            Require(rootPrefab.GetComponent<LotterySystem>() != null, "GameRoot has LotterySystem");
            Require(rootPrefab.GetComponentInChildren<IceCreamEater>(true) != null, "GameRoot has IceCreamEater");
            Require(rootPrefab.GetComponentInChildren<MouthController>(true) != null, "GameRoot has MouthController");
            Require(rootPrefab.GetComponentInChildren<GameUI>(true) != null, "GameRoot has GameUI");
            Require(rootPrefab.GetComponentInChildren<JuicyFeedbacks>(true) != null, "GameRoot has JuicyFeedbacks");
            Require(rootPrefab.GetComponentsInChildren<MMF_Player>(true).Length >= 3, "GameRoot has Feel MMF_Player components");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(rootPrefab);
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Require(GameObject.Find("GameRoot") != null, "SampleScene has GameRoot");
        Require(GameObject.Find("EventSystem") != null, "SampleScene has EventSystem");
        Require(GameObject.Find("Main Camera") != null, "SampleScene has Main Camera");
        if (SceneManager.sceneCount > 1)
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("XueGao validation passed.");
    }

    private static void EnsureFolders()
    {
        CreateFolder("Assets", "XueGao");
        CreateFolder(Root, "Art");
        CreateFolder(Root, "Data");
        CreateFolder(Root, "Prefabs");
    }

    private static void CreateFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void ValidateReadableSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Require(sprite != null, path + " sprite exists");
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        Require(importer != null && importer.isReadable, path + " is readable");
        Require(importer != null && importer.textureType == TextureImporterType.Sprite, path + " is sprite");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException("XueGao validation failed: " + message);
        }
    }

    private static Dictionary<string, Sprite> CreateSprites()
    {
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        sprites["MouthPreview"] = CreateCircleSprite("MouthPreview", 128, new Color(1f, 0.2f, 0.2f, 0.55f), new Color(1f, 0.92f, 0.88f, 0.7f));
        sprites["Stick"] = CreateStickSprite("Stick", "再来一根");
        sprites["OldIce"] = CreateIceCreamSprite("OldIce", new Color(0.55f, 0.9f, 1f, 1f), new Color(0.18f, 0.58f, 0.95f, 1f));
        sprites["CreamIce"] = CreateIceCreamSprite("CreamIce", new Color(1f, 0.92f, 0.72f, 1f), new Color(1f, 0.65f, 0.25f, 1f));
        sprites["ChocolateIce"] = CreateIceCreamSprite("ChocolateIce", new Color(0.36f, 0.18f, 0.09f, 1f), new Color(0.92f, 0.5f, 0.2f, 1f));
        sprites["LuxuryIce"] = CreateIceCreamSprite("LuxuryIce", new Color(1f, 0.28f, 0.55f, 1f), new Color(0.4f, 0.85f, 1f, 1f));
        return sprites;
    }

    private static Sprite CreateIceCreamSprite(string name, Color primary, Color secondary)
    {
        int width = 512;
        int height = 512;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(width * 0.5f, height * 0.58f);
        float halfWidth = width * 0.23f;
        float halfHeight = height * 0.35f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x - center.x) / halfWidth;
                float ny = (y - center.y) / halfHeight;
                bool isCream = nx * nx + ny * ny <= 1f && y > height * 0.18f;
                if (!isCream)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float stripe = Mathf.Sin((x + y) * 0.045f) * 0.5f + 0.5f;
                texture.SetPixel(x, y, Color.Lerp(primary, secondary, stripe * 0.38f));
            }
        }

        return SaveSprite(name, texture, 128f);
    }

    private static Sprite CreateStickSprite(string name, string label)
    {
        int width = 256;
        int height = 512;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs(x - width * 0.5f) / (width * 0.25f);
                bool inside = nx < 1f && y > 16 && y < height - 16;
                texture.SetPixel(x, y, inside ? new Color(0.78f, 0.55f, 0.32f, 1f) : Color.clear);
            }
        }

        return SaveSprite(name, texture, 128f);
    }

    private static Sprite CreateCircleSprite(string name, int size, Color ring, Color fill)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance > size * 0.48f)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    texture.SetPixel(x, y, distance < size * 0.22f ? fill : ring);
                }
            }
        }

        return SaveSprite(name, texture, 128f);
    }

    private static Sprite SaveSprite(string name, Texture2D texture, float pixelsPerUnit)
    {
        string path = Art + "/" + name + ".png";
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static List<IceCreamDefinition> CreateDefinitions(Dictionary<string, Sprite> sprites)
    {
        List<IceCreamDefinition> definitions = new List<IceCreamDefinition>
        {
            CreateDefinition("OldIce", "老冰棍", 0, 1, sprites["OldIce"], sprites["Stick"], new Color(0.55f, 0.9f, 1f), new Color(0.18f, 0.58f, 0.95f)),
            CreateDefinition("CreamIce", "奶油雪糕", 80, 2, sprites["CreamIce"], sprites["Stick"], new Color(1f, 0.92f, 0.72f), new Color(1f, 0.65f, 0.25f)),
            CreateDefinition("ChocolateIce", "巧克力脆皮", 250, 4, sprites["ChocolateIce"], sprites["Stick"], new Color(0.36f, 0.18f, 0.09f), new Color(0.92f, 0.5f, 0.2f)),
            CreateDefinition("LuxuryIce", "豪华雪糕", 800, 8, sprites["LuxuryIce"], sprites["Stick"], new Color(1f, 0.28f, 0.55f), new Color(0.4f, 0.85f, 1f))
        };
        return definitions;
    }

    private static IceCreamDefinition CreateDefinition(string assetName, string displayName, int price, int multiplier, Sprite fullSprite, Sprite stickSprite, Color primary, Color secondary)
    {
        string path = Data + "/" + assetName + ".asset";
        IceCreamDefinition definition = AssetDatabase.LoadAssetAtPath<IceCreamDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<IceCreamDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }

        definition.displayName = displayName;
        definition.price = price;
        definition.prizeMultiplier = multiplier;
        definition.fullSprite = fullSprite;
        definition.stickSprite = stickSprite;
        definition.primaryColor = primary;
        definition.secondaryColor = secondary;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static GameObject SavePlayAreaPrefab(Dictionary<string, Sprite> sprites)
    {
        GameObject root = new GameObject("IceCreamPlayArea");
        root.transform.position = new Vector3(-1.9f, -0.2f, 0f);

        GameObject iceCream = new GameObject("IceCreamSprite");
        iceCream.transform.SetParent(root.transform, false);
        SpriteRenderer iceRenderer = iceCream.AddComponent<SpriteRenderer>();
        iceRenderer.sortingOrder = 2;

        GameObject stick = new GameObject("StickReveal");
        stick.transform.SetParent(root.transform, false);
        SpriteRenderer stickRenderer = stick.AddComponent<SpriteRenderer>();
        stickRenderer.sprite = sprites["Stick"];
        stickRenderer.sortingOrder = 1;
        stick.SetActive(false);

        GameObject preview = new GameObject("MouthPreview");
        preview.transform.SetParent(root.transform, false);
        SpriteRenderer previewRenderer = preview.AddComponent<SpriteRenderer>();
        previewRenderer.sprite = sprites["MouthPreview"];
        previewRenderer.sortingOrder = 5;

        IceCreamEater eater = root.AddComponent<IceCreamEater>();
        MouthController mouth = root.AddComponent<MouthController>();
        SetObject(eater, "iceCreamRenderer", iceRenderer);
        SetObject(eater, "stickRenderer", stickRenderer);
        SetObject(mouth, "preview", preview.transform);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/IceCreamPlayArea.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject SaveUIPrefab()
    {
        GameObject canvasObject = new GameObject("GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUI));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 600);

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.sizeDelta = new Vector2(960, 600);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Image background = CreatePanel("Background", canvasObject.transform, new Color(0.98f, 0.95f, 0.88f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        background.raycastTarget = false;

        Image topBar = CreatePanel("TopBar", canvasObject.transform, new Color(0.16f, 0.32f, 0.45f), new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -70f), Vector2.zero);
        HorizontalLayoutGroup topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(16, 16, 10, 10);
        topLayout.spacing = 16;
        topLayout.childForceExpandWidth = true;

        Text money = CreateText("MoneyText", topBar.transform, "￥0", font, 25, Color.white, TextAnchor.MiddleLeft);
        Text sticks = CreateText("SticksText", topBar.transform, "雪糕棍 0 根", font, 23, Color.white, TextAnchor.MiddleLeft);
        Text current = CreateText("CurrentIceCreamText", topBar.transform, "老冰棍 x1", font, 23, Color.white, TextAnchor.MiddleLeft);
        Text mouth = CreateText("MouthLevelText", topBar.transform, "嘴巴 Lv.1", font, 23, Color.white, TextAnchor.MiddleLeft);

        Text progressText = CreateText("ProgressText", canvasObject.transform, "已吃 0% / 80%", font, 22, new Color(0.15f, 0.2f, 0.23f), TextAnchor.MiddleCenter);
        Stretch(progressText.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.56f, 0f), new Vector2(0f, 52f), new Vector2(0f, 84f));
        Slider progress = CreateSlider("ProgressSlider", canvasObject.transform, new Vector2(0.08f, 0f), new Vector2(0.56f, 0f), new Vector2(0f, 22f), new Vector2(0f, 48f));

        Image side = CreatePanel("SidePanel", canvasObject.transform, new Color(0.92f, 0.98f, 1f, 0.96f), new Vector2(0.62f, 0f), Vector2.one, new Vector2(12f, 18f), new Vector2(-18f, -88f));
        VerticalLayoutGroup sideLayout = side.gameObject.AddComponent<VerticalLayoutGroup>();
        sideLayout.padding = new RectOffset(14, 14, 14, 14);
        sideLayout.spacing = 8;
        sideLayout.childControlWidth = true;
        sideLayout.childForceExpandWidth = true;
        sideLayout.childControlHeight = false;

        CanvasGroup prizeGroup = side.gameObject.AddComponent<CanvasGroup>();
        Text prize = CreateText("PrizeText", side.transform, "吃完雪糕后会展示雪糕棒并开奖。", font, 22, new Color(0.1f, 0.2f, 0.26f), TextAnchor.MiddleCenter);
        prize.rectTransform.sizeDelta = new Vector2(0f, 70f);
        Text history = CreateText("HistoryText", side.transform, "开奖记录：暂无", font, 17, new Color(0.14f, 0.18f, 0.2f), TextAnchor.UpperLeft);
        history.rectTransform.sizeDelta = new Vector2(0f, 92f);

        Button mouthButton = CreateButton("MouthUpgradeButton", side.transform, "升级嘴巴", font);
        Button autoButton = CreateButton("AutoBiteUpgradeButton", side.transform, "升级自动吃", font);
        Button luckButton = CreateButton("LuckUpgradeButton", side.transform, "升级幸运值", font);

        Text shopTitle = CreateText("ShopTitle", side.transform, "雪糕商店", font, 23, new Color(0.1f, 0.18f, 0.22f), TextAnchor.MiddleCenter);
        shopTitle.rectTransform.sizeDelta = new Vector2(0f, 34f);

        GameObject shopRoot = new GameObject("ShopRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
        shopRoot.transform.SetParent(side.transform, false);
        VerticalLayoutGroup shopLayout = shopRoot.GetComponent<VerticalLayoutGroup>();
        shopLayout.spacing = 6;
        shopLayout.childControlWidth = true;
        shopLayout.childControlHeight = false;
        shopLayout.childForceExpandWidth = true;

        Button shopPrefab = CreateButton("ShopButtonPrefab", shopRoot.transform, "Shop Item", font);
        shopPrefab.gameObject.SetActive(false);

        GameUI gameUI = canvasObject.GetComponent<GameUI>();
        SetObject(gameUI, "moneyText", money);
        SetObject(gameUI, "sticksText", sticks);
        SetObject(gameUI, "currentIceCreamText", current);
        SetObject(gameUI, "mouthLevelText", mouth);
        SetObject(gameUI, "progressSlider", progress);
        SetObject(gameUI, "progressText", progressText);
        SetObject(gameUI, "prizeText", prize);
        SetObject(gameUI, "historyText", history);
        SetObject(gameUI, "mouthUpgradeButton", mouthButton);
        SetObject(gameUI, "mouthUpgradeText", mouthButton.GetComponentInChildren<Text>());
        SetObject(gameUI, "autoBiteUpgradeButton", autoButton);
        SetObject(gameUI, "autoBiteUpgradeText", autoButton.GetComponentInChildren<Text>());
        SetObject(gameUI, "luckUpgradeButton", luckButton);
        SetObject(gameUI, "luckUpgradeText", luckButton.GetComponentInChildren<Text>());
        SetObject(gameUI, "shopRoot", shopRoot.transform);
        SetObject(gameUI, "shopButtonPrefab", shopPrefab);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObject, Prefabs + "/GameUI.prefab");
        Object.DestroyImmediate(canvasObject);
        return prefab;
    }

    private static GameObject SaveGameRootPrefab(GameObject playAreaPrefab, GameObject uiPrefab, List<IceCreamDefinition> definitions)
    {
        GameObject root = new GameObject("GameRoot");
        GameObject playArea = (GameObject)PrefabUtility.InstantiatePrefab(playAreaPrefab, root.transform);
        GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, root.transform);

        GameManager gameManager = root.AddComponent<GameManager>();
        LotterySystem lottery = root.AddComponent<LotterySystem>();

        GameObject feedbackRoot = new GameObject("FeelFeedbacks");
        feedbackRoot.transform.SetParent(root.transform, false);
        MMF_Player biteFeedbacks = CreateFeedbackPlayer("BiteFeedbacks", feedbackRoot.transform);
        MMF_Player completeFeedbacks = CreateFeedbackPlayer("CompleteFeedbacks", feedbackRoot.transform);
        MMF_Player prizeFeedbacks = CreateFeedbackPlayer("PrizeFeedbacks", feedbackRoot.transform);
        JuicyFeedbacks juicy = feedbackRoot.AddComponent<JuicyFeedbacks>();

        IceCreamEater eater = playArea.GetComponent<IceCreamEater>();
        MouthController mouth = playArea.GetComponent<MouthController>();
        GameUI gameUI = ui.GetComponent<GameUI>();

        SetObject(gameManager, "iceCreams", definitions);
        SetObject(gameManager, "eater", eater);
        SetObject(gameManager, "mouth", mouth);
        SetObject(gameManager, "lottery", lottery);
        SetObject(gameManager, "ui", gameUI);
        SetObject(gameManager, "feedbacks", juicy);

        Transform iceCreamTransform = playArea.transform.Find("IceCreamSprite");
        Transform stickTransform = playArea.transform.Find("StickReveal");
        CanvasGroup prizePanel = ui.transform.Find("SidePanel").GetComponent<CanvasGroup>();
        SetObject(juicy, "biteFeedbacks", biteFeedbacks);
        SetObject(juicy, "completeFeedbacks", completeFeedbacks);
        SetObject(juicy, "prizeFeedbacks", prizeFeedbacks);
        SetObject(juicy, "iceCreamTarget", iceCreamTransform);
        SetObject(juicy, "stickTarget", stickTransform);
        SetObject(juicy, "prizePanel", prizePanel);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/GameRoot.prefab");
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static MMF_Player CreateFeedbackPlayer(string name, Transform parent)
    {
        GameObject playerObject = new GameObject(name);
        playerObject.transform.SetParent(parent, false);
        return playerObject.AddComponent<MMF_Player>();
    }

    private static void BuildScene(GameObject rootPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (GameObject gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(gameObject);
        }

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.4f;
        camera.backgroundColor = new Color(0.98f, 0.95f, 0.88f);

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        eventSystem.AddComponent<RuntimeUIInputBinder>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
        PrefabUtility.InstantiatePrefab(rootPrefab);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Text CreateText(string name, Transform parent, string content, Font font, int size, Color color, TextAnchor anchor)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        gameObject.transform.SetParent(parent, false);
        Text text = gameObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        return text;
    }

    private static Image CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        Stretch(image.rectTransform, anchorMin, anchorMax, offsetMin, offsetMax);
        return image;
    }

    private static Button CreateButton(string name, Transform parent, string label, Font font)
    {
        Image image = CreatePanel(name, parent, new Color(1f, 0.78f, 0.32f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        image.rectTransform.sizeDelta = new Vector2(0f, 68f);
        Button button = image.gameObject.AddComponent<Button>();
        Text text = CreateText("Label", image.transform, label, font, 19, new Color(0.1f, 0.14f, 0.16f), TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        Stretch(sliderObject.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
        Slider slider = sliderObject.GetComponent<Slider>();

        Image background = CreatePanel("Background", sliderObject.transform, new Color(0.78f, 0.78f, 0.72f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fillArea = CreatePanel("Fill Area", sliderObject.transform, Color.clear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fill = CreatePanel("Fill", fillArea.transform, new Color(0.35f, 0.75f, 0.95f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = background;
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static void SetObject(Object target, string propertyName, object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("Missing serialized property " + propertyName + " on " + target.name);
            return;
        }

        if (property.isArray && value is System.Collections.IList list)
        {
            property.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = list[i] as Object;
            }
        }
        else if (value is Object objectValue)
        {
            property.objectReferenceValue = objectValue;
        }
        else
        {
            Debug.LogError("Unsupported serialized assignment for " + propertyName);
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
}
