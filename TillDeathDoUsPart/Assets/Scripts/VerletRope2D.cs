using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
public class VerletRope2D : NetworkBehaviour
{
    public class VerletPoint
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public bool isPinned;

        public VerletPoint(Vector2 pos, bool pinned = false)
        {
            position = pos;
            oldPosition = pos;
            isPinned = pinned;
        }

        public void Update(Vector2 gravity, float friction)
        {
            if (isPinned) return;

            Vector2 velocity = (position - oldPosition) * friction;
            oldPosition = position;
            position += velocity + gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
        }
    }

    public class VerletConstraint
    {
        public VerletPoint p1;
        public VerletPoint p2;
        public float length;

        public VerletConstraint(VerletPoint point1, VerletPoint point2, float targetLength)
        {
            p1 = point1;
            p2 = point2;
            length = targetLength;
        }

        public void Resolve()
        {
            Vector2 delta = p2.position - p1.position;
            float distance = delta.magnitude;

            if (distance == 0) return;

            float difference = (distance - length) / distance;

            if (p1.isPinned && !p2.isPinned)
            {
                p2.position -= delta * difference;
            }
            else if (!p1.isPinned && p2.isPinned)
            {
                p1.position += delta * difference;
            }
            else if (!p1.isPinned && !p2.isPinned)
            {
                Vector2 correction = delta * difference * 0.5f;
                p1.position += correction;
                p2.position -= correction;
            }
        }
    }

    [SerializeField] private int segmentCount = 30;
    [SerializeField] private float segmentLength = 0.2f;
    [SerializeField] private Vector2 gravity = new Vector2(0, -9.81f);
    [Range(0.9f, 1f)]
    [SerializeField] private float friction = 0.99f;
    [Range(1, 20)]
    [SerializeField] private int constraintIterations = 10;
    [SerializeField] private float maxRopeLength = 6f;

    [SerializeField] public Transform player1Transform;
    [SerializeField] public Transform player2Transform;

    private LineRenderer lineRenderer;
    private List<VerletPoint> points = new List<VerletPoint>();
    private List<VerletConstraint> constraints = new List<VerletConstraint>();
    private Rigidbody2D rb1;
    private Rigidbody2D rb2;

    public override void OnNetworkSpawn()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;

        Vector2 startPos = transform.position;
        for (int i = 0; i < segmentCount; i++)
        {
            bool pinned = (i == 0 || i == segmentCount - 1);
            points.Add(new VerletPoint(startPos + new Vector2(0, -i * segmentLength), pinned));
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            constraints.Add(new VerletConstraint(points[i], points[i + 1], segmentLength));
        }
    }

    void Update()
    {
        if (player1Transform == null || player2Transform == null)
        {
            FindPlayers();
        }
        UpdateLineRenderer();
    }

    void FixedUpdate()
    {
        if (!IsSpawned) return;
        Simulate();
        ApplyRopePull();
    }

    void FindPlayers()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        List<Transform> foundPlayers = new List<Transform>();
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (netObj != null && netObj.CompareTag("Player"))
            {
                foundPlayers.Add(netObj.transform);
            }
        }

        if (foundPlayers.Count >= 1)
        {
            player1Transform = foundPlayers[0];
            rb1 = player1Transform.GetComponent<Rigidbody2D>();
        }
        if (foundPlayers.Count >= 2)
        {
            player2Transform = foundPlayers[1];
            rb2 = player2Transform.GetComponent<Rigidbody2D>();
        }
    }

    void Simulate()
    {
        if (player1Transform != null && points.Count > 0)
        {
            points[0].position = player1Transform.position;
            points[0].oldPosition = player1Transform.position;
        }

        if (player2Transform != null && points.Count > 0)
        {
            int lastIndex = points.Count - 1;
            points[lastIndex].position = player2Transform.position;
            points[lastIndex].oldPosition = player2Transform.position;
        }

        foreach (var point in points)
        {
            point.Update(gravity, friction);
        }

        for (int iter = 0; iter < constraintIterations; iter++)
        {
            foreach (var constraint in constraints)
            {
                constraint.Resolve();
            }
        }
    }

    void ApplyRopePull()
    {
        if (player1Transform == null || player2Transform == null) return;

        Vector2 dir = player2Transform.position - player1Transform.position;
        float currentDistance = dir.magnitude;

        if (currentDistance > maxRopeLength)
        {
            float excess = currentDistance - maxRopeLength;
            Vector2 pullDir = dir.normalized;

            if (rb1 != null && rb2 != null)
            {
                rb1.position += pullDir * (excess * 0.5f);
                rb2.position -= pullDir * (excess * 0.5f);
            }
            else if (rb1 != null)
            {
                rb1.position += pullDir * excess;
            }
            else if (rb2 != null)
            {
                rb2.position -= pullDir * excess;
            }
        }
    }

    void UpdateLineRenderer()
    {
        if (points.Count == 0) return;

        Vector3[] linePositions = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            linePositions[i] = new Vector3(points[i].position.x, points[i].position.y, 0f);
        }
        lineRenderer.SetPositions(linePositions);
    }

    public void ResetRope()
    {
        if (player1Transform == null || player2Transform == null) return;

        Vector2 p1Pos = player1Transform.position;
        Vector2 p2Pos = player2Transform.position;

        for (int i = 0; i < points.Count; i++)
        {
            float t = (float)i / (points.Count - 1);
            Vector2 interpolatedPos = Vector2.Lerp(p1Pos, p2Pos, t);
            points[i].position = interpolatedPos;
            points[i].oldPosition = interpolatedPos;
        }
    }
}
