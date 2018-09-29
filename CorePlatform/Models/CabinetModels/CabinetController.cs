using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CorePlatform.Models.CabinetModels
{
    /// <summary>
    /// An enrolled cabinet controller.
    /// </summary>
    public class CabinetController
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>
        /// Revocation GUID of the JWT token.
        /// </summary>
        public Guid RevocationGuid { get; set; }

        /// <summary>
        /// Connection ID of the SignalR hub.
        /// This should be null when disconnected.
        /// </summary>
        public string HubConnectionId { get; set; }

        /// <summary>
        /// User ID of the owner user.
        /// This is the user that added the controller to management.
        [Display(Name = "User ID")]
        public string UserId { get; set; }

        /// <summary>
        /// Nickname of the controller.
        /// Max length is 16 as it is the most that can fit on the FMC screen.
        /// FMC name display is a work in progress.
        /// </summary>
        [Display(Name = "Nickname")]
        [MaxLength(16)]
        public string Nickname { get; set; }

        /// <summary>
        /// Hostname of the controller.
        /// </summary>
        [Display(Name = "Hostname")]
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Whether the controller is authorized and
        /// has a trust relationship with the server.
        /// </summary>
        [Display(Name = "Authorized")]
        public bool Authorized { get; set; } = false;

        /// <summary>
        /// Whether the FMC (failsafe management controller)
        /// is responding or not.
        /// </summary>
        [Display(Name = "FMC")]
        public bool FmcResponding { get; set; } = false;

        [NotMapped]
        public string RowStyle;

        /// <summary>
        /// Removes useless and sensitive information from the controller object.
        /// </summary>
        public CabinetController StripUselessInfo()
        {
            return new CabinetController
            {
                Id = Id,
                UserId = UserId,
                Nickname = Nickname,
                Hostname = Hostname,
                Authorized = Authorized,
            };
        }

        public void PrettifyStyles()
        {
            if (String.IsNullOrEmpty(HubConnectionId) && RevocationGuid != new Guid())
            {
                RowStyle = "danger";
            }
            else if (RevocationGuid == new Guid())
            {
                RowStyle = "warning";
            }
            else
            {
                RowStyle = "success";
            }
        }
    }
}
