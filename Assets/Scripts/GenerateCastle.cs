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
    private bool hasEpicPersonBeenPlaced = false;
    private GameObject player;
    private GameObject epicPerson;

    public GameObject castleTowerPrefab;
    public GameObject castleWallPrefab;
    public GameObject castleFlatPrefab;

    void Start()
    {
        player = GameObject.Find("Player");
        epicPerson = GameObject.Find("Epic Person");
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

                    bool cellIsInCastleZone = isInCastleZone(cell);

                    bool cellNeedsGenerating = !cellsGenerated.ContainsKey(cell);

                    if (cellIsInCastleZone && cellNeedsGenerating)
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
        
        /* create a tower in this cell */

        GameObject castleTower = createCastleTower(cellCenter);
        cellGameObjects.Add(castleTower);

        /* create a flat, with some probability, and at a random altitude */

        float chanceOfFlat = 0.8f;
        float rand = Random.Range(0.0f, 1.0f);
        if (rand <= chanceOfFlat)
        {
            float cellRightX = (cell.x + 1) * cellSize;
            float cellNorthZ = (cell.z + 1) * cellSize;
            float castleFlatAltitude = Random.Range(-150, -50);
            Vector3 castleFlatPosition = new Vector3(cellRightX, castleFlatAltitude, cellNorthZ);

            GameObject castleFlat = createCastleFlat(castleFlatPosition, cellSize, cellSize);
            cellGameObjects.Add(castleFlat);

            /* if the person hasn't been placed yet, place them on this flat */

            if (!hasEpicPersonBeenPlaced)
            {
                float castleFlatTop = castleFlatPosition.y + castleFlat.transform.localScale.y / 2;
                epicPerson.transform.position = new Vector3(cellRightX, castleFlatTop, cellCenterZ + 1);

                hasEpicPersonBeenPlaced = true;
            }
        }

        return cellGameObjects;
    }

    private GameObject createCastleTower(Vector3 position)
    {
        GameObject castleTower = Instantiate(castleTowerPrefab, position, Quaternion.identity);

        castleTower.transform.parent = transform;

        return castleTower;
    }

    private GameObject createCastleFlat(Vector3 position, float xLength, float zLength)
    {
        GameObject castleFlat = Instantiate(castleFlatPrefab, position, Quaternion.identity);
        
        float sameY = castleFlat.transform.localScale.y;

        castleFlat.transform.localScale = new Vector3(xLength, sameY, zLength);

        castleFlat.transform.parent = transform;
        
        return castleFlat;
    }
}
