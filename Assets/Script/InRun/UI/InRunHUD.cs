using UnityEngine;

public class InRunHUD : MonoBehaviour
{
    private InRunDirector director;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle centerStyle;

    public void Bind(InRunDirector targetDirector)
    {
        director = targetDirector;
    }

    private void OnGUI()
    {
        if (director == null || !director.IsHudVisible)
            return;

        EnsureStyles();

        Rect panel = new Rect(12f, 12f, 320f, 178f);
        GUI.Box(panel, string.Empty);

        GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 10f, panel.width - 24f, panel.height - 20f));
        GUILayout.Label("IN-RUN DEBUG HUD", titleStyle);
        GUILayout.Space(4f);
        GUILayout.Label($"Phase: {director.CurrentPhase}", labelStyle);
        GUILayout.Label($"Theme: {director.CurrentThemeLabel}", labelStyle);
        GUILayout.Label($"Loop: {director.CurrentLoopLabel}", labelStyle);
        GUILayout.Label($"Theme Id: {director.CurrentThemeId}", labelStyle);
        GUILayout.Label($"Timer: {director.CurrentLoopTimerText}", labelStyle);
        GUILayout.Label($"Pulse: {director.CurrentPulseStatusText}", labelStyle);
        GUILayout.Label($"Enemies: {director.CurrentActiveEnemyCount}", labelStyle);
        GUILayout.Label($"Threat: {director.CurrentActiveThreat:0.00}", labelStyle);
        GUILayout.EndArea();

        if (director.CurrentPhase != InRunPhase.CombatLoopActive && director.CurrentPhase != InRunPhase.PulseReady)
            return;

        Rect centerRect = new Rect(0f, Screen.height * 0.36f, Screen.width, 80f);
        GUI.Label(centerRect, $"PULSE\nPress {director.CurrentPulseKeyName} to cash out", centerStyle);
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
        }

        if (centerStyle != null)
            return;

        centerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };
    }
}
