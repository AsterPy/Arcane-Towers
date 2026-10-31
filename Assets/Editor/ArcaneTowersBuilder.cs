using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class ArcaneTowersBuilder
{
    // ==========================================
    // МЕНЮ КЕРУВАННЯ СЦЕНОЮ ТА ПРЕФАБАМИ
    // (Всі дані, позиції та баланс зберігаються в Assets/Prefabs/)
    // ==========================================

    [MenuItem("Arcane Towers/Зберегти зміни сцени в префаби", false, 1)]
    public static void SaveHierarchyToPrefabs()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Arcane Towers",
                "Зупиніть режим гри (Play Mode ▶️) перед збереженням префабів!\n\n" +
                "У Unity будь-які зміни чи переміщення об'єктів під час Play Mode є тимчасовими й автоматично скасовуються рушієм після зупинки гри.\n\n" +
                "Налаштуйте розташування мана-бару чи інших об'єктів у режимі редагування (Edit Mode), а потім натисніть «Зберегти зміни сцени в префаби».",
                "Зрозуміло");
            return;
        }

        string folder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            string prefabName = root.name;
            if (prefabName == "Canvas" || prefabName == "UI Canvas")
            {
                prefabName = "[UI CANVAS]";
                root.name = "[UI CANVAS]";
            }

            if (prefabName.StartsWith("[") || prefabName == "Main Camera")
            {
                string path = $"{folder}/{prefabName}.prefab";
                try
                {
                    PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
                    count++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Arcane Towers] Прямий запис префаба {root.name}: {ex.Message}");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Arcane Towers] Успішно збережено {count} префабів у {folder}/ та збережено сцену!");
        EditorUtility.DisplayDialog("Arcane Towers",
            $"Успішно збережено {count} префабів у Assets/Prefabs/ та саму сцену!\n\n" +
            "Всі ваші переміщення об'єктів ([UI CANVAS], карти, башти, арена) надійно збережені в префаби.",
            "Чудово!");
    }

    [MenuItem("Arcane Towers/Зібрати сцену", false, 2)]
    public static void BuildScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Arcane Towers", "Зупиніть режим Play перед білдом сцени!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Arcane Towers",
            "Зібрати сцену гри?\nЯкщо в папці Assets/Prefabs/ є ваші збережені префаби ([UI CANVAS], [TOWERS], [ARENA]), вони будуть завантажені зі збереженими позиціями.",
            "Зібрати сцену", "Скасувати")) return;

        ExecuteBuild(true);
    }

    // ==========================================
    // ОСНОВНИЙ БІЛДИНГ СЦЕНИ
    // ==========================================
    public static void ExecuteBuild(bool showDialog = true)
    {
        try
        {
            QualitySettings.globalTextureMipmapLimit = 0; // Force full-resolution textures without downscaling

            string[] baseSpritePaths = new string[]
            {
                "Assets/Sprites/map.png", "Assets/Sprites/river.png", "Assets/Sprites/road.png",
                "Assets/Sprites/bridge.png", "Assets/Sprites/lock.png", "Assets/Sprites/enemy_lock.png",
                "Assets/Sprites/tower.png", "Assets/Sprites/enemy_tower.png", "Assets/Sprites/board.png",
                "Assets/Sprites/cards/card_swordsman.png", "Assets/Sprites/cards/card_archer.png",
                "Assets/Sprites/cards/card_golem.png", "Assets/Sprites/cards/card_dron.png",
                "Assets/Sprites/cards/card_fball.png", "Assets/Sprites/cards/card_heal.png"
            };

            foreach (string p in baseSpritePaths)
            {
                OptimizeSpriteImport(p);
            }

            if (System.IO.Directory.Exists("Assets/Sprites/cards"))
            {
                string[] cardPngs = System.IO.Directory.GetFiles("Assets/Sprites/cards", "*.png", System.IO.SearchOption.AllDirectories);
                foreach (string cp in cardPngs)
                {
                    OptimizeSpriteImport(cp.Replace('\\', '/'));
                }
            }

            // 1. Load all custom user sprites
            Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/map.png");
            Sprite riverSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/river.png");
            Sprite roadSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/road.png");
            Sprite bridgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/bridge.png");
            Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/lock.png");
            Sprite enemyLockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/enemy_lock.png");
            Sprite towerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/tower.png");
            Sprite enemyTowerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/enemy_tower.png");
            Sprite boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/board.png");
            Sprite gradientSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/arena_gradient.png");

            Sprite cardSwordsman = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cards/card_swordsman.png");
            Sprite cardArcher = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cards/card_archer.png");
            Sprite cardGolem = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cards/card_golem.png");
            Sprite cardDrone = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cards/card_dron.png");
            Sprite cardFball = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cards/card_fball.png");
            Sprite cardHeal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/cards/card_heal.png");

            // 2. Safe cleanup of active scene roots (prevents MissingReferenceException)
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            for (int i = rootObjects.Length - 1; i >= 0; i--)
            {
                if (rootObjects[i] != null)
                {
                    Object.DestroyImmediate(rootObjects[i]);
                }
            }

            // ==========================================
            // 1. CAMERA (Fitted 1:1 to 1080x1920 Canvas)
            // ==========================================
            GameObject camPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Main Camera.prefab");
            Camera cam = null;
            if (camPrefab != null)
            {
                GameObject camObj = PrefabUtility.InstantiatePrefab(camPrefab) as GameObject;
                camObj.name = "Main Camera";
                cam = camObj.GetComponent<Camera>();
            }

            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.orthographic = true;
                cam.orthographicSize = 9.6f;
                cam.transform.position = new Vector3(0, 0, -10);
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
                cam.allowMSAA = true;
                cam.gameObject.AddComponent<ArcaneCameraAspect>();
                cam.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                if (cam.GetComponent<ArcaneCameraAspect>() == null)
                {
                    cam.gameObject.AddComponent<ArcaneCameraAspect>();
                }
                if (cam.GetComponent<AudioListener>() == null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                }
            }

            // ==========================================
            // 2. ROOT GROUP: [ARENA]
            // ==========================================
            GameObject arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/[ARENA].prefab");
            GameObject arenaRoot = null;
            if (arenaPrefab != null)
            {
                arenaRoot = PrefabUtility.InstantiatePrefab(arenaPrefab) as GameObject;
                arenaRoot.name = "[ARENA]";

                Transform riverT = arenaRoot.transform.Find("River_Water");
                if (riverT != null)
                {
                    SpriteRenderer riverSr = riverT.GetComponent<SpriteRenderer>();
                    if (riverSr != null && riverSr.sprite == null)
                    {
                        riverSr.sprite = riverSprite;
                    }
                }
            }
            else
            {
                arenaRoot = new GameObject("[ARENA]");

                // Full Screen Ground (map.png, native 1080x1920 at 100 PPU = 10.8 x 19.2 units)
                GameObject ground = new GameObject("Arena_Ground");
                ground.transform.SetParent(arenaRoot.transform, false);
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = Vector3.one;
                SpriteRenderer groundSr = ground.AddComponent<SpriteRenderer>();
                groundSr.sprite = mapSprite;
                groundSr.color = Color.white;
                groundSr.sortingOrder = -10;

                // Roads (road.png, 174x855 at 100 PPU = 1.74 x 8.55 units)
                MakeRoad("Road_Bottom_Left", new Vector2(-2.7f, -5.2f), new Vector3(1f, 1f, 1f), roadSprite, -8, arenaRoot.transform);
                MakeRoad("Road_Bottom_Right", new Vector2(2.7f, -5.2f), new Vector3(1f, 1f, 1f), roadSprite, -8, arenaRoot.transform);
                MakeRoad("Road_Top_Left", new Vector2(-2.7f, 5.2f), new Vector3(1f, -1f, 1f), roadSprite, -8, arenaRoot.transform);
                MakeRoad("Road_Top_Right", new Vector2(2.7f, 5.2f), new Vector3(1f, -1f, 1f), roadSprite, -8, arenaRoot.transform);

                // River (river.png, 1080x288 at 100 PPU = 10.8 x 2.88 units)
                GameObject river = new GameObject("River_Water");
                river.transform.SetParent(arenaRoot.transform, false);
                river.transform.position = Vector3.zero;
                river.transform.localScale = Vector3.one;
                SpriteRenderer riverSr = river.AddComponent<SpriteRenderer>();
                riverSr.sprite = riverSprite;
                riverSr.color = Color.white;
                riverSr.sortingOrder = -6;

                // Bridges (bridge.png, 200x371 at 100 PPU = 2.0 x 3.71 units)
                MakeBridgeSprite("Bridge_Left", new Vector2(-2.7f, 0f), bridgeSprite, -4, arenaRoot.transform);
                MakeBridgeSprite("Bridge_Right", new Vector2(2.7f, 0f), bridgeSprite, -4, arenaRoot.transform);

                PrefabUtility.SaveAsPrefabAssetAndConnect(arenaRoot, "Assets/Prefabs/[ARENA].prefab", InteractionMode.AutomatedAction);
            }

            // Ensure Arena Gradient Overlay (covers arena, under UI Canvas)
            Transform gradT = arenaRoot.transform.Find("Arena_Gradient");
            if (gradT == null)
            {
                GameObject gradObj = new GameObject("Arena_Gradient");
                gradObj.transform.SetParent(arenaRoot.transform, false);
                gradObj.transform.position = Vector3.zero;
                SpriteRenderer gradSr = gradObj.AddComponent<SpriteRenderer>();
                gradSr.sprite = gradientSprite;
                gradSr.sortingOrder = 20;
                float sx = 10.8f / (gradientSprite != null ? gradientSprite.rect.width / gradientSprite.pixelsPerUnit : 2.56f);
                float sy = 19.2f / (gradientSprite != null ? gradientSprite.rect.height / gradientSprite.pixelsPerUnit : 5.12f);
                gradObj.transform.localScale = new Vector3(sx, sy, 1f);
            }

            // ==========================================
            // 3. ROOT GROUP: [TOWERS]
            // ==========================================
            GameObject towersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/[TOWERS].prefab");
            GameObject towersRoot = null;
            if (towersPrefab != null)
            {
                towersRoot = PrefabUtility.InstantiatePrefab(towersPrefab) as GameObject;
                towersRoot.name = "[TOWERS]";
            }
            else
            {
                towersRoot = new GameObject("[TOWERS]");

                // Player Towers (Bottom)
                MakeSpriteTower("Player Left Tower", new Vector2(-2.7f, -3.8f), towerSprite, true, false, towersRoot.transform);
                MakeSpriteTower("Player Right Tower", new Vector2(2.7f, -3.8f), towerSprite, true, false, towersRoot.transform);
                MakeSpriteTower("Player Fortress", new Vector2(0f, -5.8f), lockSprite, true, true, towersRoot.transform);

                // Enemy Towers (Top)
                MakeSpriteTower("Enemy Left Tower", new Vector2(-2.7f, 3.8f), enemyTowerSprite, false, false, towersRoot.transform);
                MakeSpriteTower("Enemy Right Tower", new Vector2(2.7f, 3.8f), enemyTowerSprite, false, false, towersRoot.transform);
                MakeSpriteTower("Enemy Fortress", new Vector2(0f, 5.8f), enemyLockSprite, false, true, towersRoot.transform);

                PrefabUtility.SaveAsPrefabAssetAndConnect(towersRoot, "Assets/Prefabs/[TOWERS].prefab", InteractionMode.AutomatedAction);
            }

            // ==========================================
            // 4. ROOT GROUP: [MANAGEMENT]
            // Завантажуємо з префаба якщо існує, або створюємо
            // ==========================================
            GameObject mgmtPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/[MANAGEMENT].prefab");
            GameObject mgmtRoot = null;
            ArcaneGameManager gameMgr = null;
            ArcaneDeckManager deckMgr = null;
            ArcaneAudio audioMgr = null;
            ArcaneUnit unit = null;

            if (mgmtPrefab != null)
            {
                mgmtRoot = PrefabUtility.InstantiatePrefab(mgmtPrefab) as GameObject;
                mgmtRoot.name = "[MANAGEMENT]";
                gameMgr = mgmtRoot.GetComponentInChildren<ArcaneGameManager>(true);
                deckMgr = mgmtRoot.GetComponentInChildren<ArcaneDeckManager>(true);
                audioMgr = mgmtRoot.GetComponentInChildren<ArcaneAudio>(true);
                unit = mgmtRoot.GetComponentInChildren<ArcaneUnit>(true);
            }
            else
            {
                mgmtRoot = new GameObject("[MANAGEMENT]");

                GameObject unitObj = new GameObject("Unit_Template");
                unitObj.transform.SetParent(mgmtRoot.transform, false);
                unitObj.transform.position = new Vector3(30, 30, 0);
                SpriteRenderer usr = unitObj.AddComponent<SpriteRenderer>();
                usr.sprite = ArcaneVisuals.GetUnitSprite("warrior", true);
                unit = unitObj.AddComponent<ArcaneUnit>();
                unitObj.SetActive(false);

                GameObject mgrObj = new GameObject("Arcane Game Manager");
                mgrObj.transform.SetParent(mgmtRoot.transform, false);
                gameMgr = mgrObj.AddComponent<ArcaneGameManager>();
                deckMgr = mgrObj.AddComponent<ArcaneDeckManager>();
                audioMgr = mgrObj.AddComponent<ArcaneAudio>();

                // Set up initial deck with custom card sprites & balanced movement speeds
                deckMgr.initialDeck.Clear();
                deckMgr.initialDeck.Add(new CardInfo
                {
                    id = "warrior",
                    cardName = "Воїн",
                    cost = 3,
                    cardType = CardType.Troop,
                    cardColor = new Color(0.35f, 0.7f, 1f),
                    cardSprite = cardSwordsman,
                    maxHp = 850f,
                    damage = 90f,
                    moveSpeed = 0.85f,
                    attackRange = 0.85f,
                    attackInterval = 1.2f,
                    unitCategory = TargetCategory.Ground,
                    targetCategories = TargetCategory.Ground | TargetCategory.Building,
                    isRanged = false,
                    modelScale = 0.575f,
                    healthBarOffsetY = 0.50f,
                    healthBarWidth = 0.30f
                });
                deckMgr.initialDeck.Add(new CardInfo
                {
                    id = "archer",
                    cardName = "Лучник",
                    cost = 3,
                    cardType = CardType.Troop,
                    cardColor = new Color(0.45f, 1f, 0.55f),
                    cardSprite = cardArcher,
                    maxHp = 350f,
                    damage = 110f,
                    moveSpeed = 0.75f,
                    attackRange = 3.3f,
                    attackInterval = 1.1f,
                    unitCategory = TargetCategory.Ground,
                    targetCategories = TargetCategory.Ground | TargetCategory.Air | TargetCategory.Building,
                    isRanged = true,
                    projectileSpeed = 7.5f,
                    modelScale = 0.475f,
                    healthBarOffsetY = 0.50f,
                    healthBarWidth = 0.30f
                });
                deckMgr.initialDeck.Add(new CardInfo
                {
                    id = "golem",
                    cardName = "Голем",
                    cost = 5,
                    cardType = CardType.Troop,
                    cardColor = new Color(0.75f, 0.5f, 0.95f),
                    cardSprite = cardGolem,
                    maxHp = 2200f,
                    damage = 150f,
                    moveSpeed = 0.45f,
                    attackRange = 0.9f,
                    attackInterval = 2.0f,
                    unitCategory = TargetCategory.Ground,
                    targetCategories = TargetCategory.Building,
                    isRanged = false,
                    modelScale = 1.15f,
                    healthBarOffsetY = 1.05f,
                    healthBarWidth = 0.60f
                });
                deckMgr.initialDeck.Add(new CardInfo
                {
                    id = "drone",
                    cardName = "Дрон",
                    cost = 4,
                    cardType = CardType.Troop,
                    cardColor = new Color(0.2f, 0.95f, 0.9f),
                    cardSprite = cardDrone,
                    maxHp = 500f,
                    damage = 95f,
                    moveSpeed = 0.95f,
                    attackRange = 1.2f,
                    attackInterval = 0.9f,
                    unitCategory = TargetCategory.Air,
                    targetCategories = TargetCategory.Ground | TargetCategory.Building,
                    isRanged = true,
                    projectileSpeed = 8f,
                    modelScale = 1.1f,
                    healthBarOffsetY = 0.82f,
                    healthBarWidth = 0.44f
                });
                deckMgr.initialDeck.Add(new CardInfo
                {
                    id = "fireball",
                    cardName = "Вогн. куля",
                    cost = 4,
                    cardType = CardType.Spell,
                    cardColor = new Color(1f, 0.45f, 0.15f),
                    cardSprite = cardFball,
                    spellType = SpellType.Damage,
                    spellRadius = 2.2f,
                    spellPower = 400f
                });
                deckMgr.initialDeck.Add(new CardInfo
                {
                    id = "heal",
                    cardName = "Лікування",
                    cost = 3,
                    cardType = CardType.Spell,
                    cardColor = new Color(0.3f, 1f, 0.45f),
                    cardSprite = cardHeal,
                    spellType = SpellType.Heal,
                    spellRadius = 2.5f,
                    spellPower = 350f
                });

                PrefabUtility.SaveAsPrefabAssetAndConnect(mgmtRoot, "Assets/Prefabs/[MANAGEMENT].prefab", InteractionMode.AutomatedAction);
            }

            // ==========================================
            // 5. ROOT GROUP: [UI CANVAS]
            // Завантажуємо з префаба якщо існує, або створюємо
            // ==========================================
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/[UI CANVAS].prefab");
            GameObject uiRoot = null;
            Canvas canvas = null;

            Text timerText = null;
            Text energyText = null;
            Image energyFillImage = null;
            Button topRestartBtn = null;

            GameObject goPanel = null;
            Text statusTitle = null;
            Text detailsText = null;
            Button goRestartBtn = null;

            Button[] handButtons = new Button[4];
            Image[] handBgs = new Image[4];
            GameObject selFrame = null;
            Image ncImg = null;

            if (uiPrefab != null)
            {
                // Завантажуємо збережений префаб користувача з усіма його позиціями
                uiRoot = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
                uiRoot.name = "[UI CANVAS]";

                canvas = uiRoot.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 10f;
                }

                // Підтягуємо посилання на елементи інтерфейсу
                timerText = FindComponentDeep<Text>(uiRoot.transform, "TimerText");
                energyText = FindComponentDeep<Text>(uiRoot.transform, "EnergyText");
                energyFillImage = FindComponentDeep<Image>(uiRoot.transform, "EnergyFill");
                topRestartBtn = FindComponentDeep<Button>(uiRoot.transform, "RestartBtn");

                Transform goTransform = FindDeepChild(uiRoot.transform, "GameOverPanel");
                if (goTransform != null)
                {
                    goPanel = goTransform.gameObject;
                    statusTitle = FindComponentDeep<Text>(goTransform, "StatusTitle");
                    detailsText = FindComponentDeep<Text>(goTransform, "DetailsText");
                    goRestartBtn = FindComponentDeep<Button>(goTransform, "RestartMatchBtn");
                    goPanel.SetActive(false);
                }

                Transform dpTransform = FindDeepChild(uiRoot.transform, "DeckPanel");
                if (dpTransform != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Transform slot = FindDeepChild(dpTransform, $"CardSlot_{i}");
                        if (slot != null)
                        {
                            handButtons[i] = slot.GetComponent<Button>();
                            handBgs[i] = slot.GetComponent<Image>();
                            if (handButtons[i] != null)
                            {
                                handButtons[i].transition = Selectable.Transition.None;
                            }
                        }
                    }

                    Transform sel = FindDeepChild(dpTransform, "SelectionIndicator");
                    if (sel != null)
                    {
                        selFrame = sel.gameObject;
                        selFrame.SetActive(false);
                    }

                    Transform nc = FindDeepChild(dpTransform, "NextCardSlot");
                    if (nc != null)
                    {
                        ncImg = nc.GetComponent<Image>();
                    }
                }
            }
            else
            {
                // Якщо префаба ще немає, генеруємо стандартний UI і зберігаємо його в префаб
                uiRoot = new GameObject("[UI CANVAS]");

                canvas = new GameObject("Canvas").AddComponent<Canvas>();
                canvas.transform.SetParent(uiRoot.transform, false);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10f;
                canvas.sortingOrder = 50;
                CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // Exactly 9:16 Portrait Canvas
                scaler.matchWidthOrHeight = 1f; // Match Height (keeps native resolution & aspect ratio)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.transform.SetParent(uiRoot.transform, false);
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

                // --- TOP BAR: Timer & Energy (Inside Canvas) ---
                GameObject topBar = new GameObject("TopHUD");
                topBar.transform.SetParent(canvas.transform, false);
                RectTransform tbRt = topBar.AddComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0, 0.92f);
                tbRt.anchorMax = new Vector2(1, 1.0f);
                tbRt.offsetMin = Vector2.zero;
                tbRt.offsetMax = Vector2.zero;
                Image tbBg = topBar.AddComponent<Image>();
                tbBg.color = new Color(0.06f, 0.07f, 0.11f, 0.94f);

                timerText = MakeText(topBar.transform, "TimerText", "04:00", new Vector2(0.5f, 0.68f), new Vector2(260, 52), 48, Color.yellow);

                GameObject energyBarFrame = new GameObject("EnergyBarFrame");
                energyBarFrame.transform.SetParent(topBar.transform, false);
                RectTransform ebRt = energyBarFrame.AddComponent<RectTransform>();
                ebRt.anchorMin = new Vector2(0.5f, 0.24f);
                ebRt.anchorMax = new Vector2(0.5f, 0.24f);
                ebRt.sizeDelta = new Vector2(510, 44);
                Image ebBg = energyBarFrame.AddComponent<Image>();
                ebBg.color = new Color(0.12f, 0.14f, 0.20f);

                energyText = MakeText(energyBarFrame.transform, "EnergyText", "5/10", new Vector2(0f, 0.5f), new Vector2(85, 38), 24, Color.white);
                energyText.rectTransform.pivot = new Vector2(0f, 0.5f);
                energyText.rectTransform.anchoredPosition = new Vector2(14f, 0f);
                energyText.alignment = TextAnchor.MiddleCenter;
                energyText.fontStyle = FontStyle.Bold;

                topRestartBtn = MakeButton(topBar.transform, "RestartBtn", "↺ ЗАНОВО", new Vector2(0.88f, 0.5f), new Vector2(160, 52), 22);

                // --- BOTTOM DECK PANEL (Inside Canvas using board.png) ---
                GameObject deckPanel = new GameObject("DeckPanel");
                deckPanel.transform.SetParent(canvas.transform, false);
                RectTransform dpRt = deckPanel.AddComponent<RectTransform>();
                dpRt.anchorMin = new Vector2(0.5f, 0f);
                dpRt.anchorMax = new Vector2(0.5f, 0f);
                dpRt.pivot = new Vector2(0.5f, 0f);
                dpRt.anchoredPosition = new Vector2(0f, 0f);
                dpRt.sizeDelta = new Vector2(1072, 388);
                Image dpBg = deckPanel.AddComponent<Image>();
                dpBg.sprite = boardSprite;
                dpBg.color = Color.white;

                Sprite[] initialHandSprites = new Sprite[] { cardSwordsman, cardArcher, cardGolem, cardDrone };
                float[] slotOffsets = new float[] { -220, -25, 170, 365 };
                for (int i = 0; i < 4; i++)
                {
                    GameObject cardObj = new GameObject($"CardSlot_{i}");
                    cardObj.transform.SetParent(deckPanel.transform, false);
                    RectTransform cRt = cardObj.AddComponent<RectTransform>();
                    cRt.anchoredPosition = new Vector2(slotOffsets[i], 180);
                    cRt.sizeDelta = new Vector2(180, 268);

                    Image cImg = cardObj.AddComponent<Image>();
                    cImg.sprite = initialHandSprites[i];
                    cImg.color = Color.white;
                    cImg.preserveAspect = true;
                    Button cBtn = cardObj.AddComponent<Button>();
                    cBtn.targetGraphic = cImg;
                    cBtn.transition = Selectable.Transition.None;

                    handButtons[i] = cBtn;
                    handBgs[i] = cImg;
                }

                selFrame = new GameObject("SelectionIndicator");
                selFrame.transform.SetParent(deckPanel.transform, false);
                RectTransform selRt = selFrame.AddComponent<RectTransform>();
                selRt.sizeDelta = new Vector2(192, 280);
                Image selImg = selFrame.AddComponent<Image>();
                selImg.color = new Color(1f, 0.92f, 0.25f, 0.65f);
                selFrame.SetActive(false);

                MakeText(deckPanel.transform, "NextLabel", "ДАЛІ:", new Vector2(0.5f, 0.5f), new Vector2(100, 30), 22, Color.white).rectTransform.anchoredPosition = new Vector2(-435, 275);
                GameObject nextSlot = new GameObject("NextCardSlot");
                nextSlot.transform.SetParent(deckPanel.transform, false);
                RectTransform ncRt = nextSlot.AddComponent<RectTransform>();
                ncRt.anchoredPosition = new Vector2(-435, 155);
                ncRt.sizeDelta = new Vector2(120, 179);
                ncImg = nextSlot.AddComponent<Image>();
                ncImg.sprite = cardFball;
                ncImg.color = Color.white;
                ncImg.preserveAspect = true;

                // --- GAME OVER PANEL (Inside Canvas) ---
                goPanel = new GameObject("GameOverPanel");
                goPanel.transform.SetParent(canvas.transform, false);
                RectTransform goRt = goPanel.AddComponent<RectTransform>();
                goRt.anchorMin = Vector2.zero;
                goRt.anchorMax = Vector2.one;
                goRt.offsetMin = Vector2.zero;
                goRt.offsetMax = Vector2.zero;
                Image goBg = goPanel.AddComponent<Image>();
                goBg.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);

                statusTitle = MakeText(goPanel.transform, "StatusTitle", "ПЕРЕМОГА!", new Vector2(0.5f, 0.62f), new Vector2(900, 120), 72, Color.yellow);
                detailsText = MakeText(goPanel.transform, "DetailsText", "Всі умови виконані.", new Vector2(0.5f, 0.50f), new Vector2(900, 80), 30, Color.white);
                goRestartBtn = MakeButton(goPanel.transform, "RestartMatchBtn", "ГРАТИ ЗНОВУ", new Vector2(0.5f, 0.38f), new Vector2(340, 75), 30);
                goPanel.SetActive(false);

                // Зберігаємо свіжостворений UI Canvas як префаб
                PrefabUtility.SaveAsPrefabAssetAndConnect(uiRoot, "Assets/Prefabs/[UI CANVAS].prefab", InteractionMode.AutomatedAction);
            }

            // Wire Managers
            if (gameMgr != null)
            {
                if (gameMgr.timerText == null) gameMgr.timerText = timerText;
                if (gameMgr.energyText == null) gameMgr.energyText = energyText;
                if (gameMgr.energyBarFill == null) gameMgr.energyBarFill = energyFillImage;
                if (gameMgr.statusText == null) gameMgr.statusText = statusTitle;
                if (gameMgr.resultDetailsText == null) gameMgr.resultDetailsText = detailsText;
                if (gameMgr.gameOverPanel == null) gameMgr.gameOverPanel = goPanel;
                if (gameMgr.topRestartButton == null) gameMgr.topRestartButton = topRestartBtn;
                if (gameMgr.gameOverRestartButton == null) gameMgr.gameOverRestartButton = goRestartBtn;
                if (gameMgr.energyDropSprite == null) gameMgr.energyDropSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/energy.png");
                gameMgr.EnsureEnergyDropsUI();
            }

            if (deckMgr != null)
            {
                if (deckMgr.unitBasePrefab == null) deckMgr.unitBasePrefab = unit;
                if (deckMgr.handButtons == null || deckMgr.handButtons.Length == 0) deckMgr.handButtons = handButtons;
                if (deckMgr.handNameTexts == null) deckMgr.handNameTexts = new Text[0];
                if (deckMgr.handCostTexts == null) deckMgr.handCostTexts = new Text[0];
                if (deckMgr.handBackgrounds == null || deckMgr.handBackgrounds.Length == 0) deckMgr.handBackgrounds = handBgs;
                if (deckMgr.selectedIndicator == null) deckMgr.selectedIndicator = selFrame;
                if (deckMgr.nextCardBackground == null) deckMgr.nextCardBackground = ncImg;
            }

            // Save Scene cleanly
            string folder = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            string scenePath = folder + "/ArcaneTowers.unity";

            EditorSceneManager.MarkSceneDirty(activeScene);
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene, scenePath);
            }
            else
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!buildScenes.Exists(s => s.path == scenePath))
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log("[Arcane Towers] Сцену успішно побудовано та збережено!");

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Arcane Towers",
                    "Сцену успішно зібрано!\n\n" +
                    (uiPrefab != null ? "✅ Інтерфейс [UI CANVAS] завантажено з вашого збереженого префаба.\n" : "✅ Інтерфейс [UI CANVAS] згенеровано та збережено в префаб.\n") +
                    (towersPrefab != null ? "✅ Башти [TOWERS] завантажено з префаба.\n" : "✅ Башти [TOWERS] згенеровано та збережено в префаб.\n") +
                    (arenaPrefab != null ? "✅ Арену [ARENA] завантажено з префаба.\n\n" : "✅ Арену [ARENA] згенеровано та збережено в префаб.\n\n") +
                    "Всі налаштовані позиції та компоненти підключені!\n" +
                    "Запускайте Play (▶️) та тестуйте!", "Чудово!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Arcane Towers] Помилка побудови сцени: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Помилка білду", "Сталася помилка:\n" + ex.Message, "OK");
        }
    }

    // ==========================================
    // ДОПОМІЖНІ МЕТОДИ ПОШУКУ ТА ГЕНЕРАЦІЇ
    // ==========================================

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private static T FindComponentDeep<T>(Transform parent, string name) where T : Component
    {
        Transform t = FindDeepChild(parent, name);
        return t != null ? t.GetComponent<T>() : null;
    }

    public static void OptimizeSpriteImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                dirty = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                dirty = true;
            }

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            if (standalone.textureCompression != TextureImporterCompression.Uncompressed || !standalone.overridden)
            {
                standalone.overridden = true;
                standalone.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(standalone);
                dirty = true;
            }

            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (defaultSettings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defaultSettings);
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }

    private static GameObject MakeRoad(string name, Vector2 pos, Vector3 scale, Sprite sprite, int order, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = order;
        return go;
    }

    private static GameObject MakeBridgeSprite(string name, Vector2 pos, Sprite sprite, int order, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = order;
        return go;
    }

    private static ArcaneTower MakeSpriteTower(string name, Vector2 pos, Sprite sprite, bool player, bool core, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;

        // Shadow
        GameObject shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(go.transform, false);
        float shadowY = core ? -1.2f : -0.95f;
        shadowObj.transform.localPosition = new Vector3(0, shadowY, 0);
        Vector3 shadowScale = core ? new Vector3(2.8f, 1.2f, 1f) : new Vector3(2.2f, 0.9f, 1f);
        shadowObj.transform.localScale = shadowScale;
        SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = ArcaneVisuals.GetShadowSprite();
        shadowSr.sortingOrder = 0;

        // Tower Body
        GameObject bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(go.transform, false);
        bodyObj.transform.localScale = Vector3.one;
        SpriteRenderer sr = bodyObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = Color.white;
        sr.sortingOrder = 2;

        ArcaneTower tower = go.AddComponent<ArcaneTower>();
        tower.isPlayer = player;
        tower.isCore = core;
        tower.maxHp = core ? 2500f : 1600f;
        tower.hp = tower.maxHp;
        tower.attackRange = 3.6f;
        tower.damage = core ? 130f : 110f;
        tower.towerRadius = core ? 1.45f : 1.05f;
        tower.healthBarOffsetY = core ? 1.55f : 1.25f;
        tower.healthBarWidth = core ? 0.75f : 0.52f;
        return tower;
    }

    private static Font GetFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch { }

        if (font == null)
        {
            try
            {
                Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
                if (fonts != null && fonts.Length > 0)
                    font = fonts[0];
            }
            catch { }
        }

        if (font == null)
        {
            try
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            catch { }
        }

        return font;
    }

    private static Text MakeText(Transform parent, string name, string value, Vector2 anchor, Vector2 size, int fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = GetFont();
        text.fontStyle = FontStyle.Bold;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = text.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size, int fontSize = 20)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.20f, 0.25f, 0.35f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        MakeText(go.transform, "Label", label, new Vector2(0.5f, 0.5f), size, fontSize, Color.white);
        return button;
    }
}
