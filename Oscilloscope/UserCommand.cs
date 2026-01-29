namespace Oscilloscope;

internal sealed record UserCommand(string Name, byte[] Send, int Length);
