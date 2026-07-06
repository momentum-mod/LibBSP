using System;

namespace LibBSP {

	/// <summary>
	/// Holds the light grid data for a BSP.
	/// </summary>
	public class LightGrid : ILump {

		/// <summary>
		/// The <see cref="BSP"/> this <see cref="ILump"/> came from.
		/// </summary>
		public BSP Bsp { get; set; }

		/// <summary>
		/// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="ILump"/>.
		/// </summary>
		public LumpInfo LumpInfo { get; set; }

		/// <summary>
		/// Array of <c>byte</c>s used as the data source for light grid info.
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
		/// Parses the passed <c>byte</c> array into a <see cref="LightGrid"/> object.
		/// </summary>
		/// <param name="data">Array of <c>byte</c>s to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public LightGrid(byte[] data, BSP bsp, LumpInfo lumpInfo = default(LumpInfo)) {
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
			if (type.IsSubtypeOf(MapType.Quake3)
				&& !type.IsSubtypeOf(MapType.CoD)
				&& !type.IsSubtypeOf(MapType.UberTools)) {
				return 15;
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

	}
}
