#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_5 || UNITY_5_3_OR_NEWER
#define UNITY
#endif

using System;
using System.Reflection;

namespace LibBSP {

	/// <summary>
	/// Holds a single leaf ambient lighting index entry from a Source engine BSP. This corresponds to the
	/// LDR (lump 52) and HDR (lump 51) leaf ambient index lumps. There is one entry per leaf, mapping that
	/// leaf to a run of samples in the corresponding <see cref="LeafAmbientLighting"/> lump.
	/// <para>
	/// Version 0 stores the count and first sample as <c>unsigned short</c>s, while version 1 stores them
	/// as <c>unsigned int</c>s.
	/// </para>
	/// </summary>
	public struct LeafAmbientIndex : ILumpObject {

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
		/// Gets or sets the number of ambient lighting samples referenced by the leaf at this index.
		/// </summary>
		public uint AmbientSampleCount {
			get {
				if (LumpVersion == 0) {
					return BitConverter.ToUInt16(Data, 0);
				}
				return BitConverter.ToUInt32(Data, 0);
			}
			set {
				if (LumpVersion == 0) {
					BitConverter.GetBytes((ushort)value).CopyTo(Data, 0);
				} else {
					BitConverter.GetBytes(value).CopyTo(Data, 0);
				}
			}
		}

		/// <summary>
		/// Gets or sets the index of the first ambient lighting sample referenced by the leaf at this index.
		/// </summary>
		public uint FirstAmbientSample {
			get {
				if (LumpVersion == 0) {
					return BitConverter.ToUInt16(Data, 2);
				}
				return BitConverter.ToUInt32(Data, 4);
			}
			set {
				if (LumpVersion == 0) {
					BitConverter.GetBytes((ushort)value).CopyTo(Data, 2);
				} else {
					BitConverter.GetBytes(value).CopyTo(Data, 4);
				}
			}
		}

		/// <summary>
		/// Creates a new <see cref="LeafAmbientIndex"/> object from a <c>byte</c> array.
		/// </summary>
		/// <param name="data"><c>byte</c> array to parse.</param>
		/// <param name="parent">The <see cref="ILump"/> this <see cref="LeafAmbientIndex"/> came from.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public LeafAmbientIndex(byte[] data, ILump parent = null) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Data = data;
			Parent = parent;
		}

		/// <summary>
		/// Creates a new <see cref="LeafAmbientIndex"/> by copying the fields in <paramref name="source"/>, using
		/// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
		/// to use when creating the new <see cref="LeafAmbientIndex"/>.
		/// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
		/// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
		/// </summary>
		/// <param name="source">The <see cref="LeafAmbientIndex"/> to copy.</param>
		/// <param name="parent">
		/// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="LeafAmbientIndex"/>.
		/// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
		/// </param>
		public LeafAmbientIndex(LeafAmbientIndex source, ILump parent) {
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

			AmbientSampleCount = source.AmbientSampleCount;
			FirstAmbientSample = source.FirstAmbientSample;
		}

		/// <summary>
		/// Factory method to parse a <c>byte</c> array into a <see cref="Lump{LeafAmbientIndex}"/>.
		/// </summary>
		/// <param name="data">The data to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <returns>A <see cref="Lump{LeafAmbientIndex}"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
		public static Lump<LeafAmbientIndex> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			return new Lump<LeafAmbientIndex>(data, GetStructLength(bsp.MapType, lumpInfo.version), bsp, lumpInfo);
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
				// Version 0 stores two unsigned shorts, version 1 stores two unsigned ints.
				if (lumpVersion == 0) {
					return 4;
				}
				return 8;
			}

			throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
		}

		/// <summary>
		/// Gets the index for the LDR leaf ambient index lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 52;
			}

			return -1;
		}

		/// <summary>
		/// Gets the index for the HDR leaf ambient index lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
		public static int GetIndexForHDRLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 51;
			}

			return -1;
		}

	}
}
