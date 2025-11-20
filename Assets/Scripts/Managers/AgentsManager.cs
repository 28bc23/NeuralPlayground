using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AgentsManager : MonoBehaviour
{
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
    

    private void Awake()
    {
        bestTextStatic = bestText;
        bestFeTextStatic = bestFeText;
        agentPrefabStatic = agentPrefab;
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
            go.GetComponent<AgentBase>().gender = sw;
            go.GetComponent<AgentBase>().Inicialize();
            sw = !sw;
            go.GetComponent<NN>().RandomInicialize();
        }
        genText.text = "Gen: " + gen;
        wasGen0 = true;
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

        if (agentBase.score > bestBase.score && !agentBase.gender)
        {
            Destroy(best);
            best = agent;
            best.SetActive(false);
            Debug.Log("New best: " + best.name + " foodEaten=" + agentBase.score);
            bestTextStatic.text = "Best male agent: " + best.name + ", score=" + agentBase.score;
        }else if(agentBase.score > bestBaseFe.score && agentBase.gender)
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
        if (transform.childCount == 2)
        {
            SpawnNextGen();
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
}
