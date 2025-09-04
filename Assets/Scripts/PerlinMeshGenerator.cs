using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PerlinSurfaceGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Range(2, 200)] public int WidthX = 50;
    [Range(2, 200)] public int WidthY = 50;

    [Header("Perlin Noise Settings")]
    [Range(0.1f, 50f)] public float PerlinNoiseScale = 10f;
    public bool LockNoiseShape = true;
    [Range(0.1f, 10f)] public float PerlinNoiseShapeX = 2f;
    [Range(0.1f, 10f)] public float PerlinNoiseShapeY = 2f;
    [Range(0.1f, 20f)] public float HeightMultiplier = 5f;

    [Header("Rendering")]
    public Material SurfaceMaterial;
    [Range(0.1f, 20f)] public float UVScale = 1f;

    [Header("Editor Options")]
    public bool AutoUpdate = false;

    private Mesh mesh;

    private void OnValidate()
    {
        if (LockNoiseShape)
        {
            PerlinNoiseShapeY = PerlinNoiseShapeX;
        }
        
        if (AutoUpdate)
        {
            GenerateMesh();
        }
    }

    public void GenerateMesh()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Surface";

        Vector3[] vertices = new Vector3[(WidthX + 1) * (WidthY + 1)];
        int[] triangles = new int[WidthX * WidthY * 6];
        Vector2[] uv = new Vector2[vertices.Length];

        // Generate vertices
        int vertIndex = 0;
        for (int y = 0; y <= WidthY; y++)
        {
            for (int x = 0; x <= WidthX; x++)
            {
                float xCoord = (float)x / PerlinNoiseScale;
                float yCoord = (float)y / PerlinNoiseScale;
                float height = Mathf.PerlinNoise(xCoord * PerlinNoiseShapeX, yCoord * PerlinNoiseShapeY) * HeightMultiplier;
                vertices[vertIndex] = new Vector3(x, height, y);
                uv[vertIndex] = new Vector2(((float)x / WidthX) * UVScale, ((float)y / WidthY) * UVScale);
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

        if (SurfaceMaterial != null)
        {
            GetComponent<MeshRenderer>().sharedMaterial = SurfaceMaterial;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PerlinSurfaceGenerator))]
public class PerlinSurfaceGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PerlinSurfaceGenerator generator = (PerlinSurfaceGenerator)target;
        if (GUILayout.Button("Generate Mesh"))
        {
            generator.GenerateMesh();
        }
    }
}
#endif
