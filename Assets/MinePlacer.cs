using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MinePlacer — Instancia y destruye el prefab visual de mina en el tablero.
///
/// SETUP:
///   1. Arrastra este script a un GameObject vacío en la escena (ej. "MinePlacer").
///   2. Asigna el prefab de tu asset de mina en el campo "minePrefab".
///   3. El sistema de minas se conecta automáticamente.
///
/// El prefab se instancia encima de la casilla del track donde se colocó la mina.
/// Cuando la mina es activada, el objeto se destruye automáticamente.
/// </summary>
public class MinePlacer : MonoBehaviour
{
    public static MinePlacer Instance { get; private set; }

    [Header("=== PREFAB VISUAL ===")]
    [Tooltip("Arrastra aquí el prefab 3D de la mina")]
    public GameObject minePrefab;

    [Tooltip("Altura sobre la casilla donde aparece la mina")]
    public float heightOffset = 0.25f;

    // Mapa: índice del track → instancia del GameObject
    private Dictionary<int, GameObject> _mineObjects = new Dictionary<int, GameObject>();

    // ─────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnEnable()
    {
        // Suscribirse al evento de mina activada para destruir el objeto
        if (MineSystem.Instance != null)
            MineSystem.Instance.OnMineTriggered += OnMineTriggered;
    }

    void OnDisable()
    {
        if (MineSystem.Instance != null)
            MineSystem.Instance.OnMineTriggered -= OnMineTriggered;
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA — llamada desde TurnManager
    // ─────────────────────────────────────────────

    /// <summary>
    /// Instancia el prefab de mina en la casilla indicada del track.
    /// </summary>
    public void PlaceMineVisual(int trackIndex, PlayerColor ownerColor)
    {
        if (minePrefab == null)
        {
            Debug.LogWarning("[MinePlacer] No hay prefab de mina asignado.");
            return;
        }

        int len = BoardManager.Instance.TrackLength;
        int wrapped = ((trackIndex % len) + len) % len;

        // No duplicar si ya hay una mina visual ahí
        if (_mineObjects.ContainsKey(wrapped)) return;

        Transform square = BoardManager.Instance.GetSquare(wrapped);
        if (square == null) return;

        Vector3 spawnPos = square.position + Vector3.up * heightOffset;
        GameObject mineObj = Instantiate(minePrefab, spawnPos, Quaternion.identity);
        mineObj.name = $"Mine_{ownerColor}_{wrapped}";

        _mineObjects[wrapped] = mineObj;
    }

    /// <summary>
    /// Coloca visualmente varias minas de una vez (conveniencia).
    /// </summary>
    public void PlaceMinesVisual(PlayerColor ownerColor, int startTrackIndex, int fullRoll, int minesToPlace)
    {
        if (minesToPlace <= 0) return;
        int actualMove = fullRoll - minesToPlace;
        int len = BoardManager.Instance.TrackLength;

        for (int i = 1; i <= minesToPlace; i++)
        {
            int minePos = ((startTrackIndex + actualMove + i) % len + len) % len;
            PlaceMineVisual(minePos, ownerColor);
        }
    }

    /// <summary>
    /// Elimina el objeto visual de mina en la casilla dada.
    /// </summary>
    public void RemoveMineVisual(int trackIndex)
    {
        int len = BoardManager.Instance.TrackLength;
        int wrapped = ((trackIndex % len) + len) % len;

        if (_mineObjects.TryGetValue(wrapped, out GameObject obj))
        {
            _mineObjects.Remove(wrapped);
            if (obj != null) StartCoroutine(DestroyWithEffect(obj));
        }
    }

    // ─────────────────────────────────────────────
    // EVENTO — mina activada por MineSystem
    // ─────────────────────────────────────────────
    private void OnMineTriggered(PieceController piece, PlayerColor mineOwner)
    {
        // La mina ya fue eliminada del diccionario en MineSystem; aquí destruimos el visual.
        // Buscamos por posición (la mina fue removida de _activeMines, pero aún tenemos el GO).
        // Buscamos entre todos los mineObjects cuál está más cerca de la posición de la ficha.
        int len = BoardManager.Instance.TrackLength;
        int wrapped = ((piece.TrackIndex % len) + len) % len;
        RemoveMineVisual(wrapped);
    }

    // ─────────────────────────────────────────────
    // UTILIDAD
    // ─────────────────────────────────────────────
    private IEnumerator DestroyWithEffect(GameObject obj)
    {
        // Pequeña animación de escala hacia 0 antes de destruir
        float t = 0f;
        float duration = 0.3f;
        Vector3 startScale = obj.transform.localScale;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = 1f - Mathf.Clamp01(t / duration);
            obj.transform.localScale = startScale * p;
            yield return null;
        }
        Destroy(obj);
    }

    /// <summary>Limpia todos los objetos de mina (reinicio de partida).</summary>
    public void ClearAllMineVisuals()
    {
        foreach (var kvp in _mineObjects)
            if (kvp.Value != null) Destroy(kvp.Value);
        _mineObjects.Clear();
    }
}
