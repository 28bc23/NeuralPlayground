using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AgentsManager : MonoBehaviour
{
    InputSystem_Actions inputActions;
    [SerializeField] GameObject statsPanel;
    [SerializeField] NNGraphMaker nnPanel;
    [SerializeField] TMP_Text nameStats;
    [SerializeField] TMP_Text scoreStats;
    [SerializeField] TMP_Text killsStats;
    [SerializeField] TMP_Text foodStats;
    [SerializeField] TMP_Text childrenStats;
    [SerializeField] TMP_Text timerTMP;

    [SerializeField] string scoreStr = "Score: ";
    [SerializeField] string killsStr = "Kills: ";
    [SerializeField] string foodStr = "Food eaten: ";
    [SerializeField] string childrenStr = "Chindren: ";
    [SerializeField] string timerStr = "Time: ";
    [SerializeField] float timeLimit = 60f;

    [SerializeField] int agentCount = 30;
    [SerializeField] Vector2 spawningArea = new Vector2(100, 100);
    [SerializeField] GameObject agentPrefab;
    [SerializeField] TMP_Text genText;
    [SerializeField] TMP_Text bestText;
    [SerializeField] TMP_Text bestFeText;
    [SerializeField] FoodManager foodManager;
    static TMP_Text bestTextStatic;
    static TMP_Text bestFeTextStatic;
    static GameObject agentPrefabStatic;

    public static GameObject best;
    public static GameObject bestFe;
    GameObject lastBest;
    public static int gen = 0;

    bool wasGen0 = false;

    static List<uint> usedIDs = new List<uint>();

    private float timer; 
    

    private void Awake()
    {
        bestTextStatic = bestText;
        bestFeTextStatic = bestFeText;
        agentPrefabStatic = agentPrefab;
        inputActions = new InputSystem_Actions();
    }

    #region Enable/Disable - inputSystem
    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.LeftClick.performed += OnClicked_Performed;
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }
    #endregion

    public void OnClicked_Performed(InputAction.CallbackContext cbx)
    {
        Vector2 mousePosStart = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosStart);
        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll (ray, Mathf.Infinity);

        foreach (RaycastHit2D hit in hits)
        {
            AgentBase agentBase;
            if(hit.transform.TryGetComponent<AgentBase>(out agentBase))
            {
                nameStats.text = agentBase.gameObject.name;
                scoreStats.text = scoreStr + agentBase.score;
                killsStats.text = killsStr + agentBase.kills;
                foodStats.text = foodStr + agentBase.foodEaten;
                childrenStats.text = childrenStr + agentBase.children;
                nnPanel.MakeGraph(agentBase.nn.CopyConnections(), agentBase.nn.CopyNodes());
                statsPanel.SetActive(true);
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bool sw = false;
        for (int i = 0; i < agentCount; i++)
        {
            float x = Random.Range(-spawningArea.x, spawningArea.x);
            float y = Random.Range(-spawningArea.y, spawningArea.y);
            Vector3 pos = new Vector3(x, y);
            Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));

            GameObject go = Instantiate(agentPrefab, pos, rot);
            go.transform.parent = transform;

            TextAsset txt = (TextAsset)Resources.Load("Last_names.all");
            string[] dict = txt.text.Split("\n"[0]);
            TextAsset txt1 = (TextAsset)Resources.Load("First_names.all");
            string[] dict1 = txt1.text.Split("\n"[0]);
            go.name = dict[Random.Range(0, dict.Length)] + " " + dict1[Random.Range(0, dict1.Length)];
            //go.GetComponent<AgentBase>().gender = sw;
            go.GetComponent<AgentBase>().Inicialize();
            sw = !sw;
            go.GetComponent<NN>().RandomInicialize();
        }
        genText.text = "Gen: " + gen;
        wasGen0 = true;
        timer = timeLimit;
    }

    public static void CheckBest(GameObject agent)
    {
        if (agent == null) return;

        AgentBase agentBase = agent.GetComponent<AgentBase>();
        if (agentBase == null)
        {
            Destroy(agent);
            return;
        }

        if (best == null)
        {
            best = agent;
            best.SetActive(false);
            Debug.Log("Initial best set: " + best.name + " foodEaten=" + agentBase.score);
            return;
        }

        if (bestFe == null)
        {
            bestFe = agent;
            bestFe.SetActive(false);
            Debug.Log("Initial best set: " + bestFe.name + " foodEaten=" + agentBase.score);
            return;
        }

        AgentBase bestBase = best.GetComponent<AgentBase>();
        AgentBase bestBaseFe = bestFe.GetComponent<AgentBase>();
        if (bestBase == null)
        {
            Destroy(best);
            best = agent;
            best.SetActive(false);
            Debug.Log("Replaced broken best with: " + best.name);
            return;
        }

        if (agentBase.score > bestBase.score /*&& !agentBase.gender*/)
        {
            Destroy(best);
            best = agent;
            best.SetActive(false);
            Debug.Log("New best: " + best.name + " foodEaten=" + agentBase.score);
            bestTextStatic.text = "Best male agent: " + best.name + ", score=" + agentBase.score;
        }else if(agentBase.score > bestBaseFe.score /*&& agentBase.gender*/)
        {
            Destroy(bestFe);
            bestFe = agent;
            bestFe.SetActive(false);
            Debug.Log("New best: " + bestFe.name + " foodEaten=" + agentBase.score);
            bestFeTextStatic.text = "Best female agent: " + bestFe.name + ", score=" + agentBase.score;
        }
        else
        {
            Destroy(agent);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //TODO: After all die or timer reaches 0 run evaluate function that will sort all agents (dead or alive) based on achived score. Take upper 10 agents
        // And crossbreed them, so we will have more spicies
        int activeCount = 0;
        foreach (Transform t in transform)
        {
            if(t.gameObject.activeInHierarchy)
                activeCount++;
        }

        if (activeCount == 0)
        {
            Evaluate();
        }

        timer -= Time.deltaTime;
        timerTMP.text = timerStr + timer;

        if(timer <= 0)
        {
            timer = timeLimit;
            timerTMP.text = timerStr + timer;

            foreach (Transform t in transform)
            {
                if (!t.gameObject.activeInHierarchy)
                    continue;
                //CheckBest(t.gameObject);
                t.gameObject.SetActive(false);
            }
            Evaluate();
        }
    }

    void SpawnNextGen()
    {
        if(!wasGen0) return;

        if(best == null)
        {
            Debug.Log("Best is null cannot make next gen");
            return;
        }
        if (bestFe == null)
        {
            Debug.Log("Best is null cannot make next gen");
            return;
        }
        gen++;

        foodManager.Clear();

        for (int i = 0; i < agentCount; i++)
        {
            float x = Random.Range(-spawningArea.x, spawningArea.x);
            float y = Random.Range(-spawningArea.y, spawningArea.y);
            Vector3 pos = new Vector3(x, y);
            Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));

            GameObject go = bestFe.GetComponent<AgentBase>().CrossbreedingG(best.GetComponent<AgentBase>(), Instantiate(agentPrefab, pos, rot));
            go.transform.parent = transform;
        }
        Destroy(best);
        Destroy(bestFe);
        best = null;
        bestFe = null;

        genText.text = "Gen: " + gen;
        timer = timeLimit;
        timerTMP.text = timerStr + timer;
    }

    public static GameObject GetPrefab()
    {
        return agentPrefabStatic;
    }


    public static uint GetFreeID()
    {
        uint id = 0;
        while (true)
        {
            id = (uint)Random.Range(0, uint.MaxValue);

            if (usedIDs.Contains(id)) continue;

            usedIDs.Add(id);
            break;
        }
        return id;
    }

    void Evaluate()
    {
        //Debug.Log("Start evalutaing");
        List<AgentBase> agents = new List<AgentBase>();
        List<AgentBase> selectedAgents = new List<AgentBase>();

        foreach(Transform t in transform)
        {
            if (!t.gameObject.activeInHierarchy)
            {
                agents.Add(t.GetComponent<AgentBase>());
            }
        }
        List<AgentBase> sortedAgents = agents.OrderByDescending(a => a.score).ToList();
        selectedAgents = sortedAgents.Take(10).ToList();
        Debug.Log($"Max: {sortedAgents[0].score}, min: {sortedAgents[agents.Count - 1].score}");
        Debug.Log($"Max: {selectedAgents.OrderByDescending(a => a.score).ToList()[0].score}, min: {selectedAgents.OrderByDescending(a => a.score).ToList()[selectedAgents.Count - 1].score}");

        bestText.text = "Last gen best score: " + sortedAgents[0].score;
        bestFeText.text = "Last gen worst score: " + sortedAgents[sortedAgents.Count - 1].score;

        while(selectedAgents.Count > 0)
        {
            int randIdx1 = Random.Range(0, selectedAgents.Count-1);
            int randIdx2 = Random.Range(0, selectedAgents.Count-1);
            for (int i = 0; i < agentCount / (10 / 2); i++)
            {
                float x = Random.Range(-spawningArea.x, spawningArea.x);
                float y = Random.Range(-spawningArea.y, spawningArea.y);
                Vector3 pos = new Vector3(x, y);
                Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));

                GameObject go = selectedAgents[randIdx1].CrossbreedingG(selectedAgents[randIdx2], Instantiate(agentPrefab, pos, rot));
                go.transform.parent = transform;
            }
            selectedAgents.RemoveAt(randIdx1);
            selectedAgents.RemoveAt(randIdx2);
        }       

        foreach(Transform t in transform)
        {
            if(!t.gameObject.activeInHierarchy)
                Destroy(t.gameObject);
        }
        gen++;
        genText.text = "Gen: " + gen;
        timer = timeLimit;
        timerTMP.text = timerStr + timer;
        //Debug.Log("Stop evalutaing");    
    }
}
