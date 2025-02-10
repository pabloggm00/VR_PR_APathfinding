using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Pathfinding : MonoBehaviour
{
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private float cellHeight = 1f;
    [SerializeField] private float cellWidth = 1f;
    [SerializeField] private float percentageWalls = 20f;
    [SerializeField] private float percentageWater = 10f;

    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject water;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject weightText;
    [SerializeField] private Transform gridParent;

    private Dictionary<Vector2, Cell> cells;
    private GameObject player;
    private Vector2 playerPosition;
    public List<Vector2> path;
    List<Vector2> neighbours;
    private bool isMoving;

    [Header("Pesos")]
    [SerializeField] private int floorWeight = 10;
    [SerializeField] private int waterWeight = 30;
    [SerializeField] private int bridgeWeight = 10;

    [Header("Edit Mode")]
    [SerializeField] private GameObject editModePanel; 
    [SerializeField] private Button editModeButton, gameModeButton;
    [SerializeField] private Button resetFloorButton;
    [SerializeField] private Button resetTextButton;
    [SerializeField] private Button wallButton, floorButton, bridgeButton, waterButton, playerButton;
    [SerializeField] private GameObject bridge;


    private bool isEditMode = false;
    private int selectedTerrain = 0;
    private GameObject previewObject;
    private Vector2 lastHoveredCell;

    private void Start()
    {
        GenerateGrid();
        SpawnPlayer();

  
        editModeButton.onClick.AddListener(() => SetEditMode(true));
        gameModeButton.onClick.AddListener(() => SetEditMode(false));
        
        wallButton.onClick.AddListener(() => selectedTerrain = 2);
        floorButton.onClick.AddListener(() => selectedTerrain = 1);
        bridgeButton.onClick.AddListener(() => selectedTerrain = 4);
        waterButton.onClick.AddListener(() => selectedTerrain = 3);
        playerButton.onClick.AddListener(() => selectedTerrain = 5);

        resetFloorButton.onClick.AddListener(() => ResetFloor());
        resetTextButton.onClick.AddListener(() => ResetTexts());
    }

    private void Update()
    {
        //cogemos la posicion del mouse y clampeamos para coger una posición del grid
        Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 gridTarget = new Vector2(Mathf.Round(target.x), Mathf.Round(target.y));

        //si estamos en editmode, podemos tener la preview
        if (isEditMode)
        {
            if (cells.ContainsKey(gridTarget))
                ShowPreview(gridTarget);
            else
                ClearPreview();
        }

        //si estoy en edit mode puedo modificar y si estoy en game mode me muevo
        if (Input.GetMouseButtonDown(0) && !isMoving)
        {
            if (cells.ContainsKey(gridTarget))
            {
                if (isEditMode)
                    EditCell(gridTarget);
                else if (!isMoving && !cells[gridTarget].isWall)
                {
                    path = FindPath(playerPosition, gridTarget);
                    StartCoroutine(MovePlayer());
                }
            }
        }

        resetTextButton.interactable = !isMoving;
        editModeButton.interactable = !isMoving;
    }

    #region GenerarGrid

    private void GenerateGrid()
    {
        cells = new Dictionary<Vector2, Cell>();
        List<Vector2> availablePositions = new List<Vector2>();

        //generamos el suelo y agregamos todas las celdas libres
        for (float x = -gridWidth; x <= gridWidth; x += cellWidth)
        {
            for (float y = -gridHeight; y <= gridHeight; y += cellHeight)
            {
                Vector2 pos = new Vector2(x, y);
                cells.Add(pos, new Cell(pos));
                availablePositions.Add(pos);
                cells[pos].movementCost = floorWeight;
            }
        }

        //generamos muros y la quitamos de las celdas posibles para generar
        int wallCount = Mathf.FloorToInt(availablePositions.Count * (percentageWalls / 100f));
        for (int i = 0; i < wallCount; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2 pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);
            cells[pos].isWall = true;
        }

        //generamos agua y la quitamos de las celdas posibles para generar
        int waterCount = Mathf.FloorToInt(availablePositions.Count * (percentageWater / 100f));
        for (int i = 0; i < waterCount; i++)
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2 pos = availablePositions[randomIndex];
            availablePositions.RemoveAt(randomIndex);

            if (!cells[pos].isWall)
            {
                cells[pos].terrainType = 1;
                cells[pos].movementCost = waterWeight;
            }
        }

        //instanciamos según el terreno o si es muro
        foreach (var kvp in cells)
        {
            GameObject obj;

            if (kvp.Value.isWall)
                obj = Instantiate(wall, kvp.Key, Quaternion.identity, gridParent);
            else if (kvp.Value.terrainType == 1)
                obj = Instantiate(water, kvp.Key, Quaternion.identity, gridParent);
            else
                obj = Instantiate(floor, kvp.Key, Quaternion.identity, gridParent);

            kvp.Value.cellObject = obj;
        }
    }

    //spawneamos al player en las posiciones libres que no son muros
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

    #endregion

    #region Movimiento y Pathfinding

    //movemos según la ruta que se ha calculado y devolvemos los colores
    private IEnumerator MovePlayer()
    {
        isMoving = true;


        yield return new WaitForSeconds(0.5f);


        foreach (Vector2 step in path)
        {
            player.transform.position = step;
            playerPosition = step;

            if (cells[step].terrainType == 0)
                cells[step].cellObject.GetComponent<SpriteRenderer>().color = Color.white;

            if (cells[step].terrainType == 4)
                cells[step].cellObject.GetComponent<SpriteRenderer>().color = bridge.GetComponent<SpriteRenderer>().color;

            if (cells[step].terrainType == 1)
                cells[step].cellObject.GetComponent<SpriteRenderer>().color = water.GetComponent<SpriteRenderer>().color;

            yield return new WaitForSeconds(0.5f);
        }

        isMoving = false;

        foreach (Vector2 cell in neighbours)
        {
            if (cells[cell].terrainType == 0)
                cells[cell].cellObject.GetComponent<SpriteRenderer>().color = Color.white;
            if (cells[cell].terrainType == 4)
                cells[cell].cellObject.GetComponent<SpriteRenderer>().color = bridge.GetComponent<SpriteRenderer>().color;
            if (cells[cell].terrainType == 1)
                cells[cell].cellObject.GetComponent<SpriteRenderer>().color = water.GetComponent<SpriteRenderer>().color;
        }
    }

    private List<Vector2> FindPath(Vector2 startPos, Vector2 endPos)
    {
        List<Vector2> finalPath = new List<Vector2>();
        List<Vector2> searchedCells = new List<Vector2>();
        List<Vector2> cellsToSearch = new List<Vector2> { startPos };
        neighbours = new List<Vector2>();

        //establecemos los costes de la posición inicial con la final
        cells[startPos].gCost = 0;
        cells[startPos].hCost = GetDistance(startPos, endPos);
        cells[startPos].fCost = cells[startPos].hCost;

        //mientras haya celdas por buscar, no terminamos
        while (cellsToSearch.Count > 0)
        {
            //buscamos la celda con menor fCost, ya que debería ser la más viable
            Vector2 currentCell = cellsToSearch[0];
            foreach (Vector2 pos in cellsToSearch)
            {
                if (cells[pos].fCost < cells[currentCell].fCost)
                    currentCell = pos;
            }

            //si la actual celda es la final, reconstruimos todo para devolver la ruta por la que ir
            if (currentCell == endPos)
            {
                //mientras que no sea la inicial, sigue el bucle. Vamos desde atrás hasta la incial para decir cual es la conexion y la pintamos de verde
                while (currentCell != startPos)
                {
                    finalPath.Add(currentCell);
                    currentCell = cells[currentCell].connection;
                    if (currentCell != startPos)
                        cells[currentCell].cellObject.GetComponent<SpriteRenderer>().color = Color.green;
                }
                finalPath.Reverse(); //para que el path sea de inicio a fin
                UpdateCellTexts(finalPath);
                return finalPath;
            }

            //Si todavía no hemos llegado a la meta, seguimos comprobando con sus vecinas para ver qué ruta es mejor

            //la eliminamos para que no busque más esta celda y agregamos a la lista de ya buscadas
            cellsToSearch.Remove(currentCell);
            searchedCells.Add(currentCell); 

            //obtenemos las celdas vecinas
            foreach (Vector2 neighbour in GetNeighbours(currentCell))
            {
                //ignoramos los muros y las ya buscadas
                if (searchedCells.Contains(neighbour) || cells[neighbour].isWall)
                    continue;

                //calculamos el nuevo coste con la vecina
                int newGCost = cells[currentCell].gCost + cells[neighbour].movementCost;

                //Comprobamos si no está en las celdas buscadas o si encontramos un camino más corto, si cumple esta segunda, actualizamos la ruta
                if (!cellsToSearch.Contains(neighbour) || newGCost < cells[neighbour].gCost)
                {
                    cells[neighbour].connection = currentCell;
                    cells[neighbour].gCost = newGCost;
                    cells[neighbour].hCost = GetDistance(neighbour, endPos);
                    cells[neighbour].fCost = cells[neighbour].gCost + cells[neighbour].hCost;
                    cells[neighbour].cellObject.GetComponent<SpriteRenderer>().color = Color.red;

                    neighbours.Add(neighbour); //para poder resetear los colores luego

                    //si es el final, lo coloreamos de amarillo para ver cuál es el destino
                    if (neighbour == endPos)
                    {
                        cells[neighbour].cellObject.GetComponent<SpriteRenderer>().color = Color.yellow;
                    }

                    //si no existe, la agregamos
                    if (!cellsToSearch.Contains(neighbour))
                        cellsToSearch.Add(neighbour);
                }
            }

            UpdateCellTexts(neighbours); //actualizamos el texto de las vecinas
        }


        return finalPath;
    }

    //obtener las celdas vecinas en las 4 direcciones
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

    //Obtenemos la distancia de dos posiciones, teniendo en cuenta también el costo del movimiento
    private int GetDistance(Vector2 pos1, Vector2 pos2)
    {
        return (int)(Mathf.Abs(pos1.x - pos2.x) + Mathf.Abs(pos1.y - pos2.y)) * cells[pos1].movementCost;
    }

    //para actualizar los textos de cada celda
    private void UpdateCellTexts(List<Vector2> positionCells)
    {
        foreach (Vector2 pos in positionCells)
        {
            Cell cell = cells[pos];
            Debug.Log(cell.cellObject);

            if (cell.cellObject != null)
            {
                TMP_Text textMesh = cell.cellObject.GetComponentInChildren<TMP_Text>();
                if (textMesh != null)
                {
                    textMesh.text = $"F: {cell.fCost}\nG: {cell.gCost}\nH: {cell.hCost}";
                }
            }
        }
    }

    #endregion

    #region Modo Edicion

    //mostramos una preview del pincel
    private void ShowPreview(Vector2 position)
    {
        if (!cells.ContainsKey(position)) return;

        //si es otra celda o es null, destruimos el preview
        if (previewObject != null && lastHoveredCell != position)
        {
            Destroy(previewObject);
        }

        lastHoveredCell = position; //guardamos la ultima posicion

        if (previewObject != null && previewObject.transform.position == (Vector3)position)
            return;

        //según qué pincel tenemos
        switch (selectedTerrain)
        {
            case 1: previewObject = Instantiate(floor, position, Quaternion.identity, gridParent); break;
            case 2: previewObject = Instantiate(wall, position, Quaternion.identity, gridParent); break;
            case 3: previewObject = Instantiate(water, position, Quaternion.identity, gridParent); break;
            case 4: previewObject = Instantiate(bridge, position, Quaternion.identity, gridParent); break;
            case 5: previewObject = Instantiate(playerPrefab, position, Quaternion.identity, gridParent); break;
        }
    }

    //limpiamos la preview
    private void ClearPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }

    //para setear el menú de juego y el de edición
    private void SetEditMode(bool editMode)
    {
        isEditMode = editMode;
        editModePanel.SetActive(editMode);
        editModeButton.interactable = !editMode;
        gameModeButton.interactable = editMode;
        resetTextButton.gameObject.SetActive(!editMode);
    }

    //editamos una celda con el pincel seleccionada y aplicamos las características de ese pincel
    private void EditCell(Vector2 position)
    {
        if (!cells.ContainsKey(position)) return;

        Cell cell = cells[position];

 
        if (selectedTerrain == 5)
        {
            player.transform.position = position;
            playerPosition = position;
            return;
        }

      
        Destroy(cell.cellObject);

        
        switch (selectedTerrain)
        {
            case 1: 
                cell.isWall = false;
                cell.movementCost = floorWeight;
                cell.terrainType = 0;
                cell.cellObject = Instantiate(floor, position, Quaternion.identity, gridParent);
                break;
            case 2:
                cell.isWall = true;
                cell.cellObject = Instantiate(wall, position, Quaternion.identity, gridParent);
                break;
            case 3: 
                cell.isWall = false;
                cell.movementCost = waterWeight;
                cell.terrainType = 1;
                cell.cellObject = Instantiate(water, position, Quaternion.identity, gridParent);
                break;
            case 4: 
                cell.isWall = false;
                cell.movementCost = bridgeWeight;
                cell.terrainType = 4;
                cell.cellObject = Instantiate(bridge, position, Quaternion.identity, gridParent);
                break;
        }
    }

    //limpiamos todos los textos
    void ResetTexts()
    {
        foreach (var cell in cells.Values)
        {
            TMP_Text textMesh = cell.cellObject.GetComponentInChildren<TMP_Text>();
            if (textMesh != null)
            {
                textMesh.text = "";
            }
        }
    }

    //llenamos todas las celdas de suelo
    void ResetFloor()
    {
        foreach (var cell in cells.Values)
        {
            Destroy(cell.cellObject);

            cell.isWall = false;
            cell.movementCost = floorWeight;
            cell.terrainType = 0;
            cell.cellObject = Instantiate(floor, cell.position, Quaternion.identity, gridParent);
        }
    }

    #endregion


    private class Cell
    {
        public Vector2 position;
        public GameObject cellObject;
        public int fCost = int.MaxValue;
        public int gCost = int.MaxValue;
        public int hCost = int.MaxValue;
        public Vector2 connection;
        public bool isWall;
        public int terrainType = 0;
        public int movementCost = 10;

        public Cell(Vector2 pos) { position = pos; }
    }
}


