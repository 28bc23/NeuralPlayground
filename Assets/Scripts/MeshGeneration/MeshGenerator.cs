using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PolygonCollider2D))]
public class MeshGenerator : MonoBehaviour
{
    [Header("Input (must be even count - if odd, last value will be duplicated)")]
    public List<float> values = new List<float>();
    public float spacing = 1f;          // vzdálenost mezi body na ose X

    [Header("Options")]
    public bool updateCollider2D = true; // aktualizovat PolygonCollider2D pokud je pøítomen
    public bool doubleSided = false;     // oboustranný mesh
    public bool centerOrigin = true;     // když true, posune mesh tak, aby jeho støed byl v (0,0)

    Mesh mesh;
    MeshFilter mf;

    void OnEnable()
    {
        mf = GetComponent<MeshFilter>();

        if (mf.sharedMesh == null)
        {
            mesh = new Mesh();
            mesh.name = "AgentMesh";
            mf.sharedMesh = mesh;
        }
        else
        {
            mesh = mf.sharedMesh;
        }
    }

    private void FixedUpdate()
    {
        // Pozn.: generování každý FixedUpdate mùže být nároèné. Pokud chceš, mùžu pøidat detekci zmìny hodnot.
    }

    [ContextMenu("Generate Mesh")]
    public void GenerateMesh()
    {
        if (values == null || values.Count < 2)
        {
            Debug.LogWarning("MeshGenerator: needs at least 2 values to build a mesh.");
            ClearMesh();
            return;
        }

        EnsureEvenCount();

        int total = values.Count;
        int half = total / 2;
        if (half < 2)
        {
            Debug.LogWarning("MeshGenerator: needs at least 4 values (2 top + 2 bottom) for a meaningful strip.");
            ClearMesh();
            return;
        }

        // Vytvoøíme vertexy v lokálních souøadnicích (zatím bez centrování)
        int vertCount = half * 2;
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];

        for (int i = 0; i < half; i++)
        {
            float x = i * spacing;
            float topY = values[i];                 // první polovina -> top
            float bottomY = values[half + i];       // druhá polovina -> bottom

            verts[i] = new Vector3(x, topY, 0f);           // top vertices [0..half-1]
            verts[i + half] = new Vector3(x, bottomY, 0f); // bottom vertices [half..2*half-1]

            float u = (float)i / Mathf.Max(1, half - 1);
            uvs[i] = new Vector2(u, 1f);
            uvs[i + half] = new Vector2(u, 0f);

            normals[i] = Vector3.back;
            normals[i + half] = Vector3.back;
        }

        // Pokud chceme centrovat origin, dopoèítáme støed bounding boxu a posuneme vertexy
        Vector3 centerOffset = Vector3.zero;
        if (centerOrigin)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].x < minX) minX = verts[i].x;
                if (verts[i].x > maxX) maxX = verts[i].x;
                if (verts[i].y < minY) minY = verts[i].y;
                if (verts[i].y > maxY) maxY = verts[i].y;
            }

            centerOffset = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);

            for (int i = 0; i < verts.Length; i++)
                verts[i] -= centerOffset;
        }

        // Triangles: for each segment i -> i+1 create two triangles
        int triCount = (half - 1) * 2;
        int[] tris = new int[triCount * 3];
        int t = 0;
        for (int i = 0; i < half - 1; i++)
        {
            int topA = i;
            int topB = i + 1;
            int botA = i + half;
            int botB = i + 1 + half;

            // triangle 1: topA, topB, botA
            tris[t++] = topA;
            tris[t++] = topB;
            tris[t++] = botA;

            // triangle 2: topB, botB, botA
            tris[t++] = topB;
            tris[t++] = botB;
            tris[t++] = botA;
        }

        // double sided?
        if (doubleSided)
        {
            int oldLen = tris.Length;
            int[] tris2 = new int[oldLen * 2];
            tris.CopyTo(tris2, 0);
            int p = oldLen;
            for (int i = 0; i < oldLen; i += 3)
            {
                tris2[p++] = tris[i + 2];
                tris2[p++] = tris[i + 1];
                tris2[p++] = tris[i + 0];
            }
            tris = tris2;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.RecalculateBounds();

        if (updateCollider2D)
        {
            var poly = GetComponent<PolygonCollider2D>();
            if (poly != null)
                UpdatePolygonCollider(poly, half, centerOffset);
        }
    }

    void ClearMesh()
    {
        if (mesh != null)
            mesh.Clear();
    }

    void EnsureEvenCount()
    {
        if (values == null) return;
        if (values.Count % 2 != 0)
        {
            // duplikuj poslední prvek, aby byl poèet sudý
            float last = values[values.Count - 1];
            values.Add(last);
            Debug.Log("[MeshGenerator] values count was odd — duplicated last value to make it even.");
        }
    }

    // Upravené: pøidáme centerOffset, abychom posunuli i collider stejným zpùsobem
    void UpdatePolygonCollider(PolygonCollider2D poly, int half, Vector3 centerOffset)
    {
        if (values == null || values.Count < 2) return;

        // path: top left->right, bottom right->left (to close polygon)
        Vector2[] path = new Vector2[half * 2];

        for (int i = 0; i < half; i++)
        {
            Vector2 p = new Vector2(i * spacing, values[i]); // top left->right
            p -= (Vector2)centerOffset;                      // shift stejným offsetem
            path[i] = p;
        }

        for (int i = 0; i < half; i++)
        {
            // bottom right->left: take bottom values from second half in reverse order
            float bottomY = values[half + (half - 1 - i)];
            float x = (half - 1 - i) * spacing;
            Vector2 p = new Vector2(x, bottomY);
            p -= (Vector2)centerOffset;
            path[half + i] = p;
        }

        poly.pathCount = 1;
        poly.SetPath(0, path);
    }

    // public helper pro externí aktualizaci
    public void SetValues(List<float> newValues)
    {
        values = new List<float>(newValues);
        ClearMesh();
        EnsureEvenCount();
        GenerateMesh();
    }

    public List<float> GetValues()
    {
        return values;
    }
}
