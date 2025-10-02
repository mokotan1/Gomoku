using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GridBasedOmokManager : MonoBehaviour
{
    [Header("게임 컴포넌트")]
    public GridManager gridManager;
    public GameObject blackStonePrefab;
    public GameObject whiteStonePrefab;
   // public GameObject blackStoneArrowPrefab;
    //public GameObject whiteStoneArrowPrefab;

    [Header("금수 표시")]
    public GameObject forbiddenMarkPrefab;

    [Header("게임 상태")]
    public bool isBlackTurn = true;
    public bool gameEnded = false;

    [Header("게임 규칙")]
    [Tooltip("흑돌의 금수(3-3, 4-4, 장목) 규칙을 활성화합니다.")]
    public bool enableForbiddenMoves = true;

    [Header("UI")]
    public Text currentPlayerText;
    public GameObject winPanel;
    public Text winnerText;

    // 게임 데이터
    private Camera mainCamera;
    private List<Vector2Int> moveHistory;

    // 금수 마크 관리용 Dictionary (오류 수정 및 최적화)
    private Dictionary<Vector2Int, GameObject> forbiddenMarks = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        mainCamera = Camera.main;
        moveHistory = new List<Vector2Int>();
        gridManager.InitializeGrid();
        UpdateUI();
        UpdateForbiddenMarks();
    }

    void Update()
    {
        if (!gameEnded && Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            UndoLastMove();
        }
    }

    void HandleMouseClick()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2Int gridPos = gridManager.WorldToGridPosition(mouseWorldPos);
        AttemptPlaceStone(gridPos);
    }

    void AttemptPlaceStone(Vector2Int gridPos)
    {
        GridCell cell = gridManager.GetGridCell(gridPos);

        if (cell == null || cell.isOccupied)
        {
            return;
        }

        if (isBlackTurn && enableForbiddenMoves && IsForbiddenMove(gridPos))
        {
            Debug.Log("금수입니다! 다른 곳에 두세요.");
            // 여기에 금수 클릭 시 효과(예: 소리, 화면 깜빡임)를 넣을 수 있습니다.
            return;
        }

        PlaceStone(gridPos);
    }

    void PlaceStone(Vector2Int gridPos)
    {
        GridCell cell = gridManager.GetGridCell(gridPos);

        cell.stoneType = isBlackTurn ? StoneType.Black : StoneType.White;
        cell.isOccupied = true;

        GameObject stonePrefab = isBlackTurn ? blackStonePrefab : whiteStonePrefab;
        Vector3 spawnPos = cell.worldPosition;
        spawnPos.z = -1f;

        cell.stoneObject = Instantiate(stonePrefab, spawnPos, Quaternion.identity);

        SpriteRenderer spriteRenderer = cell.stoneObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = "Stones";
            spriteRenderer.sortingOrder = 1;
        }

        moveHistory.Add(gridPos);

        if (CheckWinCondition(gridPos))
        {
            EndGame();
            return;
        }

        if (moveHistory.Count >= gridManager.gridWidth * gridManager.gridHeight)
        {
            EndGame(true);
            return;
        }

        isBlackTurn = !isBlackTurn;
        UpdateUI();
        UpdateForbiddenMarks();
    }

    bool CheckWinCondition(Vector2Int lastMove)
    {
        StoneType targetStone = isBlackTurn ? StoneType.Black : StoneType.White;
        
        if (isBlackTurn && enableForbiddenMoves && IsOverline(lastMove))
        {
            return false;
        }

        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(1, 1), new Vector2Int(1, -1)
        };

        foreach (Vector2Int dir in directions)
        {
            int count = CountConsecutiveStones(lastMove, dir, targetStone);
            if (count == 5)
            {
                HighlightWinningLine(lastMove, dir, targetStone);
                return true;
            }
        }

        return false;
    }

    int CountConsecutiveStones(Vector2Int start, Vector2Int direction, StoneType targetStone)
    {
        int count = 1;
        count += CountInDirection(start, direction, targetStone);
        count += CountInDirection(start, -direction, targetStone);
        return count;
    }

    int CountInDirection(Vector2Int start, Vector2Int direction, StoneType targetStone)
    {
        int count = 0;
        Vector2Int current = start + direction;
        while (gridManager.IsValidGridPosition(current.x, current.y))
        {
            GridCell cell = gridManager.GetGridCell(current);
            if (cell.stoneType != targetStone) break;
            count++;
            current += direction;
        }
        return count;
    }

    void HighlightWinningLine(Vector2Int center, Vector2Int direction, StoneType stoneType)
    {
        List<Vector2Int> winningStones = new List<Vector2Int>();
        winningStones.Add(center);
        AddWinningStonesInDirection(center, direction, stoneType, winningStones);
        AddWinningStonesInDirection(center, -direction, stoneType, winningStones);
        foreach (Vector2Int pos in winningStones)
        {
            GridCell cell = gridManager.GetGridCell(pos);
            if (cell.stoneObject != null)
            {
                StartCoroutine(AnimateWinningStone(cell.stoneObject));
            }
        }
    }

    void AddWinningStonesInDirection(Vector2Int start, Vector2Int direction, StoneType stoneType, List<Vector2Int> winningStones)
    {
        Vector2Int current = start + direction;
        while (gridManager.IsValidGridPosition(current.x, current.y))
        {
            GridCell cell = gridManager.GetGridCell(current);
            if (cell.stoneType != stoneType) break;
            winningStones.Add(current);
            current += direction;
        }
    }

    IEnumerator AnimateWinningStone(GameObject stone)
    {
        Vector3 originalScale = stone.transform.localScale;
        float animTime = 0.5f;
        float elapsed = 0f;
        while (elapsed < animTime)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + Mathf.Sin(elapsed * Mathf.PI * 4) * 0.2f;
            stone.transform.localScale = originalScale * scale;
            yield return null;
        }
        stone.transform.localScale = originalScale;
    }

    void UndoLastMove()
    {
        if (moveHistory.Count == 0 || gameEnded) return;

        Vector2Int lastMove = moveHistory.Last();
        GridCell cell = gridManager.GetGridCell(lastMove);

        if (cell.stoneObject != null) Destroy(cell.stoneObject);

        cell.stoneType = StoneType.Empty;
        cell.isOccupied = false;
        cell.stoneObject = null;

        moveHistory.RemoveAt(moveHistory.Count - 1);

        isBlackTurn = !isBlackTurn;
        UpdateUI();
        UpdateForbiddenMarks();
    }

    void UpdateUI()
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = isBlackTurn ? "흑돌 차례" : "백돌 차례";
        }
        // 턴 표시 화살표 업데이트
        //UpdateTurnIndicators();
    }

    // 턴 표시 화살표를 관리하는 함수 (가독성을 위해 분리)
  /*  void UpdateTurnIndicators()
    {
        if (blackStoneArrowPrefab != null)
        {
            blackStoneArrowPrefab.SetActive(isBlackTurn && !gameEnded);
        }
        if (whiteStoneArrowPrefab != null)
        {
            whiteStoneArrowPrefab.SetActive(!isBlackTurn && !gameEnded);
        }
    }
    */


    void EndGame(bool isDraw = false)
    {
        gameEnded = true;
        winPanel.SetActive(true);
        if (isDraw)
        {
            winnerText.text = "무승부!";
        }
        else
        {
            string winner = isBlackTurn ? "흑돌" : "백돌";
            winnerText.text = $"{winner} 승리!";
        }
        //UpdateTurnIndicators(); // 게임 종료 시 화살표 끄기
        ClearAllForbiddenMarks(); // 게임 종료 시 금수 표시 모두 제거
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 모든 금수 마크를 제거하는 함수
    void ClearAllForbiddenMarks()
    {
        foreach (var markObject in forbiddenMarks.Values)
        {
            Destroy(markObject);
        }
        forbiddenMarks.Clear();
    }

    void UpdateForbiddenMarks()
    {
        // 1. 기존의 모든 금수 마크를 제거
        ClearAllForbiddenMarks();

        // 2. 흑돌 턴이 아니거나, 규칙이 비활성화되었거나, 게임이 끝나면 실행하지 않음
        if (!isBlackTurn || !enableForbiddenMoves || gameEnded)
        {
            return;
        }

        // 3. 보드의 모든 빈 칸을 순회하며 금수 위치를 찾고 마크 생성
        for (int x = 0; x < gridManager.gridWidth; x++)
        {
            for (int y = 0; y < gridManager.gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GridCell cell = gridManager.GetGridCell(pos);

                if (cell.isOccupied) continue; // 이미 돌이 있는 곳은 건너뜀

                if (IsForbiddenMove(pos))
                {
                    Vector3 markPos = gridManager.GridToWorldPosition(pos.x, pos.y);
                    GameObject markObject = Instantiate(forbiddenMarkPrefab, markPos, Quaternion.identity);
                    forbiddenMarks.Add(pos, markObject); // Dictionary에 추가하여 관리

                    SpriteRenderer sr = markObject.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Sorting Layer를 "UI" 또는 돌보다 위에 있는 레이어로 설정하세요.
                        sr.sortingLayerName = "UI"; 
                        sr.sortingOrder = 5;
                    }
                }
            }
        }
    }

   
    private bool IsForbiddenMove(Vector2Int pos)
    {
        if (IsWinningMove(pos, StoneType.Black)) return false;
        if (IsOverline(pos)) return true;
        if (IsFourFour(pos)) return true;
        if (IsThreeThree(pos)) return true;
        return false;
    }

    private bool IsWinningMove(Vector2Int pos, StoneType stoneType)
    {
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) };
        foreach (var dir in directions)
        {
            int count = 1 + CountInDirectionWithVirtualStone(pos, dir, stoneType) + CountInDirectionWithVirtualStone(pos, -dir, stoneType);
            if (count == 5) return true;
        }
        return false;
    }

    private int CountInDirectionWithVirtualStone(Vector2Int start, Vector2Int direction, StoneType targetStone)
    {
        int count = 0;
        Vector2Int current = start + direction;
        while (gridManager.IsValidGridPosition(current.x, current.y))
        {
            GridCell cell = gridManager.GetGridCell(current);
            if (cell.stoneType != targetStone) break;
            count++;
            current += direction;
        }
        return count;
    }

    private bool IsOverline(Vector2Int pos)
    {
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) };
        foreach (var dir in directions)
        {
            int count = 1 + CountInDirectionWithVirtualStone(pos, dir, StoneType.Black) + CountInDirectionWithVirtualStone(pos, -dir, StoneType.Black);
            if (count > 5) return true;
        }
        return false;
    }

    private bool IsFourFour(Vector2Int pos)
    {
        int fourCount = 0;
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) };
        foreach (var dir in directions)
        {
            var lineInfo = CheckLine(pos, dir, StoneType.Black);
            if (lineInfo.length == 4)
            {
                fourCount++;
            }
        }
        return fourCount >= 2;
    }

    private bool IsThreeThree(Vector2Int pos)
    {
        int openThreeCount = 0;
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1) };
        foreach (var dir in directions)
        {
            var lineInfo = CheckLine(pos, dir, StoneType.Black);
            if (lineInfo.length == 3 && lineInfo.openEnds == 2)
            {
                openThreeCount++;
            }
        }
        return openThreeCount >= 2;
    }

    private (int length, int openEnds) CheckLine(Vector2Int pos, Vector2Int dir, StoneType stoneType)
    {
        int length = 1;
        int openEnds = 0;

        Vector2Int current = pos + dir;
        while (gridManager.IsValidGridPosition(current.x, current.y) && gridManager.GetGridCell(current).stoneType == stoneType)
        {
            length++;
            current += dir;
        }
        if (gridManager.IsValidGridPosition(current.x, current.y) && gridManager.GetGridCell(current).stoneType == StoneType.Empty)
        {
            openEnds++;
        }

        current = pos - dir;
        while (gridManager.IsValidGridPosition(current.x, current.y) && gridManager.GetGridCell(current).stoneType == stoneType)
        {
            length++;
            current -= dir;
        }
        if (gridManager.IsValidGridPosition(current.x, current.y) && gridManager.GetGridCell(current).stoneType == StoneType.Empty)
        {
            openEnds++;
        }
        return (length, openEnds);
    }
}