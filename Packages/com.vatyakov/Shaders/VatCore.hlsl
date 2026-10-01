#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

// VATyakov core: texel addressing (§1.1). CPU mirror: Runtime/VatMath.cs — keep both in sync.
// Frames, loops and time are computed on the CPU (§1.3); inputs are validated by the baker, not here.

// b = id / W, x = id − b·W, y = b·totalRows + row.
int2 VatTexel(uint element, uint width, uint totalRows, uint row)
{
    uint block = element / width;
    return int2(element - block * width, block * totalRows + row);
}

#endif // VATYAKOV_CORE_INCLUDED
