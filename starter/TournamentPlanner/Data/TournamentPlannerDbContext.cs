using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace TournamentPlanner.Data
{
    public enum PlayerNumber { Player1 = 1, Player2 = 2 };

    public class TournamentPlannerDbContext : DbContext
    {
        public TournamentPlannerDbContext(DbContextOptions<TournamentPlannerDbContext> options)
            : base(options)
        { }

        public DbSet<Player> Players { get; set; }
        
        public DbSet<Match> Matches { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Match>().HasOne(m => m.Player1).WithMany().HasForeignKey(m => m.Player1Id).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Match>().HasOne(m => m.Player2).WithMany().HasForeignKey(m => m.Player2Id).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Match>().HasOne(m => m.Winner).WithMany().HasForeignKey(m => m.WinnerId).OnDelete(DeleteBehavior.NoAction);
        }
        
        /// <summary>
        /// Adds a new player to the player table
        /// </summary>
        /// <param name="newPlayer">Player to add</param>
        /// <returns>Player after it has been added to the DB</returns>
        public async Task<Player> AddPlayer(Player newPlayer)
        {
            await Players.AddAsync(newPlayer);
            await SaveChangesAsync();
            return newPlayer;
        }

        /// <summary>
        /// Adds a match between two players
        /// </summary>
        /// <param name="player1Id">ID of player 1</param>
        /// <param name="player2Id">ID of player 2</param>
        /// <param name="round">Number of the round</param>
        /// <returns>Generated match after it has been added to the DB</returns>
        public async Task<Match> AddMatch(int player1Id, int player2Id, int round)
        {
            var match = new Match()
            {
                Player1 = await Players.Where(p => p.ID == player1Id).FirstOrDefaultAsync(),
                Player2 = await Players.Where(p => p.ID == player2Id).FirstOrDefaultAsync(),
                Round = round
            };
            await Matches.AddAsync(match);
            await SaveChangesAsync();
            return match;
        }

        /// <summary>
        /// Set winner of an existing game
        /// </summary>
        /// <param name="matchId">ID of the match to update</param>
        /// <param name="player">Player who has won the match</param>
        /// <returns>Match after it has been updated in the DB</returns>
        public async Task<Match> SetWinner(int matchId, PlayerNumber player)
        {
            var match = await Matches.Where(m => m.ID == matchId).FirstOrDefaultAsync();
            match.Winner = player == PlayerNumber.Player1 ? match.Player1 : match.Player2;
            await SaveChangesAsync();
            return match;
        }

        /// <summary>
        /// Get a list of all matches that do not have a winner yet
        /// </summary>
        /// <returns>List of all found matches</returns>
        public async Task<IList<Match>> GetIncompleteMatches()
        {
            return await Matches.Where(m => m.Winner == null).ToListAsync();
        }

        /// <summary>
        /// Delete everything (matches, players)
        /// </summary>
        public async Task DeleteEverything()
        {
            await using var transaction = await Database.BeginTransactionAsync();
            try
            {
                await Database.ExecuteSqlRawAsync("delete from matches");
                await Database.ExecuteSqlRawAsync("delete from players");

                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (SqlException)
            {
                await transaction.RollbackAsync();
            }
        }

        /// <summary>
        /// Get a list of all players whose name contains <paramref name="playerFilter"/>
        /// </summary>
        /// <param name="playerFilter">Player filter. If null, all players must be returned</param>
        /// <returns>List of all found players</returns>
        public async Task<IList<Player>> GetFilteredPlayers(string playerFilter = null)
        {
            if (playerFilter == null) return await Players.ToListAsync();
            return await Players.Where(p => p.Name.Contains(playerFilter)).ToListAsync();
        }

        /// <summary>
        /// Generate match records for the next round
        /// </summary>
        /// <exception cref="InvalidOperationException">Error while generating match records</exception>
        public async Task GenerateMatchesForNextRound()
        {
            await using var transaction = await Database.BeginTransactionAsync();
            try
            {
                if (Matches.Any(m => m.Winner == null))
                {
                    throw new InvalidOperationException("Match in DB has no winner");
                }

                if (Players.Count() != 32)
                {
                    throw new InvalidOperationException("Not 32 players in DB");
                }

                int nextRound;
                switch (Matches.Count()) 
                {
                    case 0: nextRound = 1;
                        break;
                    case 16: nextRound = 2;
                        break;
                    case 24: nextRound = 3;
                        break;
                    case 28: nextRound = 4;
                        break;
                    case 30: nextRound = 5;
                        break;
                    default: throw new InvalidOperationException("Invalid amount of matches in DB");
                }

                var availablePlayers =  nextRound == 1 ? await Players.ToListAsync() : await Matches.Where(m => m.Winner != null && m.Round == nextRound - 1).Select(m => m.Winner).ToListAsync();
                Random random = new Random();
                int matches = availablePlayers.Count / 2;
                for (int i = 0; i < matches; i++)
                {
                    Player player1 = Players.ToList()[random.Next(0, availablePlayers.Count)];
                    availablePlayers.Remove(player1);
                    Player player2 = Players.ToList()[random.Next(0, availablePlayers.Count)];
                    availablePlayers.Remove(player2);
                    await AddMatch(player1.ID, player2.ID, nextRound);
                }
                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (SqlException ex)
            {
                await transaction.RollbackAsync();
            }
        }
    }
}
