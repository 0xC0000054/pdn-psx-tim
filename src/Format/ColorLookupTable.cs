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
using System;
using System.Diagnostics.CodeAnalysis;

namespace PsxTimFileType.Format
{
    internal sealed class ColorLookupTable
    {
        private readonly ColorBgra[] table;

        public ColorLookupTable(BufferedBinaryReader reader, ImageType imageType)
        {
            int colorTableEntryCount = imageType switch
            {
                ImageType.Indexed4 => 16,
                ImageType.Indexed8 => 256,
                _ => throw new ArgumentException($"Image type must be Indexed4 or Indexed8, actual value: {imageType}.", nameof(imageType)),
            };

            BlockHeader blockHeader = new(reader);

            if (blockHeader.Width != colorTableEntryCount)
            {
                throw new FormatException($"The color table contains {blockHeader.Width} entries, expected {colorTableEntryCount} entries.");
            }

            table = new ColorBgra[colorTableEntryCount];

            for (int i = 0; i < table.Length; i++)
            {
                PsxRgb555Pixel value = new(reader);

                table[i] = value.ToColorBgra();
            }

            if (blockHeader.Height > 1)
            {
                // Skip any color tables after the first one.
                reader.Position += ((long)blockHeader.Width * 2) * (blockHeader.Height - 1);
            }
        }

        public ColorBgra this[int index]
        {
            get
            {
                if ((uint)index >= (uint)table.Length)
                {
                    ThrowColorTableIndexOutOfRange(index);
                }

                return table[index];
            }
        }

        [DoesNotReturn]
        private void ThrowColorTableIndexOutOfRange(int index)
            => throw new ArgumentOutOfRangeException(nameof(index), $"Must be in the range of [0,{table.Length-1}], actual value: {index}.");
    }
}
