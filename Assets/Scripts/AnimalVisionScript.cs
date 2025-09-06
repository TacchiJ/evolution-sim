using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class AnimalVision : MonoBehaviour
{
    [Header("Vision Settings")]
    public Transform eyePoint;
    public int raysX = 10;
    public int raysY = 5;
    public float coneAngleX = 60f;
    public float coneAngleY = 45f;
    public float maxDistance = 10f;

    [Header("Ray Colors by Layer")]
    public List<LayerColorPair> rayColorsList = new List<LayerColorPair>();

    // Stores [distance, layer, distance, layer, ...] for each ray
    public List<float> visionValues = new List<float>();

    private Dictionary<int, Color> rayColors = new Dictionary<int, Color>();
    private Collider[] selfColliders;

    [System.Serializable]
    public class LayerColorPair
    {
        public int layer;
        public Color color = Color.white;
    }

    private void Awake()
    {
        selfColliders = GetComponentsInChildren<Collider>();
        UpdateRayColorsDictionary();
    }

    private void OnValidate()
    {
        UpdateRayColorsDictionary();
    }

    private void UpdateRayColorsDictionary()
    {
        rayColors.Clear();
        foreach (var pair in rayColorsList)
        {
            if (!rayColors.ContainsKey(pair.layer))
                rayColors.Add(pair.layer, pair.color);
        }
    }

    private void Update()
    {
        visionValues.Clear();

        if (eyePoint == null) return;

        foreach (var dir in GetRayDirections())
        {
            if (TryRaycastIgnoringSelf(eyePoint.position, dir, out RaycastHit hit))
            {
                visionValues.Add(hit.distance);
                visionValues.Add(hit.collider.gameObject.layer);
            }
            else
            {
                visionValues.Add(maxDistance);
                visionValues.Add(-1); // -1 = no hit
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (eyePoint == null) return;

        foreach (var dir in GetRayDirections())
        {
            if (TryRaycastIgnoringSelf(eyePoint.position, dir, out RaycastHit hit))
            {
                // Get color based on layer
                Color col = Color.red; // default
                if (rayColors.TryGetValue(hit.collider.gameObject.layer, out Color c))
                    col = c;

                Gizmos.color = col;
                Gizmos.DrawLine(eyePoint.position, hit.point);

                Gizmos.color = new Color(col.r, col.g, col.b, 0.3f);
                Gizmos.DrawCube(hit.point, Vector3.one * 0.5f);
            }
            else
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(eyePoint.position, eyePoint.position + dir * maxDistance);
            }
        }
    }

    private bool TryRaycastIgnoringSelf(Vector3 origin, Vector3 dir, out RaycastHit validHit)
    {
        validHit = default;
        var hits = Physics.RaycastAll(origin, dir, maxDistance);
        if (hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (!IsSelfCollider(h.collider))
            {
                validHit = h;
                return true;
            }
        }
        return false;
    }

    private bool IsSelfCollider(Collider col)
    {
        foreach (var selfCol in selfColliders)
        {
            if (col == selfCol) return true;
        }
        return false;
    }

    private IEnumerable<Vector3> GetRayDirections()
    {
        for (int y = 0; y < raysY; y++)
        {
            float v = (raysY == 1) ? 0f : Mathf.Lerp(-coneAngleY * 0.5f, coneAngleY * 0.5f, (float)y / (raysY - 1));
            for (int x = 0; x < raysX; x++)
            {
                float h = (raysX == 1) ? 0f : Mathf.Lerp(-coneAngleX * 0.5f, coneAngleX * 0.5f, (float)x / (raysX - 1));
                Quaternion rot = Quaternion.Euler(v, h, 0f);
                yield return rot * eyePoint.forward;
            }
        }
    }

    /// <summary>
    /// Returns the current vision values [distance, layer, distance, layer, ...]
    /// </summary>
    public List<float> GetVisionValues()
    {
        return visionValues;
    }
}
