using CorePlatform.Models.CabinetModels;
using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagementController.Models
{
    /// <summary>
    /// Stores settings about the current controller.
    /// </summary>
    class ControllerSettings
    {
        [JsonIgnore]
        private IFile settingsFile;

        public string Guid;
        public string JwtToken;
        public string CCSHostname;
        public long CCSPort;
        public long CCSCertPort;

        public void ClearCCS()
        {
            // Default settings; these are not used.
            CCSHostname = "localhost";
            CCSPort = 44347;
            CCSCertPort = 44348;
        }

        /// <summary>
        /// Reads controller settings from the filesystem.
        /// </summary>
        public static async Task<ControllerSettings> ReadAsync()
        {
            IFolder rootFolder = FileSystem.Current.LocalStorage;
            IFolder configFolder = await rootFolder.CreateFolderAsync("Configuration", CreationCollisionOption.OpenIfExists);
            IFile file = await configFolder.CreateFileAsync("configuration.json", CreationCollisionOption.OpenIfExists);

            string text = await file.ReadAllTextAsync();

            if (String.IsNullOrEmpty(text))
            {
                ControllerSettings controllerSettings = new ControllerSettings()
                {
                    settingsFile = file,
                    Guid = null,

                    CCSHostname = "localhost",
                    CCSPort = 44347,
                    CCSCertPort = 44348,
                };

                await file.WriteAllTextAsync(JsonConvert.SerializeObject(controllerSettings));

                return controllerSettings;
            }

            ControllerSettings settings = JsonConvert.DeserializeObject<ControllerSettings>(text);
            settings.settingsFile = file;

            return settings;
        }

        /// <summary>
        /// Saves controller settings to the filesystem.
        /// </summary>
        public async Task SaveAsync()
        {
            await settingsFile.WriteAllTextAsync(JsonConvert.SerializeObject(this));
        }
    }
}
