using System.Collections.Generic;
using UnityEngine;

public class NNGraphMaker : MonoBehaviour
{
    [SerializeField] GameObject nodePrefab;
    [SerializeField] GameObject inNodePrefab;
    [SerializeField] GameObject outNodePrefab;
    //[SerializeField] GameObject lrPrefab;

    void Awake()
    {
        if(nodePrefab == null)
            Debug.LogError("nodePrefab is null");
        if(inNodePrefab == null)
            Debug.LogError("inNodePrefab is null");
        if(outNodePrefab == null)
            Debug.LogError("outNodePrefab is null");
    }


    public bool MakeGraph(List<ConnectionGenome> c, List<NodeGenome> n, List<NodeGenome> inn)
    {
        
        MakeConnection(MakeInNode(Vector3.zero), MakeOutNode(Vector3.zero + new Vector3(100, 0, 0)));
        return true; //success
    }

    Vector3 MakeNode(Vector3 pos)
    {
        GameObject NodeGO = Instantiate(nodePrefab);
        NodeGO.transform.SetParent(transform, true);
        NodeGO.transform.localPosition = pos;
        return NodeGO.transform.position;
    }

    Vector3 MakeInNode(Vector3 pos)
    {
        GameObject inNodeGO = Instantiate(inNodePrefab);
        inNodeGO.transform.SetParent(transform, true);
        inNodeGO.transform.localPosition = pos;
        return inNodeGO.transform.position;
    }

    Vector3 MakeOutNode(Vector3 pos)
    {
        GameObject outNodeGO = Instantiate(outNodePrefab);
        outNodeGO.transform.SetParent(transform, true);
        outNodeGO.transform.localPosition = pos;
        return outNodeGO.transform.position;
    }

    void MakeConnection(Vector3 inPos, Vector3 outPos)
    {
        GameObject go = new GameObject("RuntimeLine");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        go.transform.SetParent(transform);

        // Základní nastavení
        lr.positionCount = 2;
        lr.SetPosition(0, inPos);
        lr.SetPosition(1, outPos);
        lr.useWorldSpace = true;
        lr.widthMultiplier = 0.05f;

        Shader s = Shader.Find("Sprites/Default");
        if (s != null)
            lr.material = new Material(s);

        lr.startColor = Color.white;
        lr.endColor = Color.white;

        lr.startWidth = 1;
        lr.endWidth = 1;

        lr.sortingOrder = 0;
    }
}
