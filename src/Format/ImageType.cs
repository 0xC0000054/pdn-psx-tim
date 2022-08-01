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

namespace PsxTimFileType
{
    internal enum ImageType : byte
    {
        Indexed4 = 0,
        Indexed8 = 1,
        SixteenBit = 2,
        TwentryFourBit = 3
    }
}
