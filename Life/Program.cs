using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;


namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class boadThread
    {
        Thread BoardThread, stop;
        public bool run = false;
        string options;

        public boadThread(string optionsFile)
        {
            options = optionsFile;
            BoardThread = new Thread(this.boardRun);
            stop = new Thread(this.pause);
        }

        void boardRun()
        {
            
        }

        void pause()
        {

        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public string stateToString()
        {
            string res = "";
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    var cell = Cells[col, row];
                    if (cell.IsAlive)
                    {
                        res+="*";
                    }
                    else
                    {
                        res+=" ";
                    }
                }
                res+="\n";
            }
            return res;
        }

        public void setState(string state)
        {
            string[] subRows = state.Split('\n');
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    char s = subRows[row][col];
                    var cell = new Cell();
                    if (s == '*')
                    {
                        cell.IsAlive = true;
                    }
                    else
                    {
                        cell.IsAlive = false;
                    }
                    Cells[col, row] = cell;
                }
            }
            ConnectNeighbors();
        }
    }
    class Program
    {
        static Board board;
        static Thread BoardThread, stop;
        static public bool run = true;
        static object lockOn;

        static public void startThreads()
        {
            BoardThread = new Thread(boardRun);
            stop = new Thread(pause);
            lockOn = new object();
            BoardThread.Start();
            stop.Start();
        }

        static void boardRun()
        {
            while (true)
            {
                if (run)
                {
                    lock (lockOn)
                    {
                        // Console.Clear();
                        Console.SetCursorPosition(0, 0);
                        Render();
                        board.Advance();
                        Console.WriteLine("press any key to pause\n");
                        Thread.Sleep(110);
                    }
                }
            }
        }

        static void pause()
        {
            while (true)
            {
                Console.ReadKey();
                lock (lockOn)
                {
                    run = false;
                    while (true)
                    {
                        Console.Clear();
                        Render();
                        Console.WriteLine("\npress s to save, l to load or any other key to continue\n");
                        char c = Console.ReadKey().KeyChar;
                        if (c == 's')
                        {
                            Console.WriteLine("\nenter file name (with .txt)\n");
                            string fileName = Console.ReadLine();
                            saveToFile(fileName);
                        }
                        else if (c == 'l')
                        {
                            Console.WriteLine("\nenter file name (with .txt)\n");
                            string fileName = Console.ReadLine();
                            loadFromFile(fileName);
                        }
                        else
                        {
                            Console.Clear();
                            run = true;
                            break;
                        }
                    }
                }
            }
        }
        static private void Reset(string optionsFile = "")
        {
            if (optionsFile == "")
            {
                board = new Board(
                    width: 50,
                    height: 20,
                    cellSize: 1,
                    liveDensity: 0.5);
            } else
            {
                string optionsString = File.ReadAllText(optionsFile);
                Dictionary<string, double>  options = JsonSerializer.Deserialize<Dictionary<string, double>>(optionsString);
                board = new Board((int)options["width"], (int)options["height"], (int)options["cellSize"], options["liveDensity"]);
            }
        }
        static void Render()
        {
            Console.Write(board.stateToString());
        }

        static void saveToFile(string fileName)
        {
            File.WriteAllText(fileName, board.stateToString());
        }

        static void loadFromFile(string fileName)
        {
            string state = File.ReadAllText(fileName);
            board.setState(state);
        }
        static void Main(string[] args)
        {
            Reset("../../../options.json");
            startThreads();
        }
    }
}