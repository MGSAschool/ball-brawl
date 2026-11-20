using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDPlayerCards : HUDPanel
{
    private const float FrameHeight = 165f;
    private const float StripHeight = 46f;
    private const float Gap = 6f;

    private static readonly Rect FrameArtArea = Rect.MinMaxRect(0.121f, 0.204f, 0.867f, 0.781f);
    private static readonly Rect CoinsNumber = Rect.MinMaxRect(0.508f, 0.315f, 0.604f, 0.407f);
    private static readonly Rect PointsNumber = Rect.MinMaxRect(0.669f, 0.315f, 0.833f, 0.407f);

    private static readonly Rect[] Slots =
    {
        Rect.MinMaxRect(0.185f, 0.333f, 0.393f, 0.685f),
        Rect.MinMaxRect(0.440f, 0.433f, 0.583f, 0.676f),
        Rect.MinMaxRect(0.615f, 0.433f, 0.758f, 0.676f)
    };

    private static readonly Color[] LevelColors =
    {
        new Color(0.36f, 0.82f, 0.42f),
        new Color(1f, 0.62f, 0.2f),
        new Color(0.93f, 0.25f, 0.3f)
    };

    private class PlayerCard
    {
        public TextMeshProUGUI Points;
        public TextMeshProUGUI Coins;
        public TextMeshProUGUI Status;
        public GameObject Strip;
        public PowerupInventoryUI Box;
        public Image[] Badges = new Image[0];
        public TextMeshProUGUI[] BadgeNumbers = new TextMeshProUGUI[0];
    }

    private readonly Dictionary<PlayerEntry, PlayerCard> cards = new Dictionary<PlayerEntry, PlayerCard>();

    protected override void BuildPanel()
    {
    }

    private void Update()
    {
        if (hud == null || Match == null) return;

        for (int i = 0; i < Match.Players.Count; i++)
        {
            PlayerEntry entry = Match.Players[i];
            PlayerCard card;
            if (!cards.TryGetValue(entry, out card))
            {
                card = MakeCard(entry, i);
                cards.Add(entry, card);
            }

            card.Points.text = entry.Score.ToString();
            card.Coins.text = entry.Coins.ToString();
            card.Status.text = StatusText(entry);
            card.Strip.SetActive(card.Status.text.Length > 0);
            RefreshLevels(entry, card);
        }
    }

    private string StatusText(PlayerEntry entry)
    {
        if (entry.IsControllerLost) return UIKit.Colored("NO CONTROLLER", HUDStyle.BadHex) + " - press " + ControlHints.Cap("A");
        if (entry.IsRespawning) return UIKit.Colored("back in " + Mathf.CeilToInt(entry.RespawnAt - Time.time), "aaaaaa");

        string status = "";
        if (entry.Player.IsInvulnerable) status += UIKit.Colored("SHIELD", "7fd7ff") + " ";
        if (entry.Player.HasSuperKnockback) status += UIKit.Colored("PUNCH", "ff9f43") + " ";
        if (entry.Player.HasSpeedBoost) status += UIKit.Colored("SPEED", HUDStyle.GoodHex) + " ";
        if (entry.Player.HasDash) status += UIKit.Colored("DASH", "ffe066") + " ";
        return status.TrimEnd();
    }

    private void RefreshLevels(PlayerEntry entry, PlayerCard card)
    {
        if (entry.PowerUps == null || card.Box == null) return;

        IReadOnlyList<PowerUpModifier> items = entry.PowerUps.Items;
        bool iconsOutOfDate = false;
        for (int s = 0; s < card.Badges.Length; s++)
        {
            Image icon = card.Box.icons[s];
            bool hasItem = s < items.Count && items[s] != null;
            int level = 0;
            if (hasItem && icon.enabled && icon.sprite != null) level = Mathf.Clamp(items[s].Level, 1, 3);
            ShowLevel(card.Badges[s], card.BadgeNumbers[s], level);

            if (icon.enabled != s < items.Count) iconsOutOfDate = true;
        }

        if (iconsOutOfDate) card.Box.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
    }

    private void ShowLevel(Image badge, TextMeshProUGUI number, int level)
    {
        bool show = level > 0;
        badge.enabled = show;
        number.enabled = show;
        if (!show) return;

        badge.color = LevelColors[level - 1];
        number.text = level.ToString();
    }

    private PlayerCard MakeCard(PlayerEntry entry, int index)
    {
        Vector2 corner = new Vector2(index % 2, 1f);
        if (index >= 2) corner.y = 0f;
        bool top = corner.y > 0.5f;

        bool framed = Art != null && Art.inventoryFrame != null;
        float width = 300f;
        if (framed)
        {
            float artWidth = Art.inventoryFrame.rect.width * FrameArtArea.width;
            float artHeight = Art.inventoryFrame.rect.height * FrameArtArea.height;
            width = FrameHeight * artWidth / artHeight;
        }
        float height = FrameHeight + Gap + StripHeight;

        float margin = hud.CornerMargin;
        Vector2 offset = new Vector2(margin, -margin);
        if (corner.x > 0.5f) offset.x = -margin;
        if (!top) offset.y = margin;

        RectTransform root = UIKit.NewRect(entry.Name + " Card", Root);
        UIKit.PlaceAt(root, corner, offset, new Vector2(width, height));

        float stripPart = StripHeight / height;
        float framePart = (StripHeight + Gap) / height;
        Rect frameRect = Rect.MinMaxRect(0f, 0f, 1f, 1f - framePart);
        Rect stripRect = Rect.MinMaxRect(0f, 1f - stripPart, 1f, 1f);
        if (top)
        {
            frameRect = Rect.MinMaxRect(0f, framePart, 1f, 1f);
            stripRect = Rect.MinMaxRect(0f, 0f, 1f, stripPart);
        }

        Image frame = UIKit.NewImage("Inventory", root);
        if (framed)
        {
            frame.sprite = Art.inventoryFrame;
            UIKit.StretchToArt(frame.rectTransform, frameRect, FrameArtArea);
        }
        else
        {
            UIKit.Stretch(frame.rectTransform, frameRect);
            frame.color = HUDStyle.Panel;
        }

        PlayerCard card = new PlayerCard();
        MakePortrait(entry, frame.rectTransform);
        MakeCounters(card, frame.rectTransform);
        MakeStatsStrip(entry, card, root, stripRect);
        if (hud.PowerUpBoxPrefab != null && entry.PowerUps != null) AttachPowerUpBox(entry, index, frame.rectTransform, card);
        return card;
    }

    private void MakeCounters(PlayerCard card, RectTransform frame)
    {
        card.Coins = UIKit.NewAutoText(frame, "Coins", 10f, 30f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(card.Coins.rectTransform, CoinsNumber);

        card.Points = UIKit.NewAutoText(frame, "Points", 10f, 30f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(card.Points.rectTransform, PointsNumber);
    }

    private void MakePortrait(PlayerEntry entry, RectTransform frame)
    {
        Image portrait = UIKit.NewImage("Portrait", frame);
        UIKit.Stretch(portrait.rectTransform, UIKit.Inset(Slots[0], 0.12f));
        portrait.sprite = TextureMaker.Circle();
        portrait.color = entry.Color;
        portrait.preserveAspect = true;

        TextMeshProUGUI label = UIKit.NewAutoText(portrait.rectTransform, "Name", 14f, 40f, TextAlignmentOptions.Center, 0f);
        label.text = entry.Name;
    }

    private void MakeStatsStrip(PlayerEntry entry, PlayerCard card, RectTransform root, Rect stripRect)
    {
        Image strip = UIKit.NewImage("Stats", root);
        UIKit.Stretch(strip.rectTransform, stripRect);
        strip.color = HUDStyle.Panel;
        UIKit.AddOutline(strip.gameObject, entry.Color, 3f);

        card.Strip = strip.gameObject;

        card.Status = UIKit.NewAutoText(strip.rectTransform, "Status", 10f, 22f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(card.Status.rectTransform, Rect.MinMaxRect(0.03f, 0.1f, 0.97f, 0.9f));
    }

    private void AttachPowerUpBox(PlayerEntry entry, int index, RectTransform frame, PlayerCard card)
    {
        PowerupInventoryUI box = Instantiate(hud.PowerUpBoxPrefab, frame, false);
        box.name = entry.Name + " Power-Ups";
        box.Initialize(index + 1, entry.PowerUps.Inventory);
        entry.PowerUps.Cleared += () => box.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
        card.Box = box;

        int slotCount = box.icons.Length;
        card.Badges = new Image[slotCount];
        card.BadgeNumbers = new TextMeshProUGUI[slotCount];
        for (int s = 0; s < slotCount; s++)
        {
            Rect slot = UIKit.Inset(Slots[Mathf.Min(s + 1, Slots.Length - 1)], 0.1f);

            Image icon = box.icons[s];
            icon.rectTransform.SetParent(frame, false);
            icon.preserveAspect = true;
            icon.enabled = false;
            UIKit.Stretch(icon.rectTransform, slot);

            Image badge = UIKit.NewImage("Level", frame);
            badge.sprite = TextureMaker.Circle();
            badge.preserveAspect = true;
            UIKit.Stretch(badge.rectTransform, Rect.MinMaxRect(
                slot.xMax - slot.width * 0.42f, slot.yMin - slot.height * 0.06f,
                slot.xMax + slot.width * 0.06f, slot.yMin + slot.height * 0.42f));
            UIKit.AddOutline(badge.gameObject, new Color(0.1f, 0.05f, 0.15f, 0.9f), 2f);

            TextMeshProUGUI number = UIKit.NewAutoText(badge.rectTransform, "Number", 10f, 30f, TextAlignmentOptions.Center, 0f);
            number.outlineWidth = 0.25f;
            number.outlineColor = new Color32(30, 15, 40, 255);

            card.Badges[s] = badge;
            card.BadgeNumbers[s] = number;
            ShowLevel(badge, number, 0);
        }

        foreach (Graphic graphic in box.GetComponentsInChildren<Graphic>(true))
        {
            if (System.Array.IndexOf(box.icons, graphic) < 0) graphic.enabled = false;
        }
    }
}
