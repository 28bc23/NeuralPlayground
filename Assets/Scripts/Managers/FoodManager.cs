using UnityEngine;

public class FoodManager : MonoBehaviour
{
    [SerializeField] float spawnRate = .5f;
    [SerializeField] Vector2 spawningArea = new Vector2(100, 100);
    [SerializeField] GameObject foodPrefab;

    float timer;
    bool pause = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pause) return;

        timer += Time.deltaTime;

        if (timer >= spawnRate)
        {
            timer = 0;

            float x = Random.Range(-spawningArea.x, spawningArea.x);
            float y = Random.Range(-spawningArea.y, spawningArea.y);
            Vector3 pos = new Vector3(x, y);
            Quaternion rot = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));

            GameObject go = Instantiate(foodPrefab, pos, rot);
            go.transform.parent = transform;
        }
    }

    public void Clear()
    {
        pause = true;
        foreach (Transform go in transform)
        {
            Destroy(go.gameObject);
        }

        pause = false;
    }
}
