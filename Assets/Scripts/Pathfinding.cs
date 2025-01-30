using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private float cellHeight = 1f;
    [SerializeField] private float cellWidth = 1f;
    [SerializeField] private float percentageWalls = 1f;

    [SerializeField] private bool generatePath;
    [SerializeField] private bool visualiseGrid;

    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject floor;
    [SerializeField] private Transform gridParent;

    private bool pathGenerated;

    private Dictionary<Vector2, Cell> cells;

    [SerializeField] private List<Vector2> cellsToSearch;
    [SerializeField] private List<Vector2> searchedCells;
    [SerializeField] private List<Vector2> finalPath;

    private void Update()
    {
        if (generatePath && !pathGenerated)
        {
            GenerateGrid();
            pathGenerated = true;

        }else if (!generatePath)
        {
            pathGenerated = false; 
        }
    }

    private void GenerateGrid()
    {
        cells = new Dictionary<Vector2, Cell>();

        List<Vector2> availablePositions = new List<Vector2>();

        for (float x = -gridWidth; x <= gridWidth; x += cellWidth)
        {
            for (float y = -gridHeight; y <= gridHeight; y += cellHeight)
            {
                Vector2 pos = new Vector2(x,y);
                cells.Add(pos, new Cell(pos));
                availablePositions.Add(pos);
            }
        }


        int totalCells = availablePositions.Count;
        int wallCount = Mathf.FloorToInt(totalCells * (percentageWalls / 100f));

        for (int i = 0; i < wallCount; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2 pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);

            cells[pos].isWall = true;
        }

        if (!visualiseGrid || cells == null)
        {
            return;
        }

        foreach (KeyValuePair<Vector2, Cell> kvp in cells)
        {
            if (!kvp.Value.isWall)
            {
                Instantiate(floor, kvp.Key, Quaternion.identity, gridParent);
            }
            else
            {
                Instantiate(wall, kvp.Key, Quaternion.identity, gridParent);
            }

            //Gizmos.DrawCube(kvp.Key + (Vector2)transform.position, new Vector3(cellWidth, cellHeight));
        }
    }

    private void FindPath(Vector2 startPos, Vector2 endPos)
    {
        searchedCells = new List<Vector2>();
        cellsToSearch = new List<Vector2> { startPos };
        finalPath = new List<Vector2>();

        Cell startCell = cells[startPos];
        startCell.gCost = 0;
        startCell.hCost = GetDistance(startPos, endPos);
        startCell.fCost = GetDistance(startPos, endPos);

        while (cellsToSearch.Count > 0)
        {
            Vector2 cellToSearch = cellsToSearch[0];

            foreach (Vector2 pos in cellsToSearch)
            {
                Cell c = cells[pos];
                if (c.fCost < cells[cellToSearch].fCost || c.fCost == cells[cellToSearch].fCost && c.hCost == cells[cellToSearch].hCost)
                {
                    cellToSearch = pos;
                }
            }

            cellsToSearch.Remove(cellToSearch);
            searchedCells.Add(cellToSearch);
        }
    }

    private int GetDistance(Vector2 pos1, Vector2 pos2)
    {
        Vector2Int distancia = new Vector2Int(Mathf.Abs((int)pos1.x - (int)pos2.x), Mathf.Abs((int)pos1.y - (int)pos2.y));

        int lowest = Mathf.Min(distancia.x, distancia.y);
        int highest = Mathf.Max(distancia.x, distancia.y);

        int horizontalMovesRequired = highest - lowest;

        //calculamos el costo de la distancia al movernos a ese punto.
        //Elegimos que movernos una unidad tiene un coste 10. Por ende, al movernos en diagonal, aplicamos el teorema de pitágoras y nos da 14 (redondeando al más cercano)
        return lowest * 14 + horizontalMovesRequired * 10;
    }

    private class Cell
    {
        public Vector2 position;
        public float fCost = int.MaxValue;
        public float gCost = int.MaxValue;
        public float hCost = int.MaxValue;
        public Vector2 connection;
        public bool isWall;

        public Cell(Vector2 pos)
        {
            position = pos;
        }
    }
}


