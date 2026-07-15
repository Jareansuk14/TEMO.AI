#!/usr/bin/env python3
"""Encode/decode strings for LCA VaultCore (XOR with Lca.Url.Vault.2026)."""

import argparse
import re
import sys

KEY = b"Lca.Url.Vault.2026"


def encode(text: str) -> list[int]:
    data = text.encode("utf-8")
    return [data[i] ^ KEY[i % len(KEY)] for i in range(len(data))]


def decode(values: list[int]) -> str:
    data = bytes(v ^ KEY[i % len(KEY)] for i, v in enumerate(values))
    return data.decode("utf-8")


def parse_byte_array(text: str) -> list[int]:
    return [int(x, 16) for x in re.findall(r"0x[0-9A-Fa-f]{2}", text)]


def format_blob_entry(values: list[int], index: int | None) -> str:
    hexes = ", ".join(f"0x{b:02X}" for b in values)
    line = f"new byte[] {{ {hexes} }}"
    if index is not None:
        return f"// Blobs[{index}]  (Vk.V{index})\n        {line},"
    return line


def main() -> int:
    parser = argparse.ArgumentParser(description="LCA vault XOR encode/decode tool")
    sub = parser.add_subparsers(dest="cmd", required=True)

    enc = sub.add_parser("encode", help="Plain text -> C# byte array for VaultCore.Blobs")
    enc.add_argument("text", help="String to encode")
    enc.add_argument(
        "--index",
        type=int,
        help="VaultCore.Blobs index (Vk slot number, e.g. 2 for Vk.V2)",
    )
    enc.add_argument("--name", default="NewVaultEntry", help="Legacy label (optional)")

    dec = sub.add_parser("decode", help="C# byte array or hex list -> plain text")
    dec.add_argument(
        "input",
        help='Byte array snippet, e.g. "0x24, 0x17, 0x15" or paste from VaultCore.cs',
    )

    args = parser.parse_args()

    if args.cmd == "encode":
        values = encode(args.text)
        print(f"Plain: {args.text!r}")
        print(f"Length: {len(values)} bytes")
        if args.index is not None:
            print(f"Slot: Vk.V{args.index} -> VaultCore.Blobs[{args.index}]")
        print()
        print(format_blob_entry(values, args.index))
        print()
        print("Hex list:", ", ".join(f"0x{b:02X}" for b in values))
        return 0

    if args.cmd == "decode":
        values = parse_byte_array(args.input)
        if not values:
            print("No 0x.. bytes found in input.", file=sys.stderr)
            return 1
        print(decode(values))
        return 0

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
