using Seralyth.Managers.DiscordRPC.RPC.Payload;

namespace Seralyth.Managers.DiscordRPC.RPC.Commands
{
    internal interface ICommand
    {
        IPayload PreparePayload(long nonce);
    }
}
