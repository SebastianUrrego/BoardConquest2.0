using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameUI - Maneja toda la interfaz durante la partida.
/// Muestra: turno actual, dados, puntos de cada jugador, mensajes y pantalla de victoria.
/// </summary>
public class GameUI : MonoBehaviour
{
    [Header("=== TURNO ACTUAL ===")]
    public TextMeshProUGUI txtCurrentTurn;

    [Header("=== DADOS ===")]
    public TextMeshProUGUI txtDiceResult;

    [Header("=== MENSAJES DE ESTADO ===")]
    public TextMeshProUGUI txtStatus;

    [Header("=== MARCADORES (uno por jugador en orden) ===")]
    public TextMeshProUGUI[] txtScores = new TextMeshProUGUI[4];

    [Header("=== BOTON TIRAR DADOS ===")]
    public Button btnRoll;

    [Header("=== PANEL DE VICTORIA ===")]
    public GameObject panelVictory;
    public TextMeshProUGUI txtWinner;
    public Button btnReturnMenu;

    [Header("=== ESCENA MENU ===")]
    public string menuSceneName = "MainMenu";

    private List<PlayerData> _players;

    private void Start()
    {
        if (panelVictory != null) panelVictory.SetActive(false);

        if (btnRoll != null)
            btnRoll.onClick.AddListener(OnRollButtonPressed);

        if (btnReturnMenu != null)
            btnReturnMenu.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName));

        TurnManager.Instance.OnTurnStart     += OnTurnStart;
        TurnManager.Instance.OnStatusMessage += OnStatusMessage;
        GameManager.Instance.OnPointsChanged  += OnPointsChanged;
        GameManager.Instance.OnGameOver       += OnGameOverUI;

        _players = GameManager.Instance.GetPlayers();
        UpdateAllScores();
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart     -= OnTurnStart;
            TurnManager.Instance.OnStatusMessage -= OnStatusMessage;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPointsChanged -= OnPointsChanged;
            GameManager.Instance.OnGameOver      -= OnGameOverUI;
        }
    }

    private void OnTurnStart(PlayerData player)
    {
        if (txtCurrentTurn != null)
            txtCurrentTurn.text = $"Turno de:\n{player.Name}";
        if (txtDiceResult != null)
            txtDiceResult.text = "Dados: --";
        if (btnRoll != null)
            btnRoll.interactable = true;
    }

    private void OnStatusMessage(string msg)
    {
        if (txtStatus != null) txtStatus.text = msg;
    }

    private void OnPointsChanged(PlayerData player)
    {
        UpdateAllScores();
    }

    private void OnGameOverUI(PlayerData winner)
    {
        if (panelVictory != null) panelVictory.SetActive(true);
        if (txtWinner   != null) txtWinner.text = $"GANO: {winner.Name}\nPuntos: {winner.Score}";
        if (btnRoll     != null) btnRoll.interactable = false;
    }

    private void OnRollButtonPressed()
    {
        if (btnRoll != null) btnRoll.interactable = false;
        TurnManager.Instance.RollDice();
        StartCoroutine(WaitForDiceResult());
    }

    private IEnumerator WaitForDiceResult()
    {
        yield return new WaitUntil(() =>
            TurnManager.Instance.Phase == TurnManager.TurnPhase.WaitingForPieceSelection ||
            TurnManager.Instance.Phase == TurnManager.TurnPhase.GameOver);
        if (txtDiceResult != null)
            txtDiceResult.text = $"Dados: {TurnManager.Instance.LastDiceResult}";
    }

    private void UpdateAllScores()
    {
        if (_players == null) return;
        for (int i = 0; i < _players.Count && i < txtScores.Length; i++)
        {
            if (txtScores[i] != null)
                txtScores[i].text = $"{_players[i].Name}\n{_players[i].Score} pts";
        }
    }
}
