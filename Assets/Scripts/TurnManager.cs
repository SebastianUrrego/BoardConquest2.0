using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TurnManager - Orquesta toda la logica de turnos.
/// Fase 1: Tirada inicial para decidir orden (con retirada en empate).
/// Fase 2: Bucle de turnos entre los jugadores activos.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("=== FICHAS POR EQUIPO ===")]
    public PieceController[] yellowPieces = new PieceController[4];
    public PieceController[] greenPieces  = new PieceController[4];
    public PieceController[] redPieces    = new PieceController[4];
    public PieceController[] bluePieces   = new PieceController[4];

    [Header("=== DADO INICIAL ===")]
    public DiceController singleDie;

    private List<PlayerData> _activePlayers = new List<PlayerData>();
    private int _currentIndex = 0;

    public PlayerData CurrentPlayer =>
        (_activePlayers.Count > 0) ? _activePlayers[_currentIndex] : null;

    public enum TurnPhase { InitialRoll, WaitingForRoll, WaitingForPieceSelection, MovingPiece, GameOver }
    public TurnPhase Phase { get; private set; } = TurnPhase.InitialRoll;

    public int LastDiceResult { get; private set; } = 0;
    private PieceController _selectedPiece = null;

    public event Action<PlayerData>       OnTurnStart;
    public event Action<List<PlayerData>> OnOrderDecided;
    public event Action<string>           OnStatusMessage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        BuildPlayerList();
        StartCoroutine(InitialRollPhase());
    }

    private void BuildPlayerList()
    {
        int count = PlayerPrefs.GetInt("PlayerCount", 2);
        _activePlayers.Clear();

        _activePlayers.Add(new PlayerData("J1 Amarillo", PlayerColor.Yellow, yellowPieces));
        _activePlayers.Add(new PlayerData("J2 Verde",    PlayerColor.Green,  greenPieces));
        if (count >= 3) _activePlayers.Add(new PlayerData("J3 Rojo", PlayerColor.Red,  redPieces));
        if (count >= 4) _activePlayers.Add(new PlayerData("J4 Azul", PlayerColor.Blue, bluePieces));

        GameManager.Instance.RegisterPlayers(_activePlayers);
    }

    // ── FASE 1: Tirada inicial con soporte de empate ──
    private IEnumerator InitialRollPhase()
    {
        Phase = TurnPhase.InitialRoll;
        List<PlayerData> pending = new List<PlayerData>(_activePlayers);
        bool orderDecided = false;

        while (!orderDecided)
        {
            OnStatusMessage?.Invoke("Tirada inicial: cada jugador lanza el dado!");
            yield return new WaitForSeconds(1f);

            foreach (PlayerData p in pending)
            {
                OnStatusMessage?.Invoke($"{p.Name} esta tirando...");
                yield return new WaitForSeconds(0.5f);

                bool rolled = false;
                int rollVal = 0;
                System.Action<int> handler = (val) => { rollVal = val; rolled = true; };
                singleDie.OnRollComplete += handler;
                singleDie.Roll();
                yield return new WaitUntil(() => rolled);
                singleDie.OnRollComplete -= handler;
                p.InitialRoll = rollVal;

                OnStatusMessage?.Invoke($"{p.Name} saco {p.InitialRoll}");
                yield return new WaitForSeconds(0.8f);
            }

            int maxRoll = 0;
            foreach (var p in pending) if (p.InitialRoll > maxRoll) maxRoll = p.InitialRoll;

            List<PlayerData> tied = new List<PlayerData>();
            foreach (var p in pending) if (p.InitialRoll == maxRoll) tied.Add(p);

            if (tied.Count == 1)
            {
                PlayerData first = tied[0];
                OnStatusMessage?.Invoke($"{first.Name} empieza la partida!");
                yield return new WaitForSeconds(1f);

                int idx = _activePlayers.IndexOf(first);
                List<PlayerData> reordered = new List<PlayerData>();
                for (int i = 0; i < _activePlayers.Count; i++)
                    reordered.Add(_activePlayers[(idx + i) % _activePlayers.Count]);
                _activePlayers = reordered;

                GameManager.Instance.RegisterPlayers(_activePlayers);
                OnOrderDecided?.Invoke(_activePlayers);
                orderDecided = true;
            }
            else
            {
                string names = "";
                foreach (var p in tied) names += p.Name + ", ";
                OnStatusMessage?.Invoke($"Empate! Vuelven a tirar: {names.TrimEnd(',', ' ')}");
                yield return new WaitForSeconds(1.2f);
                pending = tied;
            }
        }

        _currentIndex = 0;
        StartCoroutine(TurnLoop());
    }

    // ── FASE 2: Bucle de turnos ──
    private IEnumerator TurnLoop()
    {
        while (!GameManager.Instance.GameOver)
        {
            PlayerData current = _activePlayers[_currentIndex];
            Phase = TurnPhase.WaitingForRoll;

            OnTurnStart?.Invoke(current);
            OnStatusMessage?.Invoke($"Turno de {current.Name}. Presiona el boton para tirar.");

            yield return new WaitUntil(() =>
                Phase == TurnPhase.WaitingForPieceSelection || GameManager.Instance.GameOver);
            if (GameManager.Instance.GameOver) break;

            OnStatusMessage?.Invoke($"Sacaste {LastDiceResult}. Selecciona una ficha.");

            yield return new WaitUntil(() =>
                Phase == TurnPhase.MovingPiece || GameManager.Instance.GameOver);
            if (GameManager.Instance.GameOver) break;

            yield return new WaitUntil(() => _selectedPiece != null && !_selectedPiece.IsMoving());

            CheckForKill(current, _selectedPiece);

            _currentIndex = (_currentIndex + 1) % _activePlayers.Count;
            _selectedPiece = null;
            Phase = TurnPhase.WaitingForRoll;
            yield return new WaitForSeconds(0.5f);
        }

        Phase = TurnPhase.GameOver;
        OnStatusMessage?.Invoke($"!{GameManager.Instance.Winner?.Name} GANO LA PARTIDA!");
    }

    public void RollDice()
    {
        if (Phase != TurnPhase.WaitingForRoll || GameManager.Instance.GameOver) return;
        StartCoroutine(RollDiceRoutine());
    }

    private IEnumerator RollDiceRoutine()
    {
        bool done = false;
        int val = 0;
        System.Action<int> handler = (v) => { val = v; done = true; };
        DiceManager.Instance.OnDiceRollComplete += handler;
        DiceManager.Instance.RollAll();
        yield return new WaitUntil(() => done);
        DiceManager.Instance.OnDiceRollComplete -= handler;
        LastDiceResult = val;
        Phase = TurnPhase.WaitingForPieceSelection;
    }

    public void SelectPiece(PieceController piece)
    {
        if (Phase != TurnPhase.WaitingForPieceSelection || GameManager.Instance.GameOver) return;

        PlayerData current = CurrentPlayer;
        bool belongs = false;
        foreach (var p in current.Pieces)
            if (p == piece) { belongs = true; break; }
        if (!belongs) return;

        _selectedPiece = piece;
        Phase = TurnPhase.MovingPiece;

        System.Action<int> lapHandler = null;
        lapHandler = (laps) =>
        {
            GameManager.Instance.OnLapCompleted(current);
            piece.OnLapCompleted -= lapHandler;
        };
        piece.OnLapCompleted += lapHandler;

        if (piece.IsAtHome) piece.LeaveHome();
        else piece.Move(LastDiceResult);
    }

    private void CheckForKill(PlayerData attacker, PieceController attackerPiece)
    {
        int attackerIdx = attackerPiece.TrackIndex;
        if (IsSafeZone(attackerIdx)) return;

        foreach (PlayerData defender in _activePlayers)
        {
            if (defender == attacker) continue;
            foreach (PieceController defPiece in defender.Pieces)
            {
                if (defPiece == null || defPiece.IsAtHome) continue;
                if (defPiece.TrackIndex == attackerIdx)
                {
                    SendToHome(defPiece, defender);
                    GameManager.Instance.OnPieceKilled(attacker);
                    OnStatusMessage?.Invoke($"{attacker.Name} capturo ficha de {defender.Name}! +1 pto");
                }
            }
        }
    }

    private bool IsSafeZone(int trackIndex)
    {
        BoardManager bm = BoardManager.Instance;
        int mod = ((trackIndex % bm.TrackLength) + bm.TrackLength) % bm.TrackLength;
        return mod == bm.yellowStartIndex || mod == bm.greenStartIndex ||
               mod == bm.redStartIndex   || mod == bm.blueStartIndex;
    }

    private void SendToHome(PieceController piece, PlayerData owner)
    {
        Transform[] homeSquares = BoardManager.Instance.GetHomeSquares(owner.Color);
        if (homeSquares != null && homeSquares.Length > piece.pieceIndex && homeSquares[piece.pieceIndex] != null)
            piece.transform.position = homeSquares[piece.pieceIndex].position;
        piece.SendMessage("ResetToHome", SendMessageOptions.DontRequireReceiver);
    }
}
