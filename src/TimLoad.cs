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
using PsxTimFileType.Format;
using System;
using System.IO;

namespace PsxTimFileType
{
    internal static class TimLoad
    {
        public static Document Load(Stream input)
        {
            Document document;

            using (BufferedBinaryReader reader = new(input, leaveOpen: true))
            {
                FileHeader header = new(reader);

                ColorLookupTable? colorLookupTable;

                if (header.HasColorLookupTable)
                {
                    colorLookupTable = new ColorLookupTable(reader, header.ImageType);
                }
                else
                {
                    colorLookupTable = null;
                    // Assume that indexed images are required to have a color table.
                    if (header.ImageType == ImageType.Indexed4 || header.ImageType == ImageType.Indexed8)
                    {
                        throw new FormatException($"{header.ImageType} image does not have a color table.");
                    }
                }

                Surface? surface = null;
                bool disposeSurface = true;

                try
                {
                    surface = ReadImage(reader, header, colorLookupTable);

                    document = new Document(surface!.Width, surface.Height);
                    document.Layers.Add(Layer.CreateBackgroundLayer(surface, takeOwnership: true));
                    disposeSurface = false;
                }
                finally
                {
                    if (disposeSurface)
                    {
                        surface?.Dispose();
                    }
                }
            }

            return document;
        }

        private static unsafe void DecodeIndexed4ImageData(byte[] source, ColorLookupTable colorLookupTable, BlockHeader imageHeader, Surface destination)
        {
            int width = imageHeader.Width;
            int height = imageHeader.Height;

            fixed (byte* data = source)
            {
                int sourceStride = imageHeader.Width * 2;

                for (int y = 0; y < height; y++)
                {
                    byte* src = data + (y * sourceStride);
                    ColorBgra* dst = destination.GetRowPointerUnchecked(y);

                    for (int x = 0; x < width; x++)
                    {
                        byte byte0 = src[0];
                        byte byte1 = src[1];

                        int index0 = byte0 & 0x0f;
                        int index1 = (byte0 >> 4) & 0x0f;
                        int index2 = byte1 & 0x0f;
                        int index3 = (byte1 >> 4) & 0x0f;

                        dst[0] = colorLookupTable[index0];
                        dst[1] = colorLookupTable[index1];
                        dst[2] = colorLookupTable[index2];
                        dst[3] = colorLookupTable[index3];

                        src += 2;
                        dst += 4;
                    }
                }
            }
        }

        private static unsafe void DecodeIndexed8ImageData(byte[] source, ColorLookupTable colorLookupTable, BlockHeader imageHeader, Surface destination)
        {
            int width = imageHeader.Width;
            int height = imageHeader.Height;

            fixed (byte* data = source)
            {
                int sourceStride = imageHeader.Width * 2;

                for (int y = 0; y < height; y++)
                {
                    byte* src = data + (y * sourceStride);
                    ColorBgra* dst = destination.GetRowPointerUnchecked(y);

                    for (int x = 0; x < width; x++)
                    {
                        byte index0 = src[0];
                        byte index1 = src[1];

                        dst[0] = colorLookupTable[index0];
                        dst[1] = colorLookupTable[index1];

                        src += 2;
                        dst += 2;
                    }
                }
            }
        }

        private static unsafe void DecodeSixteenBitImageData(byte[] source, BlockHeader imageHeader, Surface destination)
        {
            int width = imageHeader.Width;
            int height = imageHeader.Height;

            fixed (byte* data = source)
            {
                int sourceStride = imageHeader.Width * 2;

                for (int y = 0; y < height; y++)
                {
                    byte* src = data + (y * sourceStride);
                    ColorBgra* dst = destination.GetRowPointerUnchecked(y);

                    for (int x = 0; x < width; x++)
                    {
                        ushort packedPixel = (ushort)((src[0] | (src[1] << 8)));

                        *dst = new PsxRgb555Pixel(packedPixel).ToColorBgra();

                        src += 2;
                        dst++;
                    }
                }
            }
        }

        private static unsafe void DecodeTwentyFourBitImageData(byte[] source, BlockHeader imageHeader, Surface destination)
        {
            int width = imageHeader.Width / 2;
            int height = imageHeader.Height;

            fixed (byte* data = source)
            {
                int sourceStride = imageHeader.Width * 2;

                for (int y = 0; y < height; y++)
                {
                    byte* src = data + (y * sourceStride);
                    ColorBgra* dst = destination.GetRowPointerUnchecked(y);

                    for (int x = 0; x < width; x++)
                    {
                        dst[0] = ColorBgra.FromBgr(src[2], src[1], src[0]);
                        dst[1] = ColorBgra.FromBgr(src[5], src[4], src[3]);

                        src += 6;
                        dst += 2;
                    }
                }
            }
        }

        private static Surface ReadImage(BufferedBinaryReader reader, FileHeader header, ColorLookupTable? colorLookupTable)
        {
            BlockHeader imageHeader = new(reader);

            int surfaceHeight = imageHeader.Height;
            var surfaceWidth = header.ImageType switch
            {
                ImageType.Indexed4 => imageHeader.Width * 4,
                ImageType.Indexed8 => imageHeader.Width * 2,
                ImageType.SixteenBit => imageHeader.Width,
                ImageType.TwentryFourBit => imageHeader.Width / 2,
                _ => throw new FormatException($"Unsupported ImageType value: {header.ImageType}."),
            };

            byte[] imageData = reader.ReadBytes(checked(imageHeader.Width * imageHeader.Height * 2));

            Surface surface = new(surfaceWidth, surfaceHeight);

            switch (header.ImageType)
            {
                case ImageType.Indexed4:
                    DecodeIndexed4ImageData(imageData, colorLookupTable!, imageHeader, surface);
                    break;
                case ImageType.Indexed8:
                    DecodeIndexed8ImageData(imageData, colorLookupTable!, imageHeader, surface);
                    break;
                case ImageType.SixteenBit:
                    DecodeSixteenBitImageData(imageData, imageHeader, surface);
                    break;
                case ImageType.TwentryFourBit:
                    DecodeTwentyFourBitImageData(imageData, imageHeader, surface);
                    break;
                default:
                    throw new FormatException($"Unsupported ImageType value: {header.ImageType}.");
            }

            return surface;
        }

    }
}
