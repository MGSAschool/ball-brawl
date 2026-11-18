using UnityEngine;

public abstract class HUDPanel : MonoBehaviour
{
    protected MatchHUD hud;

    protected MatchManager Match { get { return hud.Match; } }
    protected RectTransform Root { get { return hud.SafeArea; } }
    protected GameArt Art { get { return hud.Art; } }

    protected bool LobbyFull
    {
        get { return Match.Players.Count >= hud.MaxPlayers; }
    }

    public void Build(MatchHUD owner)
    {
        hud = owner;
        BuildPanel();
    }

    protected abstract void BuildPanel();

    protected bool SchemeJoined(string scheme)
    {
        foreach (PlayerEntry entry in Match.Players)
        {
            if (entry.ControlScheme == scheme) return true;
        }
        return false;
    }

    protected void FadeIfBallBehind(GameObject panel)
    {
        if (!panel.activeSelf) return;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();

        float target = 1f;
        if (IsAnyBallBehind((RectTransform)panel.transform)) target = hud.BehindAlpha;
        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * 4f);
    }

    private bool IsAnyBallBehind(RectTransform rect)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        foreach (PlayerEntry entry in Match.Players)
        {
            if (entry.Player == null || entry.IsRespawning) continue;
            Vector3 screen = cam.WorldToScreenPoint(entry.Player.transform.position);
            if (screen.z > 0f && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null)) return true;
        }
        return false;
    }
}
