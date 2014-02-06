using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PP_lab2_connect4_1
{
    public class BoardClass
    {
        public const int HUMAN = 2;
        public const int CPU = 1;
        public const int HEIGHT = 6;
        public const int WIDTH = 7;

        public int[,] Board;

        public BoardClass()
        {
            Board = new int[HEIGHT, WIDTH];
        }

        public BoardClass(int[,] board)
        {
            Board = new int[HEIGHT, WIDTH];
            Board = board;
        }

        public BoardClass(string board)
        {
            int i = 0, j = 0;
            //Console.WriteLine(board);
            Board = new int[HEIGHT,WIDTH];
            foreach(char c in board)
            {
                Board[i, j] = (int)Char.GetNumericValue(c);
                j++;
                if(j==WIDTH)
                {
                    j = 0;
                    i++;
                }
            }
        }

        /// <summary>
        /// Reads board from txt file
        /// </summary>
        /// <param name="fileName">path to file</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ReadFromFile(string fileName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(fileName))
                {
                    String line;
                    int row = 0;

                    while((line=sr.ReadLine()) != null)
                    {
                        string[] exploded = line.Split(' ');
                        for (int j=0; j<exploded.Length; j++)
                        {
                            Board[row, j] = Convert.ToInt32(exploded[j]);
                        }
                        row++;
                    }

                    sr.Close();
                    return true;
                }

            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Prints board to console
        /// </summary>
        public void ToScreen()
        {
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    Console.Write(Board[i, j] + " ");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Searches for last empty place in column
        /// and places player's ID at that place
        /// </summary>
        /// <param name="column">Column to put chip in</param>
        /// <param name="player">Player making the move</param>
        public void MakeMove(int column, int player)
        {
            if (MoveLegal(column))
            {
                int rowFound = HEIGHT - 1;

                for(int i=0; i<HEIGHT; i++)
                {
                    if(Board[i, column] != 0)
                    {
                        rowFound = i - 1;
                        break;
                    }
                }

                Board[rowFound, column] = player;
            }
            else
            {
                throw new Exception("Potez nemoguc!");
            }
        }

        /// <summary>
        /// Checks whether the move is legal
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public bool MoveLegal(int column)
        {
            if (column>7 || column <1 || Board[0, column] != 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Deletes last move in the column
        /// </summary>
        /// <param name="column">Column in which to delete move</param>
        public void UndoMove(int column)
        {
            for (int i=0; i<HEIGHT; i++)
            {
                if (Board[i, column] != 0)
                {
                    Board[i, column] = 0;
                    break;
                }
            }
        }

        /// <summary>
        /// Checks for winner after last move
        /// </summary>
        /// <param name="lastMoveColumn">Column of last move</param>
        /// <returns>1 if CPU is winner
        ///          2 if Human
        ///          0 otherwise</returns>
        public int FindWinner(int lastMoveColumn)
        {
            int lastMoveRow = 0;
            int lastPlayer = 0;
            int winner = 0;

            for(int i=0; i<HEIGHT; i++)
            {
                if(Board[i, lastMoveColumn] != 0)
                {
                    lastMoveRow = i;
                    lastPlayer = Board[lastMoveRow, lastMoveColumn];
                    break;
                }
            }

            //pregled okomito
            winner = checkColumn(lastMoveColumn, lastPlayer);
            if (winner != 0) return winner;

            //pregled vodoravno
            winner = checkRows(lastMoveRow, lastPlayer);
            if (winner != 0) return winner;

            //pregled desne dijagonale
            winner = checkRightDiagonal(lastMoveRow, lastMoveColumn, lastPlayer);
            if (winner != 0) return winner;

            //pregled lijeve dijagonale
            winner = checkLeftDiagonal(lastMoveRow, lastMoveColumn, lastPlayer);
            if (winner != 0) return winner;

            return 0;
        }

        private int checkLeftDiagonal(int lastMoveRow, int lastMoveColumn, int lastPlayer)
        {
            int pocRedak = lastMoveRow;
            int pocStupac = lastMoveColumn;
            int cnt = 0;


            for (int j = 0; j < Math.Max(HEIGHT, WIDTH); j++)
            {
                if (lastMoveColumn - j < 0 || lastMoveRow - j < 0)
                {
                    break;
                }
                pocStupac = lastMoveColumn - j;
                pocRedak = lastMoveRow - j;
            }

            for (int j = 0; j < Math.Max(HEIGHT, WIDTH); j++)
            {
                if (pocStupac + j >= WIDTH || pocRedak + j >= HEIGHT)
                {
                    break;
                }
                if (Board[pocRedak + j, pocStupac + j] == lastPlayer)
                {
                    cnt++;
                    if (cnt == 4)
                    {
                        return lastPlayer;
                    }
                }

                else
                {
                    cnt = 0;
                }
            }

            return 0;
        }

        private int checkRightDiagonal(int lastMoveRow, int lastMoveColumn, int lastPlayer)
        {
            int pocRedak = lastMoveRow;
            int pocStupac = lastMoveColumn;
            int cnt = 0;


            for (int j = 0; j < Math.Max(HEIGHT, WIDTH); j++)
            {
                if (lastMoveColumn + j >= WIDTH || lastMoveRow - j < 0)
                {
                    break;
                }
                pocStupac = lastMoveColumn + j;
                pocRedak = lastMoveRow - j;
            }

            for (int j = 0; j < Math.Max(HEIGHT, WIDTH); j++)
            {
                if (pocStupac - j < 0 || pocRedak + j >= HEIGHT)
                {
                    break;
                }
                if (Board[pocRedak + j, pocStupac - j] == lastPlayer)
                {
                    cnt++;
                    if (cnt == 4)
                    {
                        return lastPlayer;
                    }
                }

                else
                {
                    cnt = 0;
                }
            }

            return 0;
        }

        private int checkRows(int lastMoveRow, int lastPlayer)
        {
            int cnt = 0;
            for (int j = 0; j < WIDTH; j++)
            {

                if (Board[lastMoveRow, j] == lastPlayer)
                {
                    cnt++;
                    if (cnt == 4)
                    {
                        return lastPlayer;
                    }
                }

                else
                {
                    cnt = 0;
                }
            }
            return 0;
        }

        private int checkColumn(int lastMoveColumn, int lastPlayer)
        {
            int cnt = 0;
            for (int j = 0; j < HEIGHT; j++)
            {
                if (Board[j, lastMoveColumn] == lastPlayer)
                {
                    cnt++;
                    if (cnt == 4)
                    {
                        return lastPlayer;
                    }
                }
                else
                {
                    cnt = 0;
                }
            }
            return 0;
        }

        internal string MyToString()
        {
            string boardString = "";
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    boardString += Board[i, j];
                }
            }
            //this.ToScreen();
            //Console.WriteLine(boardString);
            return boardString;
        }
    }
}
