
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class NN : MonoBehaviour
{
    List<NodeGenome> nodes = new List<NodeGenome>();
    List<NodeGenome> inNodes = new List<NodeGenome>();
    List<ConnectionGenome> connections = new List<ConnectionGenome>();    
    
    int inputSize;
    int outputSize = 1 + 1 + 1;

    public bool isInicialized = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public float[] Forward(float[] input)
    {
        if (!isInicialized)
        {
            Debug.LogWarning("NN not initialized.");
            return null;
        }

        if (input == null || input.Length != inputSize)
        {
            Debug.LogWarning($"Incorrect input size. Expected {inputSize}, got {input?.Length ?? 0}");
            return null;
        }

        // rychlý lookup nodeID -> node
        Dictionary<uint, NodeGenome> nodeById = new Dictionary<uint, NodeGenome>();
        foreach (var n in nodes) nodeById[n.nodeID] = n;
        foreach (var n in inNodes) nodeById[n.nodeID] = n;

        // reset uzlů
        foreach (NodeGenome inN in inNodes) inN.Clear();
        foreach (NodeGenome n in nodes) n.Clear();

        // inicializace: non-input začínáme biasem
        foreach (NodeGenome n in nodes)
        {
            n.vlaue = n.bias;
        }
        // vlož vstupy
        inNodes.Sort((a, b) => a.inov.CompareTo(b.inov));
        for (int k = 0; k < input.Length; k++)
        {
            inNodes[k].vlaue = input[k];
        }

        // prvotní krok z inputů
        HashSet<uint> nextNodesIDs = new HashSet<uint>();
        foreach (NodeGenome inNode in inNodes)
        {
            // najít outgoing connections (můžeš optimalizovat s adjacency mapou)
            List<ConnectionGenome> conns = connections.FindAll(c => c.inNode == inNode.nodeID);
            float outVal = inNode.GetValueWithActivationFunction();
            foreach (ConnectionGenome c in conns)
            {
                c.vlaue = c.weight * outVal;
                if (nodeById.TryGetValue(c.outNode, out NodeGenome outN))
                {
                    outN.vlaue += c.vlaue;
                    nextNodesIDs.Add(c.outNode);
                }
            }
        }

        // iteruj dále, dokud jsou nové dosažené uzly
        while (nextNodesIDs.Count > 0)
        {
            HashSet<uint> nextNodesIDsTemp = new HashSet<uint>();

            // najdi spojení, kde inNode je v current setu
            List<ConnectionGenome> cs = connections.FindAll(c => nextNodesIDs.Contains(c.inNode));
            foreach (ConnectionGenome c in cs)
            {
                if (!nodeById.TryGetValue(c.inNode, out NodeGenome inN)) continue;
                if (!nodeById.TryGetValue(c.outNode, out NodeGenome outN)) continue;

                float inVal = inN.GetValueWithActivationFunction();
                c.vlaue = inVal * c.weight;
                outN.vlaue += c.vlaue;
                nextNodesIDsTemp.Add(c.outNode);
            }

            nextNodesIDs = nextNodesIDsTemp;
        }

        // seber a deterministicky seřaď výstupy
        List<NodeGenome> outNodes = nodes.FindAll(n => n.IsOutput);
        if (outNodes.Count == 0)
        {
            Debug.LogWarning("No output nodes found.");
            return null;
        }
        outNodes.Sort((a, b) => a.inov.CompareTo(b.inov)); // nebo podle nodeID

        float[] outputs = new float[outNodes.Count];
        for (int i = 0; i < outNodes.Count; i++)
        {
            outputs[i] = outNodes[i].GetValueWithActivationFunction();
        }

        return outputs;
    }


    public static float ReLU(float input)
    {
        if (input <= 0)
        {
            return 0;
        }
        return input;
    }

    public static float Tanh(float x)
    {
        return (float)System.Math.Tanh(x);
    }

    public void RandomInicialize(int in_size = 38, int out_size = 1 + 1 + 1)
    {
        inputSize = in_size; outputSize = out_size;

        int inov = 0;
        for (int i = 0; i < inputSize; i++)
        {
            NodeGenome node = new NodeGenome();
            node.nodeID = AgentsManager.GetFreeID();
            node.IsInput = true;
            node.bias = 0;

            inNodes.Add(node);
        }

        for (int i = 0; i < outputSize; i++)
        {
            NodeGenome node = new NodeGenome();
            node.nodeID = AgentsManager.GetFreeID();
            node.IsOutput = true;
            node.bias = Random.Range(-1.0f, 1.0f);
            node.inov = inov;
            inov++;

            nodes.Add(node);
        }

        foreach(NodeGenome oN in nodes)
        {
            if (!oN.IsOutput) continue;

            foreach(NodeGenome iN in inNodes)
            {
                if (!iN.IsInput) continue;

                float val = 0.1f;

                if (Random.value < val)
                {
                    ConnectionGenome connection = new ConnectionGenome();

                    connection.connectionID = AgentsManager.GetFreeID();
                    connection.inNode = iN.nodeID;
                    connection.outNode = oN.nodeID;
                    connection.weight = Random.Range(-1.0f, 1.0f);
                    connection.inov = inov;

                    connections.Add(connection);
                    inov++;
                    //nigga zou are

                }
            }
        }
        
        isInicialized = true;
    }

    public void Inicialize(List<ConnectionGenome> c, List<NodeGenome> n, bool bMutate, float mutationChance, float mutationSize, int inSize)
    {
        if (n == null) Debug.LogWarning("nodes in nn inicialize is null");
        // rozdělíme vstupy a ostatní (output + hidden)
        inNodes = n.FindAll(x => x.IsInput);
        nodes = n.FindAll(x => !x.IsInput);

        connections = (c == null) ? new List<ConnectionGenome>() : c;
        inputSize = inSize;

        // Ověř, že máme očekávaný počet výstupů
        int foundOutputs = nodes.FindAll(x => x.IsOutput).Count;
        if (foundOutputs != outputSize)
        {
            Debug.LogWarning($"NN.Inicialize: expected {outputSize} outputs but found {foundOutputs} (Agent: {gameObject.name}).");
        }

        if (bMutate)
        {
            mutationChance = Mathf.Clamp(mutationChance, 0.05f, .95f);
            mutationSize = Mathf.Clamp(mutationSize, 0.1f, 1.5f);

            foreach (ConnectionGenome conn in connections)
                if (Random.value < mutationChance) conn.weight += Random.Range(-mutationSize, mutationSize);

            foreach (NodeGenome node in nodes)
                if (Random.value < mutationChance) node.bias += Random.Range(-mutationSize, mutationSize);
        }

        isInicialized = true;
    }


    public int GetInputSize()
    {
        return inputSize;
    }

    public List<ConnectionGenome> CopyConnections()
    {
        List<ConnectionGenome> copy = new List<ConnectionGenome>();
        foreach (ConnectionGenome c in connections)
        {
            ConnectionGenome cc = new ConnectionGenome();
            cc.connectionID = c.connectionID;
            cc.inov = c.inov;
            cc.inNode = c.inNode;
            cc.outNode = c.outNode;
            cc.weight = c.weight;
            c.vlaue = 0;
            copy.Add(cc);
        }

        return copy;
    }

    public List<NodeGenome> CopyNodes()
    {
        List<NodeGenome> copy = new List<NodeGenome>();
        foreach (NodeGenome n in nodes)
        {
            copy.Add(n.Copy());
        }

        foreach(NodeGenome inN in inNodes)
        {
            copy.Add(inN.Copy());
        }

        return copy;
    }

    public List<NodeGenome> CopyInNodes()
    {
        List<NodeGenome> copy = new List<NodeGenome>();
        foreach (NodeGenome n in inNodes)
        {
            NodeGenome cn = new NodeGenome();
            cn.nodeID = n.nodeID;
            cn.inov = n.inov;
            cn.IsOutput = false;
            cn.IsInput = true;
            cn.bias = 0;
            n.vlaue = 0;
            copy.Add(cn);
        }

        return copy;
    }
}


public class ConnectionGenome
{
    public uint connectionID = 0;
    public int inov = 0;

    public uint inNode = 0;
    public uint outNode = 0;

    public float weight = 0;
    public float vlaue = 0;

    public ConnectionGenome Copy()
    {
        ConnectionGenome copy = new ConnectionGenome();

        copy.connectionID = connectionID;
        copy.inov = inov;
        copy.inNode = inNode;
        copy.outNode = outNode;
        copy.weight = weight;
        copy.vlaue = 0;

        return copy;
    }
}

public class NodeGenome
{
    public uint nodeID = 0;
    public int inov = 0;
    public bool IsOutput = false;
    public bool IsInput = false;
    public float bias = 0;
    public float vlaue = 0;

    bool hasBeenActivated = false;

    public float GetValueWithActivationFunction()
    {
        if (IsInput || hasBeenActivated)
        {
            return vlaue;
        }
        else if (IsOutput)
        {
            hasBeenActivated = true;
            return NN.Tanh(vlaue);
        }
        else
        {
            hasBeenActivated = true;
            return NN.ReLU(vlaue);
        }
    }
    
    public void Clear()
    {
        vlaue = 0f;
        hasBeenActivated = false;
    }

    public NodeGenome Copy()
    {
        NodeGenome copy = new NodeGenome();

        copy.nodeID = nodeID;
        copy.inov = inov;
        copy.IsOutput = IsOutput;
        copy.IsInput = IsInput;
        copy.bias = bias;
        copy.vlaue = 0;

        return copy;
    }
}