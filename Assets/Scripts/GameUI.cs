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
}
