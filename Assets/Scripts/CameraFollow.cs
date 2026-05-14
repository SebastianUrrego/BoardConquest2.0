using System.Collections;
using UnityEngine;

/// <summary>
/// CameraFollow — cámara orbital que sigue al equipo activo.
///
/// MODOS:
///   BOARD  : vista general del equipo activo (comportamiento anterior).
///   ZOOM   : zoom suave sobre la ficha seleccionada al presionar 1-4.
///   RETURN : transición suave de vuelta a BOARD al terminar el movimiento.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    // ── Vista general ────────────────────────────
    [Header("=== VISTA GENERAL ===")]
    public bool  autoDetectCenter  = true;
    public float boardCenterX      = -2.43f;
    public float boardCenterZ      = -0.28f;
    public float cameraHeight      = 13f;
    public float distanceBack      = 10f;
    public float fov               = 60f;
    public float boardSmoothSpeed  = 3f;   // suavidad rotación orbital

    // ── Zoom sobre ficha ─────────────────────────
    [Header("=== ZOOM EN FICHA ===")]
    [Tooltip("Altura de la cámara al hacer zoom en la ficha")]
    public float zoomHeight        = 4.5f;
    [Tooltip("Distancia detrás de la ficha al hacer zoom")]
    public float zoomDistance      = 4f;
    [Tooltip("FOV durante el zoom (más pequeño = más cerca)")]
    public float zoomFOV           = 40f;
    [Tooltip("Duración de la transición de zoom (segundos)")]
    public float zoomTransition    = 0.6f;
    [Tooltip("Duración de la transición de vuelta al tablero")]
    public float returnTransition  = 0.8f;

    // ── Internos ─────────────────────────────────
    private Camera         _cam;
    private PlayerData     _currentPlayer;
    private PieceController _zoomTarget;     // ficha a enfocar (null = modo tablero)

    private enum CamMode { Board, ZoomingIn, ZoomedIn, ZoomingOut }
    private CamMode _mode = CamMode.Board;

    // Snapshots para interpolar
    private Vector3    _snapPos;
    private Quaternion _snapRot;
    private float      _snapFOV;
    private float      _transTimer;
    private float      _transDuration;

    // ─────────────────────────────────────────────
    void Awake()  { _cam = GetComponent<Camera>(); }

    void Start()
    {
        if (autoDetectCenter) DetectBoardCenter();
        _cam.fieldOfView = fov;
        StartCoroutine(SubscribeNextFrame());
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStart        -= HandleTurnStart;
            TurnManager.Instance.OnPieceHighlighted -= HandlePieceHighlighted;
        }
    }

    IEnumerator SubscribeNextFrame()
    {
        yield return null;
        var tm = TurnManager.Instance;
        if (tm == null) yield break;
        tm.OnTurnStart        += HandleTurnStart;
        tm.OnPieceHighlighted += HandlePieceHighlighted;
        if (tm.CurrentPlayer != null) HandleTurnStart(tm.CurrentPlayer);
    }

    // ── Callbacks ────────────────────────────────

    void HandleTurnStart(PlayerData player)
    {
        _currentPlayer = player;
        // Al cambiar de turno volver a vista de tablero
        StartZoomOut();
    }

    void HandlePieceHighlighted(PieceController piece)
    {
        if (piece != null)
        {
            _zoomTarget = piece;
            StartZoomIn();
        }
        else
        {
            // Ficha terminó de moverse → volver al tablero
            StartZoomOut();
        }
    }

    // ── Iniciar transiciones ──────────────────────

    void StartZoomIn()
    {
        if (_mode == CamMode.ZoomedIn || _mode == CamMode.ZoomingIn) return;
        _snapPos      = transform.position;
        _snapRot      = transform.rotation;
        _snapFOV      = _cam.fieldOfView;
        _transTimer   = 0f;
        _transDuration = zoomTransition;
        _mode         = CamMode.ZoomingIn;
    }

    void StartZoomOut()
    {
        if (_mode == CamMode.Board || _mode == CamMode.ZoomingOut) return;
        _snapPos      = transform.position;
        _snapRot      = transform.rotation;
        _snapFOV      = _cam.fieldOfView;
        _transTimer   = 0f;
        _transDuration = returnTransition;
        _mode         = CamMode.ZoomingOut;
        _zoomTarget   = null;
    }

    // ── Update ────────────────────────────────────

    void LateUpdate()
    {
        switch (_mode)
        {
            case CamMode.Board:      UpdateBoard();      break;
            case CamMode.ZoomingIn:  UpdateZoomIn();     break;
            case CamMode.ZoomedIn:   UpdateZoomedIn();   break;
            case CamMode.ZoomingOut: UpdateZoomOut();    break;
        }
    }

    // ── MODO TABLERO: orbital suave ───────────────
    void UpdateBoard()
    {
        PlayerData p = GetActivePlayerForCamera();
        if (p == null) return;

        Vector3 midpoint = GetMidpoint(p.Pieces);
        Vector3 center   = new Vector3(boardCenterX, 0, boardCenterZ);
        Vector3 dir      = (midpoint - center);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.1f) dir = Vector3.forward;
        dir.Normalize();

        Vector3    targetPos = center - dir * distanceBack + Vector3.up * cameraHeight;
        Quaternion targetRot = Quaternion.LookRotation(midpoint - targetPos);

        transform.position = targetPos;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                              Time.deltaTime * boardSmoothSpeed);
        _cam.fieldOfView   = Mathf.Lerp(_cam.fieldOfView, fov, Time.deltaTime * 5f);
    }

    // ── ZOOM ENTRANDO ─────────────────────────────
    void UpdateZoomIn()
    {
        _transTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_transTimer / _transDuration));

        Vector3    targetPos = CalcZoomPos();
        Quaternion targetRot = CalcZoomRot(targetPos);

        transform.position = Vector3.Lerp(_snapPos, targetPos, t);
        transform.rotation = Quaternion.Slerp(_snapRot, targetRot, t);
        _cam.fieldOfView   = Mathf.Lerp(_snapFOV, zoomFOV, t);

        if (t >= 1f) _mode = CamMode.ZoomedIn;
    }

    // ── ZOOM ACTIVO: sigue la ficha en tiempo real ─
    void UpdateZoomedIn()
    {
        if (_zoomTarget == null) { StartZoomOut(); return; }
        Vector3    targetPos = CalcZoomPos();
        Quaternion targetRot = CalcZoomRot(targetPos);

        // Seguimiento suave durante el movimiento
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 8f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
    }

    // ── ZOOM SALIENDO ─────────────────────────────
    void UpdateZoomOut()
    {
        _transTimer += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_transTimer / _transDuration));

        Vector3    targetPos = CalcBoardPos();
        Quaternion targetRot = CalcBoardRot(targetPos);

        transform.position = Vector3.Lerp(_snapPos, targetPos, t);
        transform.rotation = Quaternion.Slerp(_snapRot, targetRot, t);
        _cam.fieldOfView   = Mathf.Lerp(_snapFOV, fov, t);

        if (t >= 1f) _mode = CamMode.Board;
    }

    // ── Helpers de posición ───────────────────────

    PlayerData GetActivePlayerForCamera()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.InitialTurnPlayer != null)
            return TurnManager.Instance.InitialTurnPlayer;
        return _currentPlayer;
    }

    /// Posición de zoom: detrás y arriba de la ficha
    Vector3 CalcZoomPos()
    {
        if (_zoomTarget == null) return transform.position;
        Vector3 piecePos = _zoomTarget.transform.position;
        Vector3 center   = new Vector3(boardCenterX, 0, boardCenterZ);
        // Dirección desde el centro hacia la ficha
        Vector3 dir = (piecePos - center);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.1f) dir = Vector3.forward;
        dir.Normalize();
        return piecePos + dir * zoomDistance + Vector3.up * zoomHeight;
    }

    Quaternion CalcZoomRot(Vector3 fromPos)
    {
        if (_zoomTarget == null) return transform.rotation;
        Vector3 lookAt = _zoomTarget.transform.position + Vector3.up * 0.5f;
        return Quaternion.LookRotation(lookAt - fromPos);
    }

    Vector3 CalcBoardPos()
    {
        PlayerData p = GetActivePlayerForCamera();
        if (p == null) return transform.position;
        Vector3 midpoint = GetMidpoint(p.Pieces);
        Vector3 center   = new Vector3(boardCenterX, 0, boardCenterZ);
        Vector3 dir = (midpoint - center); dir.y = 0;
        if (dir.sqrMagnitude < 0.1f) dir = Vector3.forward;
        dir.Normalize();
        return center - dir * distanceBack + Vector3.up * cameraHeight;
    }

    Quaternion CalcBoardRot(Vector3 fromPos)
    {
        PlayerData p = GetActivePlayerForCamera();
        Vector3 midpoint = p != null
            ? GetMidpoint(p.Pieces)
            : new Vector3(boardCenterX, 0, boardCenterZ);
        return Quaternion.LookRotation(midpoint - fromPos);
    }

    Vector3 GetMidpoint(PieceController[] pieces)
    {
        Vector3 sum = Vector3.zero; int count = 0;
        foreach (var p in pieces)
            if (p != null) { sum += p.transform.position; count++; }
        return count == 0 ? new Vector3(boardCenterX, 0, boardCenterZ) : sum / count;
    }

    void DetectBoardCenter()
    {
        var bm = BoardManager.Instance;
        if (bm == null || bm.track == null || bm.track.Length == 0) return;
        Vector3 sum = Vector3.zero; int cnt = 0;
        foreach (var t in bm.track) { if (t != null) { sum += t.position; cnt++; } }
        if (cnt == 0) return;
        boardCenterX = (sum / cnt).x;
        boardCenterZ = (sum / cnt).z;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 c = new Vector3(boardCenterX, 0f, boardCenterZ);
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(c, 1f);
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.4f);
        UnityEditor.Handles.DrawWireDisc(c + Vector3.up * cameraHeight, Vector3.up, distanceBack);
    }
#endif
}
