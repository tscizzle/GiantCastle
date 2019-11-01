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
    private static bool isOnCastleEdge(Vector3 cell)
    {
        return cell.x == 0 || cell.z == 0;
    }

    private Dictionary<Vector3, List<GameObject>> cellsGenerated = new Dictionary<Vector3, List<GameObject>>();
    private Dictionary<Vector3, bool> cellsVisitedByPlayer = new Dictionary<Vector3, bool>();
    private GameObject player;
    private GameObject epicPerson;
    private GameObject robotKyle;

    public GameObject castleTowerPrefab;
    public GameObject castleTowerNoRoofPrefab;
    public GameObject castleWallPrefab;
    public GameObject castleFlatPrefab;

    void Start()
    {
        player = GameObject.Find("Player");
        epicPerson = GameObject.Find("Epic Person");
        robotKyle = GameObject.Find("Robot Kyle");
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

        float cellEastX = (cell.x + 1) * cellSize;
        float cellNorthZ = (cell.z + 1) * cellSize;
        float cellCenterX = (cell.x + 0.5f) * cellSize;
        float cellCenterY = 0;
        float cellCenterZ = (cell.z + 0.5f) * cellSize;

        /* create a tower in this cell, at a random altitude */

        float chanceOfRoof = 0.2f;
        float flipForRoof = Random.Range(0.0f, 1.0f);
        bool withRoof = flipForRoof <= chanceOfRoof;

        // unless this cell is on the castle edge, randomly offset its altitude
        float castleTowerY = cellCenterY + (isOnCastleEdge(cell) ? 20 : Random.Range(-100, 50));

        Vector3 castleTowerPosition = new Vector3(cellCenterX, castleTowerY, cellCenterZ);

        GameObject castleTower = createCastleTower(castleTowerPosition, withRoof);
        cellGameObjects.Add(castleTower);

        /* create a flat, with some probability, and at a random altitude */

        float chanceOfFlat = 0.8f;
        float flipForFlat = Random.Range(0.0f, 1.0f);
        if (flipForFlat <= chanceOfFlat)
        {
            float castleFlatY = cellCenterY + Random.Range(-150, -50);
            Vector3 castleFlatPosition = new Vector3(cellEastX, castleFlatY, cellNorthZ);

            GameObject castleFlat = createCastleFlat(castleFlatPosition, cellSize, cellSize);
            cellGameObjects.Add(castleFlat);
        }

        /* create walls along the outside of the castle */

        if (cell.x == 0)
        {
            Vector3 castleWallNorthSouthPosition = new Vector3(cellCenterX, 0, cellNorthZ);
            GameObject castleWallNorthSouth = createCastleWall(castleWallNorthSouthPosition, cellSize, true);
            cellGameObjects.Add(castleWallNorthSouth);
        }

        if (cell.z == 0)
        {
            Vector3 castleWallEastWestPosition = new Vector3(cellEastX, 0, cellCenterZ);
            GameObject castleWallEastWest = createCastleWall(castleWallEastWestPosition, cellSize, false);
            cellGameObjects.Add(castleWallEastWest);

            // place the epic person, if this is the corner of the castle

            if (cell.x == 0)
            {
                float castleWallTop = castleWallEastWest.transform.localScale.y / 2;
                float castleWallFront = castleWallEastWestPosition.z + (-1 * castleWallEastWest.transform.localScale.z / 2);
                epicPerson.transform.position = new Vector3(cellEastX, castleWallTop, castleWallFront + 1);
                robotKyle.transform.position = new Vector3(cellEastX + 2, castleWallTop, castleWallFront + 1);
            }
        }

        /* put all castle objects inside the same container (the Castle container this script is attached to) */

        foreach (GameObject go in cellGameObjects)
        {
            go.transform.parent = transform;
        }

        return cellGameObjects;
    }

    private GameObject createCastleTower(Vector3 position, bool withRoof)
    {
        GameObject towerPrefabToUse = withRoof ? castleTowerPrefab : castleTowerNoRoofPrefab;

        GameObject castleTower = Instantiate(towerPrefabToUse, position, Quaternion.identity);


        return castleTower;
    }

    private GameObject createCastleWall(Vector3 position, float length, bool isNorthSouth)
    {
        Quaternion rotation = isNorthSouth ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;

        GameObject castleWall = Instantiate(castleWallPrefab, position, rotation);

        float sameY = castleWall.transform.localScale.y;
        float sameZ = castleWall.transform.localScale.z;

        castleWall.transform.localScale = new Vector3(length, sameY, sameZ);

        return castleWall;
    }

    private GameObject createCastleFlat(Vector3 position, float xLength, float zLength)
    {
        GameObject castleFlat = Instantiate(castleFlatPrefab, position, Quaternion.identity);

        float sameY = castleFlat.transform.localScale.y;

        castleFlat.transform.localScale = new Vector3(xLength, sameY, zLength);

        return castleFlat;
    }
}
