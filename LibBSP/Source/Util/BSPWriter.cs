using System;
using System.IO;

namespace LibBSP {

	/// <summary>
	/// Handles reading of a BSP file.
	/// </summary>
	public class BSPWriter {

		private BSP _bsp;
		private int _numLumps;

		/// <summary>
		/// Constructs a new <see cref="BSPWriter"/> for the given <paramref name="bsp"/>.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> to write.</param>
		public BSPWriter(BSP bsp) {
			_bsp = bsp;
			_numLumps = BSP.GetNumLumps(_bsp.MapType);
		}

		/// <summary>
		/// Writes the <see cref="BSP"/> to the file at <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The file path to write the <see cref="BSP"/> to.</param>
		public void WriteBSP(string path) {
			WriteBSP(path, false);
		}

		/// <summary>
		/// Writes the <see cref="BSP"/> to the file at <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The file path to write the <see cref="BSP"/> to.</param>
		/// <param name="compress">Whether to LZMA compress the lumps. Only Source engine maps can be compressed.</param>
		/// <exception cref="NotSupportedException"><paramref name="compress"/> is set for a map that isn't a Source engine map.</exception>
		public void WriteBSP(string path, bool compress) {
			if (_bsp.MapType.IsSubtypeOf(MapType.Source)) {
				WriteSourceBSP(path, compress);
				return;
			}

			if (compress) {
				throw new NotSupportedException("Only Source engine maps can be compressed.");
			}

			BSPHeader header = _bsp.Header.Regenerate();
			byte[][] lumpBytes = GetLumpsBytes();

			if (File.Exists(path)) {
				File.Delete(path);
			}

			WriteAllData(path, header.Data, lumpBytes);
			_bsp.MapName = Path.GetFileNameWithoutExtension(path);
			_bsp.UpdateHeader(header);
			_bsp.Reader.BspFile = new FileInfo(path);
		}

		/// <summary>
		/// Writes a Source engine <see cref="BSP"/> to the file at <paramref name="path"/>, with every lump aligned to 4 bytes.
		/// </summary>
		/// <param name="path">The file path to write the <see cref="BSP"/> to.</param>
		/// <param name="compress">Whether to LZMA compress the lumps.</param>
		private void WriteSourceBSP(string path, bool compress) {
			int gameLumpIndex = GameLump.GetIndexForLump(_bsp.MapType);
			int pakFileIndex = PakFile.GetIndexForLump(_bsp.MapType);

			LumpInfo[] lumpInfos = new LumpInfo[_numLumps];
			byte[][] lumpBytes = new byte[_numLumps][];
			int offset = BSPHeader.GetSourceHeaderLength(_bsp.MapType);

			// Read every lump before writing, in case the BSP is overwriting the file it was read from
			for (int i = 0; i < _numLumps; ++i) {
				offset = Align(offset);

				LumpInfo info = _bsp[i];
				info.offset = offset;
				info.flags = 0;
				info.ident = 0;
				info.lumpFile = null;

				byte[] bytes;
				if (i == gameLumpIndex) {
					// Game lumps point into the file, so the lump has to be rebuilt at its new offset. It
					// can't be compressed as a whole, each game lump is compressed instead.
					bytes = _bsp.GameLump.GetBytes(compress, offset);
				} else {
					bytes = GetLumpBytes(i);

					// The pakfile is a zip archive, which compresses its files itself
					if (compress && i != pakFileIndex && bytes.Length > 0) {
						byte[] compressed = Lzma.Compress(bytes);
						if (compressed.Length < bytes.Length) {
							info.ident = bytes.Length;
							bytes = compressed;
						}
					}
				}

				info.length = bytes.Length;
				if (bytes.Length == 0) {
					info.offset = 0;
				}

				lumpInfos[i] = info;
				lumpBytes[i] = bytes;
				offset += bytes.Length;
			}

			BSPHeader header = BSPHeader.CreateSourceHeader(_bsp, lumpInfos, _bsp.Header.MapRevision);

			if (File.Exists(path)) {
				File.Delete(path);
			}

			using (FileStream stream = File.OpenWrite(path)) {
				stream.Write(header.Data, 0, header.Data.Length);

				for (int i = 0; i < _numLumps; ++i) {
					if (lumpBytes[i].Length == 0) {
						continue;
					}

					while (stream.Position < lumpInfos[i].offset) {
						stream.WriteByte(0);
					}
					stream.Write(lumpBytes[i], 0, lumpBytes[i].Length);
				}
			}

			_bsp.MapName = Path.GetFileNameWithoutExtension(path);
			_bsp.UpdateHeader(header);
			_bsp.Reader.BspFile = new FileInfo(path);

			for (int i = 0; i < _numLumps; ++i) {
				ILump lump = _bsp.GetLoadedLump(i);
				if (lump != null) {
					lump.LumpInfo = lumpInfos[i];
				}
			}
		}

		/// <summary>
		/// Gets the data from lump <paramref name="index"/>, uncompressed.
		/// </summary>
		/// <param name="index">The index of the lump.</param>
		/// <returns>The lump's data.</returns>
		private byte[] GetLumpBytes(int index) {
			ILump lump = _bsp.GetLoadedLump(index);
			if (lump != null) {
				return lump.GetBytes();
			}

			if (_bsp.Reader.BspFile != null && _bsp.Reader.BspFile.Exists) {
				return _bsp.Reader.ReadLump(_bsp.Header.GetLumpInfo(index));
			}

			return new byte[0];
		}

		/// <summary>
		/// Rounds <paramref name="offset"/> up to a multiple of 4.
		/// </summary>
		/// <param name="offset">The offset to align.</param>
		/// <returns>The aligned offset.</returns>
		private static int Align(int offset) {
			return (offset + 3) & ~3;
		}

		/// <summary>
		/// Gets the data from each lump as byte arrays and returns the result.
		/// </summary>
		/// <returns>Each lump's data as a byte array.</returns>
		private byte[][] GetLumpsBytes() {
			byte[][] lumpBytes = new byte[_numLumps][];
			for (int i = 0; i < _numLumps; i++) {
				ILump lump = _bsp.GetLoadedLump(i);
				byte[] bytes;
				if (lump != null) {
					bytes = lump.GetBytes();
				} else {
					if (_bsp.Reader.BspFile != null && _bsp.Reader.BspFile.Exists) {
						bytes = _bsp.Reader.ReadLump(_bsp.Header.GetLumpInfo(i));
					} else {
						bytes = new byte[0];
					}
				}
				lumpBytes[i] = bytes;
			}

			return lumpBytes;
		}

		/// <summary>
		/// Writes the header data and all the lumps to <paramref name="path"/> sequentially.
		/// </summary>
		/// <param name="path">The path to write the BSP to.</param>
		/// <param name="header">The header data for the BSP.</param>
		/// <param name="lumpBytes">The data for each lump.</param>
		private void WriteAllData(string path, byte[] header, byte[][] lumpBytes) {
			using (FileStream stream = File.OpenWrite(path)) {
				stream.Seek(0, SeekOrigin.Begin);
				stream.Write(header, 0, header.Length);
				int offset = header.Length;

				for (int i = 0; i < _numLumps; ++i) {
					stream.Write(lumpBytes[i], 0, lumpBytes[i].Length);
					offset += lumpBytes[i].Length;
				}
			}
		}

	}
}
