using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum OrbitTypes
{
    Planet, // Default self-rotation behavior
    Moon, // Always face the orbit center
    Comet // Face the direction of movement
}

[System.Serializable]
public class OrbitParameters
{
    [Header("Orbit Settings")]
    [Tooltip("Orbit center")]
    public Transform orbitCenter;
    
    [Tooltip("Semi-major axis (long radius of ellipse)")]
    public float semiMajorAxis = 1000f;
    
    [Tooltip("Semi-minor axis (short radius of ellipse)")]
    public float semiMinorAxis = 800f;
    
    [Tooltip("Orbital movement speed")]
    [Range(0f, 1f)]
    public float orbitSpeed = 1f;
    public float orbitSpeedCoefficient = 1f;
    
    [Tooltip("Orbital inclination angle")]
    [Range(0f, 360f)]
    public float orbitInclination = 0f;
    
    [Tooltip("Orbital rotation angle")]
    [Range(0f, 360f)]
    public float orbitRotation = 0f;

    [Header("Self Rotation Settings")]
    [Tooltip("Self Rotation Type:\n- Planet: Self rotate.\n- Moon: Face toward orbit center.\n- Comet: Face the direction of movement.")]
    public OrbitTypes orbitType = OrbitTypes.Planet;
    [Tooltip("Self Rotation speed")]
    [Range(0f, 360f)]
    public float rotationSpeed = 30f;
    
    [Tooltip("Self Rotation axis")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Orbit Visualization")]
    [Tooltip("Show orbit path line")]
    public bool showOrbitPath = true;
    
    [Tooltip("Orbit line segments")]
    [Range(32, 256)]
    public int orbitSegments = 64;
    
    [Tooltip("Orbit line color")]
    public Color orbitColor = Color.white;
    
    [Header("Constraint Settings")]
    [Tooltip("Initial angle on orbit (0-360 degrees)")]
    [Range(0f, 360f)]
    public float startingAngle = 0f;
}

public class Orbit_manual : MonoBehaviour
{
    [SerializeField] private OrbitParameters orbitParams = new OrbitParameters();
    
    // Public accessor for editor
    public OrbitParameters OrbitParams => orbitParams;
    
    [Header("Constraint Control")]
    [Tooltip("Enable orbit constraint")]
    public bool isActive = false;
    
    [Tooltip("Auto-move to starting position when activated")]
    public bool autoMoveToStart = true;
    
    private float currentOrbitAngle = 0f;
    [SerializeField] private float currentOrbitSpeed = 0f;
    private bool wasActiveLastFrame = false;
    
    void Start()
    {
        //storedPosition = transform.position;
        
        if (orbitParams.orbitCenter == null)
        {
            GameObject center = new GameObject("OrbitCenter");
            center.transform.position = Vector3.zero;
            orbitParams.orbitCenter = center.transform;
            Debug.LogWarning("Can not find the orbital center, created a default one.");
        }

        // Initialize based on activation state
        if (isActive)
        {
            ActivateOrbit();
        }
        else
        {
            DeactivateOrbit();
        }
        
        if(autoMoveToStart && isActive)
        {
            MoveToStartingPosition();
            StartOrbitSpeed();
        }
    }
    
    void Update()
    {
        // Check for activation state change
        if (isActive != wasActiveLastFrame)
        {
            if (isActive)
            {
                ActivateOrbit();
            }
            else
            {
                DeactivateOrbit();
            }
            wasActiveLastFrame = isActive;
        }
        
        // Only update orbit if active and orbit center exists
        if (!isActive || orbitParams.orbitCenter == null)
        {
            return;
        }
        
        UpdateOrbit();
        UpdateRotation();
    }
    
    void UpdateOrbit()
    {
        currentOrbitAngle += currentOrbitSpeed * orbitParams.orbitSpeedCoefficient * Time.deltaTime;

        float x = orbitParams.semiMajorAxis * Mathf.Cos(currentOrbitAngle);
        float z = orbitParams.semiMinorAxis * Mathf.Sin(currentOrbitAngle);

        Vector3 orbitPosition = new Vector3(x, 0, z);
        
        if (orbitParams.orbitInclination != 0f)
        {
            orbitPosition = Quaternion.AngleAxis(orbitParams.orbitInclination, Vector3.forward) * orbitPosition;
        }
        
        if (orbitParams.orbitRotation != 0f)
        {
            orbitPosition = Quaternion.AngleAxis(orbitParams.orbitRotation, Vector3.up) * orbitPosition;
        }

        transform.position = orbitParams.orbitCenter.position + orbitPosition;
    }
    
    void UpdateRotation()
    {
        if (orbitParams.orbitType == OrbitTypes.Moon)
        {
            Vector3 directionToCenter = (orbitParams.orbitCenter.position - transform.position).normalized;
            transform.forward = directionToCenter;
        }
        else if (orbitParams.orbitType == OrbitTypes.Comet)
        {
            float tangentX = -orbitParams.semiMajorAxis * Mathf.Sin(currentOrbitAngle);
            float tangentZ = orbitParams.semiMinorAxis * Mathf.Cos(currentOrbitAngle);
            
            Vector3 tangentDirection = new Vector3(tangentX, 0, tangentZ).normalized;
            
            // Apply orbit inclination and rotation to the tangent direction
            if (orbitParams.orbitInclination != 0f)
            {
                tangentDirection = Quaternion.AngleAxis(orbitParams.orbitInclination, Vector3.forward) * tangentDirection;
            }
            
            if (orbitParams.orbitRotation != 0f)
            {
                tangentDirection = Quaternion.AngleAxis(orbitParams.orbitRotation, Vector3.up) * tangentDirection;
            }
            
            // Account for clockwise rotation (negative speed)
            if (currentOrbitSpeed < 0f)
            {
                tangentDirection = -tangentDirection;
            }
            
            // Set comet to face the direction of movement
            if (tangentDirection.magnitude > 0.001f)
            {
                transform.forward = tangentDirection;
            }
        }
        else
        {
            if (orbitParams.rotationSpeed > 0f)
            {
                transform.Rotate(orbitParams.rotationAxis.normalized * orbitParams.rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (orbitParams.orbitCenter == null) return;
        
        if (orbitParams.showOrbitPath)
        {
            DrawOrbitPath();
        }
    }
    
    void DrawOrbitPath()
    {
        Gizmos.color = orbitParams.orbitColor;
        
        Vector3 orbitCenterPosition = orbitParams.orbitCenter.position;
        Vector3 previousPoint = Vector3.zero;
        
        // Draw orbit path
        for (int i = 0; i <= orbitParams.orbitSegments; i++)
        {
            float angle = (float)i / orbitParams.orbitSegments * 2f * Mathf.PI;
            
            float x = orbitParams.semiMajorAxis * Mathf.Cos(angle);
            float z = orbitParams.semiMinorAxis * Mathf.Sin(angle);
            
            Vector3 orbitPoint = new Vector3(x, 0, z);
            
            if (orbitParams.orbitInclination != 0f)
            {
                orbitPoint = Quaternion.AngleAxis(orbitParams.orbitInclination, Vector3.forward) * orbitPoint;
            }
            
            if (orbitParams.orbitRotation != 0f)
            {
                orbitPoint = Quaternion.AngleAxis(orbitParams.orbitRotation, Vector3.up) * orbitPoint;
            }

            Vector3 worldPoint = orbitCenterPosition + orbitPoint;

            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, worldPoint);
            }
            
            previousPoint = worldPoint;
        }
        
        // Draw orbit center
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(orbitCenterPosition, 0.5f);
        
        // Draw current object position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Draw starting position marker
        Vector3 startingPosition = CalculateOrbitPosition(orbitParams.startingAngle * Mathf.Deg2Rad);
        Vector3 startingWorldPos = orbitCenterPosition + startingPosition;
        
        // Draw starting position as a green sphere
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startingWorldPos, 0.4f);
        
        // Draw a line from center to starting position
        Gizmos.color = Color.green;
        Gizmos.DrawLine(orbitCenterPosition, startingWorldPos);
        
        // Draw direction arrow for starting position
        Vector3 directionFromCenter = (startingWorldPos - orbitCenterPosition).normalized;
        Vector3 arrowTip = startingWorldPos + directionFromCenter * 0.8f;
        Gizmos.DrawLine(startingWorldPos, arrowTip);
    }
    
    public void SetOrbitParameters(Transform newOrbitCenter, float majorAxis, float minorAxis, float speed)
    {
        orbitParams.orbitCenter = newOrbitCenter;
        orbitParams.semiMajorAxis = majorAxis;
        orbitParams.semiMinorAxis = minorAxis;
        orbitParams.orbitSpeed = speed;
    }

    public void SetOrbitSpeed(float speed)
    {
        currentOrbitSpeed = speed;
    }

    public void StartOrbitSpeed()
    {
        if (currentOrbitSpeed == 0f)
        {
            currentOrbitSpeed = orbitParams.orbitSpeed;
        }
    }
    
    public float GetCurrentAngle()
    {
        return currentOrbitAngle * Mathf.Rad2Deg;
    }
    
    public void SetCurrentAngle(float angleDegrees)
    {
        currentOrbitAngle = angleDegrees * Mathf.Deg2Rad;
    }
    
    public void UpdateStartingAngle()
    {
        if (orbitParams.orbitCenter == null) return;
        
        // Update current angle to match starting angle
        currentOrbitAngle = orbitParams.startingAngle * Mathf.Deg2Rad;
        
        // Move object to the new starting position
        if (isActive && autoMoveToStart)
        {
            MoveToStartingPosition();
        }
    }
    
    [ContextMenu("Activate Orbit")]
    public void ActivateOrbit()
    {
        if (orbitParams.orbitCenter == null) return;
        
        isActive = true;
        
        currentOrbitAngle = orbitParams.startingAngle * Mathf.Deg2Rad;
        
        if (autoMoveToStart)
        {
            MoveToStartingPosition();
        }
        
        Debug.Log($"Orbit activated for {gameObject.name} at angle {orbitParams.startingAngle} degrees");
    }
    
    [ContextMenu("Deactivate Orbit")]
    public void DeactivateOrbit()
    {
        isActive = false;
        Debug.Log($"Orbit deactivated for {gameObject.name}");
    }
    
    public void MoveToStartingPosition()
    {
        if (orbitParams.orbitCenter == null) return;
        
        // Calculate position based on the starting angle, not current angle
        float startingAngleRad = orbitParams.startingAngle * Mathf.Deg2Rad;
        Vector3 startPosition = CalculateOrbitPosition(startingAngleRad);
        transform.position = orbitParams.orbitCenter.position + startPosition;
        
        // Update current angle to match the starting angle
        currentOrbitAngle = startingAngleRad;
    }
    
    private Vector3 CalculateOrbitPosition(float angle)
    {
        float x = orbitParams.semiMajorAxis * Mathf.Cos(angle);
        float z = orbitParams.semiMinorAxis * Mathf.Sin(angle);
        
        Vector3 orbitPosition = new Vector3(x, 0, z);
        
        if (orbitParams.orbitInclination != 0f)
        {
            orbitPosition = Quaternion.AngleAxis(orbitParams.orbitInclination, Vector3.forward) * orbitPosition;
        }
        
        if (orbitParams.orbitRotation != 0f)
        {
            orbitPosition = Quaternion.AngleAxis(orbitParams.orbitRotation, Vector3.up) * orbitPosition;
        }
        
        return orbitPosition;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Orbit_manual))]
public class OrbitEditor_manual : Editor
{
    private float lastStartingAngle = 0f;
    
    public override void OnInspectorGUI()
    {
        Orbit_manual orbit = (Orbit_manual)target;
        
        // Store the current starting angle
        lastStartingAngle = orbit.OrbitParams.startingAngle;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        // Check if starting angle changed
        if (Mathf.Abs(orbit.OrbitParams.startingAngle - lastStartingAngle) > 0.01f)
        {
            orbit.UpdateStartingAngle();
            SceneView.RepaintAll(); // Refresh the scene view to update gizmos
        }
        
        EditorGUILayout.Space();
        
        // Constraint control buttons
        EditorGUILayout.LabelField("Orbit Constraint Control", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = orbit.isActive ? Color.red : Color.green;
        if (GUILayout.Button(orbit.isActive ? "Deactivate" : "Activate", GUILayout.Height(30), GUILayout.Width(150)))
        {
            if (orbit.isActive)
            {
                orbit.DeactivateOrbit();
            }
            else
            {
                orbit.ActivateOrbit();
            }
        }
        GUI.backgroundColor = Color.white;
        
        if (GUILayout.Button("Move to Start", GUILayout.Height(30), GUILayout.Width(150)))
        {
            orbit.MoveToStartingPosition();
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Debug Movement Control", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Start Orbit Moving", GUILayout.Height(30), GUILayout.Width(150)))
        {
            orbit.StartOrbitSpeed();
        }

        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Pause Orbit Moving", GUILayout.Height(30), GUILayout.Width(150)))
        {
            orbit.SetOrbitSpeed(0f);
        }
        EditorGUILayout.EndHorizontal();
        
        // Status info
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Status: {(orbit.isActive ? "Active" : "Inactive")}");
        EditorGUILayout.LabelField($"Current Angle: {orbit.GetCurrentAngle():F1}°");
        EditorGUILayout.LabelField($"Starting Angle: {orbit.OrbitParams.startingAngle:F1}°");
    }
}
#endif
