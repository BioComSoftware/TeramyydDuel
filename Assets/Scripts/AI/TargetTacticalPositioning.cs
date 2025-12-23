using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tactical positioning AI for the Target object. Maneuvers the Target to maximize
/// damage potential by prioritizing shots at crew on deck, damaged ship parts, and
/// critical systems (lift/engine). Maintains optimal firing position while moving.
/// 
/// Attach this script to the main Target GameObject (parent).
/// </summary>
public class TargetTacticalPositioning : MonoBehaviour
{
    [Header("Movement Control")]
    [Tooltip("Enable/disable tactical movement AI")]
    public bool enableMovement = true;

    [Tooltip("Maximum horizontal movement speed (m/s)")]
    public float maxHorizontalSpeed = 10f;

    [Tooltip("Maximum vertical movement speed (m/s)")]
    public float maxVerticalSpeed = 5f;

    [Tooltip("How quickly the Target can change direction (m/s²)")]
    public float acceleration = 3f;

    [Header("Target References")]
    [Tooltip("The ship to position against. Will auto-find 'Ship' if not assigned.")]
    public Transform targetShip;

    [Tooltip("Reference to the cannon for checking firing solutions")]
    public TargetCannonAim cannonAim;

    [Header("Tactical Priorities")]
    [Tooltip("Weight for positioning to hit crew members on deck (0-1)")]
    [Range(0f, 1f)]
    public float crewTargetingPriority = 0.4f;

    [Tooltip("Weight for positioning to hit already damaged parts (0-1)")]
    [Range(0f, 1f)]
    public float damagedPartsPriority = 0.3f;

    [Tooltip("Weight for positioning to hit critical systems - lift/engine (0-1)")]
    [Range(0f, 1f)]
    public float criticalSystemsPriority = 0.3f;

    [Header("Positioning Constraints")]
    [Tooltip("Minimum distance from ship to maintain (meters)")]
    public float minDistanceFromShip = 15f;

    [Tooltip("Maximum distance from ship to maintain (meters)")]
    public float maxDistanceFromShip = 50f;

    [Tooltip("Preferred distance for optimal firing (meters)")]
    public float optimalFiringDistance = 30f;

    [Tooltip("Minimum height above ship deck for clear shots (meters)")]
    public float minHeightAboveDeck = 5f;

    [Tooltip("Maximum height above ship deck (meters)")]
    public float maxHeightAboveDeck = 25f;

    [Header("Update Rates")]
    [Tooltip("How often to recalculate tactical position (seconds)")]
    public float tacticalUpdateInterval = 0.5f;

    [Tooltip("How often to scan for crew members and damaged parts (seconds)")]
    public float targetScanInterval = 1f;

    [Header("Debug")]
    [Tooltip("Enable debug visualization and logging")]
    public bool enableDebugLogging = false;

    [Tooltip("Show debug gizmos in Scene view")]
    public bool showDebugGizmos = true;

    // Internal state
    private Vector3 _currentVelocity = Vector3.zero;
    private Vector3 _targetPosition = Vector3.zero;
    private float _nextTacticalUpdate = 0f;
    private float _nextTargetScan = 0f;
    private Rigidbody _rigidbody;

    // Cached tactical data
    private List<Transform> _crewMembers = new List<Transform>();
    private List<Health> _damagedParts = new List<Health>();
    private Transform _deckTransform;
    private Transform _engineTransform;
    private Transform _liftDeviceTransform;
    private Vector3 _deckCenter;
    private Bounds _shipBounds;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.linearDamping = 0.5f;
            _rigidbody.angularDamping = 0.5f;
        }

        if (enableDebugLogging)
        {
            Debug.Log("[TargetTacticalPositioning] Initialized");
        }
    }

    void Start()
    {
        // Auto-find ship if not assigned
        if (targetShip == null)
        {
            GameObject shipObj = GameObject.Find("Ship");
            if (shipObj != null)
            {
                targetShip = shipObj.transform;
                if (enableDebugLogging)
                {
                    Debug.Log($"[TargetTacticalPositioning] Auto-found ship at {targetShip.position}");
                }
            }
            else
            {
                Debug.LogError("[TargetTacticalPositioning] Could not find 'Ship' GameObject!");
                enabled = false;
                return;
            }
        }

        // Auto-find cannon aim component if not assigned
        if (cannonAim == null)
        {
            cannonAim = GetComponent<TargetCannonAim>();
        }

        // Initialize target position to current position
        _targetPosition = transform.position;

        // Initial scan
        ScanShipForTargets();
        CalculateTacticalPosition();
    }

    void FixedUpdate()
    {
        if (!enableMovement || targetShip == null)
        {
            return;
        }

        // Periodic target scanning
        if (Time.time >= _nextTargetScan)
        {
            ScanShipForTargets();
            _nextTargetScan = Time.time + targetScanInterval;
        }

        // Periodic tactical position recalculation
        if (Time.time >= _nextTacticalUpdate)
        {
            CalculateTacticalPosition();
            _nextTacticalUpdate = Time.time + tacticalUpdateInterval;
        }

        // Move toward tactical position
        MoveTowardsTarget();
    }

    /// <summary>
    /// Scans the ship for crew members, damaged parts, and critical systems
    /// </summary>
    void ScanShipForTargets()
    {
        if (targetShip == null) return;

        // Find crew members on deck
        _crewMembers.Clear();
        CrewMember[] allCrew = targetShip.GetComponentsInChildren<CrewMember>();
        foreach (var crew in allCrew)
        {
            if (crew != null && crew.gameObject.activeInHierarchy)
            {
                _crewMembers.Add(crew.transform);
            }
        }

        // Find damaged ship parts
        _damagedParts.Clear();
        Health[] allHealthComponents = targetShip.GetComponentsInChildren<Health>();
        foreach (var health in allHealthComponents)
        {
            if (health != null && health.currentHealth < health.maxHealth)
            {
                _damagedParts.Add(health);
            }
        }

        // Find deck
        if (_deckTransform == null)
        {
            Transform deckSearch = targetShip.Find("Model/Deck");
            if (deckSearch != null)
            {
                _deckTransform = deckSearch;
            }
        }

        // Calculate deck center
        if (_deckTransform != null)
        {
            Renderer deckRenderer = _deckTransform.GetComponentInChildren<Renderer>();
            if (deckRenderer != null)
            {
                _deckCenter = deckRenderer.bounds.center;
            }
            else
            {
                _deckCenter = _deckTransform.position;
            }
        }
        else
        {
            _deckCenter = targetShip.position;
        }

        // Find critical systems
        if (_engineTransform == null)
        {
            Transform engineSearch = targetShip.Find("Model/Engine");
            if (engineSearch != null)
            {
                _engineTransform = engineSearch;
            }
        }

        if (_liftDeviceTransform == null)
        {
            Transform liftSearch = targetShip.Find("Model/LiftDevice");
            if (liftSearch != null)
            {
                _liftDeviceTransform = liftSearch;
            }
        }

        // Calculate ship bounds
        Renderer[] shipRenderers = targetShip.GetComponentsInChildren<Renderer>();
        if (shipRenderers.Length > 0)
        {
            _shipBounds = shipRenderers[0].bounds;
            for (int i = 1; i < shipRenderers.Length; i++)
            {
                _shipBounds.Encapsulate(shipRenderers[i].bounds);
            }
        }

        if (enableDebugLogging)
        {
            Debug.Log($"[TargetTacticalPositioning] Scan: {_crewMembers.Count} crew, {_damagedParts.Count} damaged parts, " +
                     $"Engine={_engineTransform != null}, Lift={_liftDeviceTransform != null}");
        }
    }

    /// <summary>
    /// Calculates the optimal tactical position based on weighted priorities
    /// </summary>
    void CalculateTacticalPosition()
    {
        if (targetShip == null) return;

        Vector3 idealPosition = transform.position;
        float totalWeight = 0f;
        Vector3 weightedSum = Vector3.zero;

        // Priority 1: Position to hit crew members on deck
        if (crewTargetingPriority > 0f && _crewMembers.Count > 0)
        {
            Vector3 crewCenter = CalculateCenterOfMass(_crewMembers);
            Vector3 crewTargetPos = CalculateFirePositionForTarget(crewCenter);
            weightedSum += crewTargetPos * crewTargetingPriority;
            totalWeight += crewTargetingPriority;

            if (enableDebugLogging)
            {
                Debug.Log($"[TargetTacticalPositioning] Crew center: {crewCenter}, Target pos: {crewTargetPos}");
            }
        }

        // Priority 2: Position to hit damaged parts
        if (damagedPartsPriority > 0f && _damagedParts.Count > 0)
        {
            // Find the most damaged part
            Health mostDamaged = _damagedParts.OrderBy(h => h.currentHealth / h.maxHealth).FirstOrDefault();
            if (mostDamaged != null)
            {
                Vector3 damagedPos = mostDamaged.transform.position;
                Vector3 damagedTargetPos = CalculateFirePositionForTarget(damagedPos);
                weightedSum += damagedTargetPos * damagedPartsPriority;
                totalWeight += damagedPartsPriority;

                if (enableDebugLogging)
                {
                    Debug.Log($"[TargetTacticalPositioning] Most damaged: {mostDamaged.name} ({mostDamaged.currentHealth}/{mostDamaged.maxHealth}), Target pos: {damagedTargetPos}");
                }
            }
        }

        // Priority 3: Position to hit critical systems
        if (criticalSystemsPriority > 0f)
        {
            Vector3 criticalCenter = Vector3.zero;
            int criticalCount = 0;

            if (_engineTransform != null)
            {
                criticalCenter += _engineTransform.position;
                criticalCount++;
            }

            if (_liftDeviceTransform != null)
            {
                criticalCenter += _liftDeviceTransform.position;
                criticalCount++;
            }

            if (criticalCount > 0)
            {
                criticalCenter /= criticalCount;
                Vector3 criticalTargetPos = CalculateFirePositionForTarget(criticalCenter);
                weightedSum += criticalTargetPos * criticalSystemsPriority;
                totalWeight += criticalSystemsPriority;

                if (enableDebugLogging)
                {
                    Debug.Log($"[TargetTacticalPositioning] Critical systems center: {criticalCenter}, Target pos: {criticalTargetPos}");
                }
            }
        }

        // Calculate weighted average position
        if (totalWeight > 0f)
        {
            idealPosition = weightedSum / totalWeight;
        }
        else
        {
            // Fallback: position to fire at deck center
            idealPosition = CalculateFirePositionForTarget(_deckCenter);
        }

        // Apply constraints
        idealPosition = ApplyPositionConstraints(idealPosition);

        _targetPosition = idealPosition;

        if (enableDebugLogging)
        {
            Debug.Log($"[TargetTacticalPositioning] New target position: {_targetPosition}, Distance: {Vector3.Distance(_targetPosition, targetShip.position):F1}m");
        }
    }

    /// <summary>
    /// Calculates a good firing position to hit a specific target point on the ship
    /// </summary>
    Vector3 CalculateFirePositionForTarget(Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0; // Project to horizontal plane

        // Position at optimal firing distance from the target point
        Vector3 direction = -toTarget.normalized;
        Vector3 horizontalPosition = targetPoint + direction * optimalFiringDistance;

        // Set height to fire down at the deck (favors hitting crew and deck-level targets)
        float heightAboveDeck = Mathf.Lerp(minHeightAboveDeck, maxHeightAboveDeck, 0.6f);
        horizontalPosition.y = targetPoint.y + heightAboveDeck;

        return horizontalPosition;
    }

    /// <summary>
    /// Applies distance and height constraints to the calculated position
    /// </summary>
    Vector3 ApplyPositionConstraints(Vector3 position)
    {
        Vector3 shipPos = targetShip.position;
        Vector3 toPosition = position - shipPos;
        
        // Constrain horizontal distance
        Vector3 horizontalOffset = new Vector3(toPosition.x, 0, toPosition.z);
        float distance = horizontalOffset.magnitude;

        if (distance < minDistanceFromShip)
        {
            horizontalOffset = horizontalOffset.normalized * minDistanceFromShip;
        }
        else if (distance > maxDistanceFromShip)
        {
            horizontalOffset = horizontalOffset.normalized * maxDistanceFromShip;
        }

        // Constrain vertical height
        float targetHeight = Mathf.Clamp(position.y, 
            _deckCenter.y + minHeightAboveDeck, 
            _deckCenter.y + maxHeightAboveDeck);

        return shipPos + horizontalOffset + Vector3.up * (targetHeight - shipPos.y);
    }

    /// <summary>
    /// Moves the Target toward the calculated tactical position
    /// </summary>
    void MoveTowardsTarget()
    {
        Vector3 currentPos = transform.position;
        Vector3 toTarget = _targetPosition - currentPos;

        // Separate horizontal and vertical movement
        Vector3 horizontalTarget = new Vector3(toTarget.x, 0, toTarget.z);
        float verticalTarget = toTarget.y;

        // Calculate desired velocity
        Vector3 desiredHorizontalVelocity = horizontalTarget.normalized * Mathf.Min(horizontalTarget.magnitude * acceleration, maxHorizontalSpeed);
        float desiredVerticalVelocity = Mathf.Clamp(verticalTarget * acceleration, -maxVerticalSpeed, maxVerticalSpeed);

        Vector3 desiredVelocity = desiredHorizontalVelocity + Vector3.up * desiredVerticalVelocity;

        // Smoothly adjust velocity
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, desiredVelocity, acceleration * Time.fixedDeltaTime);

        // Apply velocity to rigidbody
        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = _currentVelocity;
        }
        else
        {
            transform.position += _currentVelocity * Time.fixedDeltaTime;
        }
    }

    /// <summary>
    /// Calculates the center of mass for a list of transforms
    /// </summary>
    Vector3 CalculateCenterOfMass(List<Transform> transforms)
    {
        if (transforms.Count == 0) return targetShip.position;

        Vector3 sum = Vector3.zero;
        foreach (var t in transforms)
        {
            if (t != null)
            {
                sum += t.position;
            }
        }
        return sum / transforms.Count;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        // Draw target position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_targetPosition, 1f);
        Gizmos.DrawLine(transform.position, _targetPosition);

        // Draw crew positions
        Gizmos.color = Color.red;
        foreach (var crew in _crewMembers)
        {
            if (crew != null)
            {
                Gizmos.DrawWireSphere(crew.position, 0.5f);
            }
        }

        // Draw deck center
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_deckCenter, 1f);

        // Draw critical systems
        Gizmos.color = Color.magenta;
        if (_engineTransform != null)
        {
            Gizmos.DrawWireCube(_engineTransform.position, Vector3.one * 2f);
        }
        if (_liftDeviceTransform != null)
        {
            Gizmos.DrawWireCube(_liftDeviceTransform.position, Vector3.one * 2f);
        }

        // Draw distance constraints
        if (targetShip != null)
        {
            Gizmos.color = Color.green;
            DrawWireCircle(targetShip.position, minDistanceFromShip);
            Gizmos.color = Color.blue;
            DrawWireCircle(targetShip.position, maxDistanceFromShip);
            Gizmos.color = Color.yellow;
            DrawWireCircle(targetShip.position, optimalFiringDistance);
        }
    }

    void DrawWireCircle(Vector3 center, float radius, int segments = 32)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Quaternion.Euler(0, 0, 0) * Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            Vector3 newPoint = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    /// <summary>
    /// Public method to set movement enabled/disabled at runtime
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        enableMovement = enabled;
        if (!enabled && _rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _currentVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Gets the current tactical effectiveness score (0-1)
    /// </summary>
    public float GetTacticalEffectiveness()
    {
        float score = 0f;
        float totalWeight = crewTargetingPriority + damagedPartsPriority + criticalSystemsPriority;
        if (totalWeight == 0f) return 0f;

        // Check if position allows hitting crew
        if (crewTargetingPriority > 0f && _crewMembers.Count > 0)
        {
            // Simplified check - could be enhanced with actual line of sight
            score += crewTargetingPriority;
        }

        // Check distance from ship
        float distance = Vector3.Distance(transform.position, targetShip.position);
        float distanceScore = 1f - Mathf.Abs(distance - optimalFiringDistance) / maxDistanceFromShip;
        score *= Mathf.Clamp01(distanceScore);

        return score / totalWeight;
    }
}
