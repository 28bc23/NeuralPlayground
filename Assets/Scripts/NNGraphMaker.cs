using System;
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
        GameObject inNodeGO = Instantiate(inNodePrefab);
        inNodeGO.transform.SetParent(transform, true);
        inNodeGO.transform.localPosition = Vector3.zero;

        GameObject outNodeGO = Instantiate(outNodePrefab);
        outNodeGO.transform.SetParent(transform, true);
        outNodeGO.transform.localPosition = Vector3.zero + new Vector3(100, 0, 0);

        GameObject go = new GameObject("RuntimeLine");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        go.transform.SetParent(transform);

        // Základní nastavení
        lr.positionCount = 2;
        lr.SetPosition(0, inNodeGO.transform.position);
        lr.SetPosition(1, outNodeGO.transform.position);
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
        return true; //success
    }
}
