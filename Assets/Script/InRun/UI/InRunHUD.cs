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
        GUILayout.Label($"Loop Score: {director.CurrentLoopScore}", labelStyle);
        GUILayout.Label($"Loop Grade: {director.CurrentLoopGrade}", labelStyle);
        GUILayout.Label($"Run Currency: {director.CurrentRunCurrency}", labelStyle);
        if (!string.IsNullOrWhiteSpace(director.CurrentBossName))
            GUILayout.Label($"Boss: {director.CurrentBossName}", labelStyle);
        GUILayout.EndArea();

        switch (director.CurrentPhase)
        {
            case InRunPhase.CombatLoopActive:
            case InRunPhase.PulseReady:
                DrawPulsePrompt();
                break;
            case InRunPhase.LoopReward:
                DrawRewardPanel();
                break;
            case InRunPhase.BossActive:
                DrawBossPanel();
                break;
            case InRunPhase.BossReward:
                DrawRewardPanel(true);
                break;
            case InRunPhase.Shop:
                DrawShopPanel();
                break;
        }
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

    private void DrawPulsePrompt()
    {
        Rect centerRect = new Rect(0f, Screen.height * 0.36f, Screen.width, 80f);
        GUI.Label(centerRect, $"PULSE\nPress {director.CurrentPulseKeyName} to cash out", centerStyle);
    }

    private void DrawRewardPanel(bool isBossReward = false)
    {
        RewardRollResult result = director.CurrentRewardResult;
        if (result == null)
            return;

        Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.2f, 520f, 260f);
        GUI.Box(panel, string.Empty);
        GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, panel.height - 24f));
        GUILayout.Label(isBossReward ? "BOSS REWARD  Grade SSS" : $"LOOP RESULT  Grade {director.CurrentLoopGrade}", titleStyle);
        if (!isBossReward)
            GUILayout.Label($"Score {director.CurrentLoopScore}   Currency +{director.CurrentLoopCurrencyGain}", labelStyle);
        GUILayout.Label($"Pick {Mathf.Max(0, result.picksAllowed - result.picksMade)} reward(s)", labelStyle);
        GUILayout.Space(8f);

        for (int i = 0; i < result.choices.Count; i++)
        {
            RewardChoice choice = result.choices[i];
            string state = choice.selected ? "[TAKEN]" : $"[{i + 1}]";
            GUILayout.Label($"{state} {choice.displayName}  +{choice.currencyBonus}c", labelStyle);
            GUILayout.Label(choice.description, labelStyle);
            GUILayout.Space(6f);
        }

        GUILayout.EndArea();
    }

    private void DrawBossPanel()
    {
        Rect panel = new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.22f, 480f, 120f);
        GUI.Box(panel, string.Empty);
        GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, panel.height - 24f));
        GUILayout.Label("BOSS ENCOUNTER", titleStyle);
        GUILayout.Label($"Current Boss: {director.CurrentBossName}", labelStyle);
        GUILayout.Label("Defeat the boss to unlock SSS reward.", labelStyle);
        GUILayout.EndArea();
    }

    private void DrawShopPanel()
    {
        var offers = director.CurrentShopOffers;
        if (offers == null)
            return;

        Rect panel = new Rect(Screen.width * 0.5f - 280f, Screen.height * 0.18f, 560f, 300f);
        GUI.Box(panel, string.Empty);
        GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, panel.height - 24f));
        GUILayout.Label($"SHOP  Currency {director.CurrentRunCurrency}", titleStyle);
        GUILayout.Label("Press number to buy, Space/Enter/N to continue", labelStyle);
        GUILayout.Space(8f);

        for (int i = 0; i < offers.Count; i++)
        {
            var offer = offers[i];
            string state = offer.purchased ? "[BOUGHT]" : $"[{i + 1}]";
            GUILayout.Label($"{state} {offer.displayName}  Cost {offer.cost}", labelStyle);
            GUILayout.Label(offer.description, labelStyle);
            GUILayout.Space(6f);
        }

        GUILayout.EndArea();
    }
}
