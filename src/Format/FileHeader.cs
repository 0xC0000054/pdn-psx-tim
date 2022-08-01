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

using System;

namespace PsxTimFileType.Format
{
    internal sealed class FileHeader
    {
        private const uint FileSignature = 0x10;
        private const int HasClutMask = 8;
        private const int ImageTypeMask = 7;

        public FileHeader(BufferedBinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader, nameof(reader));

            uint signature = reader.ReadUInt32();

            if (signature != FileSignature)
            {
                throw new FormatException("The PSX TIM file signature is invalid.");
            }

            uint flags = reader.ReadUInt32();

            HasColorLookupTable = (flags & HasClutMask) != 0;
            ImageType = (ImageType)(flags & ImageTypeMask);
        }

        public bool HasColorLookupTable { get; }

        public ImageType ImageType { get; }
    }
}
