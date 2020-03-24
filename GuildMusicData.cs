using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using DSharpPlus.Entities;
namespace DiscordUrie_DSharpPlus
{
    public class GuildMusicData
    {
        public ulong GuildId { get; }
        public List<LavalinkTrack> Queue { get; }
        public LavalinkTrack NowPlaying { get; internal set; }
        public DiscordGuild Guild { get; }
        public DiscordChannel Channel { get; }
        public DiscordChannel UpdateChannel { get; }

        public GuildMusicData(DiscordGuild guild, DiscordChannel voiceChannel, DiscordChannel updateChannel)
        {
            this.GuildId = guild.Id;
            this.Guild = guild;
            this.Channel = voiceChannel;
            this.UpdateChannel = updateChannel;
            this.Queue = new List<LavalinkTrack>();
        }

        public void Enqueue(LavalinkTrack track)
            => this.Queue.Add(track);

        public void RemoveQueue(int index)
            => this.Queue.RemoveAt(index);

        public void ClearQueue()
            => this.Queue.Clear();

        public void ClearNP()
            => NowPlaying = null;

        public void StartNext()
        {
            this.NowPlaying = this.GetNext();
            this.Queue.RemoveAt(0);
        }

        public LavalinkTrack GetNext()
            => this.Queue.First();

    }

}