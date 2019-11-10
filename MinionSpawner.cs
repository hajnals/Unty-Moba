using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionSpawner : MonoBehaviour {
    // Public config parameters
    public int numberOfMinions = 2;

    // Object references
    [SerializeField]
    List<Transform> waypoints = new List<Transform>();

    // The prefab of the minion
    [SerializeField]
    GameObject MinionPrefab;

    // The material of the minion
    [SerializeField]
    Material MinionMaterial;

    // For adding tags to the gameobject
    [SerializeField]
    List<string> tags = new List<string>();

    [SerializeField]
    int spawnTime = 10;

    // Minion counter
    uint MinCnt = 0;

    // Spawner coroutine
    IEnumerator spawner;

    // Start is called before the first frame update
    void Start()
    {
        // Start spawning minions coroutine
        spawner = SpawnMinions();
        StartCoroutine(spawner);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Spawning minions indefinetly at every 10 sec
    IEnumerator SpawnMinions() {
        // Spawn minions indefinetly
        while (true) {
            // Debug.Log("Spawning minions!");
            // Spawn minions numberOfMinions minions
            for (int n = 0; n < numberOfMinions; n++) {
                // Instantiate minion
                GameObject minion = Instantiate(MinionPrefab,
                                                transform.position + transform.forward * n * 1.1f + transform.up,
                                                transform.rotation);
                // Set targets for this minion
                minion.GetComponent<NewMinionCtrl>().SetWaypoints(waypoints);

                // Assign the minion material
                Renderer minRend = minion.GetComponent<Renderer>();
                if (minRend != null) {
                    minRend.material = MinionMaterial;
                }
                else {
                    Debug.LogWarningFormat("MinionSpawner/SpawnMinions, We have a problem here!");
                }

                // Set tag for the minion the same as the spawner
                minion.tag = tag;
                minion.name = $"{tag}_Minion_{MinCnt}";
                MinCnt++;
            }

            // Wait for 10 secound before spawing minions again
            yield return new WaitForSeconds(1000);
        }
    }
}
