using System;
using System.ComponentModel.DataAnnotations;

namespace WebChoiCoCaro.Models
{
    public class GameHistory
    {
        [Key]
        public int Id { get; set; }

        public string RoomId { get; set; }

        // store player identifiers/names
        public string PlayerXId { get; set; }
        public string PlayerOId { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        // winner value expected by GameHub (e.g. "X", "O", "Draw")
        public string Winner { get; set; }
    }
}