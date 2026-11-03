namespace Tonwerk.Theory;

/// <summary>A distance between two pitches in semitones.</summary>
public readonly struct Interval(int semitones)
{
    public readonly int Semitones = semitones;

    public static Pitch operator +(Pitch p, Interval i) => p + i.Semitones;

#pragma warning disable IDE1006 // Naming Styles

    public static Interval P1 { get; } = new(0);
    public static Interval m2 { get; } = new(1);
    public static Interval M2 { get; } = new(2);
    public static Interval m3 { get; } = new(3);
    public static Interval M3 { get; } = new(4);
    public static Interval P4 { get; } = new(5);
    public static Interval TT { get; } = new(6); // tritone
    public static Interval P5 { get; } = new(7);
    public static Interval m6 { get; } = new(8);
    public static Interval M6 { get; } = new(9);
    public static Interval m7 { get; } = new(10);
    public static Interval M7 { get; } = new(11);
    public static Interval P8 { get; } = new(12);

#pragma warning restore IDE1006 // Naming Styles
}
