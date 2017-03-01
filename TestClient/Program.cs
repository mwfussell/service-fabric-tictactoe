using Game.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Player.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var player1 = ActorProxy.Create<IPlayer>(ActorId.CreateRandom(), 
                                            new Uri("fabric:/ActorTicTacToeApplication/PlayerActorService"));
            var player2 = ActorProxy.Create<IPlayer>(ActorId.CreateRandom(), 
                                            new Uri("fabric:/ActorTicTacToeApplication/PlayerActorService"));
            var gameId = ActorId.CreateRandom();
            var game = ActorProxy.Create<IGame>(gameId, 
                                            new Uri("fabric:/ActorTicTacToeApplication/GameActorService"));
            var rand = new Random();
           
            var result1 = player1.JoinGameAsync(gameId, "Player 1");
            var result2 = player2.JoinGameAsync(gameId, "Player 2");

            if (!result1.Result || !result2.Result)
            {
                Console.WriteLine("Failed to join game.");
                return;
            }
            var player1Task = Task.Run(() => { MakeMove(player1, game, gameId); });
            var player2Task = Task.Run(() => { MakeMove(player2, game, gameId); });
            var gameTask = Task.Run(() =>
            {
                string winner = "";
                Console.Clear();
                Console.CursorVisible = false;
                PrintGrid();
                while (winner == "")
                {
                    var board = game.GetGameBoardAsync().Result;
                    PrintBoard(board);
                    winner = game.GetWinnerAsync().Result;
                    Task.Delay(500).Wait();
                }
                Console.CursorLeft = 20;
                Console.CursorTop = 12;
                Console.WriteLine("Winner is: " + winner);
                Console.CursorVisible = true;
            });

            gameTask.Wait();
            Console.Read();

        }
        private static async void MakeMove(IPlayer player, IGame game, ActorId gameId)
        {
            Random rand = new Random();
            while (true)
            {
                await player.MakeMoveAsync(gameId, rand.Next(0, 3), rand.Next(0, 3));
                await Task.Delay(rand.Next(300, 1000));
            }
        }
        private static void PrintGrid()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("_______ _____ _______    _______ _______ _______     _______  _____  _______");
            Console.WriteLine("   |      |   |      ___    |    |_____| |        ___   |    |     | |______");
            Console.WriteLine("   |    __|__ |_____        |    |     | | _____        |    |_____| |______");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("                         ╔═══╦═══╦═══╗");
            Console.WriteLine("                         ║   ║   ║   ║");
            Console.WriteLine("                         ╠═══╬═══╬═══╣");
            Console.WriteLine("                         ║   ║   ║   ║");
            Console.WriteLine("                         ╠═══╬═══╬═══╣");
            Console.WriteLine("                         ║   ║   ║   ║");
            Console.WriteLine("                         ╚═══╩═══╩═══╝");
        }
        private static void PrintBoard(int[] board)
        {
            for (int i = 0; i < board.Length; i++)
            {
                Console.CursorLeft = (i % 3) * 4 + 26;
                Console.CursorTop = (i / 3) * 2 + 5;
                if (board[i] == -1)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" X ");
                }
                else if (board[i] == 1)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(" O ");
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
