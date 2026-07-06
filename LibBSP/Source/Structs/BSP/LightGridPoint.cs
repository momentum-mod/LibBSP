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
	/// Holds all data for a single point in the light grid (lump 15) of a Quake 3 BSP.
	/// Each point stores a baked ambient and directional lighting sample along with the
	/// direction the directional component points in, encoded as a latitude/longitude pair.
	/// </summary>
	public struct LightGridPoint : ILumpObject {

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
		/// Gets or sets the ambient light color for this <see cref="LightGridPoint"/>.
		/// </summary>
		public Color Ambient {
			get {
				return ColorExtensions.FromArgb(255, Data[0], Data[1], Data[2]);
			}
			set {
				byte[] bytes = value.GetBytes();
				Data[0] = bytes[0];
				Data[1] = bytes[1];
				Data[2] = bytes[2];
			}
		}

		/// <summary>
		/// Gets or sets the directional light color for this <see cref="LightGridPoint"/>.
		/// </summary>
		public Color Directed {
			get {
				return ColorExtensions.FromArgb(255, Data[3], Data[4], Data[5]);
			}
			set {
				byte[] bytes = value.GetBytes();
				Data[3] = bytes[0];
				Data[4] = bytes[1];
				Data[5] = bytes[2];
			}
		}

		/// <summary>
		/// Gets or sets the longitude byte of the encoded direction the <see cref="Directed"/> light points in.
		/// This is <c>latLong[0]</c> in the Quake 3 grid point structure.
		/// </summary>
		public byte Longitude {
			get {
				return Data[6];
			}
			set {
				Data[6] = value;
			}
		}

		/// <summary>
		/// Gets or sets the latitude byte of the encoded direction the <see cref="Directed"/> light points in.
		/// This is <c>latLong[1]</c> in the Quake 3 grid point structure.
		/// </summary>
		public byte Latitude {
			get {
				return Data[7];
			}
			set {
				Data[7] = value;
			}
		}

		/// <summary>
		/// Creates a new <see cref="LightGridPoint"/> object from a <c>byte</c> array.
		/// </summary>
		/// <param name="data"><c>byte</c> array to parse.</param>
		/// <param name="parent">The <see cref="ILump"/> this <see cref="LightGridPoint"/> came from.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public LightGridPoint(byte[] data, ILump parent = null) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Data = data;
			Parent = parent;
		}

		/// <summary>
		/// Creates a new <see cref="LightGridPoint"/> by copying the fields in <paramref name="source"/>, using
		/// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
		/// to use when creating the new <see cref="LightGridPoint"/>.
		/// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
		/// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
		/// </summary>
		/// <param name="source">The <see cref="LightGridPoint"/> to copy.</param>
		/// <param name="parent">
		/// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="LightGridPoint"/>.
		/// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
		/// </param>
		public LightGridPoint(LightGridPoint source, ILump parent) {
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

			Ambient = source.Ambient;
			Directed = source.Directed;
			Latitude = source.Latitude;
			Longitude = source.Longitude;
		}

		/// <summary>
		/// Factory method to parse a <c>byte</c> array into a <see cref="Lump{LightGridPoint}"/>.
		/// </summary>
		/// <param name="data">The data to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <returns>A <see cref="Lump{LightGridPoint}"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
		public static Lump<LightGridPoint> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			return new Lump<LightGridPoint>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
		}

		/// <summary>
		/// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
		/// </summary>
		/// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
		/// <param name="lumpVersion">The version number for the lump.</param>
		/// <returns>The length, in <c>byte</c>s, of this struct.</returns>
		/// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
		public static int GetStructLength(MapType mapType, int lumpVersion = 0) {
			if (mapType == MapType.Quake3
				|| mapType == MapType.ET) {
				return 8;
			}

			throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
		}

		/// <summary>
		/// Gets the index for this lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type == MapType.Quake3
				|| type == MapType.ET) {
				return 15;
			}

			return -1;
		}

	}
}
