using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        if (TurnManager.Instance == null || txtDiceResult == null) return;
        int d = TurnManager.Instance.LastDiceTotal;
        if (d > 0) txtDiceResult.text = $"Dados: {d}";
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

    // ── Botones de Control Generados Dinámicamente ──
    void CreateControlPanel()
    {
        // El Panel de Control se crea directamente sobre el Canvas para evitar problemas de layout
        GameObject panelObj = new GameObject("ControlPanel_Generated");
        panelObj.transform.SetParent(this.transform.parent, false); // Parent to GameCanvas
        
        RectTransform rt = panelObj.AddComponent<RectTransform>();
        // Anclado a la izquierda, debajo del Panel_TurnInfo (que suele estar arriba a la izquierda)
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

        // Botón Tirar Dado
        CreateButton(panelObj.transform, "Tirar Dado", () => {
            if (TurnManager.Instance != null) TurnManager.Instance.RollDice();
        }, new Color(0.2f, 0.7f, 0.2f, 0.95f));

        for (int i = 0; i < 4; i++)
        {
            int pieceIndex = i;
            CreateButton(panelObj.transform, $"Mover Ficha {i+1}", () => {
                if (TurnManager.Instance != null) TurnManager.Instance.SelectPiece(pieceIndex);
            }, new Color(0.1f, 0.4f, 0.8f, 0.95f));
        }

        // Botones de Minas
        CreateButton(panelObj.transform, "Usar Mina (Resta 1)", () => {
            if (TurnManager.Instance != null) TurnManager.Instance.SelectMines(1);
        }, new Color(0.8f, 0.2f, 0.2f, 0.95f));

        CreateButton(panelObj.transform, "No usar Mina (Pasar)", () => {
            if (TurnManager.Instance != null) TurnManager.Instance.SelectMines(0);
        }, new Color(0.4f, 0.4f, 0.4f, 0.95f));
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

    void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action, Color? color = null)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);

        Image bg = btnObj.AddComponent<Image>();
        bg.color = color ?? new Color(0.1f, 0.4f, 0.8f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.minWidth = 120;
        le.minHeight = 60;

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 24;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
    }
}
