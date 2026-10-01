#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Reflection;

namespace LibBSP {
#if UNITY
	using Color = UnityEngine.Color32;
#elif GODOT
	using Color = Godot.Color;
#elif NEOAXIS
	using Color = NeoAxis.ColorByte;
#else
	using Color = System.Drawing.Color;
#endif

	/// <summary>
	/// Holds a single leaf ambient lighting sample from a Source engine BSP. This corresponds to the
	/// LDR (lump 56) and HDR (lump 55) leaf ambient lighting lumps.
	/// <para>
	/// Each sample stores a <c>CompressedLightCube</c>: an ambient light color projected onto each of the
	/// six cardinal axes, where each color is stored as an RGB triple with a shared signed exponent
	/// (<c>ColorRGBExp32</c>). Version 1 samples additionally store a sampling position within the leaf's
	/// bounding box, encoded as a fixed point fraction (0 = mins, 255 = maxs) on each axis.
	/// </para>
	/// </summary>
	public struct LeafAmbientLighting : ILumpObject {

		/// <summary>
		/// The number of cube faces (cardinal axes) stored in the <c>CompressedLightCube</c> of a sample.
		/// </summary>
		public const int NumCubeSides = 6;

		/// <summary>
		/// The <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
		/// </summary>
		public ILump Parent { get; private set; }

		/// <summary>
		/// Array of <c>byte</c>s used as the data source for this <see cref="ILumpObject"/>.
		/// </summary>
		public byte[] Data { get; private set; }

		/// <summary>
		/// The <see cref="LibBSP.MapType"/> to use to interpret <see cref="Data"/>.
		/// </summary>
		public MapType MapType {
			get {
				if (Parent == null || Parent.Bsp == null) {
					return MapType.Undefined;
				}
				return Parent.Bsp.MapType;
			}
		}

		/// <summary>
		/// The version number of the <see cref="ILump"/> this <see cref="ILumpObject"/> came from.
		/// </summary>
		public int LumpVersion {
			get {
				if (Parent == null) {
					return 0;
				}
				return Parent.LumpInfo.version;
			}
		}

		/// <summary>
		/// Gets the color (RGB only) for the given cube side of this sample's <c>CompressedLightCube</c>.
		/// The shared exponent is available through <see cref="GetExponent"/>.
		/// </summary>
		/// <param name="cubeSide">The cube side to get the color for. Must be in the range [0, <see cref="NumCubeSides"/>).</param>
		/// <returns>The RGB color for the given cube side.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="cubeSide"/> was out of range.</exception>
		public Color GetColor(int cubeSide) {
			if (cubeSide < 0 || cubeSide >= NumCubeSides) {
				throw new ArgumentOutOfRangeException("cubeSide");
			}
			int offset = cubeSide * 4;
			return ColorExtensions.FromArgb(255, Data[offset], Data[offset + 1], Data[offset + 2]);
		}

		/// <summary>
		/// Sets the color (RGB only) for the given cube side of this sample's <c>CompressedLightCube</c>.
		/// The shared exponent is left untouched; use <see cref="SetExponent"/> to change it.
		/// </summary>
		/// <param name="cubeSide">The cube side to set the color for. Must be in the range [0, <see cref="NumCubeSides"/>).</param>
		/// <param name="value">The RGB color to set.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="cubeSide"/> was out of range.</exception>
		public void SetColor(int cubeSide, Color value) {
			if (cubeSide < 0 || cubeSide >= NumCubeSides) {
				throw new ArgumentOutOfRangeException("cubeSide");
			}
			int offset = cubeSide * 4;
			byte[] bytes = value.GetBytes();
			Data[offset] = bytes[0];
			Data[offset + 1] = bytes[1];
			Data[offset + 2] = bytes[2];
		}

		/// <summary>
		/// Gets the shared signed exponent for the given cube side of this sample's <c>CompressedLightCube</c>.
		/// </summary>
		/// <param name="cubeSide">The cube side to get the exponent for. Must be in the range [0, <see cref="NumCubeSides"/>).</param>
		/// <returns>The signed exponent for the given cube side.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="cubeSide"/> was out of range.</exception>
		public sbyte GetExponent(int cubeSide) {
			if (cubeSide < 0 || cubeSide >= NumCubeSides) {
				throw new ArgumentOutOfRangeException("cubeSide");
			}
			return (sbyte)Data[(cubeSide * 4) + 3];
		}

		/// <summary>
		/// Sets the shared signed exponent for the given cube side of this sample's <c>CompressedLightCube</c>.
		/// </summary>
		/// <param name="cubeSide">The cube side to set the exponent for. Must be in the range [0, <see cref="NumCubeSides"/>).</param>
		/// <param name="value">The signed exponent to set.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="cubeSide"/> was out of range.</exception>
		public void SetExponent(int cubeSide, sbyte value) {
			if (cubeSide < 0 || cubeSide >= NumCubeSides) {
				throw new ArgumentOutOfRangeException("cubeSide");
			}
			Data[(cubeSide * 4) + 3] = (byte)value;
		}

		/// <summary>
		/// Gets or sets the X coordinate of this sample's position within its leaf's bounding box, encoded
		/// as a fixed point fraction (0 = mins, 255 = maxs). Only present in lump version 1 and greater.
		/// </summary>
		public byte X {
			get {
				if (LumpVersion > 0) {
					return Data[24];
				}
				return 0;
			}
			set {
				if (LumpVersion > 0) {
					Data[24] = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the Y coordinate of this sample's position within its leaf's bounding box, encoded
		/// as a fixed point fraction (0 = mins, 255 = maxs). Only present in lump version 1 and greater.
		/// </summary>
		public byte Y {
			get {
				if (LumpVersion > 0) {
					return Data[25];
				}
				return 0;
			}
			set {
				if (LumpVersion > 0) {
					Data[25] = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the Z coordinate of this sample's position within its leaf's bounding box, encoded
		/// as a fixed point fraction (0 = mins, 255 = maxs). Only present in lump version 1 and greater.
		/// </summary>
		public byte Z {
			get {
				if (LumpVersion > 0) {
					return Data[26];
				}
				return 0;
			}
			set {
				if (LumpVersion > 0) {
					Data[26] = value;
				}
			}
		}

		/// <summary>
		/// Creates a new <see cref="LeafAmbientLighting"/> object from a <c>byte</c> array.
		/// </summary>
		/// <param name="data"><c>byte</c> array to parse.</param>
		/// <param name="parent">The <see cref="ILump"/> this <see cref="LeafAmbientLighting"/> came from.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public LeafAmbientLighting(byte[] data, ILump parent = null) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Data = data;
			Parent = parent;
		}

		/// <summary>
		/// Creates a new <see cref="LeafAmbientLighting"/> by copying the fields in <paramref name="source"/>, using
		/// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
		/// to use when creating the new <see cref="LeafAmbientLighting"/>.
		/// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
		/// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
		/// </summary>
		/// <param name="source">The <see cref="LeafAmbientLighting"/> to copy.</param>
		/// <param name="parent">
		/// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="LeafAmbientLighting"/>.
		/// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
		/// </param>
		public LeafAmbientLighting(LeafAmbientLighting source, ILump parent) {
			Parent = parent;

			if (parent != null && parent.Bsp != null) {
				if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.MapType == parent.Bsp.MapType && source.LumpVersion == parent.LumpInfo.version) {
					Data = new byte[source.Data.Length];
					Array.Copy(source.Data, Data, source.Data.Length);
					return;
				} else {
					Data = new byte[GetStructLength(parent.Bsp.MapType, parent.LumpInfo.version)];
				}
			} else {
				if (source.Parent != null && source.Parent.Bsp != null) {
					Data = new byte[GetStructLength(source.Parent.Bsp.MapType, source.Parent.LumpInfo.version)];
				} else {
					Data = new byte[GetStructLength(MapType.Undefined, 0)];
				}
			}

			for (int i = 0; i < NumCubeSides; ++i) {
				SetColor(i, source.GetColor(i));
				SetExponent(i, source.GetExponent(i));
			}
			X = source.X;
			Y = source.Y;
			Z = source.Z;
		}

		/// <summary>
		/// Factory method to parse a <c>byte</c> array into a <see cref="Lump{LeafAmbientLighting}"/>.
		/// </summary>
		/// <param name="data">The data to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <returns>A <see cref="Lump{LeafAmbientLighting}"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
		public static Lump<LeafAmbientLighting> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			return new Lump<LeafAmbientLighting>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
		}

		/// <summary>
		/// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
		/// </summary>
		/// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
		/// <param name="lumpVersion">The version number for the lump.</param>
		/// <returns>The length, in <c>byte</c>s, of this struct.</returns>
		/// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
		public static int GetStructLength(MapType mapType, int lumpVersion = 0) {
			if (mapType.IsSubtypeOf(MapType.Source)) {
				// Version 1 adds a byte position (x, y, z) plus a padding byte to the CompressedLightCube (6 * ColorRGBExp32).
				// Any other version, including garbage versions in some older maps, is read as a CompressedLightCube per leaf.
				if (lumpVersion == 1) {
					return 28;
				}
				return 24;
			}

			throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
		}

		/// <summary>
		/// Gets the index for the LDR leaf ambient lighting lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 56;
			}

			return -1;
		}

		/// <summary>
		/// Gets the index for the HDR leaf ambient lighting lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
		public static int GetIndexForHDRLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 55;
			}

			return -1;
		}

	}
}
