using System.Text.Json.Serialization;

namespace Oscilloscope;

[JsonDerivedType(typeof(SimpleUserCommand), "Simple")]
internal interface IUserCommand
{
    string Name { get; }

    string Color { get; }

    string FontColor { get; }

    int Length { get; }

    Task<Memory<byte>> GetSendDataAsync();

    Task HandleDataAsync(Memory<byte> data);
}
