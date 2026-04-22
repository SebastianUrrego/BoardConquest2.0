using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DiceManager - Controla los 2 dados juntos.
/// Lanza ambos, espera que terminen y suma el resultado.
/// Adjunta este script a un GameObject vacío llamado "DiceManager".
/// </summary>
public class DiceManager : MonoBehaviour
{
    public static DiceManager Instance { get; private set; }

    // ─────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────
    [Header("=== DADOS ===")]
    [Tooltip("Arrastra aquí el GameObject del Dado1")]
    public DiceController dice1;

    [Tooltip("Arrastra aquí el GameObject del Dado2")]
    public DiceController dice2;

    [Header("=== RETRASO ENTRE DADOS ===")]
    [Tooltip("Segundos de diferencia entre el lanzamiento de cada dado")]
    public float diceOffset = 0.1f;

    [Header("=== TEST (solo en desarrollo) ===")]
    [Tooltip("Tecla para tirar los dados manualmente")]
    public Key rollKey = Key.R;
    public bool showTestUI = true;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    private bool _isRolling = false;
    private int  _totalResult = 0;
    private int  _dice1Result = 0;
    private int  _dice2Result = 0;

    public int TotalResult  => _totalResult;
    public bool IsRolling() => _isRolling;

    // ─────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Se dispara cuando ambos dados terminaron.
    /// Parámetro: suma total de los 2 dados.
    /// </summary>
    public event Action<int> OnDiceRollComplete;

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Tecla R para tirar dados en modo test
        if (showTestUI && Keyboard.current[rollKey].wasPressedThisFrame)
            RollAll();
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Lanza los 2 dados. Cuando terminan dispara OnDiceRollComplete con la suma.
    /// Llama este método desde tu botón de UI o lógica de turnos.
    /// </summary>
    public void RollAll()
    {
        if (_isRolling) return;
        StartCoroutine(RollBothRoutine());
    }

    // ─────────────────────────────────────────────
    // LÓGICA INTERNA
    // ─────────────────────────────────────────────
    private IEnumerator RollBothRoutine()
    {
        _isRolling    = true;
        _totalResult  = 0;
        _dice1Result  = 0;
        _dice2Result  = 0;

        bool dice1Done = false;
        bool dice2Done = false;

        // Subscribirse a los eventos de cada dado
        dice1.OnRollComplete += (val) => { _dice1Result = val; dice1Done = true; };
        dice2.OnRollComplete += (val) => { _dice2Result = val; dice2Done = true; };

        // Lanzar dado 1
        dice1.Roll();

        // Pequeño retraso antes del dado 2 (se ve más natural)
        yield return new WaitForSeconds(diceOffset);

        // Lanzar dado 2
        dice2.Roll();

        // Esperar a que ambos terminen
        yield return new WaitUntil(() => dice1Done && dice2Done);

        // Desuscribirse
        dice1.OnRollComplete -= (val) => { _dice1Result = val; dice1Done = true; };
        dice2.OnRollComplete -= (val) => { _dice2Result = val; dice2Done = true; };

        _totalResult = _dice1Result + _dice2Result;
        _isRolling   = false;

        Debug.Log($"[DiceManager] Dado1: {_dice1Result} | Dado2: {_dice2Result} | Total: {_totalResult}");

        OnDiceRollComplete?.Invoke(_totalResult);
    }

    // ─────────────────────────────────────────────
    // UI DE TEST
    // ─────────────────────────────────────────────
    private void OnGUI()
    {
        if (!showTestUI) return;

        GUIStyle style = new GUIStyle(GUI.skin.box) { fontSize = 16 };
        string estado = _isRolling ? "Tirando..." : $"Dado1: {_dice1Result}  Dado2: {_dice2Result}  Total: {_totalResult}";

        GUI.Box(new Rect(10, 110, 300, 60),
            $"{estado}\n[R] Tirar dados",
            style);
    }
}
