using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// PieceController — Movimiento por casillas con animación de salto.
/// 
/// REGLA DE ORO: transform.localScale NUNCA se modifica.
/// La animación de salto usa SOLO transform.position (arco en Y).
/// Esto garantiza que la ficha siempre sea visible durante el movimiento.
/// </summary>
public class PieceController : MonoBehaviour
{
    // ── Identidad ─────────────────────────────────
    [Header("=== IDENTIDAD ===")]
    public PlayerColor teamColor;
    [Range(0,3)] public int pieceIndex;

    // ── Animación ─────────────────────────────────
    [Header("=== ANIMACIÓN ===")]
    public float jumpHeight        = 1.0f;   // altura del arco de salto
    public float jumpDuration      = 0.20f;  // duración de cada casilla
    public float pauseBetweenJumps = 0.03f;  // pausa entre casillas

    [Header("=== AJUSTE VISUAL ===")]
    [Tooltip("Sube o baja la ficha para que pise exactamente la casilla")]
    public float yOffset = 0.5f; 

    // ── Estado ────────────────────────────────────
    [Header("=== ESTADO ===")]
    [SerializeField] private bool _isAtHome   = true;
    [SerializeField] private int  _laps       = 0;
    [SerializeField] private int  _trackIndex = 0;

    public bool IsAtHome   => _isAtHome;
    public int  Laps       => _laps;
    public int  TrackIndex => _trackIndex;

    private bool _isMoving = false;

    // ── Eventos ───────────────────────────────────
    public event Action<int> OnLapCompleted;
    public event Action      OnMoveComplete;

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────
    public bool IsMoving() => _isMoving;

    public void LeaveHome()
    {
        if (!_isAtHome || _isMoving) return;
        _isAtHome   = false;
        _trackIndex = BoardManager.Instance.GetStartIndex(teamColor);
        var target  = BoardManager.Instance.GetSquare(_trackIndex);
        if (target != null)
            StartCoroutine(LeaveHomeRoutine(target.position));
    }

    private IEnumerator LeaveHomeRoutine(Vector3 targetPos)
    {
        _isMoving = true;
        yield return StartCoroutine(JumpTo(transform.position, targetPos, null));
        _isMoving = false;
        OnMoveComplete?.Invoke();
    }

    /// <summary>Mueve N casillas hacia adelante.</summary>
    public void Move(int steps)
    {
        if (_isAtHome || _isMoving) return;
        StartCoroutine(MoveRoutine(steps));
    }

    /// <summary>Regresa la ficha a casa (al ser capturada).</summary>
    public void ResetToHome()
    {
        StopAllCoroutines();
        _isMoving   = false;
        _isAtHome   = true;
        _trackIndex = 0;
    }

    // ─────────────────────────────────────────────
    // MOVIMIENTO
    // ─────────────────────────────────────────────
    private IEnumerator MoveRoutine(int steps)
    {
        _isMoving = true;
        int total = BoardManager.Instance.TrackLength;

        for (int i = 0; i < steps; i++)
        {
            int next   = _trackIndex + 1;
            var nextSq = BoardManager.Instance.GetSquare(next);
            if (nextSq == null) break;

            bool lap = (next % total) == BoardManager.Instance.GetStartIndex(teamColor);

            yield return StartCoroutine(JumpTo(transform.position, nextSq.position, () =>
            {
                _trackIndex = next;
                if (lap) { _laps++; OnLapCompleted?.Invoke(_laps); }
            }));

            if (i < steps - 1)
                yield return new WaitForSeconds(pauseBetweenJumps);
        }

        _isMoving = false;
        OnMoveComplete?.Invoke();
    }

    // ─────────────────────────────────────────────
    // ANIMACIÓN DE SALTO — SOLO POSICIÓN, SIN ESCALA
    // ─────────────────────────────────────────────
    private IEnumerator JumpTo(Vector3 from, Vector3 to, Action onArrived)
    {
        // Aplicar el offset vertical para que pise la casilla correctamente
        to.y += yOffset;

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            // Posición: interpolación horizontal + arco vertical
            // transform.localScale NO SE TOCA
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            yield return null;
        }

        // Posición exacta al llegar
        transform.position = to;
        onArrived?.Invoke();
    }

    // Expandir bounds del MeshRenderer durante el movimiento
    // para evitar que el frustum culling oculte la ficha al saltar
    private MeshRenderer _mr;
    private void OnEnable() { _mr = GetComponent<MeshRenderer>(); }
    private void LateUpdate()
    {
        if (_mr == null || !_isMoving) return;
        _mr.bounds = new UnityEngine.Bounds(
            transform.position + Vector3.up,
            new Vector3(1f, 4f, 1f));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEngine.Color c =
            teamColor == PlayerColor.Yellow ? UnityEngine.Color.yellow :
            teamColor == PlayerColor.Green  ? UnityEngine.Color.green  :
            teamColor == PlayerColor.Red    ? UnityEngine.Color.red    :
                                              UnityEngine.Color.cyan;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.18f);
    }
#endif
}
