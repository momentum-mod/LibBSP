using System;

namespace LibBSP {

	/// <summary>
	/// Level-wide feature flags in a Source engine BSP (<c>LVLFLAGS_*</c>).
	/// </summary>
	[Flags]
	public enum LevelFlags : uint {
		None = 0,
		BakedStaticPropLightingNonHDR = 0x00000001,
		BakedStaticPropLightingHDR = 0x00000002,
		LightmapAlpha = 0x00000004,
		BakedStaticPropLighting3 = 0x00000008,
		LightmapAlpha3 = 0x00000010,
		BakedStaticPropLighting3NoSun = 0x00000020,
		LightstylesWithCSM = 0x00000040,
		/// <summary>
		/// Momentum Mod: LDR lightmaps are encoded exactly instead of in 1/1024 steps of linear light.
		/// </summary>
		ExactLDRLightmaps = 0x00000080,
		DisableBakedAO = 0x40000000,
		LegacyPropBehavior = 0x80000000,
	}

	/// <summary>
	/// Holds the level flags of a Source engine BSP (<c>dflagslump_t</c>).
	/// </summary>
	public class MapFlags : ILump {

		private bool _hadData;

		/// <summary>
		/// The <see cref="BSP"/> this <see cref="ILump"/> came from.
		/// </summary>
		public BSP Bsp { get; set; }

		/// <summary>
		/// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="ILump"/>.
		/// </summary>
		public LumpInfo LumpInfo { get; set; }

		/// <summary>
		/// The level's flags.
		/// </summary>
		public LevelFlags Flags { get; set; }

		/// <summary>
		/// Gets the length of this lump in bytes. A BSP without the lump and without flags keeps it out.
		/// </summary>
		public int Length {
			get {
				return (_hadData || Flags != LevelFlags.None) ? 4 : 0;
			}
		}

		/// <summary>
		/// Parses the passed <c>byte</c> array into a <see cref="MapFlags"/> object.
		/// </summary>
		/// <param name="data">Array of <c>byte</c>s to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public MapFlags(byte[] data, BSP bsp, LumpInfo lumpInfo = default(LumpInfo)) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			if (data.Length >= 4) {
				Flags = (LevelFlags)BitConverter.ToUInt32(data, 0);
				_hadData = true;
			}

			Bsp = bsp;
			LumpInfo = lumpInfo;
		}

		/// <summary>
		/// Gets the index for this lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)
				&& type != MapType.Source17
				&& type != MapType.Source18
				&& type != MapType.Source19) {
				return 59;
			}

			return -1;
		}

		/// <summary>
		/// Gets all the data in this lump as a byte array.
		/// </summary>
		/// <returns>The data.</returns>
		public byte[] GetBytes() {
			if (Length == 0) {
				return new byte[0];
			}

			return BitConverter.GetBytes((uint)Flags);
		}

	}
}
