using System.IO;
using System.Media;

namespace DeskNotes.Addon.Timer;

internal static class TimerSoundPlayer
{
    private static readonly string WindowsMediaDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "Media");

    public static void Play(TimerSoundProfile profile)
    {
        if (profile == TimerSoundProfile.Off)
            return;

        Task.Run(() =>
        {
            try
            {
                PlayCore(profile);
            }
            catch
            {
                // keep timer completion silent on audio errors
            }
        });
    }

    private static void PlayCore(TimerSoundProfile profile)
    {
        switch (profile)
        {
            case TimerSoundProfile.Soft:
                SystemSounds.Asterisk.Play();
                break;

            case TimerSoundProfile.Classic:
                if (!PlayWindowsMedia("Windows Notify System Generic.wav"))
                    SystemSounds.Asterisk.Play();
                break;

            case TimerSoundProfile.Alarm:
                SystemSounds.Exclamation.Play();
                Thread.Sleep(220);
                SystemSounds.Exclamation.Play();
                break;

            case TimerSoundProfile.Bell:
                if (!PlayWindowsMedia("Windows Notify Calendar.wav")
                    && !PlayWindowsMedia("Windows Notify Messaging.wav"))
                {
                    PlayGeneratedChime();
                }

                break;
        }
    }

    private static bool PlayWindowsMedia(string fileName)
    {
        var path = Path.Combine(WindowsMediaDir, fileName);
        if (!File.Exists(path))
            return false;

        using var player = new SoundPlayer(path);
        player.PlaySync();
        return true;
    }

    private static void PlayGeneratedChime()
    {
        using var stream = new MemoryStream(GenerateChimeWav());
        stream.Position = 0;
        using var player = new SoundPlayer(stream);
        player.PlaySync();
    }

    private static byte[] GenerateChimeWav()
    {
        const int sampleRate = 22050;
        int[] notes = [523, 659, 784];
        var samples = new List<short>();

        foreach (var freq in notes)
        {
            const int noteMs = 180;
            var noteSamples = sampleRate * noteMs / 1000;
            for (var i = 0; i < noteSamples; i++)
            {
                var t = i / (double)sampleRate;
                var envelope = Math.Min(1.0, i / 80.0) * Math.Max(0, 1.0 - (i - noteSamples + 40) / 40.0);
                var value = Math.Sin(2 * Math.PI * freq * t) * envelope * 0.35;
                samples.Add((short)(value * short.MaxValue));
            }

            var gapSamples = sampleRate * 40 / 1000;
            for (var i = 0; i < gapSamples; i++)
                samples.Add(0);
        }

        return BuildWav(samples, sampleRate);
    }

    private static byte[] BuildWav(IReadOnlyList<short> samples, int sampleRate)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = samples.Count * sizeof(short);

        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (var sample in samples)
            writer.Write(sample);

        return stream.ToArray();
    }
}