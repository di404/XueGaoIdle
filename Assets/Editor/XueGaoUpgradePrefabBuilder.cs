using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XueGao;

public static class XueGaoUpgradePrefabBuilder
{
    private const string Prefabs = "Assets/XueGao/Prefabs";
    private const string UpgradeItemPath = Prefabs + "/UpgradeItem.prefab";
    private const string GameUIPath = Prefabs + "/GameUI.prefab";
    private const string GameRootPath = Prefabs + "/GameRoot.prefab";

    private static readonly UpgradeConfig[] Configs =
    {
        new UpgradeConfig(UpgradeType.BiteSize, "大口咬", "咬痕范围更大，每次能吃掉更多雪糕。", "Assets/XueGao/Art/MouthPreview.png", 20, 1.8f, 20, 1),
        new UpgradeConfig(UpgradeType.AutoBite, "自动吃", "聚焦吃雪糕时，自动帮你连续咬雪糕。", "Assets/XueGao/Art/Stick.png", 50, 2f, 12, 0),
        new UpgradeConfig(UpgradeType.Luck, "幸运值", "降低谢谢参与权重，让中奖概率更友好。", "Assets/XueGao/Art/LuxuryIce.png", 80, 1.9f, 15, 0),
        new UpgradeConfig(UpgradeType.TableSpace, "大桌面", "桌面容量增加，可以同时摆更多雪糕。", "Assets/XueGao/Art/OldIce.png", 120, 2.05f, 6, 0),
        new UpgradeConfig(UpgradeType.PrizeBonus, "奖金加成", "每次中奖结算时获得额外奖金。", "Assets/XueGao/Art/CreamIce.png", 160, 2.15f, 10, 0)
    };

    [MenuItem("XueGao/Build Upgrade Prefabs")]
    public static void BuildUpgradePrefabs()
    {
        GameObject itemPrefab = SaveUpgradeItemPrefab();
        UpdateGameUIPrefab(itemPrefab);
        UpdateGameRootPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("XueGao upgrade prefabs and bindings were built.");
    }

    private static GameObject SaveUpgradeItemPrefab()
    {
        GameObject root = new GameObject("UpgradeItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(UpgradeItemView));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 94f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.98f, 0.96f, 0.88f, 0.98f);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 94f;
        layout.preferredHeight = 94f;
        layout.flexibleWidth = 1f;

        Image icon = CreateImage("Icon", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 8f), new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.92f));
        Text name = CreateText("NameText", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(110f, -18f), new Vector2(-122f, 24f), 17, TextAnchor.MiddleLeft, new Color(0.1f, 0.14f, 0.16f, 1f));
        Text description = CreateText("DescriptionText", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(110f, -47f), new Vector2(-122f, 34f), 12, TextAnchor.UpperLeft, new Color(0.22f, 0.24f, 0.24f, 1f));
        Text level = CreateText("LevelText", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -18f), new Vector2(54f, 22f), 14, TextAnchor.MiddleRight, new Color(0.15f, 0.3f, 0.36f, 1f));
        Button button = CreateButton("UpgradeButton", root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 22f), new Vector2(78f, 30f), "￥20");
        Text price = button.GetComponentInChildren<Text>(true);

        SerializedObject view = new SerializedObject(root.GetComponent<UpgradeItemView>());
        view.FindProperty("iconImage").objectReferenceValue = icon;
        view.FindProperty("nameText").objectReferenceValue = name;
        view.FindProperty("descriptionText").objectReferenceValue = description;
        view.FindProperty("levelText").objectReferenceValue = level;
        view.FindProperty("upgradeButton").objectReferenceValue = button;
        view.FindProperty("priceText").objectReferenceValue = price;
        view.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UpgradeItemPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void UpdateGameUIPrefab(GameObject itemPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameUIPath);
        try
        {
            Transform panel = root.transform.Find("UpgradePanel");
            if (panel == null)
            {
                throw new InvalidOperationException("UpgradePanel not found in GameUI prefab.");
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(258f, 530f);
            panelRect.anchoredPosition = new Vector2(-16f, -64f);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            DeleteChildren(panel, "MouthUpgradeButton", "AutoBiteUpgradeButton", "LuckUpgradeButton",
                "BiteSizeUpgradeItem", "AutoBiteUpgradeItem", "LuckUpgradeItem", "TableSpaceUpgradeItem", "PrizeBonusUpgradeItem");

            UpgradeItemView[] views = new UpgradeItemView[Configs.Length];
            for (int i = 0; i < Configs.Length; i++)
            {
                UpgradeConfig config = Configs[i];
                GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab, panel);
                item.name = config.Type + "UpgradeItem";
                ConfigureUpgradeItem(item.GetComponent<UpgradeItemView>(), config, true);
                views[i] = item.GetComponent<UpgradeItemView>();
            }

            GameUI ui = root.GetComponent<GameUI>();
            SerializedObject uiObject = new SerializedObject(ui);
            SerializedProperty items = uiObject.FindProperty("upgradeItems");
            items.arraySize = views.Length;
            for (int i = 0; i < views.Length; i++)
            {
                items.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            }

            uiObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, GameUIPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateGameRootPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(GameRootPath);
        try
        {
            UpgradeManager upgradeManager = root.GetComponent<UpgradeManager>();
            if (upgradeManager == null)
            {
                upgradeManager = root.AddComponent<UpgradeManager>();
            }

            ConfigureUpgradeManager(upgradeManager);

            GameManager gameManager = root.GetComponent<GameManager>();
            SerializedObject gameManagerObject = new SerializedObject(gameManager);
            gameManagerObject.FindProperty("upgradeManager").objectReferenceValue = upgradeManager;
            gameManagerObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, GameRootPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureUpgradeManager(UpgradeManager upgradeManager)
    {
        SerializedObject managerObject = new SerializedObject(upgradeManager);
        SerializedProperty upgrades = managerObject.FindProperty("upgrades");
        upgrades.arraySize = Configs.Length;
        for (int i = 0; i < Configs.Length; i++)
        {
            UpgradeConfig config = Configs[i];
            SerializedProperty entry = upgrades.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("type").enumValueIndex = (int)config.Type;
            entry.FindPropertyRelative("displayName").stringValue = config.DisplayName;
            entry.FindPropertyRelative("description").stringValue = config.Description;
            entry.FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(config.IconPath);
            entry.FindPropertyRelative("baseCost").intValue = config.BaseCost;
            entry.FindPropertyRelative("costGrowth").floatValue = config.CostGrowth;
            entry.FindPropertyRelative("maxLevel").intValue = config.MaxLevel;
            entry.FindPropertyRelative("displayLevelOffset").intValue = config.DisplayLevelOffset;
            entry.FindPropertyRelative("level").intValue = 0;
        }

        managerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureUpgradeItem(UpgradeItemView view, UpgradeConfig config, bool setPreviewText)
    {
        SerializedObject viewObject = new SerializedObject(view);
        viewObject.FindProperty("upgradeType").enumValueIndex = (int)config.Type;
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        if (!setPreviewText)
        {
            return;
        }

        Transform transform = view.transform;
        SetText(transform, "NameText", config.DisplayName);
        SetText(transform, "DescriptionText", config.Description);
        SetText(transform, "LevelText", "Lv." + config.DisplayLevelOffset);
        SetText(transform, "UpgradeButton/PriceText", "￥" + config.BaseCost);

        Image icon = transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(config.IconPath);
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
        }
    }

    private static void DeleteChildren(Transform parent, params string[] names)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            for (int j = 0; j < names.Length; j++)
            {
                if (child.name == names[j])
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    break;
                }
            }
        }
    }

    private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.color = color;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 0.76f, 0.25f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text price = CreateText("PriceText", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8f, -4f), 14, TextAnchor.MiddleCenter, new Color(0.08f, 0.1f, 0.1f, 1f));
        price.text = label;
        return button;
    }

    private static void SetText(Transform root, string path, string value)
    {
        Text text = root.Find(path)?.GetComponent<Text>();
        if (text != null)
        {
            text.text = value;
        }
    }

    private readonly struct UpgradeConfig
    {
        public UpgradeConfig(UpgradeType type, string displayName, string description, string iconPath, int baseCost, float costGrowth, int maxLevel, int displayLevelOffset)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
            IconPath = iconPath;
            BaseCost = baseCost;
            CostGrowth = costGrowth;
            MaxLevel = maxLevel;
            DisplayLevelOffset = displayLevelOffset;
        }

        public UpgradeType Type { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string IconPath { get; }
        public int BaseCost { get; }
        public float CostGrowth { get; }
        public int MaxLevel { get; }
        public int DisplayLevelOffset { get; }
    }
}
