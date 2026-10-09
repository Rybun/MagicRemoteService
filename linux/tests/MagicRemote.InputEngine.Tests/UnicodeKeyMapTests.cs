using MagicRemote.InputEngine;
using Xunit;

namespace MagicRemote.InputEngine.Tests;

public class UnicodeKeyMapTests {
	[Theory]
	[InlineData('a', EvdevCodes.KEY_A, false)]
	[InlineData('z', EvdevCodes.KEY_Z, false)]
	[InlineData('A', EvdevCodes.KEY_A, true)]
	[InlineData('Z', EvdevCodes.KEY_Z, true)]
	[InlineData('0', EvdevCodes.KEY_0, false)]
	[InlineData('9', EvdevCodes.KEY_9, false)]
	[InlineData(' ', EvdevCodes.KEY_SPACE, false)]
	[InlineData('!', EvdevCodes.KEY_1, true)]
	[InlineData('@', EvdevCodes.KEY_2, true)]
	[InlineData('.', EvdevCodes.KEY_DOT, false)]
	[InlineData('?', EvdevCodes.KEY_SLASH, true)]
	public void Map_ReturnsExpectedCodeAndShift(char c, ushort expectedCode, bool expectedShift) {
		UnicodeKeyMap.Mapping? mapping = UnicodeKeyMap.Map(c);

		Assert.NotNull(mapping);
		Assert.Equal(expectedCode, mapping!.Value.Code);
		Assert.Equal(expectedShift, mapping.Value.Shift);
	}

	[Theory]
	[InlineData('ñ')]
	[InlineData('é')]
	[InlineData('中')]
	public void Map_ReturnsNull_ForUnmappedCharacters(char c) {
		Assert.Null(UnicodeKeyMap.Map(c));
	}

	[Fact]
	public void Map_AllLettersAndDigits_ResolveWithinVirtualKeyboardRange() {
		for(char c = 'a'; c <= 'z'; c++) {
			UnicodeKeyMap.Mapping? mapping = UnicodeKeyMap.Map(c);
			Assert.NotNull(mapping);
			Assert.InRange(mapping!.Value.Code, (ushort)1, (ushort)255);
		}
		for(char c = '0'; c <= '9'; c++) {
			Assert.NotNull(UnicodeKeyMap.Map(c));
		}
	}
}
