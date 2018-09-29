using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CorePlatform.Models.CabinetModels
{
    /// <summary>
    /// A command that has been scheduled to run
    /// on a cabinet controller.
    /// </summary>
    public class ScheduledCommand
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public Guid ControllerId { get; set; }
        public string Name { get; set; }
        public string Error { get; set; }

        [NotMapped]
        public IDictionary<string, dynamic> Payload
        {
            get { return _Payload == null ? null : JsonConvert.DeserializeObject<IDictionary<string, dynamic>>(_Payload); }
            set { _Payload = JsonConvert.SerializeObject(value); }
        }
        public string _Payload { get; set; }

        [NotMapped]
        public CommandState State
        {
            get { return _State == null ? CommandState.Invalid : JsonConvert.DeserializeObject<CommandState>(_State); }
            set { _State = JsonConvert.SerializeObject(value); }
        }
        public string _State { get; set; }

        [NotMapped]
        public CommandResult Result
        {
            get { return _Result == null ? CommandResult.Invalid : JsonConvert.DeserializeObject<CommandResult>(_Result); }
            set { _Result = JsonConvert.SerializeObject(value); }
        }

        public string _Result { get; set; }

        [NotMapped]
        public string StatePretty;
        [NotMapped]
        public string ResultPretty;

        [NotMapped]
        public DateTime TimeCreated
        {
            get { return _TimeCreated == null ? DateTime.Now : JsonConvert.DeserializeObject<DateTime>(_TimeCreated); }
            set { _TimeCreated = JsonConvert.SerializeObject(value); }
        }

        [NotMapped]
        public string RowStyle;

        public string _TimeCreated { get; set; }


        [Timestamp]
        public byte[] Timestamp { get; set; }

        public void PrettifyEnums()
        {
            StatePretty = State.ToString();
            ResultPretty = Result.ToString();

            if (Result == CommandResult.Invalid || Result == CommandResult.Error) {
                RowStyle = "danger";
            }
            else if (Result == CommandResult.Success)
            {
                RowStyle = "success";
            }
            else if (Result == CommandResult.Cancelled || Result == CommandResult.Expired)
            {
                RowStyle = "warning";
            }
            else
            {
                RowStyle = String.Empty;
            }
        }
    }

    public enum CommandState
    {
        Invalid,
        Scheduled,
        InProgress,
        Complete,
    }

    public enum CommandResult
    {
        None,
        Invalid,
        Success,
        Error,
        Cancelled,
        Expired,
    }
}
