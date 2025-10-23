// 파일 이름: CheckersGridManager.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]

public class CheckersGridCell 
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public PieceType pieceType;
    public GameObject pieceObject;
    public bool isOccupied;
    
    public CheckersGridCell(int x, int y, Vector3 worldPos)
    {
        gridPosition = new Vector2Int(x, y);
        worldPosition = worldPos;
        pieceType = PieceType.Empty;
        pieceObject = null;
        isOccupied = false;
    }
}

public class CheckersGridManager : MonoBehaviour 
{
    [Header("체커 그리드 설정")]
    public int gridWidth = 8;
    public int gridHeight = 8;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("시각화")]
    public GameObject boardBackground;
    public Color gridLineColor = Color.black; // 라인 색상
    public float lineWidth = 0.05f;           // 라인 두께

    private CheckersGridCell[,] gridCells;

    // --- 격자무늬 그리기를 위해 추가된 변수들 ---
    private List<LineRenderer> gridLines;
    private Material lineMaterial;
    // -----------------------------------------

    public void InitializeGrid()
    {
        CreateLineMaterial();   // 라인 재질 생성 호출
        CreateGridData();
        CreateGridVisuals();    // 격자무늬 UI 생성 호출
        SetupBoardBackground();
    }
    
    // --- 격자무늬 그리기를 위해 추가된 함수들 ---

    void CreateLineMaterial()
    {
        // 라인을 그릴 때 사용할 머티리얼(재질)을 생성합니다.
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = gridLineColor;
    }

    void CreateGridVisuals()
    {
        gridLines = new List<LineRenderer>();
        CreateGridLines();
    }
    
    void CreateGridLines()
    {
        // 모든 라인을 담을 부모 오브젝트를 생성하여 씬을 깔끔하게 유지합니다.
        GameObject gridParent = new GameObject("GridLines");
        gridParent.transform.parent = transform;
        
        // 세로 라인 (Vertical Lines)
        for (int x = 0; x <= gridWidth; x++)
        {
            // 체커 보드의 각 칸 경계에 라인을 그리기 위해 좌표를 0.5씩 보정합니다.
            Vector3 start = GridToWorldPosition(x - 0.5f, -0.5f);
            Vector3 end = GridToWorldPosition(x - 0.5f, gridHeight - 0.5f);
            CreateLine(start, end, gridParent.transform);
        }
        
        // 가로 라인 (Horizontal Lines)
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = GridToWorldPosition(-0.5f, y - 0.5f);
            Vector3 end = GridToWorldPosition(gridWidth - 0.5f, y - 0.5f);
            CreateLine(start, end, gridParent.transform);
        }
    }
    
    void CreateLine(Vector3 start, Vector3 end, Transform parent)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.parent = parent;
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        
        line.material = lineMaterial;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = 2;
        line.useWorldSpace = true; // 월드 좌표계 기준
        line.sortingLayerName = "Background"; // 2D 렌더링 순서
        line.sortingOrder = 1; // 보드 배경보다 위에 보이도록 설정
        
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        
        gridLines.Add(line);
    }
    // ----------------------------------------------------

    void CreateGridData()
    {
        gridCells = new CheckersGridCell[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GridToWorldPosition(x, y);
                gridCells[x, y] = new CheckersGridCell(x, y, worldPos);
            }
        }
    }
    
    void SetupBoardBackground()
    {
        if (boardBackground == null) return;
        boardBackground.transform.position = gridOrigin;
        // 보드 배경이 격자 라인과 정확히 맞도록 크기 조정
        boardBackground.transform.localScale = new Vector3(gridWidth * cellSize, gridHeight * cellSize, 1f);
    }
    
    public Vector3 GridToWorldPosition(int x, int y)
    {
        float worldX = gridOrigin.x + (x - (gridWidth - 1) / 2f) * cellSize;
        float worldY = gridOrigin.y + (y - (gridHeight - 1) / 2f) * cellSize;
        return new Vector3(worldX, worldY, 0f);
    }
    
    // GridToWorldPosition의 float 버전을 라인 그리기를 위해 추가
    public Vector3 GridToWorldPosition(float x, float y)
    {
        float worldX = gridOrigin.x + (x - (gridWidth - 1) / 2f) * cellSize;
        float worldY = gridOrigin.y + (y - (gridHeight - 1) / 2f) * cellSize;
        return new Vector3(worldX, worldY, 0f);
    }
    
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
    
    public CheckersGridCell GetGridCell(int x, int y)
    {
        if (IsValidGridPosition(x, y))
            return gridCells[x, y];
        return null;
    }
    
    public CheckersGridCell GetGridCell(Vector2Int gridPos)
    {
        return GetGridCell(gridPos.x, gridPos.y);
    }
    
    public bool IsValidGridPosition(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }
}