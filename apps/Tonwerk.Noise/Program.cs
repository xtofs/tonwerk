// Writes noise-<color>.wav for each color of noise.
//   (default)    time-domain synthesis: filtered white noise (Tonwerk.Synthesis.Noise)
//   --spectral   frequency-domain synthesis: random phases + 1/f^α envelope, inverse FFT

using Tonwerk.Noise;

const int Rate = 44_100;
const double Volume = 0.25;
const double Seconds = 3;

var spectral = args.Contains("--spectral");
var rng = new Random(42);

foreach (var color in Enum.GetValues<NoiseColor>())
{
    var track = spectral
        ? SpectralNoise.Generate(color, Rate, rng)
        : Noise.Generate(color, Seconds, Rate);

    var name = $"noise-{color.ToString().ToLowerInvariant()}.wav";
    using var writer = new WavWriter(name, Rate);
    writer.Write(track, Volume);
    Console.WriteLine($"{name}: {track.Duration.TotalSeconds:F2} s ({(spectral ? "spectral" : "time domain")})");
}
