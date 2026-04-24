using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("=== CENTRO DEL TABLERO ===")]
    public bool autoDetectCenter = true;
    public float boardCenterX = -2.43f;
    public float boardCenterZ = -0.28f;

    [Header("=== VISTA (BARICENTRO) ===")]
    [Tooltip("Altura de la cámara sobre el tablero")]
    public float cameraHeight = 13f;
    [Tooltip("Distancia hacia ATRÁS desde el centro del tablero")]
    public float distanceBack = 10f;
    [Tooltip("Campo de visión (Zoom)")]
    public float fov = 60f;
    [Tooltip("Velocidad con la que la cámara gira para seguir a las fichas")]
    public float rotationSmoothSpeed = 3f;

    private Camera _cam;
    private PlayerData _currentPlayer;

    void Awake() 
    { 
        _cam = GetComponent<Camera>(); 
    }

    void Start()
    {
        if (autoDetectCenter) DetectBoardCenter();
        _cam.fieldOfView = fov;
        StartCoroutine(SubscribeNextFrame());
    }

    void DetectBoardCenter()
    {
        var bm = BoardManager.Instance;
        if (bm == null || bm.track == null || bm.track.Length == 0) return;
        Vector3 sum = Vector3.zero; int cnt = 0;
        foreach (var t in bm.track) { if (t != null) { sum += t.position; cnt++; } }
        if (cnt == 0) return;
        Vector3 center = sum / cnt;
        boardCenterX = center.x; boardCenterZ = center.z;
    }

    IEnumerator SubscribeNextFrame()
    {
        yield return null;
        var tm = TurnManager.Instance;
        if (tm != null) {
            tm.OnTurnStart += HandleTurnStart;
            if (tm.CurrentPlayer != null) HandleTurnStart(tm.CurrentPlayer);
        }
    }

    void OnDestroy() 
    { 
        if (TurnManager.Instance != null) TurnManager.Instance.OnTurnStart -= HandleTurnStart; 
    }

    void HandleTurnStart(PlayerData player)
    {
        if (player == null) return;
        _currentPlayer = player;
    }

    void LateUpdate()
    {
        if (_currentPlayer == null || _currentPlayer.Pieces == null || _currentPlayer.Pieces.Length == 0)
            return;

        Vector3 midpoint = GetMidpoint(_currentPlayer.Pieces);
        Vector3 center = new Vector3(boardCenterX, 0, boardCenterZ);

        // Vector de dirección desde el centro hacia el punto medio de las fichas
        Vector3 dirToMidpoint = midpoint - center;
        dirToMidpoint.y = 0; // Mantener en plano horizontal para los cálculos de posición

        if (dirToMidpoint.sqrMagnitude < 0.1f)
            dirToMidpoint = Vector3.forward; // fallback si están exactamente en el centro

        dirToMidpoint.Normalize();

        // Posición de la cámara: la movemos hacia ATRÁS desde el centro (eje opuesto al punto medio)
        Vector3 targetPos = center - (dirToMidpoint * distanceBack) + (Vector3.up * cameraHeight);
        transform.position = targetPos;

        // Rotación: mirar hacia el punto medio de las fichas
        // Como la cámara está detrás del centro, al mirar al midpoint verá el centro del tablero en el medio de la pantalla
        Quaternion targetRotation = Quaternion.LookRotation(midpoint - transform.position);

        // Interpolar suavemente la rotación (efecto orbital suave al moverse)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    Vector3 GetMidpoint(PieceController[] pieces)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (var p in pieces)
        {
            if (p != null)
            {
                sum += p.transform.position;
                count++;
            }
        }
        if (count == 0) return new Vector3(boardCenterX, 0, boardCenterZ);
        return sum / count;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("CameraFollow/Test Recenter", false)]
    static void MenuRecenter()
    {
        var cf = UnityEngine.Object.FindObjectOfType<CameraFollow>();
        if (cf != null) { cf.DetectBoardCenter(); }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = new Vector3(boardCenterX, 0f, boardCenterZ);
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); 
        Gizmos.DrawWireSphere(c, 1f);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.5f);
        UnityEditor.Handles.DrawWireDisc(c + Vector3.up * cameraHeight, Vector3.up, distanceBack);
        #endif
    }
#endif
}
