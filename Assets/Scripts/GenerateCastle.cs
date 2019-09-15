using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateCastle : MonoBehaviour
{
    private static float cellSize = 200;
    private static float numCellsFromPlayer = 20;
    private static bool isInCastleZone(Vector3 cell)
    {
        return cell.x >= 0 && cell.z >= 0;
    }

    private Dictionary<Vector3, List<GameObject>> cellsGenerated = new Dictionary<Vector3, List<GameObject>>();
    private Dictionary<Vector3, bool> cellsVisitedByPlayer = new Dictionary<Vector3, bool>();
    private GameObject player;

    public GameObject towerPrefab;
    public GameObject wallPrefab;

    void Start()
    {
        player = GameObject.Find("Player");
    }

    void Update()
    {
        float roundedPlayerX = Mathf.Floor(player.transform.position.x / cellSize);
        float roundedPlayerZ = Mathf.Floor(player.transform.position.z / cellSize);
        Vector3 playerCurrentCell = new Vector3(roundedPlayerX, 0, roundedPlayerZ);

        bool needsAnyGenerating = !cellsVisitedByPlayer.ContainsKey(playerCurrentCell);

        if (needsAnyGenerating)
        {
            float startX = playerCurrentCell.x - numCellsFromPlayer;
            float endX = playerCurrentCell.x + numCellsFromPlayer;
            float startZ = playerCurrentCell.z - numCellsFromPlayer;
            float endZ = playerCurrentCell.z + numCellsFromPlayer;

            for (float cellIdxX = startX; cellIdxX <= endX; cellIdxX++)
            {
                for (float cellIdxZ = startZ; cellIdxZ <= endZ; cellIdxZ++)
                {
                    Vector3 cell = new Vector3(cellIdxX, 0, cellIdxZ);



                    bool cellNeedsGenerating = !cellsGenerated.ContainsKey(cell);

                    if (cellNeedsGenerating)
                    {
                        List<GameObject> cellGameObjects = generateCastleInCell(cell);

                        cellsGenerated.Add(cell, cellGameObjects);
                    }
                }
            }
            
            cellsVisitedByPlayer.Add(playerCurrentCell, true);
        }
    }

    private List<GameObject> generateCastleInCell(Vector3 cell)
    {
        List<GameObject> cellGameObjects = new List<GameObject>();

        float cellCenterX = (cell.x + 0.5f) * cellSize;
        float cellCenterZ = (cell.z + 0.5f) * cellSize;
        Vector3 cellCenter = new Vector3(cellCenterX, 0, cellCenterZ);
        
        GameObject tower = Instantiate(towerPrefab, cellCenter, Quaternion.identity);
        cellGameObjects.Add(tower);

        return cellGameObjects;
    }
}
