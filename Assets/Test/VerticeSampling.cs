using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticeSampling : MonoBehaviour
{
    [SerializeField] private MeshFilter tester;

    private Dictionary<int, Vector3Int> rv;
    private List<int> radixes;
    private Transform objTransform;
    private int count;
    private bool isPlayer;

    void Start()
    {
        Mesh mesh = tester.mesh;
        radixes = new List<int>();
        rv = new Dictionary<int, Vector3Int>();
        objTransform = tester.transform;
        isPlayer = tester.gameObject.CompareTag("Player");

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        float sampleInterval = 0.2f;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 vertex1 = objTransform.TransformPoint(vertices[triangles[i]]);
            Vector3 vertex2 = objTransform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 vertex3 = objTransform.TransformPoint(vertices[triangles[i + 2]]);

            SamplePointsAlongEdge(vertex1, vertex2, sampleInterval);
            SamplePointsAlongEdge(vertex2, vertex3, sampleInterval);
            SamplePointsAlongEdge(vertex3, vertex1, sampleInterval);
        }
        Debug.Log($"ÃÑ »ùÇÃ¸µ °³¼ö: {count}");
    }
    void SamplePointsAlongEdge(Vector3 start, Vector3 end, float interval)
    {
        float distance = Vector3.Distance(start, end);
        int numberOfSamples = Mathf.CeilToInt(distance / interval);

        Vector3 samplePoint;
        byte x, y, z;
        float size = 1 / interval;
        for (float i = 0; i <= numberOfSamples; i+=1)
        {
            float t = i / numberOfSamples;
            samplePoint = Vector3.Lerp(start, end, t);
            x = (byte)(samplePoint.x * size);
            y = (byte)(samplePoint.y * size);
            z = (byte)(samplePoint.z * size);
            int radix = (x << 16) | (y << 8) | (z << 0);

            if (!rv.ContainsKey(radix))
            {
                rv.Add(radix, new Vector3Int(x, y, z));
                Debug.Log($"[{x},{y},{z}] Sampled Point: {samplePoint}");
                count += 1;
            }
        }
    }
}
