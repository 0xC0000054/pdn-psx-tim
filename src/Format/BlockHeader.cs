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

namespace PsxTimFileType.Format
{
    internal sealed class BlockHeader
    {
        public BlockHeader(BufferedBinaryReader reader)
        {
            Length = reader.ReadUInt32();
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
        }

        public uint Length { get; }

        public ushort X { get; }

        public ushort Y { get; }

        public ushort Width { get; }

        public ushort Height { get; }
    }
}
