// 파일 이름: CheckersManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum PieceType { Empty, Black, White, BlackKing, WhiteKing }

public class CheckersManager : MonoBehaviour
{
    [Header("게임 컴포넌트")]
    public CheckersGridManager gridManager;
    public GameObject blackPiecePrefab;
    public GameObject whitePiecePrefab;
    public GameObject blackKingPrefab;
    public GameObject whiteKingPrefab;
    public GameObject possibleMoveMarkerPrefab;

    [Header("UI")]
    public Text currentPlayerText;
    public GameObject winPanel;
    public Text winnerText;
    public RectTransform uiContainer; // UI를 담고 있는 RectTransform (선택사항)

    [Header("색상 선택 UI")]
    public GameObject colorSelectionPanel; // 색상 선택 패널
    public Button whitePlayerButton; // 백돌 선택 버튼
    public Button blackPlayerButton; // 흑돌 선택 버튼

    [Header("게임 상태")]
    private bool isWhiteTurn = true;
    private bool gameEnded = false;
    private bool gameStarted = false; // 게임 시작 여부
    private bool playerIsWhite = true; // 플레이어가 백돌인지 여부
    private CheckersGridCell selectedPieceCell = null;
    private List<GameObject> moveMarkers = new List<GameObject>();
    private List<CheckersGridCell> possibleMoves = new List<CheckersGridCell>(); 

    void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("CheckersGridManager가 할당되지 않았습니다!");
            return;
        }
        
        // 색상 선택 UI 설정
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(true);
        }
        
        if (whitePlayerButton != null)
        {
            whitePlayerButton.onClick.AddListener(() => SelectPlayerColor(true));
        }
        
        if (blackPlayerButton != null)
        {
            blackPlayerButton.onClick.AddListener(() => SelectPlayerColor(false));
        }
        
        // 게임 UI는 숨김
        if (currentPlayerText != null) currentPlayerText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameStarted && !gameEnded && Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    // ==================================================================
    // ## 색상 선택 로직 ##
    // ==================================================================

    /// <summary>
    /// 플레이어가 색상을 선택하면 게임을 시작합니다.
    /// </summary>
    public void SelectPlayerColor(bool isWhite)
    {
        playerIsWhite = isWhite;
        
        // 색상 선택 패널 숨기기
        if (colorSelectionPanel != null)
        {
            colorSelectionPanel.SetActive(false);
        }
        
        // 게임 UI 표시
        if (currentPlayerText != null) currentPlayerText.gameObject.SetActive(true);
        
        // 게임 초기화 및 시작
        InitializeGame();
    }

    void InitializeGame()
    {
        gridManager.InitializeGrid();
        SetupBoard();
        SetupCameraForPlayer(); // 플레이어 색상에 맞게 카메라 설정
        gameStarted = true;
        UpdateUI();
    }

    // ==================================================================
    // ## 카메라 설정 로직 ##
    // ==================================================================

    /// <summary>
    /// 플레이어가 선택한 색의 돌이 아래쪽에 오도록 카메라를 설정합니다.
    /// </summary>
    void SetupCameraForPlayer()
    {
        if (Camera.main == null) return;

        // 백돌 플레이어면 0도(정상), 흑돌 플레이어면 180도 회전
        float targetRotation = playerIsWhite ? 0f : 180f;
        
        // 메인 카메라를 Z축 기준으로 회전
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, targetRotation);
        
        // UI도 함께 회전시켜 항상 읽기 쉽게 유지 (선택사항)
        if (uiContainer != null)
        {
            uiContainer.rotation = Quaternion.Euler(0, 0, targetRotation);
        }
    }

    // ==================================================================
    // ## 입력 및 선택 로직 ##
    // ==================================================================

    void HandleMouseClick()
    {
        if (Camera.main == null || gridManager == null) return;

        CheckersGridCell clickedCell = gridManager.GetGridCell(gridManager.WorldToGridPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition)));
        if (clickedCell == null) return;

        if (possibleMoves.Contains(clickedCell))
        {
            MovePiece(selectedPieceCell, clickedCell);
        }
        else if (IsPlayersPiece(clickedCell))
        {
            SelectPiece(clickedCell);
        }
        else
        {
            selectedPieceCell = null;
            ClearMoveMarkers();
        }
    }

    void SelectPiece(CheckersGridCell cell)
    {
        selectedPieceCell = cell;
        ClearMoveMarkers();
        FindPossibleMovesForPiece(cell); 
        ShowPossibleMoveMarkers();
    }

    // ==================================================================
    // ## 게임 규칙 계산 ##
    // ==================================================================

    void FindPossibleMovesForPiece(CheckersGridCell piece)
    {
        possibleMoves.Clear();

        // --- 1. 일반 말 (Man) ---
        if (piece.pieceType == PieceType.White || piece.pieceType == PieceType.Black)
        {
            int forwardDir = (piece.pieceType == PieceType.White) ? -1 : 1;
            
            // 단순 이동 (앞으로)
            for (int dx = -1; dx <= 1; dx += 2)
            {
                CheckersGridCell targetCell = gridManager.GetGridCell(piece.gridPosition + new Vector2Int(dx, forwardDir));
                if (targetCell != null && !targetCell.isOccupied)
                {
                    possibleMoves.Add(targetCell);
                }
            }
            
            // 점프 (앞으로)
            for (int dx = -1; dx <= 1; dx += 2)
            {
                CheckersGridCell opponentCell = gridManager.GetGridCell(piece.gridPosition + new Vector2Int(dx, forwardDir));
                CheckersGridCell landingCell = gridManager.GetGridCell(piece.gridPosition + new Vector2Int(dx * 2, forwardDir * 2));

                if (landingCell != null && !landingCell.isOccupied && opponentCell != null && IsOpponentPiece(opponentCell))
                {
                    possibleMoves.Add(landingCell);
                }
            }
        }
    
        // --- 2. 킹 (King) ---
        else if (piece.pieceType == PieceType.BlackKing || piece.pieceType == PieceType.WhiteKing)
        {
            for (int dx = -1; dx <= 1; dx += 2)
            {
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    // 단순 이동
                    CheckersGridCell targetCell = gridManager.GetGridCell(piece.gridPosition + new Vector2Int(dx, dy));
                    if (targetCell != null && !targetCell.isOccupied)
                    {
                        possibleMoves.Add(targetCell);
                    }
                    
                    // 점프
                    CheckersGridCell opponentCell = gridManager.GetGridCell(piece.gridPosition + new Vector2Int(dx, dy));
                    CheckersGridCell landingCell = gridManager.GetGridCell(piece.gridPosition + new Vector2Int(dx * 2, dy * 2));

                    if (landingCell != null && !landingCell.isOccupied && opponentCell != null && IsOpponentPiece(opponentCell))
                    {
                        possibleMoves.Add(landingCell);
                    }
                }
            }
        }
    }

    // ==================================================================
    // ## 이동 실행 및 게임 상태 변경 ##
    // ==================================================================

    void MovePiece(CheckersGridCell fromCell, CheckersGridCell toCell)
    {
        selectedPieceCell = null;
        ClearMoveMarkers();

        // 점프 확인
        if (Mathf.Abs(fromCell.gridPosition.x - toCell.gridPosition.x) == 2)
        {
            Vector2Int capturedPos = fromCell.gridPosition + (toCell.gridPosition - fromCell.gridPosition) / 2;
            CheckersGridCell capturedCell = gridManager.GetGridCell(capturedPos);
            if (capturedCell != null && capturedCell.pieceObject != null)
            {
                Destroy(capturedCell.pieceObject);
                capturedCell.pieceType = PieceType.Empty;
                capturedCell.isOccupied = false;
                capturedCell.pieceObject = null;
            }
        }
        
        // 말 이동
        PieceType movedPieceType = fromCell.pieceType;
        Destroy(fromCell.pieceObject);
        fromCell.pieceType = PieceType.Empty;
        fromCell.isOccupied = false;
        fromCell.pieceObject = null;

        PlacePiece(movedPieceType, toCell.gridPosition.x, toCell.gridPosition.y);
        
        CheckersGridCell newFinalCell = gridManager.GetGridCell(toCell.gridPosition);

        CheckForKingPromotion(newFinalCell);
        CheckWinCondition();
        if (gameEnded) return;

        SwitchTurn();
    }

    void SwitchTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        UpdateUI();
        
        // 다음 턴에 움직일 수 있는 말이 없으면 게임 종료
        bool canMove = false;
        for (int y = 0; y < 8; y++) 
        {
            for (int x = 0; x < 8; x++)
            {
                CheckersGridCell cell = gridManager.GetGridCell(x, y);
                if(cell != null && IsPlayersPiece(cell))
                {
                    FindPossibleMovesForPiece(cell);
                    if (possibleMoves.Count > 0)
                    {
                        canMove = true;
                        break;
                    }
                }
            }
            if(canMove) break;
        }

        if (!canMove && !gameEnded) 
        {
            EndGame(isWhiteTurn ? "흑돌" : "백돌");
        }
        possibleMoves.Clear();
    }

    // ==================================================================
    // ## 보드 설정 및 유틸리티 ##
    // ==================================================================
    
    void CheckForKingPromotion(CheckersGridCell cell)
    {
        if (cell.pieceType == PieceType.Black && cell.gridPosition.y == 7)
        {
            Destroy(cell.pieceObject);
            PlacePiece(PieceType.BlackKing, cell.gridPosition.x, cell.gridPosition.y);
        }
        else if (cell.pieceType == PieceType.White && cell.gridPosition.y == 0)
        {
            Destroy(cell.pieceObject);
            PlacePiece(PieceType.WhiteKing, cell.gridPosition.x, cell.gridPosition.y);
        }
    }

    void SetupBoard()
    {
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            if ((x + y) % 2 != 0)
            {
                if (y < 3) PlacePiece(PieceType.Black, x, y);
                else if (y > 4) PlacePiece(PieceType.White, x, y);
            }
    }

    void PlacePiece(PieceType type, int x, int y)
    {
        CheckersGridCell cell = gridManager.GetGridCell(x, y);
        if (cell == null || type == PieceType.Empty) return;
        GameObject prefab = null;
        switch (type)
        {
            case PieceType.Black: prefab = blackPiecePrefab; break;
            case PieceType.White: prefab = whitePiecePrefab; break;
            case PieceType.BlackKing: prefab = blackKingPrefab; break;
            case PieceType.WhiteKing: prefab = whiteKingPrefab; break;
        }
        GameObject pieceObject = Instantiate(prefab, cell.worldPosition, Quaternion.identity);
        cell.pieceType = type;
        cell.pieceObject = pieceObject;
        cell.isOccupied = true;
    }

    bool IsPlayersPiece(CheckersGridCell cell)
    {
        if (cell == null || !cell.isOccupied) return false;
        return isWhiteTurn ? (cell.pieceType == PieceType.White || cell.pieceType == PieceType.WhiteKing)
                           : (cell.pieceType == PieceType.Black || cell.pieceType == PieceType.BlackKing);
    }
    
    bool IsOpponentPiece(CheckersGridCell cell)
    {
        if (cell == null || !cell.isOccupied) return false;
        return !isWhiteTurn ? (cell.pieceType == PieceType.White || cell.pieceType == PieceType.WhiteKing)
                            : (cell.pieceType == PieceType.Black || cell.pieceType == PieceType.BlackKing);
    }

    void ShowPossibleMoveMarkers()
    {
        ClearMoveMarkers();
        foreach (var cell in possibleMoves)
        {
            if(cell != null && possibleMoveMarkerPrefab != null)
            {
                GameObject marker = Instantiate(possibleMoveMarkerPrefab, cell.worldPosition, Quaternion.identity);
                moveMarkers.Add(marker);
            }
        }
    }

    void ClearMoveMarkers()
    {
        foreach (var marker in moveMarkers) Destroy(marker);
        moveMarkers.Clear();
    }
    
    void CheckWinCondition()
    {
        bool hasWhite = false, hasBlack = false;
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
        {
            CheckersGridCell cell = gridManager.GetGridCell(x,y);
            if (cell != null && (cell.pieceType == PieceType.White || cell.pieceType == PieceType.WhiteKing)) hasWhite = true;
            if (cell != null && (cell.pieceType == PieceType.Black || cell.pieceType == PieceType.BlackKing)) hasBlack = true;
        }
        if (!gameEnded && !hasWhite) EndGame("흑돌");
        else if (!gameEnded && !hasBlack) EndGame("백돌");
    }

    void EndGame(string winner)
    {
        gameEnded = true;
        if(winPanel != null) winPanel.SetActive(true);
        if(winnerText != null) winnerText.text = $"{winner} 승리!";
    }

    void UpdateUI()
    {
        if (currentPlayerText != null)
            currentPlayerText.text = isWhiteTurn ? "백돌 차례" : "흑돌 차례";
    }

    public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}