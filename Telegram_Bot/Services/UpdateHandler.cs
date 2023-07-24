using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;
using NAudio.Wave;

using Concentus.Oggfile;
using Concentus.Structs;

namespace Telegram.Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;

    public UpdateHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
        System.Console.WriteLine("UpdateHandler!");
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        switch (update)
        {
            case { Message: { } message }:
                PlayAudio(_, message, cancellationToken);

                break;
            default:
                throw new Exception();
        }

        await Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task BotOnMessageReceived(string destinationFilePath, ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.Voice is { } messageVoice)
        {
            var fileId = messageVoice.FileId;
            var fileInfo = await botClient.GetFileAsync(fileId);
            string? filePath = fileInfo.FilePath;


            if (filePath is not null)
            {
                await using Stream fileStream = System.IO.File.Create(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath,
                    destination: fileStream,
                    cancellationToken: cancellationToken);

            }
        }
    }

    private void PlayAudio(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        const string destinationFilePath = "../voice/downloaded.ogg";
        const string convertedFilePath = "../voice/downloaded1.wav";
        var task = BotOnMessageReceived(destinationFilePath, botClient, message, cancellationToken);
        task.Wait();
        ConvertOggToWav(destinationFilePath, convertedFilePath);

        var outputDevice = new WaveOutEvent() { DeviceNumber = 2 };
        using (var mf = new MediaFoundationReader(convertedFilePath))
        using (outputDevice)
        {
            outputDevice.Init(mf);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }
        }
    }


    public static void ConvertOggToWav(string file, string result)
    {
        var filePath = $@"C:\Users\blabla\foo\bar\";

        using (FileStream fileIn = new FileStream($"{file}", FileMode.Open))
        using (MemoryStream pcmStream = new MemoryStream())
        {
            OpusDecoder decoder = OpusDecoder.Create(48000, 1);
            OpusOggReadStream oggIn = new OpusOggReadStream(decoder, fileIn);
            while (oggIn.HasNextPacket)
            {
                short[] packet = oggIn.DecodeNextPacket();
                if (packet != null)
                {
                    for (int i = 0; i < packet.Length; i++)
                    {
                        var bytes = BitConverter.GetBytes(packet[i]);
                        pcmStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            pcmStream.Position = 0;
            var wavStream = new RawSourceWaveStream(pcmStream, new WaveFormat(48000, 1));
            var sampleProvider = wavStream.ToSampleProvider();
            WaveFileWriter.CreateWaveFile16($"{result}", sampleProvider);
        }
    }
}
