using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GameManager — Singleton central del juego.
/// Maneja: jugadores activos, puntos, condicion de victoria y estado global.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== VICTORIA ===")]
    [Tooltip("Puntos necesarios para ganar la partida")]
    public int pointsToWin = 12;

    [Header("=== PUNTOS POR EVENTO ===")]
    public int pointsPerKill = 1;
    public int pointsPerLap  = 3;

    private List<PlayerData> _players = new List<PlayerData>();

    public bool       GameOver { get; private set; } = false;
    public PlayerData Winner   { get; private set; } = null;

    public event Action<PlayerData> OnPointsChanged;
    public event Action<PlayerData> OnGameOver;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void RegisterPlayers(List<PlayerData> players)
    {
        _players = players;
        GameOver  = false;
        Winner    = null;
    }

    public List<PlayerData> GetPlayers() => _players;

    public void AddPoints(PlayerData player, int amount)
    {
        if (GameOver) return;
        player.Score += amount;
        Debug.Log($"[GameManager] {player.Name}: {player.Score} puntos.");
        OnPointsChanged?.Invoke(player);
        CheckWinCondition(player);
    }

    public void OnPieceKilled(PlayerData attacker) => AddPoints(attacker, pointsPerKill);
    public void OnLapCompleted(PlayerData player)  => AddPoints(player, pointsPerLap);

    private void CheckWinCondition(PlayerData player)
    {
        if (player.Score >= pointsToWin)
        {
            GameOver = true;
            Winner   = player;
            Debug.Log($"[GameManager] {player.Name} GANO con {player.Score} puntos!");
            OnGameOver?.Invoke(player);
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public string            Name;
    public PlayerColor       Color;
    public int               Score;
    public PieceController[] Pieces;
    public int               InitialRoll = 0;

    public PlayerData(string name, PlayerColor color, PieceController[] pieces)
    {
        Name   = name;
        Color  = color;
        Score  = 0;
        Pieces = pieces;
    }
}
