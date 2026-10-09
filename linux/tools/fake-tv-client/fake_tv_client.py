#!/usr/bin/env python3
"""Simulates the WebOS TV app's WebSocket client, for testing MagicRemote.Daemon without real TV
hardware. Encodes the same binary wire format as main.js (see linux/src/MagicRemote.Protocol).

Usage examples:
    ./fake_tv_client.py --host 127.0.0.1 --port 41230 move --dx 20 --dy -5
    ./fake_tv_client.py move-abs --x 960 --y 540
    ./fake_tv_client.py wheel --delta -3
    ./fake_tv_client.py visible --state on
    ./fake_tv_client.py key --code 403 --down
    ./fake_tv_client.py key --code 403 --up
    ./fake_tv_client.py unicode --char a
    ./fake_tv_client.py shutdown
    ./fake_tv_client.py listen          # connect and print whatever the daemon sends back
"""
import argparse
import asyncio
import struct
import sys

import websockets

CLIENT_MESSAGE_TYPE = {
    "position_relative": 0x00,
    "position_absolute": 0x01,
    "wheel": 0x02,
    "visible": 0x03,
    "key": 0x04,
    "unicode": 0x05,
    "shutdown": 0x06,
}

SERVER_MESSAGE_NAME = {
    0x01: "ConfirmShutdown",
    0x02: "ToggleKeyboard",
}


def encode_position_relative(dx: int, dy: int) -> bytes:
    return struct.pack("<Bhh", CLIENT_MESSAGE_TYPE["position_relative"], dx, dy)


def encode_position_absolute(x: int, y: int) -> bytes:
    return struct.pack("<BHH", CLIENT_MESSAGE_TYPE["position_absolute"], x, y)


def encode_wheel(delta: int) -> bytes:
    return struct.pack("<Bh", CLIENT_MESSAGE_TYPE["wheel"], delta)


def encode_visible(state: bool) -> bytes:
    return struct.pack("<BB", CLIENT_MESSAGE_TYPE["visible"], 1 if state else 0)


def encode_key(code: int, down: bool) -> bytes:
    return struct.pack("<BHB", CLIENT_MESSAGE_TYPE["key"], code, 1 if down else 0)


def encode_unicode(char: str) -> bytes:
    return struct.pack("<BH", CLIENT_MESSAGE_TYPE["unicode"], ord(char))


def encode_shutdown() -> bytes:
    return struct.pack("<B", CLIENT_MESSAGE_TYPE["shutdown"])


async def send_and_maybe_listen(uri: str, payload: bytes, listen_seconds: float) -> None:
    async with websockets.connect(uri) as ws:
        print(f"connected to {uri}, sending 0x{payload.hex()}")
        await ws.send(payload)
        if listen_seconds > 0:
            try:
                async with asyncio.timeout(listen_seconds):
                    async for message in ws:
                        describe_incoming(message)
            except TimeoutError:
                pass


async def listen_only(uri: str) -> None:
    async with websockets.connect(uri) as ws:
        print(f"connected to {uri}, listening (Ctrl+C to stop)")
        async for message in ws:
            describe_incoming(message)


def describe_incoming(message) -> None:
    if isinstance(message, (bytes, bytearray)) and len(message) >= 1:
        name = SERVER_MESSAGE_NAME.get(message[0], f"unknown(0x{message[0]:02x})")
        print(f"<- {name} [0x{message.hex()}]")
    else:
        print(f"<- {message!r}")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=41230)
    parser.add_argument("--listen-after", type=float, default=1.0,
                         help="seconds to wait for a reply after sending (0 to skip)")

    sub = parser.add_subparsers(dest="command", required=True)

    p = sub.add_parser("move", help="PositionRelative")
    p.add_argument("--dx", type=int, required=True)
    p.add_argument("--dy", type=int, required=True)

    p = sub.add_parser("move-abs", help="PositionAbsolute (0-1920 x 0-1080 space)")
    p.add_argument("--x", type=int, required=True)
    p.add_argument("--y", type=int, required=True)

    p = sub.add_parser("wheel", help="Wheel")
    p.add_argument("--delta", type=int, required=True)

    p = sub.add_parser("visible", help="Visible")
    p.add_argument("--state", choices=["on", "off"], required=True)

    p = sub.add_parser("key", help="Key")
    p.add_argument("--code", type=int, required=True)
    group = p.add_mutually_exclusive_group(required=True)
    group.add_argument("--down", action="store_true")
    group.add_argument("--up", action="store_true")

    p = sub.add_parser("unicode", help="Unicode")
    p.add_argument("--char", required=True)

    sub.add_parser("shutdown", help="Shutdown")
    sub.add_parser("listen", help="Just connect and print server->client messages")

    return parser


def main() -> None:
    args = build_parser().parse_args()
    uri = f"ws://{args.host}:{args.port}/"

    if args.command == "listen":
        asyncio.run(listen_only(uri))
        return

    payload = {
        "move": lambda: encode_position_relative(args.dx, args.dy),
        "move-abs": lambda: encode_position_absolute(args.x, args.y),
        "wheel": lambda: encode_wheel(args.delta),
        "visible": lambda: encode_visible(args.state == "on"),
        "key": lambda: encode_key(args.code, args.down),
        "unicode": lambda: encode_unicode(args.char),
        "shutdown": lambda: encode_shutdown(),
    }[args.command]()

    asyncio.run(send_and_maybe_listen(uri, payload, args.listen_after))


if __name__ == "__main__":
    try:
        main()
    except (ConnectionRefusedError, OSError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        sys.exit(1)
