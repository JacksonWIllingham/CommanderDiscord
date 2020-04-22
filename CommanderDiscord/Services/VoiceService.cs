using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Audio.Streams;

namespace CommanderDiscord.Services
{
    public class VoiceService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();
        private ConcurrentDictionary<ulong, AudioInStream> audioInStreams = new ConcurrentDictionary<ulong, AudioInStream>();

        private ConcurrentDictionary<ulong, Process> ffmpegs = new ConcurrentDictionary<ulong, Process>();
        //private ConcurrentDictionary<ulong, CancellationTokenSource> cancelTokens = new ConcurrentDictionary<uint, CancellationTokenSource>();

        public async Task JoinAudio(IGuild guild, IVoiceChannel target)
        {
            IAudioClient client;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                return;
            }
            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            var audioClient = await target.ConnectAsync();

            //target.

            audioClient.StreamCreated += streamCreated;
            audioClient.StreamDestroyed += streamDestroyed;

            if (ConnectedChannels.TryAdd(guild.Id, audioClient))
            {
                // If you add a method to log happenings from this service,
                // you can uncomment these commented lines to make use of that.
                //await Log(LogSeverity.Info, $"Connected to voice on {guild.Name}.");
            }
        }

        private async Task streamCreated(ulong a, AudioInStream ais)
        {
            audioInStreams.TryAdd(a, ais);
            xxx(a, ais);
        }

        private async Task xxx(ulong a, AudioInStream ais)
        {
            try
            {
                //var streamMeUpScotty = (InputStream)ais;
                //var buffer = new byte[4096];
                //while (await streamMeUpScotty.ReadAsync(buffer, 0, buffer.Length) > 0)
                //{
                //    using (var stream = new FileStream(a.ToString(), FileMode.Append))
                //    {
                //        stream.Write(buffer, 0, buffer.Length);
                //    }
                //    //mem.Write(buffer, 0, buffer.Length);
                //}
                CancellationTokenSource source = new CancellationTokenSource();

                var psi = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-ac 2 -f s16le -ar 48000 -i pipe:0 -ac 2 -ar 44100 {a.ToString()+"-"+DateTime.UtcNow.ToString()}.wav",
                    RedirectStandardInput = true
                };

                this.ffmpegs.TryAdd(a, Process.Start(psi));

                while (this.ffmpegs.ContainsKey(a))
                {
                    if (ais.AvailableFrames > 0)
                    {
                        RTPFrame rtpFrame;
                        bool frameRead = ais.TryReadFrame(source.Token, out rtpFrame);
                        //var buff = ais.//ea.PcmData.ToArray();
                        var ffmpeg = this.ffmpegs[a];
                        await ffmpeg.StandardInput.BaseStream.WriteAsync(rtpFrame.Payload, 0, rtpFrame.Payload.Length);
                        await ffmpeg.StandardInput.BaseStream.FlushAsync();
                    }
                }
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                        Console.WriteLine("Unable to compute mean: {0}",
                                          ((TaskCanceledException)e).Message);
                    else
                        Console.WriteLine("Exception: " + e.GetType().Name);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception: " + e.GetType().Name + " : " + e.Message);
            }


        }

        private async Task streamDestroyed(ulong a)
        {
            if (audioInStreams.ContainsKey(a))
            {
                AudioInStream ais = audioInStreams[a];
                xxx(a+1, ais);
                //MemoryStream _stream = new MemoryStream();
                //ais.CopyTo(_stream);


                audioInStreams.TryRemove(a, out ais);
                Process x;
                ffmpegs.TryRemove(a, out x);
            }
        }

        //private async Task 

        public async Task LeaveAudio(IGuild guild)
        {
            IAudioClient client;
            if (ConnectedChannels.TryRemove(guild.Id, out client))
            {
                await client.StopAsync();
                //await Log(LogSeverity.Info, $"Disconnected from voice on {guild.Name}.");
            }
        }

        public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string path)
        {
            // Your task: Get a full path to the file if the value of 'path' is only a filename.
            if (!File.Exists(path))
            {
                await channel.SendMessageAsync("File does not exist.");
                return;
            }
            IAudioClient client;
            if (ConnectedChannels.TryGetValue(guild.Id, out client))
            {
                //await Log(LogSeverity.Debug, $"Starting playback of {path} in {guild.Name}");
                using (var ffmpeg = CreateProcess(path))
                using (var stream = client.CreatePCMStream(AudioApplication.Music))
                {
                    try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); }
                }
            }
        }

        private Process CreateProcess(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
        }
    }
}