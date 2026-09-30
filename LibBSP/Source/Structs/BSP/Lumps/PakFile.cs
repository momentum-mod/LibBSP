using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.IO;
using System.Linq;

namespace LibBSP {
	
	/// <summary>
	/// Holds the pak file data for a BSP.
	/// </summary>
	public class PakFile : ILump {

		/// <summary>
		/// The <see cref="BSP"/> this <see cref="ILump"/> came from.
		/// </summary>
		public BSP Bsp { get; set; }

		/// <summary>
		/// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="ILump"/>.
		/// </summary>
		public LumpInfo LumpInfo { get; set; }

		/// <summary>
		/// Array of <c>byte</c>s used as the data source for face id info.
		/// </summary>
		public byte[] Data { get; set; }

		/// <summary>
		/// Gets the length of this lump in bytes.
		/// </summary>
		public int Length {
			get {
				return Data.Length;
			}
		}

		/// <summary>
		/// Parses the passed <c>byte</c> array into a <see cref="PakFile"/> object.
		/// </summary>
		/// <param name="data">Array of <c>byte</c>s to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public PakFile(byte[] data, BSP bsp, LumpInfo lumpInfo = default(LumpInfo)) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Data = data;
			Bsp = bsp;
			LumpInfo = lumpInfo;
		}

		/// <summary>
		/// Gets the index for this lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 40;
			}

			return -1;
		}

		/// <summary>
		/// Gets all the data in this lump as a byte array.
		/// </summary>
		/// <returns>The data.</returns>
		public byte[] GetBytes() {
			return Data;
		}

		public ZipArchive GetZipArchive() {
			return ZipArchive.Open(new MemoryStream(Data));
		}

		public void SetZipArchive(ZipArchive zip, bool compress) {
			Data = GetZipBytes(zip, compress);
		}

		/// <summary>
		/// Re-zips every file in the pak file, LZMA compressed or uncompressed.
		/// </summary>
		/// <param name="compress">Whether to LZMA compress the files.</param>
		public void Rezip(bool compress) {
			Data = Rezip(Data, compress);
		}

		/// <summary>
		/// Re-zips every file in pak file data, LZMA compressed or uncompressed.
		/// </summary>
		/// <param name="data">The pak file data.</param>
		/// <param name="compress">Whether to LZMA compress the files.</param>
		/// <returns>The re-zipped data, or <paramref name="data"/> if its files are already compressed that way.</returns>
		public static byte[] Rezip(byte[] data, bool compress) {
			if (data.Length == 0) {
				return data;
			}

			CompressionType compressionType = compress ? CompressionType.LZMA : CompressionType.None;
			using (ZipArchive zip = ZipArchive.Open(new MemoryStream(data))) {
				if (zip.Entries.Where(x => !x.IsDirectory).All(x => x.CompressionType == compressionType)) {
					return data;
				}

				return GetZipBytes(zip, compress);
			}
		}

		/// <summary>
		/// Saves <paramref name="zip"/>, with all its files LZMA compressed or uncompressed.
		/// </summary>
		/// <param name="zip">The zip archive.</param>
		/// <param name="compress">Whether to LZMA compress the files.</param>
		/// <returns>The zip archive's data.</returns>
		private static byte[] GetZipBytes(ZipArchive zip, bool compress) {
			using (var stream = new MemoryStream()) {
				var compressionType = compress ? CompressionType.LZMA : CompressionType.None;
				zip.SaveTo(stream, new SharpCompress.Writers.WriterOptions(compressionType));
				return stream.ToArray();
			}
		}
	}
}
