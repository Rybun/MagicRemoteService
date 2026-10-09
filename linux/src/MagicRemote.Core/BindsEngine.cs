namespace MagicRemote.Core;

/// <summary>
/// Resolves what a remote keyCode should do: a TV's configured overrides take precedence, falling
/// back to DefaultBindTable. Kept separate from where a TV's config gets looked up (that's Fase 3's
/// concern, once TV identity is resolved per connection) — this class only knows how to merge one
/// override set with the defaults.
/// </summary>
public sealed class BindsEngine {
	public Bind[]? Resolve(ushort code, IReadOnlyDictionary<ushort, Bind[]>? overrides) {
		if(overrides is not null && overrides.TryGetValue(code, out Bind[]? overridden)) {
			return overridden;
		}
		return DefaultBindTable.Entries.TryGetValue(code, out Bind[]? defaultBind) ? defaultBind : null;
	}
}
