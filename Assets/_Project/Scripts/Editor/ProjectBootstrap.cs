using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;
using Wof.Data;
using Wof.Domain;
using Wof.Presentation;

namespace Wof.EditorTools
{
    /// <summary>
    /// One-shot project bootstrap: creates the SO assets, sprite atlas and the full
    /// Game scene with the brief's naming rules. Idempotent — safe to re-run; existing
    /// assets are overwritten. Runs headless via -executeMethod for CI-style setup.
    /// </summary>
    public static class ProjectBootstrap
    {
        private const string ArtDir = "Assets/_Project/Art";
        private const string SettingsDir = "Assets/_Project/Settings";
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";

        // ---------------------------------------------------------------- Step 1

        [MenuItem("Wof/Bootstrap/1. Import TMP Essentials")]
        public static void Step1_ImportTmpEssentials()
        {
            string pkg = Path.GetFullPath(
                "Library/PackageCache/com.unity.textmeshpro@3.0.6/Package Resources/TMP Essential Resources.unitypackage");
            if (!File.Exists(pkg)) { Debug.LogError($"TMP package not found: {pkg}"); return; }
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
            Debug.Log("[Bootstrap] TMP essentials imported.");
        }

        // ---------------------------------------------------------------- Step 2

        [MenuItem("Wof/Bootstrap/2. Create Assets")]
        public static void Step2_CreateAssets()
        {
            ConfigureSprites();
            CreateSpriteAtlas();
            var rewards = CreateRewardDefinitions();
            var wheels = CreateWheelConfigs(rewards);
            CreateTuningAndSettings(wheels);
            CreateSpriteRegistry(rewards);
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Bootstrap] Assets created.");
        }

        /// <summary>Sprites that get 9-slice borders (brief: use Sliced sprites).</summary>
        private static readonly Dictionary<string, Vector4> SlicedBorders = new Dictionary<string, Vector4>
        {
            { "UI_button_orange_standard", new Vector4(24, 24, 24, 24) },
            { "UI_button_grey_standard", new Vector4(24, 24, 24, 24) },
            { "ui_card_frame_12px_neutral", new Vector4(16, 16, 16, 16) },
            { "ui_card_frame_4px_zone", new Vector4(8, 8, 8, 8) },
            { "ui_card_frame_gardient", new Vector4(20, 20, 20, 20) },
            { "ui_card_panel_zone_bg", new Vector4(24, 24, 24, 24) },
            { "ui_card_panel_zone_super", new Vector4(24, 24, 24, 24) },
            { "ui_card_zone_map_frame", new Vector4(24, 24, 24, 24) },
        };

        private static void ConfigureSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp == null) continue;

                bool dirty = imp.textureType != TextureImporterType.Sprite;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;

                string name = Path.GetFileNameWithoutExtension(path);
                if (SlicedBorders.TryGetValue(name, out var border) && imp.spriteBorder != border)
                {
                    imp.spriteBorder = border;
                    dirty = true;
                }
                if (dirty) imp.SaveAndReimport();
            }
        }

        private static void CreateSpriteAtlas()
        {
            string atlasPath = $"{ArtDir}/wof_atlas.spriteatlas";
            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath) != null) return;

            var atlas = new SpriteAtlas();
            var packing = UnityEditor.U2D.SpriteAtlasExtensions.GetPackingSettings(atlas);
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            UnityEditor.U2D.SpriteAtlasExtensions.SetPackingSettings(atlas, packing);
            UnityEditor.U2D.SpriteAtlasExtensions.Add(atlas,
                new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(ArtDir) });
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        // ---- reward + wheel data --------------------------------------------

        private struct RewardSpec
        {
            public string File, Display, Icon;
            public RewardKind Kind;
            public int Amount;
            public RarityTier Rarity;
            public WinVfx Vfx;

            public RewardSpec(string file, RewardKind kind, string display, string icon,
                int amount, RarityTier rarity, WinVfx vfx)
            { File = file; Kind = kind; Display = display; Icon = icon; Amount = amount; Rarity = rarity; Vfx = vfx; }
        }

        private static readonly RewardSpec[] RewardSpecs =
        {
            // bronze pool
            new RewardSpec("reward_gold", RewardKind.Gold, "Gold", "UI_icon_gold", 100, RarityTier.Tier1, WinVfx.Star),
            new RewardSpec("reward_cash", RewardKind.Cash, "Cash", "UI_icon_cash", 5, RarityTier.Tier1, WinVfx.Star),
            new RewardSpec("reward_grenade_m26", RewardKind.Consumable, "M26 Grenade", "ui_icon_render_cons_grenade_m26", 2, RarityTier.Tier1, WinVfx.Star),
            new RewardSpec("reward_healthshot_regen", RewardKind.Consumable, "Regenerator", "ui_icon_render_cons_healthshot_2_regenerator", 1, RarityTier.Tier1, WinVfx.Star),
            new RewardSpec("reward_points_pistol", RewardKind.Points, "Pistol Points", "UI_Icons_Pistol_Points", 80, RarityTier.Tier1, WinVfx.Star),
            new RewardSpec("reward_chest_small", RewardKind.Chest, "Small Chest", "UI_icon_chest_small_noligt", 1, RarityTier.Tier1, WinVfx.Star),
            new RewardSpec("reward_skin_tier1_shotgun", RewardKind.WeaponSkin, "Shotgun Skin", "UI_Icon_Renders_tier1_shotgun", 1, RarityTier.Tier1, WinVfx.Star),
            // silver pool
            new RewardSpec("reward_chest_silver", RewardKind.Chest, "Silver Chest", "UI_icon_chest_silver_nolight", 1, RarityTier.Tier2, WinVfx.Star),
            new RewardSpec("reward_grenade_m67", RewardKind.Consumable, "M67 Grenade", "ui_icon_render_cons_grenade_m67", 3, RarityTier.Tier2, WinVfx.Star),
            new RewardSpec("reward_healthshot_neuro", RewardKind.Consumable, "Neurostim", "ui_icon_render_cons_healthshot_2_neurostim", 2, RarityTier.Tier2, WinVfx.Star),
            new RewardSpec("reward_points_rifle", RewardKind.Points, "Rifle Points", "UI_Icons_Rifle_Points", 150, RarityTier.Tier2, WinVfx.Star),
            new RewardSpec("reward_skin_tier2_rifle", RewardKind.WeaponSkin, "Rifle Skin", "UI_Icon_Renders_tier2_rifle", 1, RarityTier.Tier2, WinVfx.Star),
            new RewardSpec("reward_chest_standart", RewardKind.Chest, "Standard Chest", "UI_icon_chest_standart_nolight", 1, RarityTier.Tier2, WinVfx.Star),
            // golden / special pool
            new RewardSpec("reward_gold_big", RewardKind.Gold, "Gold Pile", "UI_icon_gold", 500, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_cash_big", RewardKind.Cash, "Cash Stack", "UI_icon_cash", 25, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_chest_gold", RewardKind.Chest, "Golden Chest", "UI_icon_chest_gold_nolight", 1, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_chest_super", RewardKind.Chest, "Super Chest", "UI_icon_chest_super_nolight", 1, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_skin_tier3_sniper", RewardKind.SpecialSkin, "Sniper Skin T3", "UI_Icon_Renders_tier3_sniper", 1, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_skin_tier3_smg", RewardKind.SpecialSkin, "SMG Skin T3", "UI_Icon_Renders_tier3_smg", 1, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_skin_tier3_shotgun", RewardKind.SpecialSkin, "Shotgun Skin T3", "UI_Icon_Renders_tier3_shotgun", 1, RarityTier.Special, WinVfx.GoldenShine),
            new RewardSpec("reward_bayonet_summer", RewardKind.SpecialSkin, "Bayonet Summer", "ui_icon_mle_bayonet_summer_vice", 1, RarityTier.Special, WinVfx.GoldenShine),
        };

        private static Sprite LoadIcon(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");

        private static Dictionary<string, RewardDefinition> CreateRewardDefinitions()
        {
            Directory.CreateDirectory(SettingsDir);
            var map = new Dictionary<string, RewardDefinition>();

            foreach (var spec in RewardSpecs)
            {
                string path = $"{SettingsDir}/{spec.File}.asset";
                var def = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<RewardDefinition>();
                    AssetDatabase.CreateAsset(def, path);
                }

                var so = new SerializedObject(def);
                so.FindProperty("id").stringValue = spec.File;
                so.FindProperty("kind").enumValueIndex = (int)spec.Kind;
                so.FindProperty("displayName").stringValue = spec.Display;
                so.FindProperty("icon").objectReferenceValue = LoadIcon(spec.Icon);
                so.FindProperty("baseAmount").intValue = spec.Amount;
                so.FindProperty("rarity").enumValueIndex = (int)spec.Rarity;
                so.FindProperty("winVfx").enumValueIndex = (int)spec.Vfx;
                so.ApplyModifiedPropertiesWithoutUndo();

                map[spec.File] = def;
            }
            return map;
        }

        private static Dictionary<WheelTier, WheelConfig> CreateWheelConfigs(
            Dictionary<string, RewardDefinition> rewards)
        {
            // (reward file, weight, isBomb) — bronze carries the single bomb slice.
            (string, float, bool)[] bronze =
            {
                ("reward_gold", 2f, false),
                ("reward_cash", 2f, false),
                (null, 1f, true), // BOMB
                ("reward_grenade_m26", 1.5f, false),
                ("reward_points_pistol", 1.5f, false),
                ("reward_healthshot_regen", 1.5f, false),
                ("reward_chest_small", 1f, false),
                ("reward_skin_tier1_shotgun", 0.5f, false),
            };
            (string, float, bool)[] silver =
            {
                ("reward_gold", 2f, false),
                ("reward_cash", 2f, false),
                ("reward_chest_silver", 1f, false),
                ("reward_grenade_m67", 1.5f, false),
                ("reward_points_rifle", 1.5f, false),
                ("reward_healthshot_neuro", 1.5f, false),
                ("reward_chest_standart", 1f, false),
                ("reward_skin_tier2_rifle", 0.5f, false),
            };
            (string, float, bool)[] golden =
            {
                ("reward_gold_big", 1.5f, false),
                ("reward_cash_big", 1.5f, false),
                ("reward_chest_gold", 1f, false),
                ("reward_skin_tier3_sniper", 1f, false),
                ("reward_chest_super", 1f, false),
                ("reward_skin_tier3_smg", 1f, false),
                ("reward_bayonet_summer", 0.75f, false),
                ("reward_skin_tier3_shotgun", 1f, false),
            };

            var result = new Dictionary<WheelTier, WheelConfig>
            {
                [WheelTier.Bronze] = WriteWheel("wheel_bronze", WheelTier.Bronze, bronze, rewards),
                [WheelTier.Silver] = WriteWheel("wheel_silver", WheelTier.Silver, silver, rewards),
                [WheelTier.Golden] = WriteWheel("wheel_golden", WheelTier.Golden, golden, rewards),
            };
            return result;
        }

        private static WheelConfig WriteWheel(string file, WheelTier tier,
            (string reward, float weight, bool bomb)[] entries,
            Dictionary<string, RewardDefinition> rewards)
        {
            string path = $"{SettingsDir}/{file}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<WheelConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<WheelConfig>();
                AssetDatabase.CreateAsset(cfg, path);
            }

            var so = new SerializedObject(cfg);
            so.FindProperty("tier").enumValueIndex = (int)tier;
            so.FindProperty("sliceCount").intValue = entries.Length;
            var list = so.FindProperty("slices");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var el = list.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("reward").objectReferenceValue =
                    entries[i].reward != null ? rewards[entries[i].reward] : null;
                el.FindPropertyRelative("weight").floatValue = entries[i].weight;
                el.FindPropertyRelative("isBomb").boolValue = entries[i].bomb;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return cfg;
        }

        private static void CreateTuningAndSettings(Dictionary<WheelTier, WheelConfig> wheels)
        {
            var settings = LoadOrCreate<GameSettings>($"{SettingsDir}/game_settings.asset");
            EditorUtility.SetDirty(settings);

            var tuning = LoadOrCreate<ZoneTuning>($"{SettingsDir}/zone_tuning.asset");
            tuning.safeInterval = 5;
            tuning.superInterval = 30;
            tuning.normalWheel = wheels[WheelTier.Bronze];
            tuning.safeWheel = wheels[WheelTier.Silver];
            tuning.superWheel = wheels[WheelTier.Golden];
            EditorUtility.SetDirty(tuning);
        }

        private static void CreateSpriteRegistry(Dictionary<string, RewardDefinition> rewards)
        {
            var reg = LoadOrCreate<SpriteRegistry>($"{SettingsDir}/sprite_registry.asset");

            var keys = new HashSet<string>();
            var sprites = new List<Sprite>();
            foreach (var def in rewards.Values)
            {
                if (def.Icon != null && keys.Add(def.Icon.name)) sprites.Add(def.Icon);
            }
            var death = LoadIcon("ui_card_icon_death"); // bomb icon key used by WheelBuilder
            if (death != null && keys.Add(death.name)) sprites.Add(death);

            var so = new SerializedObject(reg);
            var entries = so.FindProperty("entries");
            entries.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
            {
                var el = entries.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("key").stringValue = sprites[i].name;
                el.FindPropertyRelative("sprite").objectReferenceValue = sprites[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Berkay";
            PlayerSettings.productName = "Wheel of Fortune";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.berkay.wofdemo");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        }

        // ---------------------------------------------------------------- Step 3

        [MenuItem("Wof/Bootstrap/3. Build Scene")]
        public static void Step3_BuildScene()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<ZoneTuning>($"{SettingsDir}/zone_tuning.asset");
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>($"{SettingsDir}/game_settings.asset");
            var registry = AssetDatabase.LoadAssetAtPath<SpriteRegistry>($"{SettingsDir}/sprite_registry.asset");
            if (tuning == null || settings == null || registry == null)
            { Debug.LogError("[Bootstrap] Run Step 2 first."); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildEventSystem();
            var safeArea = BuildCanvas();

            var hud = BuildHud(safeArea);
            var wheel = BuildWheel(safeArea, settings, registry);
            var popup = BuildRewardPopup(safeArea, registry);
            var bomb = BuildBombScreen(safeArea);
            var cashout = BuildCashOutScreen(safeArea);
            var gameover = BuildGameOverScreen(safeArea);
            var inventory = BuildInventory(safeArea, registry); // last sibling -> draws on top

            var controller = new GameObject("game_controller").AddComponent<GameController>();
            var so = new SerializedObject(controller);
            so.FindProperty("tuning").objectReferenceValue = tuning;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("wheelView").objectReferenceValue = wheel;
            so.FindProperty("hudView").objectReferenceValue = hud;
            so.FindProperty("rewardPopup").objectReferenceValue = popup;
            so.FindProperty("bombScreen").objectReferenceValue = bomb;
            so.FindProperty("cashOutScreen").objectReferenceValue = cashout;
            so.FindProperty("gameOverScreen").objectReferenceValue = gameover;
            so.FindProperty("inventoryView").objectReferenceValue = inventory;
            so.ApplyModifiedPropertiesWithoutUndo();

            // overlays start hidden; Show()/Hide() toggle them at runtime
            popup.gameObject.SetActive(false);
            bomb.gameObject.SetActive(false);
            cashout.gameObject.SetActive(false);
            gameover.gameObject.SetActive(false);
            inventory.gameObject.SetActive(false);

            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[Bootstrap] Scene built and saved.");
        }

        private static void BuildCamera()
        {
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.05f, 0.06f);
            cam.orthographic = true;
        }

        private static void BuildEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform BuildCanvas()
        {
            var canvasGo = new GameObject("ui_canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // brief: Canvas mode Expand
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = AddImage("ui_image_background", (RectTransform)canvasGo.transform,
                null, new Color(0.10f, 0.07f, 0.08f), raycast: false);
            Stretch(bg.rectTransform);

            var safe = NewRect("safe_area", canvasGo.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            return safe;
        }

        // ---- screen builders -------------------------------------------------

        private static HudView BuildHud(RectTransform parent)
        {
            var hud = NewRect("ui_hud", parent);
            hud.anchorMin = new Vector2(0, 1);
            hud.anchorMax = new Vector2(1, 1);
            hud.pivot = new Vector2(0.5f, 1);
            hud.anchoredPosition = Vector2.zero;
            hud.sizeDelta = new Vector2(0, 140);

            var goldIcon = AddImage("ui_image_hud_gold", hud, LoadIcon("UI_icon_gold"), Color.white, false);
            Place(goldIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(64, 64));
            goldIcon.preserveAspect = true;

            var goldText = AddText("ui_text_currency_gold_value", hud, "0", 40, TextAlignmentOptions.MidlineLeft);
            goldText.rectTransform.pivot = new Vector2(0, 0.5f); // rect starts AT x, not centered on it
            Place(goldText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(85, 0), new Vector2(170, 64));

            var cashIcon = AddImage("ui_image_hud_cash", hud, LoadIcon("UI_icon_cash"), Color.white, false);
            Place(cashIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(64, 64));
            cashIcon.preserveAspect = true;

            var cashText = AddText("ui_text_currency_cash_value", hud, "0", 40, TextAlignmentOptions.MidlineLeft);
            cashText.rectTransform.pivot = new Vector2(0, 0.5f);
            Place(cashText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(345, 0), new Vector2(170, 64));

            var zoneTrack = BuildZoneTrack(hud);

            var runText = AddText("ui_text_runcount_value", hud, "0", 38, TextAlignmentOptions.MidlineRight);
            runText.rectTransform.pivot = new Vector2(1, 0.5f); // rect ends AT x, no off-screen spill
            Place(runText.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(160, 64));

            // inventory (run stash) button — chest icon on a small grey button
            var invBtn = AddButton("ui_button_inventory", hud, "", 1, LoadIcon("UI_button_grey_standard"));
            var invRect = (RectTransform)invBtn.transform;
            invRect.pivot = new Vector2(1, 0.5f);
            Place(invRect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-215, 0), new Vector2(86, 86));
            var invIcon = AddImage("ui_image_inventory_icon", invRect, LoadIcon("UI_icon_chest_small_noligt"), Color.white, false);
            Place(invIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 62));
            invIcon.preserveAspect = true;

            var hudView = hud.gameObject.AddComponent<HudView>();
            var hso = new SerializedObject(hudView);
            hso.FindProperty("zoneTrack").objectReferenceValue = zoneTrack;
            hso.ApplyModifiedPropertiesWithoutUndo();
            return hudView;
        }

        /// <summary>
        /// Top progress strip: a centred row of zone numbers on its own line under the
        /// currency row, with the active zone boxed (replaces the old "ZONE n" label).
        /// </summary>
        private static ZoneTrackView BuildZoneTrack(RectTransform hud)
        {
            const int cellCount = 7;
            const float cellSize = 62f;
            const float pitch = 72f;

            var track = NewRect("ui_zone_track", hud);
            // own row, centred, dropped well clear of the currency + inventory row above
            Place(track, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -178f), new Vector2(pitch * cellCount + 24f, 78f));

            // solid dark rounded bar (the *_frame sprite is hollow -> reads as two boxes)
            var bg = AddImage("ui_image_zone_track_bg", track, LoadIcon("ui_card_panel_zone_bg"), Color.white, false);
            bg.type = Image.Type.Sliced;
            Stretch(bg.rectTransform);

            var cells = new ZoneCell[cellCount];
            float startX = -pitch * (cellCount - 1) / 2f;
            for (int i = 0; i < cellCount; i++)
            {
                var cell = NewRect($"ui_zone_cell_{i}", track);
                Place(cell, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + i * pitch, 0f), new Vector2(cellSize, cellSize));

                var hl = AddImage("ui_image_zone_cell_highlight", cell,
                    LoadIcon("ui_card_panel_zone_current_white"), new Color(0.30f, 0.85f, 0.30f), false);
                hl.type = Image.Type.Sliced;
                Place(hl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cellSize, cellSize));
                hl.enabled = false; // only the active cell turns this on

                var label = AddText("ui_text_zone_cell_value", cell, "", 34, TextAlignmentOptions.Center);
                Stretch(label.rectTransform);
                label.fontStyle = FontStyles.Bold;

                var cellView = cell.gameObject.AddComponent<ZoneCell>();
                var cso = new SerializedObject(cellView);
                cso.FindProperty("highlight").objectReferenceValue = hl;
                cso.FindProperty("label").objectReferenceValue = label;
                cso.ApplyModifiedPropertiesWithoutUndo();
                cells[i] = cellView;
            }

            var view = track.gameObject.AddComponent<ZoneTrackView>();
            var so = new SerializedObject(view);
            so.FindProperty("safeInterval").intValue = ZoneRules.DefaultSafeInterval;
            so.FindProperty("superInterval").intValue = ZoneRules.DefaultSuperInterval;
            var arr = so.FindProperty("cells");
            arr.arraySize = cells.Length;
            for (int i = 0; i < cells.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static WheelView BuildWheel(RectTransform parent, GameSettings settings, SpriteRegistry registry)
        {
            var wheelRoot = NewRect("ui_wheel", parent);
            Place(wheelRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1000, 1300));

            var title = AddText("ui_text_wheel_title_value", wheelRoot, "SPIN", 64, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 625), new Vector2(800, 90));
            title.color = new Color(1f, 0.78f, 0.18f);
            title.fontStyle = FontStyles.Bold;

            var rotor = NewRect("ui_image_spin_rotor", wheelRoot);
            Place(rotor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 900));

            // cylinder bases live INSIDE the rotor so the chambers spin with the icons
            // (only the top indicator stays static, like a real revolver)
            var bronze = AddImage("ui_image_spin_bronze", rotor, LoadIcon("ui_spin_bronze_base"), Color.white, false);
            var silver = AddImage("ui_image_spin_silver", rotor, LoadIcon("ui_spin_silver_base"), Color.white, false);
            var golden = AddImage("ui_image_spin_golden", rotor, LoadIcon("ui_spin_golden_base"), Color.white, false);
            foreach (var img in new[] { bronze, silver, golden })
            {
                Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
                img.preserveAspect = true;
            }

            var sliceViews = new SliceView[8];
            for (int i = 0; i < 8; i++)
            {
                var slice = NewRect($"ui_slice_{i}", rotor);
                Place(slice, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                slice.localRotation = Quaternion.Euler(0, 0, -45f * i);

                // chamber centres sit at 0.60 of the rotor radius (~270 on the 900px rotor,
                // measured from the cylinder art), so the icon is centred there and the
                // amount badge hugs its lower edge — both land inside the chamber hole.
                var icon = AddImage("ui_image_slice_icon_value", slice, null, Color.white, false);
                Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 270), new Vector2(145, 145));
                icon.preserveAspect = true;

                var amount = AddText("ui_text_slice_amount_value", slice, "", 30, TextAlignmentOptions.Center);
                Place(amount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 205), new Vector2(150, 42));

                var view = slice.gameObject.AddComponent<SliceView>();
                var sso = new SerializedObject(view);
                sso.FindProperty("iconValue").objectReferenceValue = icon;
                sso.FindProperty("amountValue").objectReferenceValue = amount;
                sso.FindProperty("sprites").objectReferenceValue = registry;
                sso.ApplyModifiedPropertiesWithoutUndo();
                sliceViews[i] = view;
            }

            var indicator = AddImage("ui_image_spin_indicator", wheelRoot, LoadIcon("ui_spin_bronze_indicator"), Color.white, false);
            Place(indicator.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 510), new Vector2(80, 110));
            indicator.preserveAspect = true;

            var spinBtn = AddButton("ui_button_spin", wheelRoot, "SPIN", 48, LoadIcon("UI_button_orange_standard"));
            Place((RectTransform)spinBtn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -520), new Vector2(420, 130));

            var leaveBtn = AddButton("ui_button_leave", wheelRoot, "LEAVE & COLLECT", 34, LoadIcon("UI_button_grey_standard"));
            Place((RectTransform)leaveBtn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -665), new Vector2(420, 110));

            var view2 = wheelRoot.gameObject.AddComponent<WheelView>();
            var so = new SerializedObject(view2);
            so.FindProperty("rotor").objectReferenceValue = rotor;
            so.FindProperty("spinBronze").objectReferenceValue = bronze;
            so.FindProperty("spinSilver").objectReferenceValue = silver;
            so.FindProperty("spinGolden").objectReferenceValue = golden;
            so.FindProperty("indicator").objectReferenceValue = indicator;
            so.FindProperty("spinButton").objectReferenceValue = spinBtn;
            so.FindProperty("leaveButton").objectReferenceValue = leaveBtn;
            so.FindProperty("titleValue").objectReferenceValue = title;
            so.FindProperty("settings").objectReferenceValue = settings;
            var arr = so.FindProperty("slices");
            arr.arraySize = sliceViews.Length;
            for (int i = 0; i < sliceViews.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = sliceViews[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return view2;
        }

        private static RewardPopupView BuildRewardPopup(RectTransform parent, SpriteRegistry registry)
        {
            var overlay = BuildOverlay("ui_popup_reward", parent);

            var card = AddImage("ui_image_reward_card", overlay, LoadIcon("ui_card_frame_gardient"), Color.white, false);
            card.type = Image.Type.Sliced;
            Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(640, 840));

            var icon = AddImage("ui_image_reward_icon_value", card.rectTransform, null, Color.white, false);
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(330, 330));
            icon.preserveAspect = true;

            var amount = AddText("ui_text_reward_amount_value", card.rectTransform, "x1", 64, TextAlignmentOptions.Center);
            Place(amount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(420, 100));

            // collect sits centred alone, or shares the row with leave on safe/super zones;
            // the view sets the X at runtime so both fit even on 4:3.
            var collect = AddButton("ui_button_collect", overlay, "COLLECT", 40, LoadIcon("UI_button_orange_standard"));
            Place((RectTransform)collect.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -560), new Vector2(380, 130));

            // shown only on safe/super zones (toggled by the view) — bank the run + walk away
            var leave = AddButton("ui_button_reward_leave", overlay, "LEAVE & COLLECT", 32, LoadIcon("UI_button_grey_standard"));
            Place((RectTransform)leave.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(205, -560), new Vector2(380, 130));
            leave.gameObject.SetActive(false);

            var view = overlay.gameObject.AddComponent<RewardPopupView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = overlay.gameObject;
            so.FindProperty("card").objectReferenceValue = card.rectTransform;
            so.FindProperty("iconValue").objectReferenceValue = icon;
            so.FindProperty("amountValue").objectReferenceValue = amount;
            so.FindProperty("collectButton").objectReferenceValue = collect;
            so.FindProperty("leaveButton").objectReferenceValue = leave;
            so.FindProperty("sprites").objectReferenceValue = registry;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static BombExplodedView BuildBombScreen(RectTransform parent)
        {
            var overlay = BuildOverlay("ui_screen_bomb", parent);

            var titleTop = AddText("ui_text_bomb_title", overlay, "OH NO, A BOMB EXPLODED\nRIGHT IN YOUR HANDS!", 46, TextAlignmentOptions.Center);
            Place(titleTop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 540), new Vector2(950, 170));
            titleTop.color = new Color(1f, 0.35f, 0.3f);
            titleTop.fontStyle = FontStyles.Bold;

            var sub = AddText("ui_text_bomb_subtitle", overlay, "Revive yourself to keep your rewards.", 34, TextAlignmentOptions.Center);
            Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 430), new Vector2(900, 70));

            var icon = AddImage("ui_image_bomb_icon", overlay, LoadIcon("ui_card_icon_death"), Color.white, false);
            Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(360, 360));
            icon.preserveAspect = true;

            var giveUp = AddButton("ui_button_giveup", overlay, "GIVE UP", 30, LoadIcon("UI_button_grey_standard"));
            Place((RectTransform)giveUp.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-340, -460), new Vector2(290, 115));

            var reviveGold = AddButton("ui_button_revive_gold", overlay, "REVIVE", 32, LoadIcon("UI_button_orange_standard"));
            Place((RectTransform)reviveGold.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -460), new Vector2(300, 125));
            // gold cost line inside the revive button
            var costLabel = (RectTransform)reviveGold.transform.Find("ui_text_button_label");
            costLabel.anchoredPosition = new Vector2(0, 20);
            var cost = AddText("ui_text_revive_cost_value", (RectTransform)reviveGold.transform, "25", 26, TextAlignmentOptions.Center);
            Place(cost.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -26), new Vector2(200, 40));

            var reviveAd = AddButton("ui_button_revive_ad", overlay, "REVIVE (AD)", 28, LoadIcon("UI_button_orange_standard"));
            Place((RectTransform)reviveAd.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(340, -460), new Vector2(290, 115));

            var view = overlay.gameObject.AddComponent<BombExplodedView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = overlay.gameObject;
            so.FindProperty("reviveCostValue").objectReferenceValue = cost;
            so.FindProperty("giveUpButton").objectReferenceValue = giveUp;
            so.FindProperty("reviveGoldButton").objectReferenceValue = reviveGold;
            so.FindProperty("reviveAdButton").objectReferenceValue = reviveAd;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static CashOutView BuildCashOutScreen(RectTransform parent)
        {
            var overlay = BuildOverlay("ui_screen_cashout", parent);

            var summary = AddText("ui_text_cashout_summary_value", overlay, "You walked away!", 46, TextAlignmentOptions.Center);
            Place(summary.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(850, 320));

            var confirm = AddButton("ui_button_cashout_confirm", overlay, "CONFIRM", 44, LoadIcon("UI_button_orange_standard"));
            Place((RectTransform)confirm.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -400), new Vector2(420, 130));

            var view = overlay.gameObject.AddComponent<CashOutView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = overlay.gameObject;
            so.FindProperty("summaryValue").objectReferenceValue = summary;
            so.FindProperty("confirmButton").objectReferenceValue = confirm;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static InventoryView BuildInventory(RectTransform parent, SpriteRegistry registry)
        {
            var overlay = BuildOverlay("ui_screen_inventory", parent);

            var panel = AddImage("ui_image_inventory_panel", overlay, LoadIcon("ui_card_panel_zone_bg"), Color.white, false);
            panel.type = Image.Type.Sliced;
            Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(920, 1240));

            var title = AddText("ui_text_inventory_title", panel.rectTransform, "COLLECTED REWARDS", 48, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(800, 80));
            title.color = new Color(1f, 0.78f, 0.18f);
            title.fontStyle = FontStyles.Bold;

            var grid = NewRect("ui_inventory_grid", panel.rectTransform);
            Place(grid, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(820, 920));
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(150, 175);
            layout.spacing = new Vector2(14, 14);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;

            // inactive blueprint cell; InventoryView clones it per collected reward
            var template = NewRect("ui_item_template", grid);
            var cellBg = AddImage("ui_image_item_frame", template, LoadIcon("ui_card_frame_12px_neutral"), Color.white, false);
            cellBg.type = Image.Type.Sliced;
            Stretch(cellBg.rectTransform);
            var itemIcon = AddImage("ui_image_item_icon_value", template, null, Color.white, false);
            Place(itemIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(110, 110));
            var itemAmount = AddText("ui_text_item_amount_value", template, "", 26, TextAlignmentOptions.Center);
            Place(itemAmount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -62), new Vector2(140, 36));
            template.gameObject.SetActive(false);

            var empty = AddText("ui_text_inventory_empty_value", panel.rectTransform, "Nothing collected yet — spin the wheel!", 34, TextAlignmentOptions.Center);
            Place(empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(700, 90));

            var close = AddButton("ui_button_inventory_close", panel.rectTransform, "CLOSE", 36, LoadIcon("UI_button_grey_standard"));
            Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(340, 105));

            var view = overlay.gameObject.AddComponent<InventoryView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = overlay.gameObject;
            so.FindProperty("grid").objectReferenceValue = grid;
            so.FindProperty("itemTemplate").objectReferenceValue = template;
            so.FindProperty("emptyValue").objectReferenceValue = empty;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.FindProperty("sprites").objectReferenceValue = registry;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static GameOverView BuildGameOverScreen(RectTransform parent)
        {
            var overlay = BuildOverlay("ui_screen_gameover", parent);

            var title = AddText("ui_text_gameover_value", overlay, "GAME OVER", 84, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 220));
            title.fontStyle = FontStyles.Bold;

            var restart = AddButton("ui_button_restart", overlay, "RESTART", 44, LoadIcon("UI_button_orange_standard"));
            Place((RectTransform)restart.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(420, 130));

            var view = overlay.gameObject.AddComponent<GameOverView>();
            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = overlay.gameObject;
            so.FindProperty("titleValue").objectReferenceValue = title;
            so.FindProperty("restartButton").objectReferenceValue = restart;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---- low-level helpers ----------------------------------------------

        private static RectTransform BuildOverlay(string name, RectTransform parent)
        {
            var overlay = NewRect(name, parent);
            Stretch(overlay);
            // dim background; raycast ON so clicks can't reach the wheel underneath
            var bg = AddImage("ui_image_overlay_dim", overlay, null, new Color(0, 0, 0, 0.88f), raycast: true);
            Stretch(bg.rectTransform);
            return overlay;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            if (size != Vector2.zero) rt.sizeDelta = size;
        }

        private static Image AddImage(string name, RectTransform parent, Sprite sprite, Color color, bool raycast)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;   // brief: no RaycastTarget on decorative images
            img.maskable = false;          // brief: no Maskable on unnecessary images
            return img;
        }

        private static TextMeshProUGUI AddText(string name, RectTransform parent, string text,
            float size, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button AddButton(string name, RectTransform parent, string label,
            float fontSize, Sprite sprite)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;  // brief: use Sliced sprites
            img.raycastTarget = true;      // buttons must receive clicks
            img.maskable = false;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            // make the disabled state unmistakable (e.g. LEAVE is locked until zone 5/30)
            var colors = btn.colors;
            colors.disabledColor = new Color(0.32f, 0.32f, 0.32f, 0.55f);
            btn.colors = colors;

            var text = AddText("ui_text_button_label", rt, label, fontSize, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            text.fontStyle = FontStyles.Bold;
            return btn;
        }

        // ---------------------------------------------------------------- APK

        [MenuItem("Wof/Bootstrap/4. Build APK")]
        public static void BuildApk()
        {
            // IL2CPP + ARM64: modern Android devices (Pixel 7+, etc.) no longer run ARMv7-only APKs.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;

            Directory.CreateDirectory("Builds");
            var report = UnityEditor.BuildPipeline.BuildPlayer(
                new[] { ScenePath }, "Builds/wof-demo.apk", BuildTarget.Android, BuildOptions.None);
            Debug.Log($"[Bootstrap] APK build result: {report.summary.result}, size: {report.summary.totalSize}");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("APK build failed");
        }
    }
}
