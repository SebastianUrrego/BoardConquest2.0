using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MineSystem — Gestiona el sistema de minas del tablero.
///
/// REGLAS:
///   - Cada jugador empieza con 4 minas.
///   - Al usar N minas en un turno, el jugador avanza (dado - N) casillas.
///   - Las minas se colocan en las casillas que el jugador 'sacrificó'
///     (desde posición_final+1 hasta posición_completa_del_dado).
///   - No puedes usar más minas que (dado - 1).
///   - Si una ficha enemiga cae en una mina: -2 puntos y vuelve a casa.
///   - No puedes activar tus propias minas.
/// </summary>
public class MineSystem : MonoBehaviour
{
    public static MineSystem Instance { get; private set; }

    private const int MinesPerPlayer = 4;

    // Track index normalizado -> color del jugador que la puso
    private Dictionary<int, PlayerColor> _activeMines = new Dictionary<int, PlayerColor>();

    // Minas restantes por color de jugador
    private Dictionary<PlayerColor, int> _minesRemaining = new Dictionary<PlayerColor, int>();

    // Evento: (ficha golpeada, dueño de la mina)
    public event Action<PieceController, PlayerColor> OnMineTriggered;

    // Puntos que se transfieren al activarse una mina
    public int pointsToSteal = 2;

    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        InitMines();
    }

    // ─────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────
    private void InitMines()
    {
        _activeMines.Clear();
        _minesRemaining.Clear();
        foreach (PlayerColor color in Enum.GetValues(typeof(PlayerColor)))
            _minesRemaining[color] = MinesPerPlayer;
    }

    public void ResetAllMines()
    {
        InitMines();
    }

    // ─────────────────────────────────────────────
    // CONSULTAS PÚBLICAS
    // ─────────────────────────────────────────────

    public int GetMinesRemaining(PlayerColor color)
        => _minesRemaining.TryGetValue(color, out int v) ? v : 0;

    public int MaxMinesToUse(PlayerColor color, int diceRoll)
    {
        int available  = GetMinesRemaining(color);
        int maxByDice  = Mathf.Max(0, diceRoll - 1);
        return Mathf.Min(available, maxByDice);
    }

    public bool CanUseMines(PlayerColor color, int diceRoll, int minesToUse)
    {
        if (minesToUse < 0) return false;
        if (minesToUse == 0) return true;
        return minesToUse <= MaxMinesToUse(color, diceRoll);
    }

    public bool HasMineAt(int trackIndex)
        => _activeMines.ContainsKey(Wrap(trackIndex));

    public bool TryGetMineOwner(int trackIndex, out PlayerColor owner)
        => _activeMines.TryGetValue(Wrap(trackIndex), out owner);

    public Dictionary<int, PlayerColor> GetActiveMines()
        => new Dictionary<int, PlayerColor>(_activeMines);

    // ─────────────────────────────────────────────
    // COLOCAR MINAS
    // ─────────────────────────────────────────────

    public void PlaceMines(PlayerColor owner, int startTrackIndex, int fullRoll, int minesToUse)
    {
        if (minesToUse <= 0) return;
        if (!CanUseMines(owner, fullRoll, minesToUse))
        {
            Debug.LogWarning($"[MineSystem] {owner} intento usar {minesToUse} minas con dado {fullRoll} pero no puede.");
            return;
        }

        int actualMove = fullRoll - minesToUse;

        for (int i = 1; i <= minesToUse; i++)
        {
            int minePos = Wrap(startTrackIndex + actualMove + i);
            if (!_activeMines.ContainsKey(minePos))
            {
                _activeMines[minePos] = owner;
                Debug.Log($"[MineSystem] Mina de {owner} en casilla {minePos}.");
            }
        }

        _minesRemaining[owner] -= minesToUse;
        Debug.Log($"[MineSystem] {owner} tiene {_minesRemaining[owner]} minas restantes.");
    }

    // ─────────────────────────────────────────────
    // CHEQUEAR MINA AL ATERRIZAR
    // ─────────────────────────────────────────────

    /// <summary>
    /// Comprueba si la casilla donde cayó la ficha tiene una mina enemiga.
    /// Si hay mina: quita puntos al que la pisó, da puntos al dueño de la mina
    /// y envía la ficha al inicio (casa).
    /// Devuelve true si se activó una mina.
    /// </summary>
    public bool CheckAndTriggerMine(PieceController piece, PlayerData pieceOwner)
    {
        int wrapped = Wrap(piece.TrackIndex);
        if (!_activeMines.TryGetValue(wrapped, out PlayerColor mineOwner)) return false;
        if (mineOwner == pieceOwner.Color) return false; // no activas las tuyas

        // Eliminar la mina del tablero
        _activeMines.Remove(wrapped);
        Debug.Log($"[MineSystem] {pieceOwner.Name} pisó mina de {mineOwner} en casilla {wrapped}. -{pointsToSteal} pts.");

        // ── Quitar puntos a quien pisó la mina ──
        GameManager.Instance.RemovePoints(pieceOwner, pointsToSteal);

        // ── Dar puntos al dueño de la mina ──
        var allPlayers = GameManager.Instance.GetPlayers();
        PlayerData mineOwnerData = allPlayers?.Find(p => p.Color == mineOwner);
        if (mineOwnerData != null)
        {
            GameManager.Instance.AddPoints(mineOwnerData, pointsToSteal);
            Debug.Log($"[MineSystem] {mineOwnerData.Name} (dueño de la mina) recibe +{pointsToSteal} pts.");
        }

        // ── Enviar ficha a casa (visual + estado interno) ──
        var homeSquares = BoardManager.Instance.GetHomeSquares(pieceOwner.Color);
        if (homeSquares != null && piece.pieceIndex < homeSquares.Length && homeSquares[piece.pieceIndex] != null)
            piece.transform.position = homeSquares[piece.pieceIndex].position;
        piece.ResetToHome();

        OnMineTriggered?.Invoke(piece, mineOwner);
        return true;
    }

    // ─────────────────────────────────────────────
    // UTILIDAD
    // ─────────────────────────────────────────────
    private int Wrap(int index)
    {
        int len = BoardManager.Instance.TrackLength;
        if (len <= 0) return 0;
        return ((index % len) + len) % len;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_activeMines == null || BoardManager.Instance == null) return;
        foreach (var kvp in _activeMines)
        {
            var sq = BoardManager.Instance.GetSquare(kvp.Key);
            if (sq == null) continue;
            Gizmos.color = MineGizmoColor(kvp.Value);
            Gizmos.DrawCube(sq.position + Vector3.up * 0.15f, new Vector3(0.3f, 0.15f, 0.3f));
        }
    }

    private Color MineGizmoColor(PlayerColor c)
    {
        switch (c)
        {
            case PlayerColor.Yellow: return Color.yellow;
            case PlayerColor.Green:  return Color.green;
            case PlayerColor.Red:    return Color.red;
            default:                 return Color.cyan;
        }
    }
#endif
}
