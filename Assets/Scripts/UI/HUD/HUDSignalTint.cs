using UnityEngine;
using UnityEngine.UI;

public class HUDSignalTint : HUDPanel
{
    private Image tint;

    protected override void BuildPanel()
    {
        tint = UIKit.NewImage("Signal Tint", Root);
        tint.rectTransform.SetAsFirstSibling();
        UIKit.Stretch(tint.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
        tint.sprite = TextureMaker.Vignette();
        tint.color = Color.clear;
    }

    private void Update()
    {
        if (hud == null) return;

        StaphItMode staph = StaphItMode.Active;
        Color target = Color.clear;
        if (staph != null && staph.Light == StaphItMode.Signal.Red)
        {
            float alpha = Mathf.Lerp(0.18f, 0.32f, 0.5f + 0.5f * Mathf.Sin(Time.time * 10f));
            target = new Color(1f, 0.1f, 0.1f, alpha);
        }
        else if (staph != null && staph.Light == StaphItMode.Signal.Yellow)
        {
            target = new Color(1f, 0.8f, 0.1f, Mathf.PingPong(Time.time * 3f, 0.22f));
        }

        tint.color = Color.Lerp(tint.color, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }
}
