using Game.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Game
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class Game : Actor, IGame
    {
        private const string mStateName = "state";
        /// <summary>
        /// This class contains each actor's replicated state.
        /// Each instance of this class is serialized and replicated every time an actor's state is saved.
        /// For more information, see http://aka.ms/servicefabricactorsstateserialization
        /// </summary>
        [DataContract]
        public class ActorState:ICloneable

        {
            [DataMember]
            public int[] Board;
            [DataMember]
            public string Winner;
            [DataMember]
            public List<Tuple<long, string>> Players;
            [DataMember]
            public int NextPlayerIndex;
            [DataMember]
            public int NumberOfMoves;

            public object Clone()
            {
                ActorState ret = new ActorState
                {
                    NextPlayerIndex = this.NextPlayerIndex,
                    Winner = this.Winner,
                    NumberOfMoves = this.NumberOfMoves,
                    Players = new List<Tuple<long, string>>(),
                    Board = new int[this.Board.Length]
                };
                Array.Copy(this.Board, ret.Board, this.Board.Length);
                foreach (var p in this.Players)
                    ret.Players.Add(new Tuple<long, string>(p.Item1, p.Item2));
                return ret;
            }
        }

        public Game(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            return this.StateManager.TryAddStateAsync<ActorState>(mStateName,
                new ActorState()
                {
                    Board = new int[9],
                    Winner = "",
                    Players = new List<Tuple<long, string>>(),
                    NextPlayerIndex = 0,
                    NumberOfMoves = 0
                });
        }
        public async Task<bool> JoinGameAsync(long playerId, string playerName)
        {
            var state = await this.StateManager.GetStateAsync<ActorState>(mStateName);
            if (state.Players.Count >= 2    //there are already 2 players joined
                || state.Players.FirstOrDefault(p => p.Item2 == playerName) != null)    //a player with the same name has already joined
                return false;
            state.Players.Add(new Tuple<long, string>(playerId, playerName));
            return true;
        }
        public async Task<int[]> GetGameBoardAsync()
        {
            var state = await this.StateManager.GetStateAsync<ActorState>(mStateName);
            return state.Board;
        }
        public async Task<string> GetWinnerAsync()
        {
            var state = await this.StateManager.GetStateAsync<ActorState>(mStateName);
            return state.Winner;
        }
        public async Task<bool> MakeMoveAsync(long playerId, int x, int y)
        {
            var state = await this.StateManager.GetStateAsync<ActorState>(mStateName);

            if (x < 0 || x > 2 || y < 0 || y > 2    //invalid move
                || state.Players.Count != 2         //wrong # of players
                || state.NumberOfMoves >= 9         //board is already filled
                || state.Winner != "")              //a winner has already been decided
                return false;

            int index = state.Players.FindIndex(p => p.Item1 == playerId);
            if (index == state.NextPlayerIndex)     //allow move only when it's the player's turn
            {
                if (state.Board[y * 3 + x] == 0)
                {
                    int piece = index * 2 - 1;
                    state.Board[y * 3 + x] = piece;
                    state.NumberOfMoves++;

                    if (HasWon(state, piece * 3))
                        state.Winner = state.Players[index].Item2 + " (" +
                                           (piece == -1 ? "X" : "O") + ")";
                    else if (state.Winner == "" && state.NumberOfMoves >= 9)

                        state.Winner = "TIE";

                    state.NextPlayerIndex = (state.NextPlayerIndex + 1) % 2;    //set next player's turn
                    await this.StateManager.SetStateAsync<ActorState>(mStateName, state);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        private bool HasWon(ActorState state, int sum)
        {
            return state.Board[0] + state.Board[1] + state.Board[2] == sum
                || state.Board[3] + state.Board[4] + state.Board[5] == sum
                || state.Board[6] + state.Board[7] + state.Board[8] == sum
                || state.Board[0] + state.Board[3] + state.Board[6] == sum
                || state.Board[1] + state.Board[4] + state.Board[7] == sum
                || state.Board[2] + state.Board[5] + state.Board[8] == sum
                || state.Board[0] + state.Board[4] + state.Board[8] == sum
                || state.Board[2] + state.Board[4] + state.Board[6] == sum;
        }
    }
}
