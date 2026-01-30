using System.Text.Json.Serialization;
using Oscilloscope.UserCommands;

namespace Oscilloscope.Contracts;

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
