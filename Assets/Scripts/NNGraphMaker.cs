using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class NNGraphMaker : MonoBehaviour
{
    [SerializeField] GameObject nodePrefab;
    [SerializeField] GameObject inNodePrefab;
    [SerializeField] GameObject outNodePrefab;
    [SerializeField] GameObject graphStartPoint;
    [SerializeField] GameObject graphEndPoint;
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


    public bool MakeGraph(List<ConnectionGenome> c, List<NodeGenome> n)
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject == graphStartPoint || child.gameObject == graphEndPoint)
                continue;
            Destroy(child.gameObject);
        }

        float spacerX = 50f;
        float spacerY = 15f;
        float startOffsetX = graphStartPoint.transform.localPosition.x;
        float startOffsetY = graphStartPoint.transform.localPosition.y;

        float endOffsetX = graphEndPoint.transform.localPosition.x;
        float endOffsetY = graphEndPoint.transform.localPosition.y;

        List<NodeGenome> currLayerNodes = n.FindAll(x => x.IsInput == true);
        List<NodeGenome> nextLayerNodes = new List<NodeGenome>();
        int currLayerLen = currLayerNodes.Count;

        Dictionary<uint, List<Vector3>> currLayerToLastLayerPos = new Dictionary<uint, List<Vector3>>();
        Dictionary<uint, List<Vector3>> nextLayerToCurrLayerPos = new Dictionary<uint, List<Vector3>>();

        int x = 0;
        while (true)
        {
            int y = 0;
            foreach (NodeGenome node in currLayerNodes)
            {
                Vector3 nodeWorldPos;
                Vector3 nodeStartLocalPos = new Vector3(x * spacerX + startOffsetX, y * spacerY + startOffsetY, 0);
                Vector3 nodeEndLocalPos = new Vector3(endOffsetX, y * spacerY + endOffsetY, 0);
                if (node.IsInput)
                    nodeWorldPos = MakeInNode(nodeStartLocalPos); 
                else if (node.IsOutput)
                    nodeWorldPos = MakeOutNode(nodeEndLocalPos);
                else
                    nodeWorldPos = MakeNode(nodeStartLocalPos);

                List<Vector3> inPoss;
                if (currLayerToLastLayerPos.TryGetValue(node.nodeID, out inPoss))
                {
                    foreach(Vector3 inPos in inPoss)
                    {
                        MakeConnection(inPos, nodeWorldPos);
                    }                    
                }

                List<ConnectionGenome> connectionGenomeRefs = c.FindAll(x => x.inNode == node.nodeID);

                if (connectionGenomeRefs.Count > 0)
                {


                    foreach (ConnectionGenome connectionGenome in connectionGenomeRefs)
                    {
                        List<NodeGenome> nextNodeGenomeRefs = n.FindAll(x => x.nodeID == connectionGenome.outNode);
                        if (nextNodeGenomeRefs.Count > 1)
                            Debug.LogWarning("Found more than 1 outNodes for connection");
                        if (nextNodeGenomeRefs.Count < 1)
                            Debug.LogWarning("Found more less 1 outNodes for connection");

                        NodeGenome containsTest = nextLayerNodes.Find(x => x.nodeID == nextNodeGenomeRefs[0].nodeID);
                        if (containsTest == null)
                            nextLayerNodes.Add(nextNodeGenomeRefs[0]);

                        List<Vector3> poss;
                        if (nextLayerToCurrLayerPos.TryGetValue(nextNodeGenomeRefs[0].nodeID, out poss))
                        {
                            poss.Add(nodeWorldPos);
                        }
                        else
                        {
                            poss = new List<Vector3>();
                            poss.Add(nodeWorldPos);
                            nextLayerToCurrLayerPos.Add(nextNodeGenomeRefs[0].nodeID, poss);
                        }
                    }
                }
                else
                {
                }
                y++;
            }
            if (nextLayerNodes.Count == 0)
                break;
            currLayerNodes.Clear();
            currLayerNodes.AddRange(nextLayerNodes);
            nextLayerNodes.Clear();

            currLayerToLastLayerPos.Clear();
            currLayerToLastLayerPos.AddRange(nextLayerToCurrLayerPos);
            nextLayerToCurrLayerPos.Clear();
            x++;
        }
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

    UILineRenderer MakeConnection(Vector3 inPos, Vector3 outPos)
    {
        GameObject go = new GameObject("RuntimeLine");
        UILineRenderer lr = go.AddComponent<UILineRenderer>();
        go.transform.SetParent(transform);

        // Základní nastavení
        Vector2[] poss = new Vector2[2];
        poss[0] = inPos;
        poss[1] = outPos;
        lr.Points = poss;

        Shader s = Shader.Find("Sprites/Default");
        if (s != null)
            lr.material = new Material(s);

        lr.color = Color.white;

        lr.LineThickness = 1f;
        return lr;
    }
}
