using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayerOption
{
    NONE, //0
    X, // 1
    O // 2
}

public class TTT : MonoBehaviour
{
    public int Rows;
    public int Columns;
    [SerializeField] BoardView board;

    PlayerOption currentPlayer = PlayerOption.X;
    Cell[,] cells;

    Vector2Int lastXPosition;
    Vector2Int lastOPosition;

    // Start is called before the first frame update
    void Start()
    {
        cells = new Cell[Columns, Rows];

        board.InitializeBoard(Columns, Rows);

        for(int i = 0; i < Rows; i++)
        {
            for(int j = 0; j < Columns; j++)
            {
                cells[j, i] = new Cell();
                cells[j, i].current = PlayerOption.NONE;
            }
        }
    }

    public void ResetGame()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    private Vector2Int GetPosition(int index)
    {
        switch (index)
        {
            case 0:
                return new Vector2Int(0, 0);
            case 1:
                return new Vector2Int(0, 2);
            case 2:
                return new Vector2Int(2, 2);
            case 3:
                return new Vector2Int(2, 0);
            case 4:
                return new Vector2Int(0, 1);
            case 5:
                return new Vector2Int(2, 1);
            case 6:
                return new Vector2Int(1, 2);
            case 7:
                return new Vector2Int(1, 0);
            default:
                return new Vector2Int(1, 1);
        }
    }
    private bool TryPlaceSide()
    {
        if (ChooseSpace(1, 0))
            return true;
        if (ChooseSpace(0, 1))
            return true;
        if (ChooseSpace(1, 2))
            return true;
        if (ChooseSpace(2, 1))
            return true;
        return false;
    }

    public void MakeOptimalMove()
    {
        int turn = 0;
        foreach (Cell cell in cells)
        {
            if (cell.current != PlayerOption.NONE)
                turn++;
        }
        Debug.Log(turn);

        if (turn == 0)
        {
            //if starting as X, place in a corner 
            /* - - -
             * - - -
             * X - - 
             */
            ChooseSpace(GetPosition(0).x, GetPosition(0).y);
            return;
        }
        if (turn == 1)
        {
            //if starting as O, place in center
            /* - - -
             * - O -
             * - - - 
             */
            if (cells[0, 1].current == PlayerOption.X)
            {
                ChooseSpace(0, 0);
                return;
            }
            else if (cells[1, 0].current == PlayerOption.X)
            {
                ChooseSpace(0, 0);
                return;
            }
            else if (cells[2, 1].current == PlayerOption.X) 
            {
                ChooseSpace(2, 2);
                return;
            }
            else if (cells[1, 2].current == PlayerOption.X)
            {
                ChooseSpace(2, 2);
                return;
            }

            if (ChooseSpace(1, 1))
                return;
        }

        Vector2Int bestMove = new();
        int bestSum = 0;

        //edge case for unoptimal O placement
        if (turn == 2 && (lastXPosition.x + lastXPosition.y) % 2 == 0 && (lastOPosition.x + lastOPosition.y) % 2 == 0)
        {
            Debug.Log(true);
            int sum = CalculateStraightValues(0, 0);
            if (sum > bestSum)
            {
                bestSum = sum;
                bestMove = new(0, 0);
            }
            sum = CalculateStraightValues(2, 0);
            if (sum > bestSum)
            {
                bestSum = sum;
                bestMove = new(2, 0);
            }
            sum = CalculateStraightValues(0, 2);
            if (sum > bestSum)
            {
                bestSum = sum;
                bestMove = new(0, 2);
            }
            sum = CalculateStraightValues(2, 2);
            if (sum > bestSum)
            {
                bestSum = sum;
                bestMove = new(2, 2);
            }

            ChooseSpace(bestMove.x, bestMove.y);
            return;
        }
        //Check if ai can win immediately
        for (int i = 0; i < Rows*Columns; i++)
        {
            Vector2Int move = GetPosition(i);
            if (TryAIPosition(move.x, move.y, currentPlayer))
            {
                Debug.Log(move);
                Debug.Log("win/stopwin");
                ChooseSpace(move.x, move.y);
                return;
            }
            
        }

        //Get opponent
        PlayerOption counterPlayer;
        if (currentPlayer == PlayerOption.O)
            counterPlayer = PlayerOption.X;
        else
            counterPlayer = PlayerOption.O;
        // Check if ai can block a win

        for (int i = 0; i < Rows * Columns; i++)
        {
            Vector2Int move = GetPosition(i);
            if (TryAIPosition(move.x, move.y, counterPlayer))
            {
                Debug.Log(move);
                Debug.Log("win/stopwin");
                ChooseSpace(move.x, move.y);
                return;
            }
        }

        if (turn == 3)
        {
            if (ChooseSpace(1, 1))
                return;
        }

        if (turn == 4 && bestSum < 2)
        {
            if (ChooseSpace(0, 0))
                return;
            if (ChooseSpace(2, 0))
                return;
            if (ChooseSpace(2, 2))
                return;
            if (ChooseSpace(0, 2))
                return;
        }

        if (ChooseSpace(bestMove.x, bestMove.y))
            return;

        //if all else fails, just loop through entire board and try to place
        for (int i = 0; i < Rows*Columns; i++)
        {
            Vector2Int pos = GetPosition(i);
            if (ChooseSpace(pos.x, pos.y))
                return;
        }
    }

    public bool ChooseSpace(int column, int row)
    {
        // can't choose space if game is over
        if (GetWinner() != PlayerOption.NONE)
            return false;

        // can't choose a space that's already taken
        if (cells[column, row].current != PlayerOption.NONE)
            return false;

        // set the cell to the player's mark
        cells[column, row].current = currentPlayer;

        // update the visual to display X or O
        board.UpdateCellVisual(column, row, currentPlayer);

        // if there's no winner, keep playing, otherwise end the game
        if(GetWinner() == PlayerOption.NONE)
            EndTurn();
        else
        {
            Debug.Log("GAME OVER!");
        }
        if (currentPlayer == PlayerOption.X)
            lastXPosition = new(column, row);
        else
            lastOPosition = new(column, row);
        return true;
    }

    public void EndTurn()
    {
        // increment player, if it goes over player 2, loop back to player 1
        currentPlayer += 1;
        if ((int)currentPlayer > 2)
            currentPlayer = PlayerOption.X;
    }

    public bool TryAIPosition(int col, int row, PlayerOption option)
    {
        if (cells[col, row].current != PlayerOption.NONE)
            return false;

        bool canWin = false;

        //see if any positions can win for option player
        cells[col, row].current = option;
        PlayerOption possibleWin = GetWinner();

        if (possibleWin != PlayerOption.NONE)
            canWin = true;

        cells[col, row].current = PlayerOption.NONE;
        return canWin;
    }


    public int CalculateStraightValues(int col, int row)
    {
        if (cells[col, row].current != PlayerOption.NONE)
            return 0;

        PlayerOption counterPlayer;
        if (currentPlayer == PlayerOption.X)
            counterPlayer = PlayerOption.O;
        else
            counterPlayer = PlayerOption.X;

        // sum each row/column based on what's in each cell X = 1, O = -1, blank = 0
        // we have a winner if the sum = 3 (X) or -3 (O)
        int sum = 0;
        int bestSum = 0;

        // check rows
        for (int i = 0; i < Rows; i++)
        {
            sum = 0;
            for (int j = 0; j < Columns; j++)
            {
                var value = 0;
                if (cells[j, i].current == currentPlayer)
                    value = 1;
                else if (cells[j, i].current == counterPlayer)
                    value = -1;

                sum += value;
            }

            if (sum > bestSum)
                bestSum = sum;

        }

        // check columns
        for (int j = 0; j < Columns; j++)
        {
            //sum = 0;
            for (int i = 0; i < Rows; i++)
            {
                var value = 0;
                if (cells[j, i].current == currentPlayer)
                    value = 1;
                else if (cells[j, i].current == counterPlayer)
                    value = -1;

                sum += value;
            }

            if (sum > bestSum)
                bestSum = sum;

        }

        return bestSum;
    }

    public int CalculateDiagonalValues(int col, int row)
    {
        if (cells[col, row].current != PlayerOption.NONE)
            return 0;

        PlayerOption counterPlayer;
        if (currentPlayer == PlayerOption.X)
            counterPlayer = PlayerOption.O;
        else
            counterPlayer = PlayerOption.X;

        // sum each row/column based on what's in each cell X = 1, O = -1, blank = 0
        // we have a winner if the sum = 3 (X) or -3 (O)
        int sum = 0;
        int bestSum = 0;

        // check diagonals
        // top left to bottom right
        sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            int value = 0;
            if (cells[i, i].current == currentPlayer)
                value = 1;
            else if (cells[i, i].current == counterPlayer)
                value = -1;

            sum += value;
        }

        if (sum > bestSum)
            bestSum = sum;

        // top right to bottom left
        sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            int value = 0;

            if (cells[Columns - 1 - i, i].current == currentPlayer)
                value = 1;
            else if (cells[Columns - 1 - i, i].current == counterPlayer)
                value = -1;

            sum += value;
        }

        if (sum > bestSum)
            bestSum = sum;

        return bestSum;
    }
    public int CalculateMoveValue(int col, int row)
    {
        if (cells[col, row].current != PlayerOption.NONE)
            return 0;

        PlayerOption counterPlayer;
        if (currentPlayer == PlayerOption.X)
            counterPlayer = PlayerOption.O;
        else
            counterPlayer = PlayerOption.X;

        // sum each row/column based on what's in each cell X = 1, O = -1, blank = 0
        // we have a winner if the sum = 3 (X) or -3 (O)
        int sum = 0;
        int bestSum = 0;

        // check rows
        for (int i = 0; i < Rows; i++)
        {
            sum = 0;
            for (int j = 0; j < Columns; j++)
            {
                var value = 0;
                if (cells[j, i].current == currentPlayer)
                    value = 1;
                else if (cells[j, i].current == counterPlayer)
                    value = -1;

                sum += value;
            }

            if (sum > bestSum)
                bestSum = sum;

        }

        // check columns
        for (int j = 0; j < Columns; j++)
        {
            sum = 0;
            for (int i = 0; i < Rows; i++)
            {
                var value = 0;
                if (cells[j, i].current == currentPlayer)
                    value = 1;
                else if (cells[j, i].current == counterPlayer)
                    value = -1;

                sum += value;
            }

            if (sum > bestSum)
                bestSum = sum;

        }

        // check diagonals
        // top left to bottom right
        sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            int value = 0;
            if (cells[i, i].current == currentPlayer)
                value = 1;
            else if (cells[i, i].current == counterPlayer)
                value = -1;

            sum += value;
        }

        if (sum > bestSum)
            bestSum = sum;

        // top right to bottom left
        sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            int value = 0;

            if (cells[Columns - 1 - i, i].current == currentPlayer)
                value = 1;
            else if (cells[Columns - 1 - i, i].current == counterPlayer)
                value = -1;

            sum += value;
        }

        if (sum > bestSum)
            bestSum = sum;

        return bestSum;
    }

    public PlayerOption GetWinner()
    {
        // sum each row/column based on what's in each cell X = 1, O = -1, blank = 0
        // we have a winner if the sum = 3 (X) or -3 (O)
        int sum = 0;

        // check rows
        for (int i = 0; i < Rows; i++)
        {
            sum = 0;
            for (int j = 0; j < Columns; j++)
            {
                var value = 0;
                if (cells[j, i].current == PlayerOption.X)
                    value = 1;
                else if (cells[j, i].current == PlayerOption.O)
                    value = -1;

                sum += value;
            }

            if (sum == 3)
                return PlayerOption.X;
            else if (sum == -3)
                return PlayerOption.O;

        }

        // check columns
        for (int j = 0; j < Columns; j++)
        {
            sum = 0;
            for (int i = 0; i < Rows; i++)
            {
                var value = 0;
                if (cells[j, i].current == PlayerOption.X)
                    value = 1;
                else if (cells[j, i].current == PlayerOption.O)
                    value = -1;

                sum += value;
            }

            if (sum == 3)
                return PlayerOption.X;
            else if (sum == -3)
                return PlayerOption.O;

        }

        // check diagonals
        // top left to bottom right
        sum = 0;
        for(int i = 0; i < Rows; i++)
        {
            int value = 0;
            if (cells[i, i].current == PlayerOption.X)
                value = 1;
            else if (cells[i, i].current == PlayerOption.O)
                value = -1;

            sum += value;
        }

        if (sum == 3)
            return PlayerOption.X;
        else if (sum == -3)
            return PlayerOption.O;

        // top right to bottom left
        sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            int value = 0;

            if (cells[Columns - 1 - i, i].current == PlayerOption.X)
                value = 1;
            else if (cells[Columns - 1 - i, i].current == PlayerOption.O)
                value = -1;

            sum += value;
        }

        if (sum == 3)
            return PlayerOption.X;
        else if (sum == -3)
            return PlayerOption.O;

        return PlayerOption.NONE;
    }
}
