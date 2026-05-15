using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// GameUI — HUD durante la partida.
/// Se suscribe a eventos de TurnManager y GameManager.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("=== PANEL SUPERIOR IZQUIERDO ===")]
    public TextMeshProUGUI txtCurrentTurn;   // quién juega ahora
    public TextMeshProUGUI txtDiceResult;    // resultado del dado

    [Header("=== MENSAJE DE ESTADO (centro arriba) ===")]
    public TextMeshProUGUI txtStatus;

    [Header("=== MARCADORES (hasta 4) ===")]
    public TextMeshProUGUI[] txtScores = new TextMeshProUGUI[4];

    [Header("=== PANEL DE VICTORIA ===")]
    public GameObject          panelVictory;
    public TextMeshProUGUI     txtWinner;
    public Button              btnReturnMenu;
    public string              menuSceneName = "MainMenu";

    private List<PlayerData> _players;

    void Start()
    {
        if (panelVictory != null) panelVictory.SetActive(false);

        if (btnReturnMenu != null)
            btnReturnMenu.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName));

        // Aumentar el tamaño del texto existente dinámicamente
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach(var t in allTexts)
        {
            t.fontSize += 10;
        }

        CreateControlPanel();
        CreateMatchInfoPanel();

        // Suscribir eventos — nombres del NUEVO TurnManager
        TurnManager.Instance.OnStatus       += OnStatus;
        TurnManager.Instance.OnTurnStart    += OnTurnStart;
        TurnManager.Instance.OnOrderReady   += OnOrderReady;

        GameManager.Instance.OnPointsChanged += OnPointsChanged;
        GameManager.Instance.OnGameOver      += OnGameOver;

        _players = GameManager.Instance.GetPlayers();
        RefreshScores();
    }

    void Update()
    {
        // Actualizar resultado del dado en tiempo real
        if (TurnManager.Instance == null) return;

        if (txtDiceResult != null)
        {
            int d = TurnManager.Instance.LastDiceTotal;
            if (d > 0) txtDiceResult.text = $"Dados: {d}";
        }

        // Actualizar estado de botones cada frame
        RefreshButtonStates();
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnStatus    -= OnStatus;
            TurnManager.Instance.OnTurnStart -= OnTurnStart;
            TurnManager.Instance.OnOrderReady -= OnOrderReady;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= OnPointsChanged;
            GameManager.Instance.OnGameOver      -= OnGameOver;
        }
    }

    // ── Callbacks ──────────────────────────────────

    void OnStatus(string msg)
    {
        if (txtStatus != null) txtStatus.text = msg;

        // Añadir al historial
        string timeStr = System.DateTime.Now.ToString("HH:mm:ss");
        matchHistory.Add($"<color=#aaaaaa>[{timeStr}]</color> {msg}");
        if (matchHistory.Count > 10) matchHistory.RemoveAt(0); // keep last 10 entries
        
        if (txtMatchInfo != null)
        {
            txtMatchInfo.text = "<b>HISTORIAL</b>\n<size=80%>" + string.Join("\n", matchHistory) + "</size>";
        }
    }

    void OnTurnStart(PlayerData p)
    {
        if (txtCurrentTurn != null) txtCurrentTurn.text = $"Turno de:\n{p.Name}";
        if (txtDiceResult  != null) txtDiceResult.text  = "Dados: --";
        _players = GameManager.Instance.GetPlayers();
        RefreshScores();
    }

    void OnOrderReady(List<PlayerData> ordered)
    {
        _players = ordered;
        RefreshScores();
    }

    void OnPointsChanged(PlayerData p)
    {
        RefreshScores();
    }

    void OnGameOver(PlayerData winner)
    {
        if (panelVictory != null) panelVictory.SetActive(true);
        if (txtWinner    != null)
            txtWinner.text = $"GANÓ:\n{winner.Name}\n{winner.Score} puntos";
    }

    void RefreshScores()
    {
        if (_players == null) return;
        for (int i = 0; i < txtScores.Length; i++)
        {
            if (txtScores[i] == null) continue;
            if (i < _players.Count)
                txtScores[i].text = $"{_players[i].Name}\n{_players[i].Score} pts";
            else
                txtScores[i].text = "";
        }
    }

    // ── Referencias a botones para activar/desactivar ──
    private Button _btnRoll;
    private Button[] _btnPieces = new Button[4];
    private Button _btnUseMine;
    private Button _btnSkipMine;
    private TextMeshProUGUI _txtMineCount;

    // ── Botones de Control Generados Dinámicamente ──
    void CreateControlPanel()
    {
        GameObject panelObj = new GameObject("ControlPanel_Generated");
        panelObj.transform.SetParent(this.transform.parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.2f);
        rt.anchorMax = new Vector2(0.25f, 0.75f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(15, -10);
        rt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 12;
        layout.childControlHeight = false;
        layout.childControlWidth = false;

        // ── Tirar Dado ──
        _btnRoll = CreateButton(panelObj.transform, "🎲 Tirar Dado", () => {
            if (TurnManager.Instance != null) TurnManager.Instance.RollDice();
        }, new Color(0.2f, 0.7f, 0.2f, 0.95f));

        // ── Mover Fichas ──
        for (int i = 0; i < 4; i++)
        {
            int pieceIndex = i;
            _btnPieces[i] = CreateButton(panelObj.transform, $"♟ Ficha {i+1}", () => {
                if (TurnManager.Instance != null) TurnManager.Instance.SelectPiece(pieceIndex);
            }, new Color(0.1f, 0.4f, 0.8f, 0.95f));
        }

        // ── Contador de minas ──
        GameObject lblObj = new GameObject("Lbl_MineCount");
        lblObj.transform.SetParent(panelObj.transform, false);
        _txtMineCount = lblObj.AddComponent<TextMeshProUGUI>();
        _txtMineCount.text = "💣 Minas: -";
        _txtMineCount.fontSize = 22;
        _txtMineCount.color = new Color(1f, 0.8f, 0.2f);
        LayoutElement le = lblObj.AddComponent<LayoutElement>();
        le.minHeight = 30;

        // ── Botones de Minas ──
        _btnUseMine = CreateButton(panelObj.transform, "💣 Usar Mina (-1 casilla)", () => {
            if (TurnManager.Instance != null) TurnManager.Instance.SelectMines(1);
        }, new Color(0.75f, 0.15f, 0.1f, 0.95f));

        _btnSkipMine = CreateButton(panelObj.transform, "⏭ No usar Mina", () => {
            if (TurnManager.Instance != null) TurnManager.Instance.SelectMines(0);
        }, new Color(0.35f, 0.35f, 0.35f, 0.95f));

        // Estado inicial: todo desactivado excepto el dado
        RefreshButtonStates();
    }

    /// <summary>Activa/desactiva botones según la fase del turno.</summary>
    void RefreshButtonStates()
    {
        if (TurnManager.Instance == null) return;
        TurnManager.Phase phase = TurnManager.Instance.CurrentPhase;

        bool waitRoll  = phase == TurnManager.Phase.TurnWaitRoll || phase == TurnManager.Phase.InitialWait;
        bool waitMine  = phase == TurnManager.Phase.TurnWaitMines;
        bool waitPiece = phase == TurnManager.Phase.TurnWaitPiece;

        SetBtn(_btnRoll,     waitRoll);
        SetBtn(_btnUseMine,  waitMine);
        SetBtn(_btnSkipMine, waitMine);
        foreach (var b in _btnPieces) SetBtn(b, waitPiece);

        // Actualizar contador de minas
        if (_txtMineCount != null && MineSystem.Instance != null)
        {
            var cur = TurnManager.Instance.CurrentPlayer;
            if (cur != null)
            {
                int left = MineSystem.Instance.GetMinesRemaining(cur.Color);
                _txtMineCount.text = $"💣 Minas: {left}";
                _txtMineCount.color = left > 0 ? new Color(1f, 0.8f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }

    private void SetBtn(Button btn, bool interactable)
    {
        if (btn == null) return;
        btn.interactable = interactable;
        // Cambiar alpha visualmente para indicar desactivado
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, interactable ? 0.95f : 0.35f);
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.color = interactable ? Color.white : new Color(1f,1f,1f,0.4f);
    }

    private TextMeshProUGUI txtMatchInfo;
    private List<string> matchHistory = new List<string>();

    void CreateMatchInfoPanel()
    {
        GameObject panelObj = new GameObject("MatchInfoPanel_Generated");
        panelObj.transform.SetParent(this.transform.parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        // Anclado a la derecha, debajo del Panel_Scores
        rt.anchorMin = new Vector2(0.79f, 0.1f);
        rt.anchorMax = new Vector2(1f, 0.6f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(-16, 0);

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        GameObject txtObj = new GameObject("Text_MatchInfo");
        txtObj.transform.SetParent(panelObj.transform, false);
        
        txtMatchInfo = txtObj.AddComponent<TextMeshProUGUI>();
        txtMatchInfo.text = "<b>HISTORIAL</b>\n";
        txtMatchInfo.fontSize = 20; // Will be increased by 10 in Start() since we call it before font increase? Wait, we need to make sure fontSize is good.
        // Actually, if we create it after the foreach font increase, we just set the final size here.
        txtMatchInfo.fontSize = 24; 
        txtMatchInfo.alignment = TextAlignmentOptions.BottomLeft;
        txtMatchInfo.color = Color.white;
        txtMatchInfo.enableWordWrapping = true;
        
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(15, 15);
        txtRt.offsetMax = new Vector2(-15, -15);
    }

    Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Color? color = null)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);

        Color baseColor = color ?? new Color(0.1f, 0.4f, 0.8f, 0.95f);
        Image bg = btnObj.AddComponent<Image>();
        bg.color = baseColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        // Colores de estado del botón
        ColorBlock cb = btn.colors;
        cb.normalColor      = baseColor;
        cb.highlightedColor = new Color(Mathf.Min(baseColor.r + 0.15f, 1f), Mathf.Min(baseColor.g + 0.15f, 1f), Mathf.Min(baseColor.b + 0.15f, 1f), baseColor.a);
        cb.pressedColor     = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f, baseColor.a);
        cb.disabledColor    = new Color(baseColor.r, baseColor.g, baseColor.b, 0.35f);
        btn.colors = cb;

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth  = 120;
        le.minHeight = 55;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 21;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;

        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4, 0);
        txtRt.offsetMax = new Vector2(-4, 0);

        return btn;
    }
}
