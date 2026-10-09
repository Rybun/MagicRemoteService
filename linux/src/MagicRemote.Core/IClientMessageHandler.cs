using MagicRemote.Protocol;

namespace MagicRemote.Core;

/// <summary>
/// Reacts to a decoded message from the TV app. Implemented by the input-injection layer (Fase 1+);
/// the daemon's transport layer only knows how to decode bytes into <see cref="ClientMessage"/> and
/// dispatch them here.
/// </summary>
public interface IClientMessageHandler {
	Task HandleAsync(ClientMessage message, ITvSession session, CancellationToken cancellationToken);
}

/// <summary>What a message handler can do back to the TV app that sent the message.</summary>
public interface ITvSession {
	string RemoteEndpoint { get; }

	Task SendAsync(ServerMessageType message, CancellationToken cancellationToken);
}
