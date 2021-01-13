using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TournamentPlanner.Data
{
    public class Player
    {
        public int ID { get; set; }

        [Required] public string Name { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public List<Match> Matches { get; set; } = new();
    }
}
