using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSeeker : MonoBehaviour
{
    private GridGenerator gridGen;
    private RobotPathfinding pathfinding;

    [Header("Search Settings")]
    public float bombDetectRange = 5f;

    private int currentGridIndex = 0;
    private bool moving = false;

    void Start()
    {
        gridGen = FindObjectOfType<GridGenerator>();
        pathfinding = GetComponent<RobotPathfinding>();

        MoveToNextGrid();
    }

    void Update()
    {
        if (!moving) return;

        if (pathfinding.target && Vector3.Distance(transform.position, pathfinding.target.position) <= pathfinding.arrivalDistance)
        {
            moving = false;
            StartCoroutine(SearchForBomb());
        }
    }

    IEnumerator SearchForBomb()
    {
        yield return new WaitForSeconds(0.5f);

        Collider[] hits = Physics.OverlapSphere(transform.position, bombDetectRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bombs") && hit.gameObject.activeSelf)
            {
                pathfinding.target = hit.transform;
                Debug.Log("<color=yellow>[Bomb]</color> Bomb detected nearby!");
                yield break;
            }
        }

        currentGridIndex++;
        if (currentGridIndex < gridGen.gridPoints.Count)
        {
            MoveToNextGrid();
        }
        else
        {
            Debug.Log("<color=lime>[Search]</color> Finished sweeping entire map!");
        }
    }

    void MoveToNextGrid()
    {
        if (currentGridIndex >= gridGen.gridPoints.Count)
        {
            Debug.LogWarning("No more grid points to move to.");
            return;
        }

        GameObject tempTarget = new GameObject("GridPointTarget");
        tempTarget.transform.position = gridGen.gridPoints[currentGridIndex];
        Destroy(tempTarget, 2f);

        pathfinding.target = tempTarget.transform;
        moving = true;
    }
}
