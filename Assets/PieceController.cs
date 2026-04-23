using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// PieceController - Movimiento circular con animación de salto.
/// El Squash y Stretch se aplica al hijo que tiene el mesh,
/// evitando parpadeos cuando la ficha tiene estructura padre/hijo.
/// </summary>
public class PieceController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // IDENTIDAD
    // ─────────────────────────────────────────────
    [Header("=== IDENTIDAD ===")]
    public PlayerColor teamColor;
    [Range(0, 3)] public int pieceIndex;

    // ─────────────────────────────────────────────
    // REFERENCIA AL MESH
    // ─────────────────────────────────────────────
    [Header("=== MESH DEL MODELO ===")]
    [Tooltip("Arrastra aquí el hijo que tiene el MeshRenderer (el modelo visible)")]
    public Transform meshTransform;

    // ─────────────────────────────────────────────
    // ANIMACIÓN
    // ─────────────────────────────────────────────
    [Header("=== ANIMACIÓN DE SALTO ===")]
    public float jumpHeight     = 1.5f;
    public float jumpDuration   = 0.22f;
    public float pauseBetweenJumps = 0.04f;
    public bool  faceDirection  = true;

    [Header("=== REBOTE AL ATERRIZAR ===")]
    public bool  landingBounce   = true;
    public float bounceMagnitude = 0.18f;
    public float bounceDuration  = 0.09f;

    // ─────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────
    [Header("=== ESTADO (solo lectura) ===")]
    [SerializeField] private bool _isAtHome   = true;
    [SerializeField] private int  _laps       = 0;
    [SerializeField] private int  _points     = 0;
    [SerializeField] private int  _trackIndex = 0;

    public bool IsAtHome   => _isAtHome;
    public int  Laps       => _laps;
    public int  Points     => _points;
    public int  TrackIndex => _trackIndex;

    public int pointsPerLap = 1;

    private bool    _isMoving  = false;
    private Vector3 _meshBaseScale;   // escala original del MESH (no del padre)

    // ─────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────
    public event Action<int> OnLapCompleted;
    public event Action       OnMoveComplete;

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // Si no se asignó el mesh en el Inspector, busca el primer hijo con MeshRenderer
        if (meshTransform == null)
        {
            MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
            if (mr != null)
                meshTransform = mr.transform;
            else
                meshTransform = transform; // fallback: usa el padre
        }

        // Guarda la escala ORIGINAL del mesh
        _meshBaseScale = meshTransform.localScale;
    }

    // ─────────────────────────────────────────────
    // API PÚBLICA
    // ─────────────────────────────────────────────
    public void LeaveHome()
    {
        if (!_isAtHome || _isMoving) return;
        _isAtHome   = false;
        _trackIndex = BoardManager.Instance.GetStartIndex(teamColor);

        Transform target = BoardManager.Instance.GetSquare(_trackIndex);
        if (target != null)
            StartCoroutine(JumpTo(transform.position, target.position, null));
    }

    public void Move(int steps)
    {
        if (_isAtHome || _isMoving) return;
        StartCoroutine(MoveRoutine(steps));
    }

    public bool IsMoving() => _isMoving;

    // ─────────────────────────────────────────────
    // MOVIMIENTO CIRCULAR
    // ─────────────────────────────────────────────
    private IEnumerator MoveRoutine(int steps)
    {
        _isMoving = true;
        int total = BoardManager.Instance.TrackLength;

        for (int i = 0; i < steps; i++)
        {
            int nextAbsolute = _trackIndex + 1;
            Transform nextSquare = BoardManager.Instance.GetSquare(nextAbsolute);
            if (nextSquare == null) break;

            bool completesLap = (nextAbsolute % total) == BoardManager.Instance.GetStartIndex(teamColor);

            yield return StartCoroutine(JumpTo(transform.position, nextSquare.position, () =>
            {
                _trackIndex = nextAbsolute;

                if (completesLap)
                {
                    _laps++;
                    _points += pointsPerLap;
                    OnLapCompleted?.Invoke(_laps);
                    Debug.Log($"[{teamColor} ficha {pieceIndex}] Vuelta {_laps} — Puntos: {_points}");
                }
            }));

            yield return new WaitForSeconds(pauseBetweenJumps);
        }

        _isMoving = false;
        OnMoveComplete?.Invoke();

        if (landingBounce)
            yield return StartCoroutine(LandingBounce());
    }

    // ─────────────────────────────────────────────
    // ANIMACIÓN DE SALTO
    // El padre (transform) se mueve por el tablero.
    // El hijo (meshTransform) hace el Squash & Stretch.
    // Así nunca se pierde el mesh ni parpadea.
    // ─────────────────────────────────────────────
    private IEnumerator JumpTo(Vector3 from, Vector3 to, Action onArrived)
    {
        float elapsed = 0f;

        // Rotar el PADRE hacia el destino
        if (faceDirection)
        {
            Vector3 dir = to - from;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // Asegura que el mesh esté en su escala base al inicio del salto
        meshTransform.localScale = _meshBaseScale;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            // Mover el PADRE horizontalmente + arco vertical
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = pos;

            // Squash & Stretch solo en el MESH hijo
            float stretch = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            float squash  = 1f - Mathf.Sin(t * Mathf.PI) * 0.12f;
            meshTransform.localScale = new Vector3(
                _meshBaseScale.x * squash,
                _meshBaseScale.y * stretch,
                _meshBaseScale.z * squash
            );

            yield return null;
        }

        // Posición y escala exactas al llegar
        transform.position       = to;
        meshTransform.localScale = _meshBaseScale;  // ← restaura escala del mesh

        onArrived?.Invoke();
    }

    // ─────────────────────────────────────────────
    // REBOTE AL ATERRIZAR
    // ─────────────────────────────────────────────
    private IEnumerator LandingBounce()
    {
        // Aplaste
        float t = 0f;
        while (t < bounceDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / bounceDuration);
            meshTransform.localScale = new Vector3(
                _meshBaseScale.x * (1f + bounceMagnitude * n),
                _meshBaseScale.y * (1f - bounceMagnitude * n),
                _meshBaseScale.z * (1f + bounceMagnitude * n)
            );
            yield return null;
        }

        // Recuperación
        t = 0f;
        Vector3 squashedScale = new Vector3(
            _meshBaseScale.x * (1f + bounceMagnitude),
            _meshBaseScale.y * (1f - bounceMagnitude),
            _meshBaseScale.z * (1f + bounceMagnitude)
        );
        while (t < bounceDuration)
        {
            t += Time.deltaTime;
            meshTransform.localScale = Vector3.Lerp(
                squashedScale, _meshBaseScale,
                Mathf.Clamp01(t / bounceDuration)
            );
            yield return null;
        }

        // Escala final exacta
        meshTransform.localScale = _meshBaseScale;
    }

    // ─────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────
    /// <summary>Regresa la ficha a casa al ser capturada.</summary>
    public void ResetToHome()
    {
        StopAllCoroutines();
        _isMoving   = false;
        _isAtHome   = true;
        _trackIndex = 0;
        if (meshTransform != null) meshTransform.localScale = _meshBaseScale;
        Debug.Log($"[PieceController] {teamColor} ficha {pieceIndex} enviada a casa.");
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Color c = teamColor == PlayerColor.Yellow ? Color.yellow :
                  teamColor == PlayerColor.Green  ? Color.green  :
                  teamColor == PlayerColor.Red    ? Color.red    : Color.cyan;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.18f);
    }
#endif
}