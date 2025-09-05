using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaveWaterGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    public MeshRenderer meshRenderer;
    public Transform tileParent;
    public bool LockSizeShape = true;
    [Range(2, 200)] public int WidthX = 50;
    [Range(2, 200)] public int WidthY = 50;

    [Header("Wave Settings")]
    public TemperatureControllerScript temperatureController;
    private float waterPosition = 0f;
    [Range(0f, 20f)] public float LoopsX = 1f;
    [Range(0f, 20f)] public float LoopsY = 1f;
    [Range(0f, 1f)] public float OffsetX = 0f;
    [Range(0f, 1f)] public float OffsetY = 0f;
    [Range(0.1f, 20f)] public float HeightMultiplier = 5f;

    [Header("Vertex Color Gradient")]
    public List<ColorsToInterpolate> GradientColours = new List<ColorsToInterpolate>();

    [Header("Editor Options")]
    public bool AutoUpdate = false;

    private Mesh mesh;

    [System.Serializable]
    public class ColorsToInterpolate
    {
        [Range(0f, 1f)]
        public float Position = 0f;
        public Color Color = Color.white;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        waterPosition = temperatureController.GetTemperature();

        GenerateMesh();
    }


    private void OnValidate()
    {
        if (LockSizeShape)
            WidthY = WidthX;

        if (AutoUpdate)
            GenerateMesh();
    }

    public void GenerateMesh()
    {
        if (mesh == null) mesh = new Mesh();
        else mesh.Clear();

        mesh.name = "Wave Surface";

        Vector3[] vertices = new Vector3[(WidthX + 1) * (WidthY + 1)];
        int[] triangles = new int[WidthX * WidthY * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        float frequencyX = (LoopsX * 2 * Mathf.PI) / WidthX;
        float frequencyY = (LoopsY * 2 * Mathf.PI) / WidthY;

        float offsetXRadians = ((OffsetX + waterPosition) % 1f) * 2 * Mathf.PI;
        float offsetYRadians = OffsetY * 2 * Mathf.PI;

        // Generate vertices
        int vertIndex = 0;  
        for (int y = 0; y <= WidthY; y++)
        {
            for (int x = 0; x <= WidthX; x++)
            {
                float height = Mathf.Cos((x * frequencyX) + offsetXRadians) * Mathf.Cos((y * frequencyY) + offsetYRadians) * HeightMultiplier;
                vertices[vertIndex] = new Vector3(x, height, y);
                uv[vertIndex] = new Vector2(((float)x / WidthX), ((float)y / WidthY));
                vertIndex++;
            }
        }

        // Generate triangles
        int trisIndex = 0;
        int v = 0;
        for (int y = 0; y < WidthY; y++)
        {
            for (int x = 0; x < WidthX; x++)
            {
                triangles[trisIndex + 0] = v;
                triangles[trisIndex + 1] = v + WidthX + 1;
                triangles[trisIndex + 2] = v + 1;

                triangles[trisIndex + 3] = v + 1;
                triangles[trisIndex + 4] = v + WidthX + 1;
                triangles[trisIndex + 5] = v + WidthX + 2;

                v++;
                trisIndex += 6;
            }
            v++;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        ApplyVertexColors(vertices);

        if (Application.isPlaying)
        {
            GenerateTiledMeshes();
            meshRenderer.enabled = false;
        }
        else
            meshRenderer.enabled = true;
    }

    private void ApplyVertexColors(Vector3[] vertices)
    {
        if (GradientColours.Count == 0)
            return;

        Color[] colors = new Color[vertices.Length];

        float minHeight = -HeightMultiplier;
        float maxHeight = HeightMultiplier;

        for (int i = 0; i < vertices.Length; i++)
        {
            float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, vertices[i].y);
            normalizedHeight = Mathf.Clamp01(normalizedHeight);

            ColorsToInterpolate lower = GradientColours[0];
            ColorsToInterpolate upper = GradientColours[GradientColours.Count - 1];

            for (int j = 0; j < GradientColours.Count - 1; j++)
            {
                if (normalizedHeight >= GradientColours[j].Position && normalizedHeight <= GradientColours[j + 1].Position)
                {
                    lower = GradientColours[j];
                    upper = GradientColours[j + 1];
                    break;
                }
            }

            float range = upper.Position - lower.Position;
            float t = range > 0 ? (normalizedHeight - lower.Position) / range : 0f;

            colors[i] = Color.Lerp(lower.Color, upper.Color, t);
        }

        mesh.colors = colors;
    }

    private void GenerateTiledMeshes()
    {
        for (int i = tileParent.childCount - 1; i >= 0; i--)
        {
            Destroy(tileParent.GetChild(i).gameObject);
        }

        CreateTile("Tile_Original", Vector3.zero, Vector3.one);
        // CreateTile("Tile_FlipX", new Vector3(0, 0, 0), new Vector3(-1, 1, 1));
        // CreateTile("Tile_FlipY", new Vector3(0, 0, 0), new Vector3(1, 1, -1));
        // CreateTile("Tile_FlipXY", new Vector3(0, 0, 0), new Vector3(-1, 1, -1));
    }

    private void CreateTile(string name, Vector3 position, Vector3 scale)
    {
        GameObject child = new GameObject(name);
        child.transform.parent = tileParent;
        child.transform.localPosition = position;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = scale;

        MeshFilter mf = child.AddComponent<MeshFilter>();
        MeshRenderer mr = child.AddComponent<MeshRenderer>();

        mf.sharedMesh = mesh;
        mr.sharedMaterials = GetComponent<MeshRenderer>().sharedMaterials;
    }

}

#if UNITY_EDITOR
[CustomEditor(typeof(WaveWaterGenerator))]
public class WaveWaterGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WaveWaterGenerator generator = (WaveWaterGenerator)target;
        if (GUILayout.Button("Generate Mesh"))
        {
            generator.GenerateMesh();
        }
    }
}
#endif
