using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticeSampling : MonoBehaviour
{
    [SerializeField] private MeshFilter tester;
    private Transform objTransform;
    private int count;
    private bool isPlayer;

    void Start()
    {
        Mesh mesh = tester.mesh;
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

        for (int i = 0; i <= numberOfSamples; i++)
        {
            float t = i / (float)numberOfSamples;
            Vector3 samplePoint = Vector3.Lerp(start, end, t);

            Debug.Log($"[{isPlayer}]Sampled Point: {samplePoint}");
            count += 1;
        }
    }
}
