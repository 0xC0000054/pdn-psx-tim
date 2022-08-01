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
using System.IO;

namespace PsxTimFileType
{
    public sealed class TimFileType : FileType
    {
        public TimFileType()
            : base("PSX TIM", new FileTypeOptions { LoadExtensions = new[] { ".tim" } })
        {
        }

        protected override Document OnLoad(Stream input)
        {
            return TimLoad.Load(input);
        }
    }
}