using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private float cellHeight = 1f;
    [SerializeField] private float cellWidth = 1f;
    [SerializeField] private float percentageWalls = 20f;

    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform gridParent;

    private Dictionary<Vector2, Cell> cells;
    private GameObject player;
    private Vector2 playerPosition;
    public List<Vector2> path;
    private bool isMoving;

    private void Start()
    {
        GenerateGrid();
        SpawnPlayer();
    }

    private void GenerateGrid()
    {
        cells = new Dictionary<Vector2, Cell>();
        List<Vector2> availablePositions = new List<Vector2>();

        for (float x = -gridWidth; x <= gridWidth; x += cellWidth)
        {
            for (float y = -gridHeight; y <= gridHeight; y += cellHeight)
            {
                Vector2 pos = new Vector2(x, y);
                cells.Add(pos, new Cell(pos));
                availablePositions.Add(pos);
            }
        }

        int wallCount = Mathf.FloorToInt(availablePositions.Count * (percentageWalls / 100f));
        for (int i = 0; i < wallCount; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2 pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);
            cells[pos].isWall = true;
        }

        foreach (var kvp in cells)
        {
           kvp.Value.cellObject = Instantiate(kvp.Value.isWall ? wall : floor, kvp.Key, Quaternion.identity, gridParent);
        }
    }

    private void SpawnPlayer()
    {
        List<Vector2> freeCells = new List<Vector2>();
        foreach (var kvp in cells)
        {
            if (!kvp.Value.isWall) freeCells.Add(kvp.Key);
        }

        playerPosition = freeCells[Random.Range(0, freeCells.Count)];
        player = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 gridTarget = new Vector2(Mathf.Round(target.x), Mathf.Round(target.y));
            if (cells.ContainsKey(gridTarget) && !cells[gridTarget].isWall)
            {
                path = FindPath(playerPosition, gridTarget);
                StartCoroutine(MovePlayer());
            }
        }
    }

    private void HighlightedPath(List<Vector2> path)
    {
        foreach (var kvp in path)
        {
            
        }
    }

    private IEnumerator MovePlayer()
    {
        isMoving = true;

     
        yield return new WaitForSeconds(0.5f);
        

        foreach (Vector2 step in path)
        {
            player.transform.position = step;
            cells[step].cellObject.GetComponent<SpriteRenderer>().color = Color.white;
            yield return new WaitForSeconds(0.5f);
        }
        isMoving = false;
    }

    private List<Vector2> FindPath(Vector2 startPos, Vector2 endPos)
    {
        List<Vector2> finalPath = new List<Vector2>();
        List<Vector2> searchedCells = new List<Vector2>();
        List<Vector2> cellsToSearch = new List<Vector2> { startPos };

        cells[startPos].gCost = 0;
        cells[startPos].hCost = GetDistance(startPos, endPos);
        cells[startPos].fCost = cells[startPos].hCost;

        while (cellsToSearch.Count > 0)
        {
            Vector2 currentCell = cellsToSearch[0];
            foreach (Vector2 pos in cellsToSearch)
            {
                if (cells[pos].fCost < cells[currentCell].fCost)
                    currentCell = pos;
            }

            if (currentCell == endPos)
            {
                while (cells[currentCell].connection != startPos)
                {
                    finalPath.Add(currentCell);
                    currentCell = cells[currentCell].connection;
                    cells[currentCell].cellObject.GetComponent<SpriteRenderer>().color = Color.green;
                }
                finalPath.Reverse();
                return finalPath;
            }

            cellsToSearch.Remove(currentCell);
            searchedCells.Add(currentCell);

            foreach (Vector2 neighbour in GetNeighbours(currentCell))
            {
                if (searchedCells.Contains(neighbour) || cells[neighbour].isWall)
                    continue;

                int newGCost = cells[currentCell].gCost + 10;
                if (!cellsToSearch.Contains(neighbour) || newGCost < cells[neighbour].gCost)
                {
                    cells[neighbour].connection = currentCell;
                    cells[neighbour].gCost = newGCost;
                    cells[neighbour].hCost = GetDistance(neighbour, endPos);
                    cells[neighbour].fCost = cells[neighbour].gCost + cells[neighbour].hCost;
                    cells[neighbour].cellObject.GetComponent<SpriteRenderer>().color = Color.red;
                    if (!cellsToSearch.Contains(neighbour))
                        cellsToSearch.Add(neighbour);
                }
            }
        }
        return finalPath;
    }

    private List<Vector2> GetNeighbours(Vector2 cellPos)
    {
        List<Vector2> neighbors = new List<Vector2>();
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in directions)
        {
            Vector2 neighbor = cellPos + dir;
            if (cells.ContainsKey(neighbor))
                neighbors.Add(neighbor);
        }
        return neighbors;
    }

    private int GetDistance(Vector2 pos1, Vector2 pos2)
    {
        return (int)(Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y)) * 10;
    }

    private class Cell
    {
        public Vector2 position;
        public GameObject cellObject;
        public int fCost = int.MaxValue;
        public int gCost = int.MaxValue;
        public int hCost = int.MaxValue;
        public Vector2 connection;
        public bool isWall;
        public Cell(Vector2 pos) { position = pos; }
    }
}


