using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPI;

namespace PP_lab2_connect4_1
{
    class Program
    {
        private const int MaxDepth = 8;
        private const string fileName = "board.txt";

        static void Main(string[] args)
        {
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;

                // ako je master
                if (comm.Rank == 0)
                {
                    int results = 0;
                    Stopwatch stopwatch = new Stopwatch();

                    // ucitavanje ploce iz datoteke i ispisivanje na ekran
                    BoardClass board = new BoardClass();
                    board.ReadFromFile(fileName);
                    board.ToScreen();
                    Console.WriteLine("");

                    // ima li odmah pobjednika?
                    for (int i=0; i< BoardClass.WIDTH; i++)
                    {
                        if (board.FindWinner(i) != 0)
                        {
                            board.ToScreen();
                            string winner;
                            winner = board.FindWinner(i) == 1 ? "CPU" : "HUMAN";
                            Console.WriteLine("Pobjednik je: " + winner);
                            return;
                        }
                    }

                    // ako nema, krecemo dalje, prvo svima saljemo kopiju tablice
                    stopwatch.Start();
                    SerMessage message = new SerMessage{MsgType = "Tablica", Board = board.MyToString()};

                    while (true)
                    {
                        Dictionary<String, Double> Tasks = new Dictionary<string, double>();

                        for (int i=1; i < comm.Size; i++)
                        {
                            comm.Send(message, i, 0);
                        }
                    // ----------------------------------------------------------------

                        // Primanje i slanje zahtjeva dok svi ne budu rjeseni
                        bool ended = false;

                        while (!ended)
                        {
                            // Primanje zathjeva
                            var rcvMessage = comm.Receive<SerMessage>(Communicator.anySource, 0);
                            int curSender = rcvMessage.ProcessID;

                            // ako je primljena poruka zahtjev, saljem zadatak
                            if (rcvMessage.MsgType == "Zahtjev")
                            {
                                try
                                {
                                    int[] task = fetchTask(Tasks);
                                    SerMessage TaskMessage = new SerMessage
                                                                 {
                                                                     MsgType = "Zadatak",
                                                                     MoveCPU = task[0],
                                                                     MovePlayer = task[1]
                                                                 };
                                    comm.Send(TaskMessage, rcvMessage.ProcessID, 0);
                                    //Console.WriteLine("Primio zahtjev od: " + rcvMessage.ProcessID +
                                    //                  " Saljem zadatak: " + task[0] + "," + task[1]);
                                }
                                catch(Exception ex)
                                {
                                    //ended = true;
                                }
                            }

                            // ako je primljena poruka rezultat izracuna
                            else if (rcvMessage.MsgType == "Rezultat")
                            {
                                //Console.WriteLine("Primio Rezultat od: " + curSender + " iznos: " + rcvMessage.Evaluation + " Dosad: " + (results+1));
                                results++;
                                //Console.WriteLine("REZULTATA PRIMLJENO: "+results);
                                Tasks[rcvMessage.MoveCPU + "," + rcvMessage.MovePlayer] = rcvMessage.Evaluation;
                                if (results == BoardClass.WIDTH * BoardClass.WIDTH)
                                    ended = true;
                            }
                        }

                        Console.WriteLine("Gotovo! Primljeno rezultata: " + results);
                        Console.WriteLine("");

                        // posalji poruku radnicima da su gotovi (da izadu iz petlje)
                        for (int i = 1; i < comm.Size; i++)
                        {
                            SerMessage porukaZaKraj = new SerMessage();
                            porukaZaKraj.MsgType = "Kraj";
                            comm.Send(porukaZaKraj, i, 0);
                        }

                        // Konacni izracun
                        double[] columnsResults = new double[BoardClass.WIDTH];
                        for (int i = 0; i < BoardClass.WIDTH; i++)
                        {
                            for (int j = 0; j < BoardClass.WIDTH; j++)
                            {
                                columnsResults[i] += Tasks[i + "," + j] / BoardClass.WIDTH;
                            }
                        }
                        stopwatch.Stop();
                        int broj = 0;
                        foreach (double d in columnsResults)
                        {
                            Console.WriteLine(broj + ": " + d);
                            broj++;
                        }
                        // prikaz rezultata:
                        double bestValue = columnsResults.Max();
                        var bestColumn = columnsResults.ToList().IndexOf(bestValue);
                        Console.WriteLine("\nNajbolji stupac: " + bestColumn + " vrijednost: " + bestValue);
                        Console.WriteLine("Vrijeme izvođenja: {0}", stopwatch.Elapsed);
                        break;
                    }
                }

                //____________________________________________________________________________

                //ako je worker
                else
                {
                    bool ended = false;
                    BoardClass boardCopy = new BoardClass();

                    while (!ended)
                    {
                        SerMessage rcvMessage = comm.Receive<SerMessage>(0, 0);

                        // ako je primljena poruka oznaka kraja, prekini posao
                        if (rcvMessage.MsgType == "Kraj") break;

                        // ako je primljena tablica, stvori lokalnu kopiju
                        if (rcvMessage.MsgType == "Tablica")
                        {
                            boardCopy = new BoardClass(rcvMessage.Board);
                            //Console.WriteLine("");
                            //boardCopy.ToScreen();
                        }

                        // baci se na posao
                        while(true)
                        {
                            // Saljem zahtjev za zadatakom
                            SerMessage request = new SerMessage{MsgType = "Zahtjev", ProcessID = comm.Rank};
                            comm.Send(request, 0, 0);
                            
                            // Ocekujem zadatak:
                            SerMessage taskMsg = comm.Receive<SerMessage>(0, 0);
                            if (taskMsg.MsgType == "Kraj")
                            {
                                ended = true;
                                break;
                            }
                            int moveCPU = taskMsg.MoveCPU;
                            int movePlayer = taskMsg.MovePlayer;

                            // Prvo potez racunala
                            try
                            {
                                boardCopy.MakeMove(moveCPU, BoardClass.CPU);
                            }
                            catch (Exception)
                            {
                                //Console.WriteLine("Ilegalan potez!");
                            }
                            if (boardCopy.FindWinner(moveCPU) == BoardClass.CPU)
                            {
                                // Ako je racunalo pobjednik, dojavi to masteru
                                SerMessage resultMsg = new SerMessage
                                {
                                    MsgType = "Rezultat",
                                    MoveCPU = moveCPU,
                                    MovePlayer = movePlayer,
                                    Evaluation = 1,
                                    ProcessID = comm.Rank
                                };

                                comm.Send(resultMsg, 0, 0);
                                boardCopy.UndoMove(moveCPU);
                                Console.WriteLine("POBJEDNIK je: CPU");
                            }

                            // Zatim potez igraca
                            try
                            {
                                boardCopy.MakeMove(movePlayer, BoardClass.HUMAN);
                            }
                            catch (Exception)
                            {
                                //Console.WriteLine("Ilegalan potez!");
                            }
                            if (boardCopy.FindWinner(movePlayer) == BoardClass.HUMAN)
                            {
                                // Ako je racunalo pobjednik, dojavi to masteru
                                SerMessage resultMsg = new SerMessage
                                {
                                    MsgType = "Rezultat",
                                    MoveCPU = moveCPU,
                                    MovePlayer = movePlayer,
                                    Evaluation = -1,
                                    ProcessID = comm.Rank
                                };

                                comm.Send(resultMsg, 0, 0);
                                boardCopy.UndoMove(movePlayer);
                                boardCopy.UndoMove(moveCPU);
                                Console.WriteLine("POBJEDNIK je: Igrac");
                            }

                            // ako nema pobjednika, ulazimo u rekurzivno izracunavanje
                            SerMessage resultMsg1 = new SerMessage
                            {
                                MsgType = "Rezultat",
                                MoveCPU = moveCPU,
                                MovePlayer = movePlayer,
                                ProcessID = comm.Rank
                            };
                            resultMsg1.Evaluation = evaluate(boardCopy, BoardClass.HUMAN, movePlayer, MaxDepth - 3);
                            boardCopy.UndoMove(movePlayer);
                            boardCopy.UndoMove(moveCPU);
                            comm.Send(resultMsg1,0,0);
                        }
                    }
                }
            }

        }

        private static double evaluate(BoardClass boardCopy, int lastPlayer, int columnToPlay, int depth)
        {
            double result = 0;
            double resultSum = 0;
            int movesCNT = 0;
            bool allBad = true;
            bool allGood = true;

            BoardClass board = new BoardClass(boardCopy.MyToString());

            int winner = board.FindWinner(columnToPlay);
            // Ako je racunalo pobjednik, vracamo 1
            if (winner == BoardClass.CPU) return 1;
            
            // Ako je igrac pobjednik, vracamo -1
            if (winner == BoardClass.HUMAN) return -1;

            // Ako smo dosli do kraja vracamo 0
            if (depth == 0) return 0;

            // Ako nista od toga, idemo na izracun
            //depth--;
            int nextPlayer = lastPlayer == BoardClass.CPU ? BoardClass.HUMAN : BoardClass.CPU;

            for (int i=0; i<BoardClass.WIDTH; i++)
            {
                if(board.MoveLegal(i))
                {
                    movesCNT++;
                    board.MakeMove(i, nextPlayer);
                    result = evaluate(board, nextPlayer, i, depth-1);
                    board.UndoMove(i);

                    if (result == 1 && nextPlayer == BoardClass.CPU) return 1;
                    if (result == -1 && nextPlayer == BoardClass.HUMAN) return -1;
                    if (result > -1) allBad = false;
                    if (result != 1) allGood = false;

                    resultSum += result;
                }
            }

            if (allGood) return 1;
            if (allBad) return -1;
            //Console.WriteLine(movesCNT);
            return resultSum/movesCNT;
        }

        private static int[] fetchTask(Dictionary<string, double> Tasks)
        {
            int[] returnTasks = new int[2];

            for(int i=0; i<BoardClass.WIDTH; i++)
            {
                for (int j=0; j<BoardClass.WIDTH; j++)
                {
                    if (!Tasks.ContainsKey(i + "," + j))
                    {
                        Tasks[i + "," + j] = 0;
                        returnTasks[0] = i;
                        returnTasks[1] = j;
                        return returnTasks;
                    }
                }
            }

            throw new Exception("");
        }
    }
}
