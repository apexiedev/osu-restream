#region License

//
// The Open Toolkit Library License
//
// Copyright (c) 2006 - 2009 the Open Toolkit library.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights to 
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

#endregion


using System;

namespace osum.Helpers.Audio
{
    /// <summary>Encapsulates a pointer to a decoded sound buffer.</summary>
    public class SoundData
    {
        private byte[] buffer;
        private SoundFormat format;

        #region --- Constructors ---

        /// <internal />
        /// <summary>Constructs a new SoundData object.</summary>
        /// <param name="format">The SoundFormat of these SoundData.</param>
        /// <param name="data">An array of PCM buffer.</param>
        internal SoundData(SoundFormat format, byte[] data)
        {
            if (data == null) throw new ArgumentNullException("buffer", "Must be a valid array of samples.");
            if (data.Length == 0) throw new ArgumentOutOfRangeException("buffer", "Data length must be higher than 0.");

            SoundFormat = format;

            buffer = new byte[data.Length];
            Array.Copy(data, buffer, data.Length);
        }

        #endregion

        /// <summary>Gets the raw PCM buffer.</summary>
        public byte[] Data
        {
            get => buffer;
            internal set => buffer = value;
        }

        /// <summary>Gets the SoundFormat of the SoundData.</summary>
        public SoundFormat SoundFormat
        {
            get => format;
            internal set => format = value;
        }
    }
}