using System;
using System.ComponentModel.DataAnnotations;

namespace WebChoiCoCaro.Models
{
    public class MoveRecord
    {
        [Key]
        public int Id { get; set; }

        public int GameHistoryId { get; set; }

        // 'X' or 'O'
        public string PlayerSymbol { get; set; }

        public int Row { get; set; }
        public int Col { get; set; }

        public DateTime MoveTime { get; set; }
    }
}