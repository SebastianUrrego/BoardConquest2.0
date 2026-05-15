using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TurnManager — Orquesta fases de juego.
///
/// CONTROLES:
///   ESPACIO     → tirar dado (tirada inicial Y turno normal)
///   0-4         → cuantas minas usar (en fase TurnWaitMines)
///   1 / 2 / 3 / 4 → seleccionar ficha del equipo activo
/// </summary>
[DefaultExecutionOrder(100)]
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
        InitialAnimating,  // dado animandose
        TurnWaitRoll,      // turno normal: esperando ESPACIO
        TurnAnimating,     // dados animandose
        TurnWaitMines,     // esperando que decida cuantas minas usar (0-4)
        TurnWaitPiece,     // esperando que elija ficha (teclas 1-4)
        TurnMoving,        // ficha moviendose
        GameOver
    }
    public Phase CurrentPhase { get; private set; } = Phase.InitialWait;

    public int        LastDiceTotal      { get; private set; } = 0;
    public int        MinesUsedThisTurn  { get; private set; } = 0;
    public PlayerData InitialTurnPlayer  { get; private set; }

    // Flags de input (seteados en Update, consumidos en coroutines)
    private bool _spaceDown    = false;
    private int  _pieceKeyDown = -1;  // 0-3
    private int  _mineKeyDown  = -1;  // 0-4: cantidad de minas a usar

    // Posicion del track ANTES de mover (para saber donde colocar minas)
    private int _pieceTrackBeforeMove = 0;

    // Ficha seleccionada para el turno
    private PieceController _movingPiece  = null;
    public  PieceController HighlightedPiece { get; private set; }

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

    public bool autoStart = true;

    void Start()
    {
        if (autoStart)
        {
            InitGame();
        }
    }

    public void InitGame()
    {
        StartCoroutine(WaitOneFrameThenStart());
    }

    IEnumerator WaitOneFrameThenStart()
    {
        yield return null;
        BuildPlayers();
        TeleportAllPiecesToHome();
        StartCoroutine(PhaseInitialRoll());
    }

    void TeleportAllPiecesToHome()
    {
        foreach (var p in _players)
        {
            var homes = BoardManager.Instance.GetHomeSquares(p.Color);
            if (homes == null) continue;
            for (int i = 0; i < p.Pieces.Length; i++)
            {
                if (p.Pieces[i] != null && i < homes.Length && homes[i] != null)
                {
                    p.Pieces[i].transform.position = homes[i].position;
                    p.Pieces[i].ResetToHome();
                }
            }
        }
    }

    void Update()
    {
        /* Teclado desactivado a petición del usuario
        if (Input.GetKeyDown(KeyCode.Space)) _spaceDown = true;

        // Seleccion de ficha (1-4) — solo fuera de la fase de minas
        if (CurrentPhase != Phase.TurnWaitMines)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) _pieceKeyDown = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) _pieceKeyDown = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) _pieceKeyDown = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) _pieceKeyDown = 3;
        }

        // Seleccion de minas (0-4) — solo en la fase de minas
        if (CurrentPhase == Phase.TurnWaitMines)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) _mineKeyDown = 0;
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) _mineKeyDown = 1;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) _mineKeyDown = 2;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) _mineKeyDown = 3;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) _mineKeyDown = 4;
        }
        */
    }

    // ─────────────────────────────────────────────
    void BuildPlayers()
    {
        int count = GameInitializer.ActivePlayers > 0
            ? GameInitializer.ActivePlayers
            : PlayerPrefs.GetInt("PlayerCount", 2);
            
        _players.Clear();
        for (int i = 0; i < count; i++)
        {
            int colorInt = PlayerPrefs.GetInt("PlayerColor_" + i, i);
            PlayerColor pColor = (PlayerColor)colorInt;
            PieceController[] pieces = null;
            string colorName = "";

            switch (pColor)
            {
                case PlayerColor.Yellow: pieces = yellowPieces; colorName = "Amarillo"; break;
                case PlayerColor.Green:  pieces = greenPieces;  colorName = "Verde";    break;
                case PlayerColor.Red:    pieces = redPieces;    colorName = "Rojo";     break;
                case PlayerColor.Blue:   pieces = bluePieces;   colorName = "Azul";     break;
            }

            _players.Add(new PlayerData($"J{i + 1} {colorName}", pColor, pieces));
        }

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

                _spaceDown = false;
                yield return new WaitUntil(() => _spaceDown);
                _spaceDown = false;

                CurrentPhase = Phase.InitialAnimating;
                OnStatus?.Invoke($"{p.Name} tirando...");

                bool done = false; int val = 0;
                Action<int> h = v => { val = v; done = true; };
                singleDie.StopAllCoroutines();
                {
                    var f = typeof(DiceController).GetField("_isRolling",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    f?.SetValue(singleDie, false);
                }
                singleDie.OnRollComplete += h;
                singleDie.Roll();
                float _t = 0f;
                while (!done && _t < 5f) { _t += Time.deltaTime; yield return null; }
                if (!done) val = UnityEngine.Random.Range(1, 7);
                singleDie.OnRollComplete -= h;
                p.InitialRoll = val;

                OnStatus?.Invoke($"{p.Name} saco {val}!");
                yield return new WaitForSeconds(1f);
            }

            int max = 0;
            foreach (var p in pending) if (p.InitialRoll > max) max = p.InitialRoll;
            var tied = pending.FindAll(p => p.InitialRoll == max);

            if (tied.Count == 1)
            {
                var first = tied[0];
                OnStatus?.Invoke($"!{first.Name} empieza!");
                yield return new WaitForSeconds(1.2f);

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
            CurrentPhase      = Phase.TurnWaitRoll;
            MinesUsedThisTurn = 0;
            HighlightedPiece  = null;
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

            // ── Fase de minas: el jugador decide cuantas usar ──
            int maxMines = MineSystem.Instance != null
                ? MineSystem.Instance.MaxMinesToUse(current.Color, total)
                : 0;

            if (maxMines > 0)
            {
                CurrentPhase  = Phase.TurnWaitMines;
                _mineKeyDown  = -1;
                int minesLeft = MineSystem.Instance.GetMinesRemaining(current.Color);
                OnStatus?.Invoke(
                    $"Resultado: {total}  |  Minas disponibles: {minesLeft}  |  " +
                    $"Usa [0-{maxMines}] minas (cada una resta 1 avance)");

                // Esperar que el jugador presione una tecla valida
                yield return new WaitUntil(() =>
                {
                    if (_mineKeyDown < 0) return false;
                    int chosen = _mineKeyDown;
                    _mineKeyDown = -1;
                    if (chosen > maxMines) return false; // invalido, esperar de nuevo
                    MinesUsedThisTurn = chosen;
                    return true;
                });

                // Reducir el total del dado
                LastDiceTotal -= MinesUsedThisTurn;
                OnStatus?.Invoke(
                    $"Usas {MinesUsedThisTurn} mina(s). Avanzo: {LastDiceTotal}  |  Elige ficha [1-4]");
            }
            else
            {
                // No tiene minas o el dado es 1, saltar la fase
                MinesUsedThisTurn = 0;
                OnStatus?.Invoke($"Resultado: {total}  |  Elige ficha [1-4]");
            }

            // ── Esperar eleccion de ficha (teclas 1-4) ──
            CurrentPhase  = Phase.TurnWaitPiece;
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
                if (chosen.IsMoving()) return false;

                HighlightedPiece = chosen;
                OnPieceHighlighted?.Invoke(chosen);
                _movingPiece = chosen;
                return true;
            });

            // ── Guardar posicion antes de mover y colocar minas ──
            _pieceTrackBeforeMove = _movingPiece.IsAtHome
                ? BoardManager.Instance.GetStartIndex(current.Color)
                : _movingPiece.TrackIndex;

            if (MinesUsedThisTurn > 0 && MineSystem.Instance != null)
            {
                // fullRoll = LastDiceTotal + MinesUsedThisTurn (el valor original antes de reducir)
                int fullRoll = LastDiceTotal + MinesUsedThisTurn;
                MineSystem.Instance.PlaceMines(current.Color, _pieceTrackBeforeMove, fullRoll, MinesUsedThisTurn);
                int remaining = MineSystem.Instance.GetMinesRemaining(current.Color);
                OnStatus?.Invoke($"Mina(s) colocada(s)! Te quedan {remaining} minas. Moviendo...");
            }

            // ── Mover ficha ──
            CurrentPhase = Phase.TurnMoving;
            if (MinesUsedThisTurn == 0)
                OnStatus?.Invoke($"Moviendo {HighlightedPiece.name}...");

            Action<int> lapH = null;
            lapH = _ => { GameManager.Instance.OnLapCompleted(current); _movingPiece.OnLapCompleted -= lapH; };
            _movingPiece.OnLapCompleted += lapH;

            if (_movingPiece.IsAtHome) 
            {
                _movingPiece.LeaveHome();
                yield return new WaitUntil(() => !_movingPiece.IsMoving());
                
                // Después de salir de casa, usar el resultado de los dados para avanzar
                if (LastDiceTotal > 0)
                {
                    _movingPiece.Move(LastDiceTotal);
                }
            }
            else
            {
                _movingPiece.Move(LastDiceTotal);
            }

            yield return new WaitUntil(() => !_movingPiece.IsMoving());

            // ── Revisar minas (antes que kills, ya que la mina manda a casa) ──
            if (MineSystem.Instance != null && !_movingPiece.IsAtHome)
            {
                bool hitMine = MineSystem.Instance.CheckAndTriggerMine(_movingPiece, current);
                if (hitMine)
                {
                    int mineOwnerColor = -1; // ya gestionado dentro de CheckAndTriggerMine
                    OnStatus?.Invoke($"{current.Name} piso una mina! -2 puntos. Vuelve a casa.");
                    yield return new WaitForSeconds(1.5f);
                    // No hacer CheckKills si fue enviado a casa por la mina
                    goto EndTurn;
                }
            }

            // ── Revisar kills ──
            CheckKills(current, _movingPiece);

            EndTurn:
            HighlightedPiece = null;
            OnPieceHighlighted?.Invoke(null);
            _turnIndex = (_turnIndex + 1) % _players.Count;
            CurrentPhase = Phase.TurnWaitRoll;
            yield return new WaitForSeconds(0.3f);
        }

        CurrentPhase = Phase.GameOver;
        OnStatus?.Invoke($"!{GameManager.Instance.Winner?.Name} GANO LA PARTIDA!");
    }

    // ─────────────────────────────────────────────
    // API PUBLICA
    // ─────────────────────────────────────────────
    public void RollDice() { if (CurrentPhase == Phase.TurnWaitRoll || CurrentPhase == Phase.InitialWait) _spaceDown = true; }

    public void SelectPiece(int index) { if (CurrentPhase == Phase.TurnWaitPiece) _pieceKeyDown = index; }

    public void SelectMines(int count) { if (CurrentPhase == Phase.TurnWaitMines) _mineKeyDown = count; }

    // ─────────────────────────────────────────────
    // SISTEMA DE MATAR
    // ─────────────────────────────────────────────
    void CheckKills(PlayerData attacker, PieceController ap)
    {
        int total = BoardManager.Instance.TrackLength;
        int idx = ap.TrackIndex;
        int idxWrapped = ((idx % total) + total) % total;

        if (IsSafe(idx)) return;
        foreach (var def in _players)
        {
            if (def == attacker) continue;
            foreach (var dp in def.Pieces)
            {
                if (dp == null || dp.IsAtHome) continue;
                
                int defIdxWrapped = ((dp.TrackIndex % total) + total) % total;
                if (defIdxWrapped == idxWrapped)
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
