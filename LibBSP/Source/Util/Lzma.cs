using System;
using System.IO;
using SharpCompress.Compressors.LZMA;

namespace LibBSP {

	/// <summary>
	/// Compresses and decompresses LZMA data in the format used by Source engine lumps.
	/// </summary>
	/// <remarks>
	/// Compressed data starts with a 17 byte header: "LZMA", the uncompressed length, the compressed length
	/// (excluding the header) and the 5 LZMA properties bytes.
	/// </remarks>
	public static class Lzma {

		/// <summary>
		/// The length of the header in front of compressed data.
		/// </summary>
		public const int HeaderLength = 17;

		/// <summary>
		/// "LZMA" represented as int32.
		/// </summary>
		private const int Magic = 0x414D5A4C;

		/// <summary>
		/// Checks whether <paramref name="length"/> bytes of <paramref name="data"/> starting at
		/// <paramref name="offset"/> hold complete LZMA compressed data.
		/// </summary>
		/// <param name="data">The data to check.</param>
		/// <param name="offset">The offset the compressed data would start at.</param>
		/// <param name="length">The number of bytes available, or -1 for the rest of <paramref name="data"/>.</param>
		/// <returns>Whether the data is compressed.</returns>
		public static bool IsCompressed(byte[] data, int offset = 0, int length = -1) {
			if (length < 0) {
				length = data.Length - offset;
			}

			if (length < HeaderLength || BitConverter.ToInt32(data, offset) != Magic) {
				return false;
			}

			long compressedLength = BitConverter.ToUInt32(data, offset + 8);
			return HeaderLength + compressedLength <= length;
		}

		/// <summary>
		/// Gets the uncompressed length stored in the header of compressed data.
		/// </summary>
		/// <param name="data">The compressed data.</param>
		/// <param name="offset">The offset the compressed data starts at.</param>
		/// <returns>The length of the data once decompressed.</returns>
		public static int GetUncompressedLength(byte[] data, int offset = 0) {
			return BitConverter.ToInt32(data, offset + 4);
		}

		/// <summary>
		/// Decompresses the LZMA compressed data starting at <paramref name="offset"/> in <paramref name="data"/>.
		/// </summary>
		/// <param name="data">The compressed data.</param>
		/// <param name="offset">The offset the compressed data starts at.</param>
		/// <returns>The decompressed data.</returns>
		/// <exception cref="InvalidDataException">The data isn't compressed or is truncated.</exception>
		public static byte[] Decompress(byte[] data, int offset = 0) {
			if (!IsCompressed(data, offset)) {
				throw new InvalidDataException("Data is not LZMA compressed.");
			}

			int uncompressedLength = GetUncompressedLength(data, offset);
			int compressedLength = BitConverter.ToInt32(data, offset + 8);
			byte[] properties = new byte[5];
			Array.Copy(data, offset + 12, properties, 0, properties.Length);

			byte[] output = new byte[uncompressedLength];
			using (MemoryStream input = new MemoryStream(data, offset + HeaderLength, compressedLength, false))
			using (LzmaStream lzma = new LzmaStream(properties, input, compressedLength, uncompressedLength)) {
				int read = 0;
				while (read < output.Length) {
					int count = lzma.Read(output, read, output.Length - read);
					if (count == 0) {
						throw new InvalidDataException("LZMA compressed data ended early.");
					}
					read += count;
				}
			}

			return output;
		}

		/// <summary>
		/// Compresses <paramref name="data"/>, including the header.
		/// </summary>
		/// <param name="data">The data to compress.</param>
		/// <returns>The compressed data.</returns>
		public static byte[] Compress(byte[] data) {
			using (MemoryStream output = new MemoryStream()) {
				// The header needs the compressed length, so write the data after it first
				output.SetLength(HeaderLength);
				output.Position = HeaderLength;

				byte[] properties;
				// Disposing the encoder flushes it, but leaves the output stream open
				using (LzmaStream lzma = new LzmaStream(new LzmaEncoderProperties(), false, output)) {
					lzma.Write(data, 0, data.Length);
					properties = lzma.Properties;
				}

				byte[] bytes = output.ToArray();
				BitConverter.GetBytes(Magic).CopyTo(bytes, 0);
				BitConverter.GetBytes(data.Length).CopyTo(bytes, 4);
				BitConverter.GetBytes(bytes.Length - HeaderLength).CopyTo(bytes, 8);
				properties.CopyTo(bytes, 12);

				return bytes;
			}
		}

	}
}
