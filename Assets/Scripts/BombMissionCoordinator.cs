using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombMissionCoordinator : MonoBehaviour
{
    public MultiTargetPathfinder multiTargetPathfinder;
    public RobotPathfinding pathfinding;
    public LocalAStarSearch localAStarSearch;

    private bool waitingLocalSearch = false;

    void Start()
    {
        if (multiTargetPathfinder == null) multiTargetPathfinder = GetComponent<MultiTargetPathfinder>();
        if (pathfinding == null) pathfinding = GetComponent<RobotPathfinding>();
        if (localAStarSearch == null) localAStarSearch = GetComponent<LocalAStarSearch>();

        multiTargetPathfinder.StartMultiTargetNavigation();
    }

    void Update()
    {
        if (multiTargetPathfinder == null || pathfinding == null || localAStarSearch == null)
            return;

        if (waitingLocalSearch)
        {
            // Tunggu sampai LocalAStar selesai
            if (!localAStarSearch.isSearching)
            {
                Debug.Log("<color=lime>[Mission]</color> Local search finished, moving to next target...");
                waitingLocalSearch = false; // <-- INI FIX-nya!
                multiTargetPathfinder.ForceMoveToNextTarget();
            }
            return;
        }

        if (pathfinding.target != null && pathfinding.distanceToTarget <= pathfinding.arrivalDistance)
        {
            Debug.Log("<color=cyan>[Mission]</color> Arrived at center. Starting Local A* search...");
            waitingLocalSearch = true;
            localAStarSearch.BeginSearch();
        }
    }
}
