using Uncrowned.Combat;
using Uncrowned.Core;
using Uncrowned.Enemies;
using Uncrowned.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Uncrowned.UI
{
    /// <summary>uGUI HUD built in code: health, Valor pips, ability cooldowns, wave counter.</summary>
    public class Hud : MonoBehaviour
    {
        public Health player;
        public WarriorCombat combat;
        public EnemySpawner spawner;

        Font font;
        Image hpFill;
        Text hpText;
        Image[] pips;
        Image[] cdOverlays;
        Text[] cdTexts;
        Text waveText;
        Text centerText;
        Image warCryGlow;

        static readonly Ability[] Slots = { Ability.Combo, Ability.Dash, Ability.WideSwing, Ability.WarCry, Ability.Ultimate };
        static readonly string[] Keys = { "ЛКМ", "Space", "ПКМ", "Q", "R" };
        static readonly string[] Names = { "Серия", "Рывок", "Взмах", "Клич", "Суд короны" };

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
            var hpBg = MakeImage(root, "HP Bg", new Color(0.1f, 0.05f, 0.05f, 0.85f), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(520, 30));
            hpFill = MakeImage(hpBg.transform, "HP Fill", new Color(0.8f, 0.15f, 0.15f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(512, 22));
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpText = MakeText(hpBg.transform, "HP Text", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(512, 30));

            // Valor pips
            int max = combat.Valor.max;
            pips = new Image[max];
            float pipW = 44, gap = 8;
            float startX = -(max - 1) * (pipW + gap) * 0.5f;
            for (int i = 0; i < max; i++)
                pips[i] = MakeImage(root, "Valor " + i, Color.gray, new Vector2(0.5f, 0), new Vector2(startX + i * (pipW + gap), 118), new Vector2(pipW, 14));

            // Ability slots
            cdOverlays = new Image[Slots.Length];
            cdTexts = new Text[Slots.Length];
            float slot = 86, sgap = 14;
            float sx = -(Slots.Length - 1) * (slot + sgap) * 0.5f;
            for (int i = 0; i < Slots.Length; i++)
            {
                bool ult = Slots[i] == Ability.Ultimate;
                var bg = MakeImage(root, "Slot " + Names[i], ult ? new Color(0.35f, 0.25f, 0.05f, 0.95f) : new Color(0.15f, 0.15f, 0.2f, 0.95f),
                    new Vector2(0.5f, 0), new Vector2(sx + i * (slot + sgap), 55), new Vector2(slot, slot * 0.7f));
                MakeText(bg.transform, "Name", 15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(slot, 24)).text = Names[i];
                MakeText(bg.transform, "Key", 13, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(slot, 20)).text = Keys[i];
                var ov = MakeImage(bg.transform, "Cooldown", new Color(0, 0, 0, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slot, slot * 0.7f));
                ov.type = Image.Type.Filled;
                ov.fillMethod = Image.FillMethod.Vertical;
                ov.fillOrigin = (int)Image.OriginVertical.Top;
                cdOverlays[i] = ov;
                cdTexts[i] = MakeText(bg.transform, "CD", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(slot, slot * 0.7f));
            }

            warCryGlow = MakeImage(root, "WarCry", new Color(1f, 0.4f, 0.2f, 0f), new Vector2(0.5f, 0), new Vector2(0, 118), new Vector2(560, 22));
            warCryGlow.transform.SetSiblingIndex(0);

            waveText = MakeText(root, "Wave", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(600, 40));
            var help = MakeText(root, "Help", 18, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(240, -130), new Vector2(420, 200));
            help.text = "WASD — движение, мышь — прицел\nЛКМ — серия ударов (3-й широкий)\nПКМ — широкий взмах\nSpace — рывок\nQ — боевой клич (Доблесть не сгорает)\nR — Суд короны (тратит всю Доблесть)";
            help.color = new Color(1, 1, 1, 0.6f);
            centerText = MakeText(root, "Center", 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1000, 120));
        }

        void Update()
        {
            if (hpFill == null) return;

            hpFill.fillAmount = player.Current / player.max;
            hpText.text = $"{Mathf.CeilToInt(player.Current)} / {player.max}";

            var valor = combat.Valor;
            bool full = valor.IsFull;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 10f);
            for (int i = 0; i < pips.Length; i++)
            {
                bool on = i < valor.Charges;
                pips[i].color = !on ? new Color(0.2f, 0.2f, 0.25f, 0.9f)
                    : full ? new Color(1f, 0.85f, 0.2f) * pulse
                    : new Color(0.95f, 0.75f, 0.3f);
            }
            warCryGlow.color = new Color(1f, 0.4f, 0.2f, combat.WarCryActive ? 0.45f : 0f);

            for (int i = 0; i < Slots.Length; i++)
            {
                cdOverlays[i].fillAmount = combat.CooldownFraction(Slots[i]);
                float rem = combat.CooldownRemaining(Slots[i]);
                cdTexts[i].text = rem > 0.05f && Slots[i] != Ability.Combo ? rem.ToString(rem < 1 ? "0.0" : "0") : "";
            }

            waveText.text = spawner.Wave > 0 ? $"Волна {spawner.Wave}   ·   Врагов: {spawner.Alive}" : "Приготовься...";
            centerText.text = player.IsDead ? $"Ты пал на волне {spawner.Wave}\nEnter — заново" : "";
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
