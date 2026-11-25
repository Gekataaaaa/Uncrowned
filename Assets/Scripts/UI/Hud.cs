using Uncrowned.Combat;
using Uncrowned.Core;
using Uncrowned.Enemies;
using Uncrowned.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Uncrowned.UI
{
    /// <summary>uGUI HUD built in code: health, mana, ability buttons with cooldowns, stage progress.</summary>
    public class Hud : MonoBehaviour
    {
        public Health player;
        public WarriorCombat combat;
        public StageDirector stage;

        Font font;
        Image hpFill;
        Text hpText;
        Image manaFill;
        Text manaText;
        Image[] icons;
        Image[] cdOverlays;
        Text[] cdTexts;
        Text waveText;
        Text centerText;
        Image warCryGlow;

        static readonly Ability[] Slots = { Ability.Combo, Ability.Dash, Ability.WideSwing, Ability.WarCry, Ability.Ultimate };
        static readonly string[] Keys = { "LMB", "Shift", "RMB", "Q", "R" };

        Image bossBar;
        GameObject bossBarRoot;
        static readonly string[] Names = { "Sword Chain", "Dash", "Sweeping Arc", "War Cry", "Crown's Judgement" };

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HUD Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            // Health bar
            var hpBg = MakeImage(root, "HP Bg", new Color(0.1f, 0.05f, 0.05f, 0.85f), new Vector2(0, 0), new Vector2(330, 92), new Vector2(520, 30));
            hpFill = MakeImage(hpBg.transform, "HP Fill", new Color(0.8f, 0.15f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(512, 22));
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpText = MakeText(hpBg.transform, "HP Text", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(512, 30));

            // Mana bar
            var manaBg = MakeImage(root, "Mana Bg", new Color(0.04f, 0.05f, 0.12f, 0.85f), new Vector2(0, 0), new Vector2(330, 58), new Vector2(520, 24));
            manaFill = MakeImage(manaBg.transform, "Mana Fill", new Color(0.2f, 0.45f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(512, 16));
            manaFill.type = Image.Type.Filled;
            manaFill.fillMethod = Image.FillMethod.Horizontal;
            manaText = MakeText(manaBg.transform, "Mana Text", 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(512, 24));

            // Round ability buttons: disc body, pixel icon, radial cooldown sweep, border ring, key label.
            icons = new Image[Slots.Length];
            cdOverlays = new Image[Slots.Length];
            cdTexts = new Text[Slots.Length];
            const float size = 100f, ultSize = 124f, sgap = 24f;
            float total = 0;
            foreach (var s in Slots) total += (s == Ability.Ultimate ? ultSize : size) + sgap;
            float x = -(total - sgap) - 50f; // right-aligned, 50px from the edge
            for (int i = 0; i < Slots.Length; i++)
            {
                bool ult = Slots[i] == Ability.Ultimate;
                float d = ult ? ultSize : size;
                x += d * 0.5f;
                var btn = MakeImage(root, "Button " + Names[i], ult ? new Color(0.3f, 0.2f, 0.05f, 0.95f) : new Color(0.13f, 0.13f, 0.18f, 0.95f),
                    new Vector2(1, 0), new Vector2(x, 92), new Vector2(d, d));
                btn.sprite = IconFactory.Disc;

                var icon = MakeImage(btn.transform, "Icon", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d * 0.66f, d * 0.66f));
                icon.sprite = IconFactory.Icon(Slots[i]);
                icons[i] = icon;

                var ov = MakeImage(btn.transform, "Cooldown", new Color(0, 0, 0, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d, d));
                ov.sprite = IconFactory.Disc;
                ov.type = Image.Type.Filled;
                ov.fillMethod = Image.FillMethod.Radial360;
                ov.fillOrigin = (int)Image.Origin360.Top;
                ov.fillClockwise = false;
                cdOverlays[i] = ov;

                var ring = MakeImage(btn.transform, "Ring", ult ? new Color(1f, 0.8f, 0.25f) : new Color(0.55f, 0.6f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d, d));
                ring.sprite = IconFactory.RingSprite;

                cdTexts[i] = MakeText(btn.transform, "CD", ult ? 36 : 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(d, d));
                cdTexts[i].fontStyle = FontStyle.Bold;

                var keyBg = MakeImage(btn.transform, "KeyBg", new Color(0.05f, 0.05f, 0.08f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, -d * 0.5f + 2), new Vector2(64, 24));
                MakeText(keyBg.transform, "Key", 16, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 24)).text = Keys[i];

                x += d * 0.5f + sgap;
            }

            warCryGlow = MakeImage(root, "WarCry", new Color(1f, 0.4f, 0.2f, 0f), new Vector2(0, 0), new Vector2(330, 75), new Vector2(548, 76));
            warCryGlow.transform.SetSiblingIndex(0);

            waveText = MakeText(root, "Wave", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(600, 40));
            var help = MakeText(root, "Help", 18, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(240, -130), new Vector2(420, 200));
            help.text = "A / D - move,  Space - jump,  Mouse - aim\nLMB - Sword Chain toward the cursor (3rd hit is heavy)\nRMB - Sweeping Arc, hits both sides\nShift - Dash left / right (toward the cursor)\nQ - War Cry: +20% damage and attack speed\nR - Crown's Judgement: launch, then slam\nEsc - World Map";

            // Boss health bar, shown during the boss fight.
            var bossBg = MakeImage(root, "Boss Bg", new Color(0.1f, 0.05f, 0.1f, 0.85f), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(820, 26));
            bossBar = MakeImage(bossBg.transform, "Boss Fill", new Color(0.6f, 0.2f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(812, 18));
            bossBar.type = Image.Type.Filled;
            bossBar.fillMethod = Image.FillMethod.Horizontal;
            MakeText(bossBg.transform, "Boss Name", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(820, 30)).text = stage.bossName;
            bossBarRoot = bossBg.gameObject;
            help.color = new Color(1, 1, 1, 0.6f);
            centerText = MakeText(root, "Center", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1000, 120));
        }

        void Update()
        {
            if (hpFill == null) return;

            hpFill.fillAmount = player.Current / player.max;
            hpText.text = $"{Mathf.CeilToInt(player.Current)} / {player.max}";

            var mana = combat.Mana;
            manaFill.fillAmount = mana.Current / mana.max;
            manaText.text = $"{Mathf.FloorToInt(mana.Current)} / {mana.max}";
            warCryGlow.color = new Color(1f, 0.4f, 0.2f, combat.WarCryActive ? 0.45f : 0f);

            for (int i = 0; i < Slots.Length; i++)
            {
                // Not enough mana: icon goes dim blue.
                icons[i].color = combat.CanAfford(Slots[i]) ? Color.white : new Color(0.35f, 0.45f, 0.9f, 0.55f);
                cdOverlays[i].fillAmount = combat.CooldownFraction(Slots[i]);
                float rem = combat.CooldownRemaining(Slots[i]);
                cdTexts[i].text = rem > 0.05f && Slots[i] != Ability.Combo ? rem.ToString(rem < 1 ? "0.0" : "0") : "";
            }

            bool bossFight = stage.Current == StageDirector.State.BossFight && stage.Boss != null;
            bossBarRoot.SetActive(bossFight);
            if (bossFight) bossBar.fillAmount = stage.Boss.Health.Current / stage.Boss.Health.max;

            waveText.text = stage.Current switch
            {
                StageDirector.State.Road => $"Stage {stage.stageId}   ·   {Mathf.RoundToInt(stage.Progress * 100)}%",
                StageDirector.State.BossFight => $"Stage {stage.stageId}   ·   BOSS",
                _ => $"Stage {stage.stageId} cleared"
            };
            centerText.text = player.IsDead ? "You have fallen\nEnter - try again    Esc - world map"
                : stage.Current == StageDirector.State.Cleared
                    ? (WorldMapData.TownAfter(stage.stageId) is { } town ? "Stage cleared!\nEnter - travel to " + town.title : "Stage cleared!\nEnter - return to the world map")
                    : "";
        }

        Image MakeImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.Square;
            img.color = color;
            return img;
        }

        Text MakeText(Transform parent, string name, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }
    }
}
