using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CorePlatform.Models.CabinetModels
{
    /// <summary>
    /// A physical cabinet.
    /// </summary>
    public class Cabinet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string Nickname { get; set; } = "Default";
        public Guid ControllerId { get; set; }
        public bool SecurityArmed { get; set; } = false;
        public bool SecurityAlerted { get; set; } = false;
        public bool FireAlerted { get; set; } = false;
        public bool Override { get; set; } = false;

        [Display(Name = "OneView Hostname")]
        public string OneviewHostname { get; set; }

        [Display(Name = "OneView Username")]
        public string OneviewUsername { get; set; }

        [Display(Name = "OneView Password")]
        public string OneviewPassword { get; set; }
    }
}
