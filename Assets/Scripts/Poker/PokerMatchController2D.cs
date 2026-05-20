using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PokerMatchController2D : MonoBehaviour
{
    [Header("World Link")]
    [SerializeField] private RuntimeHouseInterior2D houseInterior;
    [SerializeField] private string tableLabel = "Poker Table";
    [SerializeField] private KeyCode sitKey = KeyCode.E;

    [Header("Table")]
    [SerializeField] private Vector2 tableOffset = new Vector2(0f, 0.35f);
    [SerializeField] private Vector2 tableVisualSize = new Vector2(2.65f, 1.15f);
    [SerializeField] private Vector2 tableTriggerSize = new Vector2(3.2f, 2.15f);
    [SerializeField] private bool createTableVisual = true;
    [SerializeField] private Color tableColor = new Color32(45, 115, 74, 255);
    [SerializeField] private Color tableRailColor = new Color32(92, 62, 40, 255);

    [Header("Match")]
    [SerializeField] private int startingChips = 100;
    [SerializeField] private int ante = 5;
    [SerializeField] private int betSize = 10;
    [SerializeField] private float matchCameraZoom = 3.7f;

    private GameObject tableVisual;
    private PlayerMovement2D cachedPlayer;
    private PlayerMovement2D seatedPlayer;
    private OfflinePokerMatch match;
    private bool isPlayerNearTable;
    private bool hasStoredCameraZoom;
    private float cameraZoomBeforeMatch = 4.25f;

    private GUIStyle actionButtonStyle;
    private GUIStyle centerLabelStyle;
    private GUIStyle chipStyle;
    private GUIStyle darkCardTextStyle;
    private GUIStyle hintStyle;
    private GUIStyle labelStyle;
    private GUIStyle logStyle;
    private GUIStyle panelStyle;
    private GUIStyle redCardTextStyle;
    private GUIStyle resultStyle;
    private GUIStyle smallLabelStyle;
    private GUIStyle titleStyle;

    private Texture2D cardBackTexture;
    private Texture2D cardBorderTexture;
    private Texture2D cardFaceTexture;
    private Texture2D emptyCardTexture;
    private Texture2D feltTexture;
    private Texture2D logTexture;
    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D railTexture;
    private Texture2D seatTexture;
    private Texture2D statTexture;

    private void Awake()
    {
        if (houseInterior == null)
        {
            houseInterior = GetComponent<RuntimeHouseInterior2D>();
        }

        EnsureTableVisual();
    }

    private void OnEnable()
    {
        EnsureTableVisual();
    }

    private void OnDisable()
    {
        ReturnToExploration();
    }

    private void Update()
    {
        if (!Application.isPlaying || houseInterior == null)
        {
            return;
        }

        UpdateTableVisual();

        if (match != null)
        {
            return;
        }

        PlayerMovement2D player = ResolvePlayer();
        isPlayerNearTable = houseInterior.IsInside && IsPlayerInTableArea(player);

        if (isPlayerNearTable && Input.GetKeyDown(sitKey))
        {
            StartMatch(player);
        }
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || houseInterior == null)
        {
            return;
        }

        EnsureGuiStyles();

        if (match != null)
        {
            DrawMatchUi();
            return;
        }

        if (isPlayerNearTable)
        {
            DrawSitPrompt();
        }
    }

    private void StartMatch(PlayerMovement2D player)
    {
        if (player == null)
        {
            return;
        }

        seatedPlayer = player;
        seatedPlayer.SetInputEnabled(false);
        StoreAndApplyMatchCameraZoom();
        match = new OfflinePokerMatch(startingChips, ante, betSize);
        isPlayerNearTable = false;
    }

    private void ReturnToExploration()
    {
        if (seatedPlayer != null)
        {
            seatedPlayer.SetInputEnabled(true);
            seatedPlayer = null;
        }

        RestoreCameraZoom();
        match = null;
    }

    private void EnsureTableVisual()
    {
        if (!createTableVisual || tableVisual != null || houseInterior == null)
        {
            return;
        }

        tableVisual = new GameObject(tableLabel);
        tableVisual.hideFlags = Application.isPlaying ? HideFlags.None : HideFlags.DontSaveInEditor;

        AddRect(tableVisual.transform, "Table Rail", Vector2.zero, tableVisualSize + new Vector2(0.35f, 0.25f), tableRailColor, -26);
        AddRect(tableVisual.transform, "Felt", Vector2.zero, tableVisualSize, tableColor, -25);
        AddRect(tableVisual.transform, "Dealer Placeholder", new Vector2(0f, 0.92f), new Vector2(0.55f, 0.55f), new Color32(202, 179, 126, 255), -23);
        AddRect(tableVisual.transform, "Player Chair", new Vector2(0f, -0.95f), new Vector2(0.95f, 0.42f), new Color32(112, 75, 50, 255), -24);
        AddRect(tableVisual.transform, "Bot Chair", new Vector2(0f, 0.95f), new Vector2(0.95f, 0.42f), new Color32(112, 75, 50, 255), -24);
        UpdateTableVisual();
    }

    private void UpdateTableVisual()
    {
        if (tableVisual == null || houseInterior == null)
        {
            return;
        }

        tableVisual.transform.position = houseInterior.GetInteriorWorldPoint(tableOffset);
        tableVisual.SetActive(houseInterior.IsInside || match != null);
    }

    private static void AddRect(Transform parent, string rectName, Vector2 localPosition, Vector2 size, Color color, int sortingOrder)
    {
        GameObject rect = new GameObject(rectName);
        rect.transform.SetParent(parent, false);
        rect.transform.localPosition = localPosition;
        SceneRect2D sceneRect = rect.AddComponent<SceneRect2D>();
        sceneRect.Size = size;
        sceneRect.Color = color;
        sceneRect.SortingOrder = sortingOrder;
    }

    private PlayerMovement2D ResolvePlayer()
    {
        if (cachedPlayer == null || !cachedPlayer.isActiveAndEnabled)
        {
            cachedPlayer = FindAnyObjectByType<PlayerMovement2D>(FindObjectsInactive.Exclude);
        }

        return cachedPlayer;
    }

    private bool IsPlayerInTableArea(PlayerMovement2D player)
    {
        if (player == null || houseInterior == null)
        {
            return false;
        }

        Vector2 center = houseInterior.GetInteriorWorldPoint(tableOffset);
        Bounds tableBounds = new Bounds(center, new Vector3(Mathf.Max(0.1f, tableTriggerSize.x), Mathf.Max(0.1f, tableTriggerSize.y), 10f));
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (playerCollider != null)
        {
            return tableBounds.Intersects(playerCollider.bounds);
        }

        return tableBounds.Contains(player.transform.position);
    }

    private void StoreAndApplyMatchCameraZoom()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            camera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        }

        if (camera == null)
        {
            return;
        }

        if (!hasStoredCameraZoom)
        {
            cameraZoomBeforeMatch = camera.orthographicSize;
            hasStoredCameraZoom = true;
        }

        camera.orthographicSize = Mathf.Max(1f, matchCameraZoom);
    }

    private void RestoreCameraZoom()
    {
        if (!hasStoredCameraZoom)
        {
            return;
        }

        Camera camera = Camera.main;

        if (camera == null)
        {
            camera = FindAnyObjectByType<Camera>(FindObjectsInactive.Exclude);
        }

        if (camera != null)
        {
            camera.orthographicSize = Mathf.Max(1f, cameraZoomBeforeMatch);
        }

        hasStoredCameraZoom = false;
    }

    private void DrawSitPrompt()
    {
        const float width = 460f;
        const float height = 86f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 28f, width, height);
        GUI.Box(rect, GUIContent.none, panelStyle);

        GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, rect.width - 40f, 28f), tableLabel, titleStyle);
        GUI.Label(new Rect(rect.x + 20f, rect.y + 46f, rect.width - 40f, 24f), $"Pulsa {sitKey} para sentarte en la mesa offline.", labelStyle);
    }

    private void DrawMatchUi()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);

        float panelWidth = Mathf.Min(1080f, Screen.width - 44f);
        float panelHeight = Mathf.Min(730f, Screen.height - 44f);
        Rect panel = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.Box(panel, GUIContent.none, panelStyle);

        DrawHeader(panel);

        Rect tableRect = new Rect(panel.x + 28f, panel.y + 82f, panel.width - 56f, panel.height - 168f);
        DrawPokerTable(tableRect);

        Rect actionRect = new Rect(panel.x + 28f, panel.yMax - 70f, panel.width - 56f, 48f);

        if (match.IsComplete)
        {
            DrawResultActions(actionRect);
        }
        else
        {
            DrawPlayerActions(actionRect);
        }
    }

    private void DrawHeader(Rect panel)
    {
        Rect titleRect = new Rect(panel.x + 28f, panel.y + 20f, panel.width * 0.48f, 34f);
        GUI.Label(titleRect, "Poker House - Mesa 1", titleStyle);

        Rect statusRect = new Rect(panel.x + 28f, panel.y + 51f, panel.width * 0.55f, 24f);
        GUI.Label(statusRect, match.StatusText, labelStyle);

        float statWidth = 118f;
        float startX = panel.xMax - 28f - statWidth * 3f - 18f;
        DrawStat(new Rect(startX, panel.y + 20f, statWidth, 50f), "Ronda", RoundLabel(match.Round));
        DrawStat(new Rect(startX + statWidth + 9f, panel.y + 20f, statWidth, 50f), "Pot", match.Pot.ToString());
        DrawStat(new Rect(startX + (statWidth + 9f) * 2f, panel.y + 20f, statWidth, 50f), "Call", match.PlayerToCall.ToString());
    }

    private void DrawPokerTable(Rect tableRect)
    {
        GUI.DrawTexture(tableRect, railTexture);
        Rect feltRect = new Rect(tableRect.x + 12f, tableRect.y + 12f, tableRect.width - 24f, tableRect.height - 24f);
        GUI.DrawTexture(feltRect, feltTexture);

        bool compact = feltRect.width < 780f;
        float logWidth = compact ? 0f : 260f;
        Rect playRect = compact
            ? new Rect(feltRect.x + 20f, feltRect.y + 20f, feltRect.width - 40f, feltRect.height - 40f)
            : new Rect(feltRect.x + 24f, feltRect.y + 24f, feltRect.width - logWidth - 52f, feltRect.height - 48f);

        Rect logRect = compact
            ? new Rect(feltRect.x + 24f, feltRect.yMax - 118f, feltRect.width - 48f, 96f)
            : new Rect(playRect.xMax + 22f, feltRect.y + 24f, logWidth, feltRect.height - 48f);

        DrawSeat(new Rect(playRect.x + 6f, playRect.y + 4f, 180f, 54f), "Bot", match.BotChips, false);
        DrawCardRow(new Rect(playRect.x + 210f, playRect.y + 4f, playRect.width - 220f, 104f), match.BotHoleCards, 2, !match.IsComplete);

        Rect potRect = new Rect(playRect.x + playRect.width * 0.5f - 76f, playRect.y + playRect.height * 0.5f - 76f, 152f, 58f);
        DrawPot(potRect);

        Rect communityRect = new Rect(playRect.x + playRect.width * 0.5f - 212f, playRect.y + playRect.height * 0.5f - 8f, 424f, 104f);
        DrawBoardLabel(new Rect(communityRect.x, communityRect.y - 26f, communityRect.width, 20f), "COMUNIDAD");
        DrawCardRow(communityRect, match.CommunityCards, 5, false);

        DrawSeat(new Rect(playRect.x + 6f, playRect.yMax - 58f, 180f, 54f), "Tu", match.PlayerChips, true);
        DrawCardRow(new Rect(playRect.x + 210f, playRect.yMax - 108f, playRect.width - 220f, 104f), match.PlayerHoleCards, 2, false);

        if (match.IsComplete)
        {
            DrawResultPanel(new Rect(playRect.x + 18f, playRect.y + playRect.height * 0.5f + 108f, playRect.width - 36f, 70f));
        }

        DrawLogPanel(logRect);
    }

    private void DrawStat(Rect rect, string label, string value)
    {
        GUI.DrawTexture(rect, statTexture);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, 16f), label, smallLabelStyle);
        GUI.Label(new Rect(rect.x + 8f, rect.y + 24f, rect.width - 16f, 22f), value, chipStyle);
    }

    private void DrawSeat(Rect rect, string label, int chips, bool isPlayer)
    {
        GUI.DrawTexture(rect, seatTexture);
        string seatLabel = isPlayer ? "Jugador" : "Bot";
        GUI.Label(new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 20f), seatLabel, smallLabelStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 27f, rect.width - 24f, 22f), $"{label}  {chips} fichas", labelStyle);
    }

    private void DrawPot(Rect rect)
    {
        GUI.DrawTexture(rect, statTexture);
        DrawCenteredLabel(new Rect(rect.x, rect.y + 8f, rect.width, 18f), "POT", centerLabelStyle);
        DrawCenteredLabel(new Rect(rect.x, rect.y + 27f, rect.width, 24f), match.Pot.ToString(), chipStyle);
    }

    private void DrawBoardLabel(Rect rect, string text)
    {
        DrawCenteredLabel(rect, text, centerLabelStyle);
    }

    private void DrawCardRow(Rect rect, IReadOnlyList<PokerCard> cards, int slotCount, bool hidden)
    {
        float cardWidth = Mathf.Min(74f, (rect.width - Mathf.Max(0, slotCount - 1) * 10f) / Mathf.Max(1, slotCount));
        float cardHeight = cardWidth * 1.38f;
        float totalWidth = cardWidth * slotCount + 10f * Mathf.Max(0, slotCount - 1);
        float startX = rect.x + Mathf.Max(0f, (rect.width - totalWidth) * 0.5f);
        float startY = rect.y + Mathf.Max(0f, (rect.height - cardHeight) * 0.5f);

        for (int i = 0; i < slotCount; i++)
        {
            Rect cardRect = new Rect(startX + i * (cardWidth + 10f), startY, cardWidth, cardHeight);

            if (cards != null && i < cards.Count)
            {
                DrawCard(cardRect, cards[i], hidden);
            }
            else
            {
                DrawEmptyCard(cardRect);
            }
        }
    }

    private void DrawCard(Rect rect, PokerCard card, bool hidden)
    {
        GUI.DrawTexture(rect, cardBorderTexture);
        Rect inner = Inset(rect, 3f);

        if (hidden)
        {
            GUI.DrawTexture(inner, cardBackTexture);
            DrawCardBackPattern(inner);
            DrawCenteredLabel(new Rect(inner.x + 4f, inner.y + inner.height * 0.5f - 10f, inner.width - 8f, 20f), "COZY", centerLabelStyle);
            return;
        }

        GUI.DrawTexture(inner, cardFaceTexture);
        GUIStyle textStyle = IsRed(card.Suit) ? redCardTextStyle : darkCardTextStyle;
        string rank = RankLabel(card.Rank);
        string suit = SuitLabel(card.Suit);

        GUI.Label(new Rect(inner.x + 6f, inner.y + 4f, inner.width - 12f, 22f), rank, textStyle);
        DrawCenteredLabel(new Rect(inner.x, inner.y + inner.height * 0.5f - 16f, inner.width, 32f), suit, textStyle);
        GUI.Label(new Rect(inner.x + 6f, inner.yMax - 25f, inner.width - 12f, 22f), rank, textStyle);
    }

    private void DrawEmptyCard(Rect rect)
    {
        GUI.DrawTexture(rect, emptyCardTexture);
        Rect inner = Inset(rect, 3f);
        DrawCenteredLabel(inner, "-", centerLabelStyle);
    }

    private void DrawCardBackPattern(Rect rect)
    {
        float stripeHeight = Mathf.Max(2f, rect.height * 0.08f);
        Rect topStripe = new Rect(rect.x + 8f, rect.y + 10f, rect.width - 16f, stripeHeight);
        Rect bottomStripe = new Rect(rect.x + 8f, rect.yMax - 10f - stripeHeight, rect.width - 16f, stripeHeight);
        GUI.DrawTexture(topStripe, railTexture);
        GUI.DrawTexture(bottomStripe, railTexture);
    }

    private void DrawResultPanel(Rect rect)
    {
        GUI.DrawTexture(rect, statTexture);
        DrawCenteredLabel(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 22f), ResultTitle(), resultStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 34f, rect.width - 28f, 22f), $"Tu mano: {match.PlayerBestHand.DisplayName}    Bot: {match.BotBestHand.DisplayName}", labelStyle);
    }

    private void DrawLogPanel(Rect rect)
    {
        GUI.DrawTexture(rect, logTexture);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 22f), "Historial", labelStyle);

        IReadOnlyList<string> entries = match.LogEntries;
        float lineY = rect.y + 42f;

        for (int i = 0; i < entries.Count; i++)
        {
            GUI.Label(new Rect(rect.x + 14f, lineY, rect.width - 28f, 32f), entries[i], logStyle);
            lineY += 34f;

            if (lineY > rect.yMax - 28f)
            {
                break;
            }
        }
    }

    private void DrawPlayerActions(Rect rect)
    {
        string callLabel = match.PlayerToCall > 0 ? $"Igualar {match.PlayerToCall}" : "Pasar";
        string betLabel = match.PlayerToCall > 0 ? $"Subir {match.BetSize}" : $"Apostar {match.BetSize}";
        float spacing = 12f;
        float buttonWidth = (rect.width - spacing * 2f) / 3f;

        if (GUI.Button(new Rect(rect.x, rect.y, buttonWidth, rect.height), callLabel, actionButtonStyle))
        {
            match.PlayerCheckOrCall();
        }

        if (GUI.Button(new Rect(rect.x + buttonWidth + spacing, rect.y, buttonWidth, rect.height), betLabel, actionButtonStyle))
        {
            match.PlayerBetOrRaise();
        }

        if (GUI.Button(new Rect(rect.x + (buttonWidth + spacing) * 2f, rect.y, buttonWidth, rect.height), "Retirarse", actionButtonStyle))
        {
            match.PlayerFold();
        }
    }

    private void DrawResultActions(Rect rect)
    {
        float spacing = 12f;
        float buttonWidth = (rect.width - spacing) * 0.5f;

        if (GUI.Button(new Rect(rect.x, rect.y, buttonWidth, rect.height), "Nueva mano", actionButtonStyle))
        {
            match.StartNewHand();
        }

        if (GUI.Button(new Rect(rect.x + buttonWidth + spacing, rect.y, buttonWidth, rect.height), "Volver al interior", actionButtonStyle))
        {
            ReturnToExploration();
        }
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        overlayTexture = MakeTexture(new Color32(4, 7, 8, 165));
        panelTexture = MakeTexture(new Color32(26, 31, 30, 248));
        railTexture = MakeTexture(new Color32(90, 60, 40, 255));
        feltTexture = MakeTexture(new Color32(21, 93, 62, 255));
        statTexture = MakeTexture(new Color32(38, 47, 44, 245));
        seatTexture = MakeTexture(new Color32(45, 56, 52, 235));
        logTexture = MakeTexture(new Color32(19, 25, 24, 222));
        cardBorderTexture = MakeTexture(new Color32(29, 33, 34, 255));
        cardFaceTexture = MakeTexture(new Color32(245, 241, 230, 255));
        cardBackTexture = MakeTexture(new Color32(47, 86, 132, 255));
        emptyCardTexture = MakeTexture(new Color32(78, 107, 91, 130));

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(16, 16, 16, 16)
        };
        panelStyle.normal.background = panelTexture;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22
        };
        titleStyle.normal.textColor = new Color32(239, 228, 201, 255);

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            wordWrap = true
        };
        labelStyle.normal.textColor = new Color32(232, 228, 214, 255);

        smallLabelStyle = new GUIStyle(labelStyle)
        {
            fontSize = 12
        };
        smallLabelStyle.normal.textColor = new Color32(170, 183, 173, 255);

        hintStyle = new GUIStyle(labelStyle)
        {
            fontSize = 13
        };
        hintStyle.normal.textColor = new Color32(210, 216, 204, 255);

        centerLabelStyle = new GUIStyle(labelStyle)
        {
            fontSize = 14
        };
        centerLabelStyle.normal.textColor = new Color32(223, 229, 216, 255);

        chipStyle = new GUIStyle(labelStyle)
        {
            fontSize = 22
        };
        chipStyle.normal.textColor = new Color32(245, 214, 126, 255);

        resultStyle = new GUIStyle(labelStyle)
        {
            fontSize = 20
        };
        resultStyle.normal.textColor = new Color32(245, 214, 126, 255);

        logStyle = new GUIStyle(labelStyle)
        {
            fontSize = 12,
            wordWrap = true
        };
        logStyle.normal.textColor = new Color32(199, 207, 196, 255);

        redCardTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20
        };
        redCardTextStyle.normal.textColor = new Color32(176, 39, 42, 255);

        darkCardTextStyle = new GUIStyle(redCardTextStyle);
        darkCardTextStyle.normal.textColor = new Color32(31, 35, 36, 255);

        actionButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15
        };
    }

    private string ResultTitle()
    {
        switch (match.Result)
        {
            case PokerResult.PlayerWin:
                return "Has ganado la mano";
            case PokerResult.BotWin:
                return "El bot gana la mano";
            case PokerResult.Tie:
                return "Bote repartido";
            default:
                return "Resultados";
        }
    }

    private static string RoundLabel(PokerRound round)
    {
        switch (round)
        {
            case PokerRound.Preflop:
                return "Preflop";
            case PokerRound.Flop:
                return "Flop";
            case PokerRound.Turn:
                return "Turn";
            case PokerRound.River:
                return "River";
            case PokerRound.Finished:
                return "Resultados";
            default:
                return "Poker";
        }
    }

    private static string RankLabel(PokerRank rank)
    {
        switch (rank)
        {
            case PokerRank.Ace:
                return "A";
            case PokerRank.King:
                return "K";
            case PokerRank.Queen:
                return "Q";
            case PokerRank.Jack:
                return "J";
            case PokerRank.Ten:
                return "10";
            default:
                return ((int)rank).ToString();
        }
    }

    private static string SuitLabel(PokerSuit suit)
    {
        switch (suit)
        {
            case PokerSuit.Hearts:
                return "H";
            case PokerSuit.Diamonds:
                return "D";
            case PokerSuit.Clubs:
                return "C";
            case PokerSuit.Spades:
                return "S";
            default:
                return "?";
        }
    }

    private static bool IsRed(PokerSuit suit)
    {
        return suit == PokerSuit.Hearts || suit == PokerSuit.Diamonds;
    }

    private static void DrawCenteredLabel(Rect rect, string text, GUIStyle style)
    {
        int fontSize = Mathf.Max(10, style.fontSize);
        float width = Mathf.Min(rect.width, Mathf.Max(18f, text.Length * fontSize * 0.62f));
        float height = Mathf.Min(rect.height, fontSize + 8f);
        Rect labelRect = new Rect(rect.x + (rect.width - width) * 0.5f, rect.y + (rect.height - height) * 0.5f, width, height);
        GUI.Label(labelRect, text, style);
    }

    private static Rect Inset(Rect rect, float inset)
    {
        return new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
