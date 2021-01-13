using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TournamentPlanner.Data;

namespace TournamentPlanner.Controllers
{
    [ApiController]
    [Route("api")]
    public class CoronaStatisticsController : ControllerBase
    {
        private readonly TournamentPlannerDbContext context;

        public CoronaStatisticsController(TournamentPlannerDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        [Route("players")]
        public async Task<IEnumerable<Player>> GetPlayers([FromQuery(Name="name")] string filter) => await context.GetFilteredPlayers(filter);
        
        
        [HttpPost]
        [Route("players")]
        public async Task<Player> AddPlayer([FromBody] Player newPlayer) => await context.AddPlayer(newPlayer);

        [HttpGet]
        [Route("matches/open")]
        public async Task<IEnumerable<Match>> GetOpenMatches() => await context.GetIncompleteMatches();
        
        
        [HttpPost]
        [Route("matches/generate")]
        public async Task GenerateMatchesForNextRound() => await context.GenerateMatchesForNextRound();
    }
}