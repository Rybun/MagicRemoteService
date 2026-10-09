using MagicRemote.Core;
using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.Core.Tests;

public class BindsEngineTests {
	[Fact]
	public void Resolve_FallsBackToDefaultTable_WhenNoOverrides() {
		BindsEngine engine = new();

		Bind[]? binds = engine.Resolve(0x0025 /* left arrow */, overrides: null);

		Bind.Keyboard keyboard = Assert.IsType<Bind.Keyboard>(Assert.Single(binds!));
		Assert.Equal([EvdevCodes.KEY_LEFT], keyboard.Codes);
	}

	[Fact]
	public void Resolve_PrefersOverrideOverDefault() {
		BindsEngine engine = new();
		Dictionary<ushort, Bind[]> overrides = new() {
			[0x0025] = [new Bind.Mouse(MouseButton.Middle)]
		};

		Bind[]? binds = engine.Resolve(0x0025, overrides);

		Bind.Mouse mouse = Assert.IsType<Bind.Mouse>(Assert.Single(binds!));
		Assert.Equal(MouseButton.Middle, mouse.Button);
	}

	[Fact]
	public void Resolve_ReturnsNull_ForUnboundCode() {
		BindsEngine engine = new();

		Bind[]? binds = engine.Resolve(0xFFFF, overrides: null);

		Assert.Null(binds);
	}

	[Fact]
	public void Resolve_FallsBackToDefault_WhenOverridesPresentButCodeNotOverridden() {
		BindsEngine engine = new();
		Dictionary<ushort, Bind[]> overrides = new() {
			[0x0030] = [new Bind.Mouse(MouseButton.Middle)]
		};

		Bind[]? binds = engine.Resolve(0x0025 /* left arrow, not overridden */, overrides);

		Bind.Keyboard keyboard = Assert.IsType<Bind.Keyboard>(Assert.Single(binds!));
		Assert.Equal([EvdevCodes.KEY_LEFT], keyboard.Codes);
	}
}
