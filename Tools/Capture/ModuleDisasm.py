"""Disassemble one address in a named client module.

Reader.py always reaches for Gamecode.dll, and the module bases overlap - N3.dll
has its own code at 0x10029C24 too, which is how a trace of n3TeleportIIR_t ends
up reading somebody else's bytes.
"""
import os, sys
import capstone
import MessageIds as mi

NAMED = {}
try:
    import NativeProtocolProbe as npp
    NAMED = getattr(npp, 'IMPORT_NAMES', {})
except Exception:
    pass


def show(module, va, count):
    p = os.path.join(mi.CLIENT, module)
    d = open(p, 'rb').read()
    off = mi._to_offset(p, va)
    if off is None:
        print('address not in', module)
        return
    md = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_32)
    for ins in md.disasm(d[off:off + count * 8], va, count):
        print('  0x%08X  %-9s %s' % (ins.address, ins.mnemonic, ins.op_str))


if __name__ == '__main__':
    show(sys.argv[1], int(sys.argv[2], 0), int(sys.argv[3]) if len(sys.argv) > 3 else 40)
