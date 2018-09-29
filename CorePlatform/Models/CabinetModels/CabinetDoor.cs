using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CorePlatform.Models.CabinetModels
{
    /// <summary>
    /// A physical door on the specified cabinet
    /// that is monitored and lockable.
    /// </summary>
    public class CabinetDoor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public Guid CabinetId { get; set; }
        public bool CanLock { get; set; }
        public bool CanMonitor { get; set; }
        public bool Unlocked { get; set; }
        public bool Opened { get; set; }
        public bool Armed { get; set; }
    }
}
