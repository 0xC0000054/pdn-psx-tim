////////////////////////////////////////////////////////////////////////
//
// This file is part of pdn-psx-tim, a FileType plugin for Paint.NET
// that adds support for the PSX TIM format.
//
// Copyright (c) 2022 Nicholas Hayes
//
// This file is licensed under the MIT License.
// See LICENSE.txt for complete licensing and attribution information.
//
////////////////////////////////////////////////////////////////////////

using PaintDotNet;

namespace PsxTimFileType.Format
{
    internal readonly struct PsxRgb555Pixel
    {
        private readonly ushort packedValue;

        public PsxRgb555Pixel(BufferedBinaryReader reader)
        {
            packedValue = reader.ReadUInt16();
        }

        public PsxRgb555Pixel(ushort packedValue)
        {
            this.packedValue = packedValue;
        }

        public ColorBgra ToColorBgra()
        {
            byte r = (byte)((packedValue & 0x1f) << 3);
            byte g = (byte)(((packedValue >> 5) & 0x1f) << 3);
            byte b = (byte)(((packedValue >> 10) & 0x1f) << 3);

            // The PSX has multiple transparency modes that use the high bit of the packed value (packedValue & 0x8000).
            // We treat all images as opaque, which matches the behavior of ImageMagick and tim2bmp.
            // TODO: Allow this behavior to be changed if PDN adds support for a FileType load configuration dialog.

            return ColorBgra.FromBgr(b, g, r);
        }
    }
}
