using UnityEngine;
using System.Collections.Generic;

public enum StoneType { Empty, Black, White }

[System.Serializable]
public class GridCell
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public StoneType stoneType;
    public GameObject stoneObject;
    public bool isOccupied;
    
    public GridCell(int x, int y, Vector3 worldPos)
    {
        gridPosition = new Vector2Int(x, y);
        worldPosition = worldPos;
        stoneType = StoneType.Empty;
        stoneObject = null;
        isOccupied = false;
    }
}

public class GridManager : MonoBehaviour
{
    [Header("2D 그리드 설정")]
    public int gridWidth = 15;
    public int gridHeight = 15;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("2D 시각화")]
    public bool showGridGizmos = true;
    public Color gridLineColor = Color.black;
    public float lineWidth = 0.05f;
    
    [Header("2D 보드 설정")]
    public GameObject boardBackground;  // 바둑판 배경 스프라이트
    
    // 그리드 데이터
    private GridCell[,] gridCells;
    private List<LineRenderer> gridLines;
    private Material lineMaterial;
    
    public void InitializeGrid()
    {
        CreateLineMaterial();
        CreateGridData();
        CreateGridVisuals();
        SetupBoardBackground();
    }
    
    void CreateLineMaterial()
    {
        // 2D용 머터리얼 생성
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = gridLineColor;
    }
    
    void CreateGridData()
    {
        gridCells = new GridCell[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GridToWorldPosition(x, y);
                gridCells[x, y] = new GridCell(x, y, worldPos);
            }
        }
    }
    
    void SetupBoardBackground()
    {
        if (boardBackground == null) return;
        
        // 보드 배경을 그리드 크기에 맞게 조정
        boardBackground.transform.position = gridOrigin;
        boardBackground.transform.localScale = Vector3.one * (gridWidth * cellSize * 0.1f);
    }
    
    // 2D 그리드 좌표를 월드 좌표로 변환
    public Vector3 GridToWorldPosition(int x, int y)
    {
        float worldX = gridOrigin.x + (x - (gridWidth - 1) / 2f) * cellSize;
        float worldY = gridOrigin.y + (y - (gridHeight - 1) / 2f) * cellSize;
        return new Vector3(worldX, worldY, 0f);  // Z는 0으로 고정
    }
    
    // 2D 월드 좌표를 그리드 좌표로 변환
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float relativeX = worldPos.x - gridOrigin.x;
        float relativeY = worldPos.y - gridOrigin.y;
        
        int gridX = Mathf.RoundToInt(relativeX / cellSize + (gridWidth - 1) / 2f);
        int gridY = Mathf.RoundToInt(relativeY / cellSize + (gridHeight - 1) / 2f);
        
        return new Vector2Int(
            Mathf.Clamp(gridX, 0, gridWidth - 1),
            Mathf.Clamp(gridY, 0, gridHeight - 1)
        );
    }
    
    public GridCell GetGridCell(int x, int y)
    {
        if (IsValidGridPosition(x, y))
            return gridCells[x, y];
        return null;
    }
    
    public GridCell GetGridCell(Vector2Int gridPos)
    {
        return GetGridCell(gridPos.x, gridPos.y);
    }
    
    public bool IsValidGridPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }
    
    void CreateGridVisuals()
    {
        gridLines = new List<LineRenderer>();
        CreateGridLines();
    }
    
    void CreateGridLines()
    {
        GameObject gridParent = new GameObject("GridLines");
        gridParent.transform.parent = transform;
        
        // 세로 라인 (Vertical Lines)
        for (int x = 0; x < gridWidth; x++)
        {
            Vector3 start = GridToWorldPosition(x, 0);
            Vector3 end = GridToWorldPosition(x, gridHeight - 1);
            CreateLine(start, end, gridParent.transform);
        }
        
        // 가로 라인 (Horizontal Lines)
        for (int y = 0; y < gridHeight; y++)
        {
            Vector3 start = GridToWorldPosition(0, y);
            Vector3 end = GridToWorldPosition(gridWidth - 1, y);
            CreateLine(start, end, gridParent.transform);
        }
    }
    
    void CreateLine(Vector3 start, Vector3 end, Transform parent)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = parent;
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        
        // 2D 설정
        line.material = lineMaterial;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.sortingLayerName = "Background";  // 2D 소팅 레이어
        line.sortingOrder = 0;
        
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        
        gridLines.Add(line);
    }
    
    // 2D 기즈모
    void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        
        Gizmos.color = gridLineColor;
        
        if (gridCells != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 cellCenter = GridToWorldPosition(x, y);
                    Gizmos.DrawWireCube(cellCenter, Vector2.one * cellSize * 0.8f);
                }
            }
        }
    }
}