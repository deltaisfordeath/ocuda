﻿using System.ComponentModel.DataAnnotations;

namespace Ocuda.Promenade.Models.Entities
{
    public class NavigationText : Abstract.BaseEntity
    {
        public int LanguageId { get; set; }
        [MaxLength(255)]
        public string AriaLabel { get; set; }
        [MaxLength(255)]
        public string Label { get; set; }
        [MaxLength(255)]
        public string Title { get; set; }
    }
}
