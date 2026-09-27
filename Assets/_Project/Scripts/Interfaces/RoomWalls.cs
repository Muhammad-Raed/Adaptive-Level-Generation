using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomWalls : MonoBehaviour
{
    public float corridorWidth = 3f;

    private float _halfSize = -1f;
    private float halfSize
    {
        get
        {
            if (_halfSize < 0)
                _halfSize = GetComponent<Renderer>().bounds.extents.x;
            return _halfSize;
        }
    }

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    public void ApplyCorridorGaps(List<Vector3> exitPoints)
    {
        Debug.Log($"ApplyCorridorGaps called on {gameObject.name} with {exitPoints.Count} exits");
        Debug.Log($"halfSize = {halfSize} for room {gameObject.name}");
        foreach (var ec in GetComponents<EdgeCollider2D>())
            Destroy(ec);

        Vector3 c = transform.position;
        float h = halfSize;
        float gapHalf = corridorWidth / 2f + 0.1f;

        var faces = new (Vector3 from, Vector3 to, bool horizontal)[]
        {
            (new Vector3(c.x - h, c.y + h, 0), new Vector3(c.x + h, c.y + h, 0), true),  // top
            (new Vector3(c.x - h, c.y - h, 0), new Vector3(c.x + h, c.y - h, 0), true),  // bottom
            (new Vector3(c.x - h, c.y - h, 0), new Vector3(c.x - h, c.y + h, 0), false), // left
            (new Vector3(c.x + h, c.y - h, 0), new Vector3(c.x + h, c.y + h, 0), false), // right
        };

        foreach (var face in faces)
        {
            List<float> gapCentres = new List<float>();
            foreach (var exit in exitPoints)
            {
                if (face.horizontal)
                {
                    bool matches = Mathf.Abs(exit.y - face.from.y) < 2f;
                    Debug.Log($"H-face y={face.from.y} vs exit y={exit.y} | match={matches}");
                    if (matches)
                        gapCentres.Add(Mathf.Clamp(exit.x,
                            face.from.x + gapHalf, face.to.x - gapHalf));
                }
                else
                {
                    bool matches = Mathf.Abs(exit.x - face.from.x) < 2f;
                    Debug.Log($"V-face x={face.from.x} vs exit x={exit.x} | match={matches}");
                    if (matches)
                        gapCentres.Add(Mathf.Clamp(exit.y,
                            face.from.y + gapHalf, face.to.y - gapHalf));
                }
            }
            Debug.Log($"Face gapCentres count: {gapCentres.Count}");
            SpawnFaceColliders(face.from, face.to, face.horizontal, gapCentres, gapHalf);
        }
    }

    private void SpawnFaceColliders(Vector3 from, Vector3 to, bool horizontal,
                                    List<float> gapCentres, float gapHalf)
    {
        float start = horizontal ? from.x : from.y;
        float end   = horizontal ? to.x   : to.y;
        float fixed_axis = horizontal ? from.y : from.x;

        var gaps = gapCentres
            .Select(g => (min: g - gapHalf, max: g + gapHalf))
            .OrderBy(g => g.min)
            .ToList();
        var merged = new List<(float min, float max)>();
        foreach (var gap in gaps)
        {
            if (merged.Count > 0 && gap.min <= merged[merged.Count - 1].max)
            {
                var last = merged[merged.Count - 1];
                merged[merged.Count - 1] = (last.min, Mathf.Max(last.max, gap.max));
            }
            else merged.Add(gap);
        }

        var segments = new List<(float from, float to)>();
        float cursor = start;
        foreach (var gap in merged)
        {
            if (gap.min > cursor)
                segments.Add((cursor, gap.min));
            cursor = gap.max;
        }
        if (cursor < end)
            segments.Add((cursor, end));

        Debug.Log($"SpawnFaceColliders: {segments.Count} segments for face from={from} to={to}");
        foreach (var seg in segments)
        {
            Debug.Log($"  segment {seg.from} -> {seg.to}");
            EdgeCollider2D ec = gameObject.AddComponent<EdgeCollider2D>();

            Vector2 p1local = transform.InverseTransformPoint(
                horizontal ? new Vector3(seg.from, fixed_axis, 0)
                        : new Vector3(fixed_axis, seg.from, 0));
            Vector2 p2local = transform.InverseTransformPoint(
                horizontal ? new Vector3(seg.to, fixed_axis, 0)
                        : new Vector3(fixed_axis, seg.to, 0));

            ec.SetPoints(new List<Vector2> { p1local, p2local });
        }
    }
}