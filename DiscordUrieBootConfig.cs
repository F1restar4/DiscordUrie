using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordUrie_DSharpPlus
{

     public class DiscordUrieBootConfig
    {
        [JsonProperty]
        public string BotToken { get; set; }
        [JsonProperty]
        public string LavalinkPassword { get; set; }
        [JsonProperty]
        public int ScpID { get; set; }
        [JsonProperty]
        public string ScpKey { get; set; }

        [JsonConstructor]
        public DiscordUrieBootConfig(string BotToken, string LavalinkPassword, int ScpID, string ScpKey)
        {
            this.BotToken = BotToken;
            this.LavalinkPassword = LavalinkPassword;
            this.ScpID = ScpID;
            this.ScpKey = ScpKey;
        }
    }

    public static class DiscordUrieBootSettings
    {
        public static DiscordUrieBootConfig GetBootConfig()
        {
            if (!File.Exists("BootConfig.json"))
            {
                var Config = new DiscordUrieBootConfig("", "", 0, "");
                File.WriteAllText("BootConfig.json", JsonConvert.SerializeObject(Config, Formatting.Indented));
                Console.WriteLine("Please fill out the BootConfig.json file.");
                Environment.Exit(0);
                throw new FileNotFoundException("A new file has been created with blank data, please fill it out", fileName: "BootConfig.json");
            }

            var data = JsonConvert.DeserializeObject<DiscordUrieBootConfig>(File.ReadAllText("BootConfig.json"));
            if (data == null || String.IsNullOrEmpty(data.BotToken) || String.IsNullOrEmpty(data.LavalinkPassword) || data.ScpID == 0 || String.IsNullOrEmpty(data.ScpKey))
            {
                Console.WriteLine("BootConfig.json is invalid, please check it and reboot");
                Environment.Exit(0);
                throw new FileLoadException();
            }

            return data;
        }

        public async static Task SaveBootConfig(DiscordUrieBootConfig config)
            =>  await File.WriteAllTextAsync("BootConfig.json", JsonConvert.SerializeObject(config, Formatting.Indented));

    }
}