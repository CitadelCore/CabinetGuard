using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CorePlatform.Models.CabinetModels
{
    /// <summary>
    /// A physical fan in a fan module.
    /// </summary>
    public class Fan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid ModuleId { get; set; }
        public int Fanspeed { get; set; }
        public int InletTemperature { get; set; }
    }
}
