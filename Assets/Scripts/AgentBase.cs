using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(MeshGenerator), typeof(NN))]
public class AgentBase : MonoBehaviour
{
    [SerializeField] float valuesSize = 2.0f;
    [SerializeField] float speedScaler = 10.0f;
    [SerializeField] float lookRange = 20f;
    [SerializeField] float weightBiasMutationChance = 0.5f;
    [SerializeField] float weightBiasMutationSize = .5f;
    [SerializeField] float newConnMutationChance = .1f;
    [SerializeField] float splitMutationChance = .05f;

    public NN nn;
    public float energy = 10;
    public float foodEnergy = 2;
    public float agentEnergy = 4;
    public int foodEaten = 0;
    public int kills = 0;
    public int children = 0;
    public int score = 0;
    public bool mood = false;

    MeshGenerator meshGenerator;
    List<float> meshVals;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        meshGenerator = GetComponent<MeshGenerator>();
        nn = GetComponent<NN>();

        energy = (int)Random.Range(energy-2, energy);
    }


    // Update is called once per frame
    void Update()
    {
        if (nn == null)
        {
            Debug.LogWarning($"{name}: NN component missing. Destroying agent to avoid further errors.");
            Destroy(gameObject);
            return;
        }
        if (!nn.isInicialized) // přidej funkci IsInicialized() v NN (viz níže)
        {
            Debug.LogWarning($"{name}: NN not initialized yet - skipping Update.");
            return;
        }

        float[] angles = new float[] { -15f, 0f, 15f };

        // prom�nn� pro ulo�en� nejbli���ch hit� z ka�d� skupiny (food)
        Vector2 forwardNearest = Vector2.zero;
        float forwardNearestDist = Mathf.Infinity;
        Vector2 backwardNearest = Vector2.zero;
        float backwardNearestDist = Mathf.Infinity;

        // prom�nn� pro ulo�en� nejbli���ch hit� pro agenta
        Vector2 forwardAgentNearest = Vector2.zero;
        float energyF = 0;
        bool moodF = false;
        bool genderF = false;
        float forwardAgentNearestDist = Mathf.Infinity;
        Vector2 backwardAgentNearest = Vector2.zero;
        float energyB = 0;
        bool moodB = false;
        bool genderB = false;
        float backwardAgentNearestDist = Mathf.Infinity;

        // origin jako Vector3 pro kreslen� a Vector2 pro raycast
        Vector3 origin3 = transform.position;
        Vector2 origin2 = origin3;

        // d�lej 3 raycasty dop�edu
        foreach (float angle in angles)
        {
            Vector3 dir3 = Quaternion.Euler(0f, 0f, angle) * transform.up; // Vector3
            Vector2 dir2 = new Vector2(dir3.x, dir3.y); // Vector2

            RaycastHit2D hit = Physics2D.Raycast(origin2, dir2, lookRange);

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                // pokud jsme zas�hli food
                if (hit.collider.CompareTag("food"))
                {
                    if (hit.distance < forwardNearestDist)
                    {
                        forwardNearestDist = hit.distance;
                        forwardNearest = hit.point;
                    }
                    Vector3 hit3 = new Vector3(hit.point.x, hit.point.y, origin3.z);
                    Debug.DrawLine(origin3, hit3, Color.green);
                }
                // pokud jsme zas�hli agenta (jin�ho ne� sebe)
                else if (hit.collider.CompareTag("agent"))
                {
                    if (hit.distance < forwardAgentNearestDist)
                    {
                        forwardAgentNearestDist = hit.distance;
                        forwardAgentNearest = hit.point;
                        energyF = hit.transform.GetComponent<AgentBase>().energy;
                        moodF = hit.transform.GetComponent<AgentBase>().mood;
                    }
                    Vector3 hit3 = new Vector3(hit.point.x, hit.point.y, origin3.z);
                    Debug.DrawLine(origin3, hit3, Color.cyan);
                }
                else
                {
                    // zas�hli n�co jin�ho (ne food ani agent) - vykresli ��ru do m�sta z�sahu �erven�
                    Vector3 hit3 = new Vector3(hit.point.x, hit.point.y, origin3.z);
                    Debug.DrawLine(origin3, hit3, Color.red);
                }
            }
            else
            {
                // nic nezas�hl - vykresli pln� paprsek (�erven�)
                Debug.DrawLine(origin3, origin3 + dir3 * lookRange, Color.red);
            }
        }

        // d�lej 3 raycasty dozadu (sm�r -transform.up, ale oto�en podle �hl�)
        foreach (float angle in angles)
        {
            Vector3 baseBack3 = -transform.up;
            Vector3 dir3 = Quaternion.Euler(0f, 0f, angle) * baseBack3;
            Vector2 dir2 = new Vector2(dir3.x, dir3.y);

            RaycastHit2D hit = Physics2D.Raycast(origin2, dir2, lookRange);

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                if (hit.collider.CompareTag("food"))
                {
                    if (hit.distance < backwardNearestDist)
                    {
                        backwardNearestDist = hit.distance;
                        backwardNearest = hit.point;
                    }
                    Vector3 hit3 = new Vector3(hit.point.x, hit.point.y, origin3.z);
                    Debug.DrawLine(origin3, hit3, Color.green);
                }
                else if (hit.collider.CompareTag("agent"))
                {
                    if (hit.distance < backwardAgentNearestDist)
                    {
                        backwardAgentNearestDist = hit.distance;
                        backwardAgentNearest = hit.point;
                        energyB = hit.transform.GetComponent<AgentBase>().energy;
                        moodB = hit.transform.GetComponent<AgentBase>().mood;
                    }
                    Vector3 hit3 = new Vector3(hit.point.x, hit.point.y, origin3.z);
                    Debug.DrawLine(origin3, hit3, Color.cyan);
                }
                else
                {
                    Vector3 hit3 = new Vector3(hit.point.x, hit.point.y, origin3.z);
                    Debug.DrawLine(origin3, hit3, Color.red);
                }
            }
            else
            {
                Debug.DrawLine(origin3, origin3 + dir3 * lookRange, Color.red);
            }
        }

        // --- p�iprav stav pro NN ---
        // zajist�me, �e meshVals nen� null (bezpe�nost)
        if (meshVals == null) meshVals = GenerateRandomValsForMesh();
        //Debug.Log(nn.GetInputSize());
        float[] state = new float[nn.GetInputSize()];
        state[0] = transform.position.x;
        state[1] = transform.position.y;
        state[2] = transform.eulerAngles.z;
        state[3] = energy;

        // p�edn� food
        state[4] = forwardNearest.x;
        state[5] = forwardNearest.y;

        // zadn� food
        state[6] = backwardNearest.x;
        state[7] = backwardNearest.y;

        // p�edn� agent (x,y)
        state[8] = forwardAgentNearest.x;
        state[9] = forwardAgentNearest.y;
        state[10] = energyF;
        state[11] = moodF ? 1:0;
        state[12] = genderF ? 1:0;

        // zadn� agent (x,y)
        state[13] = backwardAgentNearest.x;
        state[14] = backwardAgentNearest.y;
        state[15] = energyB;
        state[16] = moodB ? 1:0;
        state[17] = genderB ? 1:0;

        int idx = 18;
        foreach (float val in meshVals)
        {
            if (idx >= state.Length) break; // ochrana proti p�ete�en� pokud NN input nen� dost velk�
            state[idx] = val;
            idx++;
        }

        float[] action = nn.Forward(state);

        if (action == null || action.Length == 0) {
            Debug.LogWarning("action is null or empty in Update()", this);
            return;
        }

        float speed = action[0];
        float rotation = action[1];
        mood = (action[2] > 0.5f) ? true : false;
        //Debug.Log(speed + ", " + rotation + ", " + mood);
        transform.Rotate(Vector3.forward, rotation);
        Vector3 move = transform.up * speed * speedScaler * Time.deltaTime;
        transform.position += move;
    }

    private void FixedUpdate()
    {
        energy -= Time.fixedDeltaTime;
        if(energy <= 0)
        {
            //AgentsManager.CheckBest(gameObject);
            gameObject.SetActive(false);
        }
    }

    public void Inicialize(List<float> vals = null)
    {
        if(vals == null) meshVals = GenerateRandomValsForMesh();
        else meshVals = vals;

        meshGenerator.SetValues(meshVals);
    }

    List<float> GenerateRandomValsForMesh()
    {
        int n = Random.Range(8, 16);

        if (n % 2 != 0)
        {
            n++;
        }

        List<float> vals = new List<float>();

        for (int i = 0; i < n; i++)
        {
            vals.Add(Random.Range(-valuesSize, valuesSize));
        }

        return vals;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "food")
        {
            energy += foodEnergy;
            score++;
            foodEaten++;
            energy = Mathf.Clamp(energy, 0, 10);
            Destroy(collision.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "agent")
        {
            AgentBase a = collision.gameObject.GetComponent<AgentBase>();
            if (a.mood && mood)
            {
                if (a.energy > 10 && energy > 10)
                {

                    if (Random.value > .5f)
                    {
                        a.CrossbreedingA(this);
                        a.energy -= 5;
                        energy -= 2;
                    }
                    else
                    {
                        CrossbreedingA(a);
                        energy -= 5;
                        a.energy -= 2;
                    }
                }
            }
            else
            {
                if (a.energy > energy)
                {
                    a.energy += agentEnergy;
                    a.score += 4;
                    a.kills++;
                    a.energy = Mathf.Clamp(a.energy, 0, 10);

                    score -= 4;

                    //AgentsManager.CheckBest(gameObject);
                    gameObject.SetActive(false);
                }
                else if (a.energy < energy)
                {
                    energy += agentEnergy;
                    score += 4;
                    kills++;
                    energy = Mathf.Clamp(energy, 0, 10);

                    a.score -= 4;

                    //AgentsManager.CheckBest(a.gameObject);
                    gameObject.SetActive(false);
                }
            }
        }
    }

    public GameObject Copy(GameObject agentPrefab)
    {
        List<ConnectionGenome> c = nn.CopyConnections();
        List<NodeGenome> node = nn.CopyNodes();

        weightBiasMutationChance = Mathf.Clamp(weightBiasMutationChance, 0.05f, .95f);
        weightBiasMutationSize = Mathf.Clamp(weightBiasMutationSize, 0.1f, 1.5f);

        agentPrefab.GetComponent<NN>().Inicialize(c, node, true, weightBiasMutationChance, weightBiasMutationSize, newConnMutationChance, splitMutationChance, nn.GetInputSize(), nn.GetInov());

        List<float> vals = new List<float>();
        for (int i = 0; i < meshVals.Count; i++)
        {
            if(Random.value < .95f || vals.Count < 4)
            {
                if (Random.value < weightBiasMutationChance)
                {
                    vals.Add(meshVals[i] + Random.Range(-weightBiasMutationSize, weightBiasMutationSize));
                }
                else
                {
                    vals.Add(meshVals[i]);
                }

                if (Random.value < weightBiasMutationChance/2 && vals.Count < 20)
                {
                    vals.Add(Random.Range(-valuesSize, valuesSize));
                }

                if(vals.Count > 20)
                {
                    int n = vals.Count - 20;

                    for (int j = 0; j < n; j++)
                    {
                        vals.RemoveAt(vals.Count - 1);
                    }
                }
            }
        }

        agentPrefab.GetComponent<AgentBase>().Inicialize(vals);

        string lastName = gameObject.name.Split(" ")[0];
        TextAsset txt1 = (TextAsset)Resources.Load("First_names.all");
        string[] dict1 = txt1.text.Split("\n"[0]);
        agentPrefab.name = lastName + " " + dict1[Random.Range(0, dict1.Length)];

        return agentPrefab;
    }


    GameObject Crossbreeding(AgentBase a, GameObject agentPrefab)
    {
        agentPrefab.transform.SetParent(transform.parent);

        List<ConnectionGenome> c1 = nn.CopyConnections();
        List<NodeGenome> n1 = nn.CopyNodes();

        List<ConnectionGenome> c2 = a.nn.CopyConnections();
        List<NodeGenome> n2 = a.nn.CopyNodes();

        Dictionary<uint, uint> nodeC2ToC1Mapping = new Dictionary<uint, uint>();
        
        Dictionary<uint, uint> nodeC1ToC2Mapping = new Dictionary<uint, uint>();

        List<ConnectionGenome> connections;
        List<NodeGenome> nodes;

        int inov = (nn.GetInov() > a.nn.GetInov()) ? nn.GetInov() : a.nn.GetInov();

        if (a.score > score)
        {
            List<ConnectionGenome> tmp = c1;
            List<NodeGenome> tmpN = n1;                                               
            c1 = c2;
            c2 = tmp;

            n1 = n2;
            n2 = tmpN;
        }

        List<float> vals = new List<float>();

        #region mesh
        int vLen = a.meshVals.Count;
        if (vLen > meshVals.Count) vLen = meshVals.Count;
        int diff = Mathf.Abs(a.meshVals.Count - meshVals.Count);

        for (int i = 0; i < vLen; i++)
        {
            float val = 0.5f;
            if (a.score > score) val -= .1f;
            if (a.score < score) val += .1f;

            if(Random.value > 0.5f)
            {
                vals.Add(a.meshVals[i]);
            }
            else
            {
                vals.Add(meshVals[i]);
            }
        }

        if (a.meshVals.Count > meshVals.Count && a.score > score)
        {
            for (int i = vLen; i < diff; i++)
            {
                vals.Add(a.meshVals[i]);
            }
        }
        else if (a.meshVals.Count < meshVals.Count && a.score < score)
        {
            for (int i = vLen; i < diff; i++)
            {
                vals.Add(meshVals[i]);
            }
        }
        #endregion


        #region NN
        connections = new List<ConnectionGenome>();
        nodes = new List<NodeGenome>();

        // čitelná aliasace již existujících slovníků
        var mapP2toChild = nodeC2ToC1Mapping; // nodeID z rodiče2 -> nodeID v child
        var mapP1toChild = nodeC1ToC2Mapping; // nodeID z rodiče1 -> nodeID v child

        // --- 1) Nejprve zpracuj výstupy (abychom měli přesný počet očekávaných výstupů v potomkovi) ---
        // (předpoklad: c1/n1 je fitnější rodič díky swapu výše)
        List<NodeGenome> p1Outputs = n1.FindAll(x => x.IsOutput);
        List<NodeGenome> p2Outputs = n2.FindAll(x => x.IsOutput);

        // Pokud p1 nemá outputy, loguj varování (fallback)
        if (p1Outputs.Count == 0)
        {
            Debug.LogWarning("Crossbreeding: p1 has no outputs!");
        }
        else
        {
            // zkopíruj výstupy z p1 (fitnější rodič) do potomka a založ mapu p1->child
            for (int i = 0; i < p1Outputs.Count; i++)
            {
                NodeGenome copy = p1Outputs[i].Copy();
                nodes.Add(copy);
                if (!mapP1toChild.ContainsKey(p1Outputs[i].nodeID))
                    mapP1toChild[p1Outputs[i].nodeID] = copy.nodeID;
            }

            // mapuj p2 výstupy na existující p1 výstupy (indexově). Pokud více p2 než p1, mapuj přebytek na poslední p1.
            for (int i = 0; i < p2Outputs.Count; i++)
            {
                if (!mapP2toChild.ContainsKey(p2Outputs[i].nodeID))
                {
                    int idx = (i < p1Outputs.Count) ? i : (p1Outputs.Count - 1);
                    if (idx >= 0 && p1Outputs.Count > 0)
                    {
                        uint targetChildId = mapP1toChild[p1Outputs[idx].nodeID];
                        mapP2toChild[p2Outputs[i].nodeID] = targetChildId;
                    }
                    else
                    {
                        // fallback: pokud p1Outputs úplně chybí, zkopíruj p2 výstup do potomka
                        NodeGenome copy2 = p2Outputs[i].Copy();
                        nodes.Add(copy2);
                        mapP2toChild[p2Outputs[i].nodeID] = copy2.nodeID;
                    }
                }
            }
        }

        // --- 2) Zpracuj vstupy obdobně (důležité kvůli očekávané velikosti inputu) ---
        List<NodeGenome> p1Inputs = n1.FindAll(x => x.IsInput);
        List<NodeGenome> p2Inputs = n2.FindAll(x => x.IsInput);

        // zkopíruj vstupy z p1 do potomka a mapuj p1->child
        for (int i = 0; i < p1Inputs.Count; i++)
        {
            // pokud už ten input node existuje v nodes, přeskoč
            if (!nodes.Exists(n => n.nodeID == p1Inputs[i].nodeID))
            {
                NodeGenome copy = p1Inputs[i].Copy();
                nodes.Add(copy);
                if (!mapP1toChild.ContainsKey(p1Inputs[i].nodeID))
                    mapP1toChild[p1Inputs[i].nodeID] = copy.nodeID;
            }
            else
            {
                if (!mapP1toChild.ContainsKey(p1Inputs[i].nodeID))
                    mapP1toChild[p1Inputs[i].nodeID] = p1Inputs[i].nodeID;
            }
        }

        // mapuj p2 vstupy na p1 (indexově), fallback: kopie p2
        for (int i = 0; i < p2Inputs.Count; i++)
        {
            if (!mapP2toChild.ContainsKey(p2Inputs[i].nodeID))
            {
                int idx = (i < p1Inputs.Count) ? i : (p1Inputs.Count - 1);
                if (idx >= 0 && p1Inputs.Count > 0)
                    mapP2toChild[p2Inputs[i].nodeID] = mapP1toChild[p1Inputs[idx].nodeID];
                else
                {
                    NodeGenome copy2 = p2Inputs[i].Copy();
                    nodes.Add(copy2);
                    mapP2toChild[p2Inputs[i].nodeID] = copy2.nodeID;
                }
            }
        }

        // --- 3) Pomocná lokální funkce: zajistí, že node z daného sourceNodes existuje v potomkovi a vrátí child nodeID ---
        // Pokud již existuje mapování source->child, vrátí ho. Pokud ne, zkopíruje node z sourceNodes do child,
        // zaregistruje mapy mapSourceToChild a (volitelně) mapOppositeToChild pro oppositeId.
        uint EnsureNodeExistsForParent(
            uint sourceNodeId,
            List<NodeGenome> sourceNodes,
            Dictionary<uint, uint> mapSourceToChild,
            Dictionary<uint, uint> mapOppositeToChild,
            uint? oppositeId
        )
        {
            // pokud máme mapping source->child, použij ho
            if (mapSourceToChild != null && mapSourceToChild.ContainsKey(sourceNodeId))
                return mapSourceToChild[sourceNodeId];

            // pokud už node s tímto ID v potomkovi existuje, zaregistruj mapping a vrať
            NodeGenome exists = nodes.Find(n => n.nodeID == sourceNodeId);
            if (exists != null)
            {
                if (mapSourceToChild != null && !mapSourceToChild.ContainsKey(sourceNodeId))
                    mapSourceToChild[sourceNodeId] = exists.nodeID;
                if (oppositeId != null && mapOppositeToChild != null && !mapOppositeToChild.ContainsKey((uint)oppositeId))
                    mapOppositeToChild[(uint)oppositeId] = exists.nodeID;
                return exists.nodeID;
            }

            // najdi v sourceNodes a zkopíruj
            NodeGenome refNode = sourceNodes.Find(n => n.nodeID == sourceNodeId);
            if (refNode != null)
            {
                NodeGenome copy = refNode.Copy();
                nodes.Add(copy);

                if (mapSourceToChild != null && !mapSourceToChild.ContainsKey(sourceNodeId))
                    mapSourceToChild[sourceNodeId] = copy.nodeID;
                if (oppositeId != null && mapOppositeToChild != null && !mapOppositeToChild.ContainsKey((uint)oppositeId))
                    mapOppositeToChild[(uint)oppositeId] = copy.nodeID;
                return copy.nodeID;
            }

            // fallback
            Debug.LogWarning($"Node {sourceNodeId} not found in provided source nodes.");
            return sourceNodeId;
        }

        // --- 4) Projdi všechny connection (c1 = fitnější rodič) a sestav connections list potomka ---
        // - matching genes: náhodně vyber z rodiče1 nebo rodiče2, ale vždy remapuj nodeID na child (pomocí výše map)
        // - disjoint/excess: vezmi z c1 (fitnější) a zajisti jeho nod-y
        for (int i = 0; i < c1.Count; i++)
        {
            ConnectionGenome conn1 = c1[i];
            ConnectionGenome conn2Ref = c2.Find(c => c.inov == conn1.inov);

            if (conn2Ref != null)
            {
                bool pickFromC2 = (Random.value > 0.5f);
                ConnectionGenome chosen = pickFromC2 ? conn2Ref.Copy() : conn1.Copy();
                ConnectionGenome notChosen = pickFromC2 ? conn1.Copy() : conn2Ref.Copy();
                List<NodeGenome> sourceNodeList = pickFromC2 ? n2 : n1;

                // zajisti existence/ remap uzlů podle toho, z kterého rodiče chosen pochází
                if (pickFromC2)
                {
                    uint newIn = EnsureNodeExistsForParent(chosen.inNode, sourceNodeList, mapP2toChild, mapP1toChild, notChosen.inNode);
                    uint newOut = EnsureNodeExistsForParent(chosen.outNode, sourceNodeList, mapP2toChild, mapP1toChild, notChosen.outNode);
                    chosen.inNode = newIn;
                    chosen.outNode = newOut;
                }
                else
                {
                    uint newIn = EnsureNodeExistsForParent(chosen.inNode, sourceNodeList, mapP1toChild, mapP2toChild, notChosen.inNode);
                    uint newOut = EnsureNodeExistsForParent(chosen.outNode, sourceNodeList, mapP1toChild, mapP2toChild, notChosen.outNode);
                    chosen.inNode = newIn;
                    chosen.outNode = newOut;
                }

                if (!connections.Exists(x => x.inov == chosen.inov))
                    connections.Add(chosen);
            }
            else
            {
                // disjoint/excess -> dědí se z více fit rodiče (c1)
                ConnectionGenome chosen = conn1.Copy();

                uint newIn = EnsureNodeExistsForParent(chosen.inNode, n1, mapP1toChild, mapP2toChild, null);
                uint newOut = EnsureNodeExistsForParent(chosen.outNode, n1, mapP1toChild, mapP2toChild, null);
                chosen.inNode = newIn;
                chosen.outNode = newOut;

                if (!connections.Exists(x => x.inov == chosen.inov))
                    connections.Add(chosen);
            }
        }

        // --- 5) Safety: ujisti se, že v nodes máme všechny required input nodes (pokud jich ještě chybí) ---
        foreach (NodeGenome inN in n1.Where(x => x.IsInput))
        {
            if (!nodes.Exists(n => n.nodeID == inN.nodeID))
            {
                NodeGenome copy = inN.Copy();
                nodes.Add(copy);
                if (!mapP1toChild.ContainsKey(inN.nodeID))
                    mapP1toChild[inN.nodeID] = copy.nodeID;
            }
        }

        // (volitelně) zkontroluj konzistenci: počet output v potomkovi
        int childOutputs = nodes.FindAll(x => x.IsOutput).Count;
        if (childOutputs == 0)
        {
            Debug.LogWarning("Child has 0 outputs after crossover - this will break NN inicialize.");
        }

        int count = nodes.Count;
        nodes.RemoveAll(node =>
            !node.IsInput && !node.IsOutput &&
            !connections.Exists(conn => conn.inNode == node.nodeID || conn.outNode == node.nodeID)
        );
        count -= nodes.Count;
        if(count > 0)
            Debug.LogWarning($"Crossbreeding: pruned orphan hidden nodes: {count}, remaining nodes: {nodes.Count}, conns: {connections.Count}");

        #endregion




        weightBiasMutationChance = Mathf.Clamp(weightBiasMutationChance, 0.05f, .95f);
        weightBiasMutationSize = Mathf.Clamp(weightBiasMutationSize, 0.1f, 1.5f);

        agentPrefab.GetComponent<NN>().Inicialize(connections, nodes, true, weightBiasMutationChance, weightBiasMutationSize, newConnMutationChance, splitMutationChance, nn.GetInputSize(), inov);

        for (int i = 0; i < vals.Count; i++)
        {
            if (Random.value < weightBiasMutationChance)
            {
                vals[i] += Random.Range(-weightBiasMutationSize, weightBiasMutationSize);
            }

            if (Random.value < weightBiasMutationChance / 2 && vals.Count < 20)
            {
                vals.Add(Random.Range(-valuesSize, valuesSize));
            }

            if (vals.Count > 20)
            {
                int n = vals.Count - 20;

                for (int j = 0; j < n; j++)
                {
                    vals.RemoveAt(vals.Count - 1);
                }
            }
        }

        agentPrefab.GetComponent<AgentBase>().Inicialize(vals);

        string lastName = a.gameObject.name.Split(" ")[0];
        TextAsset txt1 = (TextAsset)Resources.Load("First_names.all");
        string[] dict1 = txt1.text.Split("\n"[0]);
        agentPrefab.name = lastName + " " + dict1[Random.Range(0, dict1.Length)];

        //Debug.Log("New Agent: " + agentPrefab.name);
        return agentPrefab;
    }

    public GameObject CrossbreedingG(AgentBase a, GameObject prefab)
    {
        return Crossbreeding(a, prefab);
    }

    void CrossbreedingA(AgentBase a)
    {
        GameObject prefab = AgentsManager.GetPrefab();
        GameObject agentPrefab = Instantiate(prefab, (transform.position + new Vector3(0, 10, 0)), transform.rotation);
        agentPrefab = Crossbreeding(a, agentPrefab);

        a.children++;
        children++;
    }
}
