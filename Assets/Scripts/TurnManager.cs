using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TurnManager — Orquesta fases de juego.
///
/// CONTROLES:
///   ESPACIO   → tirar dado (tirada inicial Y turno normal)
///   1 / 2 / 3 / 4 → seleccionar ficha del equipo activo
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("=== FICHAS POR EQUIPO ===")]
    public PieceController[] yellowPieces = new PieceController[4];
    public PieceController[] greenPieces  = new PieceController[4];
    public PieceController[] redPieces    = new PieceController[4];
    public PieceController[] bluePieces   = new PieceController[4];

    [Header("=== DADO INICIAL (1 dado) ===")]
    public DiceController singleDie;

    // ── Estado ──
    private List<PlayerData> _players = new List<PlayerData>();
    private int _turnIndex = 0;

    public PlayerData CurrentPlayer =>
        (_players.Count > 0) ? _players[_turnIndex] : null;

    public enum Phase
    {
        InitialWait,       // esperando ESPACIO del jugador actual (tirada inicial)
        InitialAnimating,  // dado animándose
        TurnWaitRoll,      // turno normal: esperando ESPACIO
        TurnAnimating,     // dados animándose
        TurnWaitPiece,     // esperando que elija ficha (teclas 1-4)
        TurnMoving,        // ficha moviéndose
        GameOver
    }
    public Phase CurrentPhase { get; private set; } = Phase.InitialWait;

    public int  LastDiceTotal   { get; private set; } = 0;
    public PlayerData InitialTurnPlayer { get; private set; }

    // Flags de input (seteados en Update, consumidos en coroutines)
    private bool _spaceDown  = false;
    private int  _pieceKeyDown = -1; // 0-3

    // Ficha seleccionada para el turno
    private PieceController _movingPiece = null;
    // Ficha actualmente resaltada
    public PieceController HighlightedPiece { get; private set; }

    // ── Eventos para la UI ──
    public event Action<string>           OnStatus;
    public event Action<PlayerData>       OnTurnStart;
    public event Action<List<PlayerData>> OnOrderReady;
    public event Action<PieceController>  OnPieceHighlighted;

    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        BuildPlayers();
        StartCoroutine(WaitOneFrameThenStart());
    }

    IEnumerator WaitOneFrameThenStart()
    {
        yield return null; // deja que GameUI se suscriba
        StartCoroutine(PhaseInitialRoll());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) _spaceDown = true;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) _pieceKeyDown = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) _pieceKeyDown = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) _pieceKeyDown = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) _pieceKeyDown = 3;
    }

    // ─────────────────────────────────────────────
    void BuildPlayers()
    {
        int count = PlayerPrefs.GetInt("PlayerCount", 4);
        _players.Clear();
        _players.Add(new PlayerData("J1 Amarillo", PlayerColor.Yellow, yellowPieces));
        _players.Add(new PlayerData("J2 Verde",    PlayerColor.Green,  greenPieces));
        if (count >= 3) _players.Add(new PlayerData("J3 Rojo", PlayerColor.Red,  redPieces));
        if (count >= 4) _players.Add(new PlayerData("J4 Azul", PlayerColor.Blue, bluePieces));
        GameManager.Instance.RegisterPlayers(_players);
    }

    // ═══════════════════════════════════════════════
    // FASE 1 — TIRADA INICIAL
    // ═══════════════════════════════════════════════
    IEnumerator PhaseInitialRoll()
    {
        List<PlayerData> pending = new List<PlayerData>(_players);
        bool decided = false;

        while (!decided)
        {
            foreach (PlayerData p in pending)
            {
                InitialTurnPlayer = p;
                CurrentPhase = Phase.InitialWait;
                OnStatus?.Invoke($"{p.Name}: presiona [ESPACIO] para lanzar el dado");

                // Esperar ESPACIO
                _spaceDown = false;
                yield return new WaitUntil(() => _spaceDown);
                _spaceDown = false;

                // Animar dado
                CurrentPhase = Phase.InitialAnimating;
                OnStatus?.Invoke($"{p.Name} tirando...");

                bool done = false; int val = 0;
                Action<int> h = v => { val = v; done = true; };
                singleDie.OnRollComplete += h;
                singleDie.Roll();
                yield return new WaitUntil(() => done);
                singleDie.OnRollComplete -= h;
                p.InitialRoll = val;

                OnStatus?.Invoke($"{p.Name} sacó {val}!");
                yield return new WaitForSeconds(1f);
            }

            int max = 0;
            foreach (var p in pending) if (p.InitialRoll > max) max = p.InitialRoll;
            var tied = pending.FindAll(p => p.InitialRoll == max);

            if (tied.Count == 1)
            {
                var first = tied[0];
                OnStatus?.Invoke($"¡{first.Name} empieza!");
                yield return new WaitForSeconds(1.2f);

                // Reordenar lista
                int idx = _players.IndexOf(first);
                var ordered = new List<PlayerData>();
                for (int i = 0; i < _players.Count; i++)
                    ordered.Add(_players[(idx + i) % _players.Count]);
                _players = ordered;
                GameManager.Instance.RegisterPlayers(_players);
                OnOrderReady?.Invoke(_players);
                decided = true;
            }
            else
            {
                string names = string.Join(", ", tied.ConvertAll(p => p.Name));
                OnStatus?.Invoke($"Empate ({names}). Vuelven a tirar.");
                yield return new WaitForSeconds(1.5f);
                pending = tied;
            }
        }

        InitialTurnPlayer = null;
        _turnIndex = 0;
        StartCoroutine(PhaseTurnLoop());
    }

    // ═══════════════════════════════════════════════
    // FASE 2 — BUCLE DE TURNOS
    // ═══════════════════════════════════════════════
    IEnumerator PhaseTurnLoop()
    {
        while (!GameManager.Instance.GameOver)
        {
            PlayerData current = _players[_turnIndex];
            CurrentPhase = Phase.TurnWaitRoll;
            HighlightedPiece = null;
            OnPieceHighlighted?.Invoke(null);
            OnTurnStart?.Invoke(current);
            OnStatus?.Invoke($"[{current.Name}] Presiona ESPACIO para tirar los dados");

            // ── Esperar ESPACIO ──
            _spaceDown = false;
            yield return new WaitUntil(() => _spaceDown);
            _spaceDown = false;

            // ── Animar dados ──
            CurrentPhase = Phase.TurnAnimating;
            OnStatus?.Invoke("Tirando dados...");
            bool done2 = false; int total = 0;
            Action<int> h2 = v => { total = v; done2 = true; };
            DiceManager.Instance.OnDiceRollComplete += h2;
            DiceManager.Instance.RollAll();
            yield return new WaitUntil(() => done2);
            DiceManager.Instance.OnDiceRollComplete -= h2;
            LastDiceTotal = total;

            // ── Esperar elección de ficha (teclas 1-4) ──
            CurrentPhase = Phase.TurnWaitPiece;
            OnStatus?.Invoke($"Resultado: {total}  |  Elige ficha [1-4]");

            _pieceKeyDown = -1;
            _movingPiece  = null;
            yield return new WaitUntil(() =>
            {
                if (_pieceKeyDown < 0) return false;
                int idx = _pieceKeyDown;
                _pieceKeyDown = -1;

                PieceController[] pool = current.Pieces;
                if (idx >= pool.Length || pool[idx] == null) return false;

                PieceController chosen = pool[idx];
                // No puede moverse si ya está moviendose
                if (chosen.IsMoving()) return false;

                // Resaltar
                HighlightedPiece = chosen;
                OnPieceHighlighted?.Invoke(chosen);
                _movingPiece = chosen;
                return true;
            });

            // ── Mover ficha ──
            CurrentPhase = Phase.TurnMoving;
            OnStatus?.Invoke($"Moviendo {HighlightedPiece.name}...");

            // Suscribir vuelta
            Action<int> lapH = null;
            lapH = _ => { GameManager.Instance.OnLapCompleted(current); _movingPiece.OnLapCompleted -= lapH; };
            _movingPiece.OnLapCompleted += lapH;

            if (_movingPiece.IsAtHome) _movingPiece.LeaveHome();
            else                       _movingPiece.Move(LastDiceTotal);

            yield return new WaitUntil(() => !_movingPiece.IsMoving());

            // ── Revisar kills ──
            CheckKills(current, _movingPiece);

            // ── Fin turno ──
            HighlightedPiece = null;
            OnPieceHighlighted?.Invoke(null);
            _turnIndex = (_turnIndex + 1) % _players.Count;
            CurrentPhase = Phase.TurnWaitRoll;
            yield return new WaitForSeconds(0.3f);
        }

        CurrentPhase = Phase.GameOver;
        OnStatus?.Invoke($"¡{GameManager.Instance.Winner?.Name} GANÓ LA PARTIDA!");
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA (llamado desde botón UI si existe)
    // ─────────────────────────────────────────────
    public void RollDice() { if (CurrentPhase == Phase.TurnWaitRoll) _spaceDown = true; }

    // ─────────────────────────────────────────────
    // SISTEMA DE MATAR
    // ─────────────────────────────────────────────
    void CheckKills(PlayerData attacker, PieceController ap)
    {
        int idx = ap.TrackIndex;
        if (IsSafe(idx)) return;
        foreach (var def in _players)
        {
            if (def == attacker) continue;
            foreach (var dp in def.Pieces)
            {
                if (dp == null || dp.IsAtHome) continue;
                if (dp.TrackIndex == idx)
                {
                    SendHome(dp, def);
                    GameManager.Instance.OnPieceKilled(attacker);
                    OnStatus?.Invoke($"{attacker.Name} capturó ficha de {def.Name}! +1 pto");
                }
            }
        }
    }

    bool IsSafe(int trackIndex)
    {
        var bm = BoardManager.Instance;
        int m = ((trackIndex % bm.TrackLength) + bm.TrackLength) % bm.TrackLength;
        return m == bm.yellowStartIndex || m == bm.greenStartIndex ||
               m == bm.redStartIndex   || m == bm.blueStartIndex;
    }

    void SendHome(PieceController p, PlayerData owner)
    {
        var homes = BoardManager.Instance.GetHomeSquares(owner.Color);
        if (homes != null && p.pieceIndex < homes.Length && homes[p.pieceIndex] != null)
            p.transform.position = homes[p.pieceIndex].position;
        p.SendMessage("ResetToHome", SendMessageOptions.DontRequireReceiver);
    }
}
