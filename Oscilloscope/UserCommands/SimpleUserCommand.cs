using Oscilloscope.Contracts;

namespace Oscilloscope.UserCommands;

internal sealed record SimpleUserCommand(
    string Name,
    string Color,
    string FontColor,
    int Length,
    byte[] Send
) : IUserCommand
{
    public Task<Memory<byte>> GetSendDataAsync() => Task.FromResult(new Memory<byte>(Send));

    public Task HandleDataAsync(Memory<byte> data) => Task.CompletedTask;
}
