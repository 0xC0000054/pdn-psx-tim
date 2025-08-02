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
using System.Buffers;
using System.IO;

namespace PsxTimFileType
{
    // Adapted from 'Problem and Solution: The Terrible Inefficiency of FileStream and BinaryReader'
    // https://jacksondunstan.com/articles/3568

    internal sealed class BufferedBinaryReader : Disposable
    {
        private Stream? stream;
        private int readOffset;
        private int readLength;

        private byte[]? buffer;
        private readonly bool leaveOpen;

        private const int MaxBufferSize = 4096;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedBinaryReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        public BufferedBinaryReader(Stream stream) : this(stream, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedBinaryReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="leaveOpen">
        /// <c>true</c> if the stream should be left open when the <see cref="BufferedBinaryReader"/>
        /// is disposed; otherwise, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        public BufferedBinaryReader(Stream stream, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            this.stream = stream;
            int bufferSize = (int)Math.Min(stream.Length, MaxBufferSize);
            buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            this.leaveOpen = leaveOpen;

            readOffset = 0;
            readLength = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (stream != null)
                {
                    if (!leaveOpen)
                    {
                        stream.Dispose();
                    }

                    stream = null;
                }

                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        /// <value>
        /// The length of the stream.
        /// </value>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public long Length
        {
            get
            {
                VerifyNotDisposed();

                return stream!.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position in the stream.
        /// </summary>
        /// <value>
        /// The position in the stream.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">value is negative.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public long Position
        {
            get
            {
                VerifyNotDisposed();

                return stream!.Position - readLength + readOffset;
            }
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
           
                VerifyNotDisposed();

                long current = Position;

                if (value != current)
                {
                    long diff = value - current;

                    long newOffset = readOffset + diff;

                    // Avoid reading from the stream if the offset is within the current buffer.
                    if (newOffset >= 0 && newOffset <= readLength)
                    {
                        readOffset = (int)newOffset;
                    }
                    else
                    {
                        // Invalidate the existing buffer.
                        readOffset = 0;
                        readLength = 0;
                        stream!.Seek(value, SeekOrigin.Begin);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream, starting from a specified point in the byte array.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="offset">The starting offset in the array.</param>
        /// <param name="count">The count.</param>
        /// <returns>The number of bytes read from the stream.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public int Read(byte[] bytes, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(bytes, nameof(bytes));
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
            
            VerifyNotDisposed();

            if (count == 0)
            {
                return 0;
            }

            if ((readOffset + count) <= readLength)
            {
                Buffer.BlockCopy(buffer!, readOffset, bytes, offset, count);
                readOffset += count;

                return count;
            }
            else
            {
                // Ensure that any bytes at the end of the current buffer are included.
                int bytesUnread = readLength - readOffset;

                if (bytesUnread > 0)
                {
                    Buffer.BlockCopy(buffer!, readOffset, bytes, offset, bytesUnread);
                }

                // Invalidate the existing buffer.
                readOffset = 0;
                readLength = 0;

                int totalBytesRead = bytesUnread;

                totalBytesRead += stream!.Read(bytes, offset + bytesUnread, count - bytesUnread);

                return totalBytesRead;
            }
        }

        /// <summary>
        /// Reads the next byte from the current stream.
        /// </summary>
        /// <returns>The next byte read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public byte ReadByte()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(byte));

            byte val = buffer![readOffset];
            readOffset += sizeof(byte);

            return val;
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream.
        /// </summary>
        /// <param name="count">The number of bytes to read..</param>
        /// <returns>An array containing the specified bytes.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public byte[] ReadBytes(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));

            VerifyNotDisposed();

            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[count];

            if ((readOffset + count) <= readLength)
            {
                Buffer.BlockCopy(buffer!, readOffset, bytes, 0, count);
                readOffset += count;
            }
            else
            {
                // Ensure that any bytes at the end of the current buffer are included.
                int bytesUnread = readLength - readOffset;

                if (bytesUnread > 0)
                {
                    Buffer.BlockCopy(buffer!, readOffset, bytes, 0, bytesUnread);
                }

                int numBytesToRead = count - bytesUnread;
                int numBytesRead = bytesUnread;
                do
                {
                    int n = stream!.Read(bytes!, numBytesRead, numBytesToRead);

                    if (n == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    numBytesRead += n;
                    numBytesToRead -= n;

                } while (numBytesToRead > 0);

                // Invalidate the existing buffer.
                readOffset = 0;
                readLength = 0;
            }

            return bytes;
        }

        /// <summary>
        /// Reads a 8-byte floating point value.
        /// </summary>
        /// <returns>The 8-byte floating point value.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public double ReadDouble()
        {
            ulong value = ReadUInt64();

            unsafe
            {
                return *(double*)&value;
            }
        }

        /// <summary>
        /// Reads a 2-byte signed integer.
        /// </summary>
        /// <returns>The 2-byte signed integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public short ReadInt16()
        {
            ushort value = ReadUInt16();

            unsafe
            {
                return *(short*)&value;
            }
        }

        /// <summary>
        /// Reads a 2-byte unsigned integer.
        /// </summary>
        /// <returns>The 2-byte unsigned integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public ushort ReadUInt16()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(ushort));

            ushort val = (ushort)(buffer![readOffset] | (buffer[readOffset + 1] << 8));

            readOffset += sizeof(ushort);

            return val;
        }

        /// <summary>
        /// Reads a 4-byte signed integer.
        /// </summary>
        /// <returns>The 4-byte signed integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public int ReadInt32()
        {
            uint value = ReadUInt32();

            unsafe
            {
                return *(int*)&value;
            }
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer.
        /// </summary>
        /// <returns>The 4-byte unsigned integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public uint ReadUInt32()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(uint));

            uint val = unchecked((uint)(buffer![readOffset]
                                     | (buffer![readOffset + 1] << 8)
                                     | (buffer![readOffset + 2] << 16)
                                     | (buffer![readOffset + 3] << 24)));

            readOffset += sizeof(uint);

            return val;
        }

        /// <summary>
        /// Reads a 4-byte floating point value.
        /// </summary>
        /// <returns>The 4-byte floating point value.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public float ReadSingle()
        {
            uint value = ReadUInt32();

            unsafe
            {
                return *(float*)&value;
            }
        }

        /// <summary>
        /// Reads a 8-byte signed integer.
        /// </summary>
        /// <returns>The 8-byte signed integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public long ReadInt64()
        {
            ulong value = ReadUInt64();

            unsafe
            {
                return *(long*)&value;
            }
        }

        /// <summary>
        /// Reads a 8-byte unsigned integer.
        /// </summary>
        /// <returns>The 8-byte unsigned integer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public ulong ReadUInt64()
        {
            VerifyNotDisposed();

            EnsureBuffer(sizeof(ulong));

            uint lo = unchecked((uint)(buffer![readOffset]
                                    | (buffer![readOffset + 1] << 8)
                                    | (buffer![readOffset + 2] << 16)
                                    | (buffer![readOffset + 3] << 24)));

            uint hi = unchecked((uint)(buffer![readOffset + 4]
                                    | (buffer![readOffset + 5] << 8)
                                    | (buffer![readOffset + 6] << 16)
                                    | (buffer![readOffset + 7] << 24)));

            readOffset += sizeof(ulong);

            return (((ulong)hi) << 32) | lo;
        }

        private void EnsureBuffer(int count)
        {
            if ((readOffset + count) > readLength)
            {
                FillBuffer(count);
            }
        }

        /// <summary>
        /// Fills the buffer with at least the number of bytes requested.
        /// </summary>
        /// <param name="minBytes">The minimum number of bytes to place in the buffer.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minBytes"/> is less than 1.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        private void FillBuffer(int minBytes)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(minBytes, 1, nameof(minBytes));

            int bytesUnread = readLength - readOffset;

            if (bytesUnread > 0)
            {
                Buffer.BlockCopy(buffer!, readOffset, buffer!, 0, bytesUnread);
            }

            int numBytesToRead = buffer!.Length - bytesUnread;
            int numBytesRead = bytesUnread;
            do
            {
                int n = stream!.Read(buffer, numBytesRead, numBytesToRead);

                if (n == 0)
                {
                    throw new EndOfStreamException();
                }

                numBytesRead += n;
                numBytesToRead -= n;
            } while (numBytesRead < minBytes);

            readOffset = 0;
            readLength = numBytesRead;
        }

        private void VerifyNotDisposed()
        {
            ObjectDisposedException.ThrowIf(stream == null, stream);
        }
    }
}
