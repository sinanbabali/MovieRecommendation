using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Entities.Models
{
    public class Film
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string OriginalLanguage { get; set; }
        public string Overview { get; set; }
        public DateTimeOffset ReleaseDate { get; set; }
        public int VoteAverage { get; set; }
        public int VoteCount { get; set; }
        public string? Note { get;set; }
        public int Point { get; set; }

    }
}
