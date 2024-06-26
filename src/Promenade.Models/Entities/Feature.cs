﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ocuda.Promenade.Models.Entities
{
    public class Feature
    {
        [MaxLength(2000)]
        public string BodyText { get; set; }

        [MaxLength(48)]
        [Required]
        public string Icon { get; set; }

        [MaxLength(5)]
        public string IconText { get; set; }

        [Key]
        [Required]
        public int Id { get; set; }

        [MaxLength(255)]
        public string ImagePath { get; set; }

        [DisplayName("Location Page Section")]
        public bool IsAtThisLocation { get; set; }

        [NotMapped]
        public bool IsNewFeature { get; set; }

        [MaxLength(255)]
        [Required]
        public string Name { get; set; }

        [NotMapped]
        public bool NeedsPopup { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(80)]
        public string Stub { get; set; }
    }
}