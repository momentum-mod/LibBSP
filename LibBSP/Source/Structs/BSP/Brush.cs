using System;
using System.Collections.Generic;
using System.Reflection;

namespace LibBSP {

	/// <summary>
	/// Holds the data used by the brush structures of all formats of BSP.
	/// </summary>
	public struct Brush : ILumpObject {

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
				return Parent.Bsp.version;
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
		/// Enumerates the <see cref="BrushSide"/>s referenced by this <see cref="Brush"/>.
		/// </summary>
		public IEnumerable<BrushSide> Sides {
			get {
				for (int i = 0; i < NumSides; ++i) {
					yield return Parent.Bsp.brushSides[FirstSideIndex + i];
				}
			}
		}

		/// <summary>
		/// Gets or sets the index of the first side of this <see cref="Brush"/>.
		/// </summary>
		[Index("brushSides")] public int FirstSideIndex {
			get {
				if (MapType == MapType.Nightfire || MapType == MapType.STEF2) {
					return BitConverter.ToInt32(Data, 4);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)
					|| (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD))
					|| (MapType & MapType.Source) == MapType.Source) {
					return BitConverter.ToInt32(Data, 0);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire || MapType == MapType.STEF2) {
					bytes.CopyTo(Data, 4);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)
					|| (MapType.IsSubtypeOf(MapType.Quake3) && !MapType.IsSubtypeOf(MapType.CoD))
					|| (MapType & MapType.Source) == MapType.Source) {
					bytes.CopyTo(Data, 0);
				}
			}
		}

		/// <summary>
		/// Gets or sets the count of sides in this <see cref="Brush"/>.
		/// </summary>
		[Count("brushSides")] public int NumSides {
			get {
				if (MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt16(Data, 0);
				} else if (MapType == MapType.STEF2) {
					return BitConverter.ToInt32(Data, 0);
				} else if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 8);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Quake3)
					|| (MapType & MapType.Source) == MapType.Source) {
					return BitConverter.ToInt32(Data, 4);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType.IsSubtypeOf(MapType.CoD)) {
					Data[0] = bytes[0];
					Data[1] = bytes[1];
				} else if (MapType == MapType.STEF2) {
					bytes.CopyTo(Data, 0);
				} else if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 8);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Quake3)
					|| (MapType & MapType.Source) == MapType.Source) {
					bytes.CopyTo(Data, 4);
				}
			}
		}

		/// <summary>
		/// Gets the <see cref="LibBSP.Texture"/> referenced by this <see cref="Brush"/>. Quake 3 engines only use this for contents.
		/// </summary>
		public Texture Texture {
			get {
				return Parent.Bsp.textures[TextureIndex];
			}
		}

		/// <summary>
		/// Gets or sets the index of the <see cref="LibBSP.Texture"/> used by this <see cref="Brush"/>. Quake 3 engines only use this for contents.
		/// </summary>
		public int TextureIndex {
			get {
				if (MapType.IsSubtypeOf(MapType.CoD)) {
					return BitConverter.ToInt16(Data, 2);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					return BitConverter.ToInt32(Data, 8);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType.IsSubtypeOf(MapType.CoD)) {
					bytes.CopyTo(Data, 2);
				} else if (MapType.IsSubtypeOf(MapType.Quake3)) {
					bytes.CopyTo(Data, 8);
				}
			}
		}

		/// <summary>
		/// Gets or sets the Contents mask for this <see cref="Brush"/>.
		/// </summary>
		public int Contents {
			get {
				if (MapType == MapType.Nightfire) {
					return BitConverter.ToInt32(Data, 0);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					return BitConverter.ToInt32(Data, 8);
				}

				return -1;
			}
			set {
				byte[] bytes = BitConverter.GetBytes(value);

				if (MapType == MapType.Nightfire) {
					bytes.CopyTo(Data, 0);
				} else if (MapType.IsSubtypeOf(MapType.Quake2)
					|| MapType.IsSubtypeOf(MapType.Source)) {
					bytes.CopyTo(Data, 8);
				}
			}
		}

		/// <summary>
		/// Creates a new <see cref="Brush"/> object from a <c>byte</c> array.
		/// </summary>
		/// <param name="data"><c>byte</c> array to parse.</param>
		/// <param name="parent">The <see cref="ILump"/> this <see cref="Brush"/> came from.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		public Brush(byte[] data, ILump parent = null) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Data = data;
			Parent = parent;
		}

		/// <summary>
		/// Creates a new <see cref="Brush"/> by copying the fields in <paramref name="source"/>, using
		/// <paramref name="parent"/> to get <see cref="LibBSP.MapType"/> and <see cref="LumpInfo.version"/>
		/// to use when creating the new <see cref="Brush"/>.
		/// If the <paramref name="parent"/>'s <see cref="BSP"/>'s <see cref="LibBSP.MapType"/> is different from
		/// the one from <paramref name="source"/>, it does not matter, because fields are copied by name.
		/// </summary>
		/// <param name="source">The <see cref="Brush"/> to copy.</param>
		/// <param name="parent">
		/// The <see cref="ILump"/> to use as the <see cref="Parent"/> of the new <see cref="Brush"/>.
		/// Use <c>null</c> to use the <paramref name="source"/>'s <see cref="Parent"/> instead.
		/// </param>
		public Brush(Brush source, ILump parent) {
			Parent = parent;

			if (parent != null && parent.Bsp != null) {
				if (source.Parent != null && source.Parent.Bsp != null && source.Parent.Bsp.version == parent.Bsp.version && source.LumpVersion == parent.LumpInfo.version) {
					Data = new byte[source.Data.Length];
					Array.Copy(source.Data, Data, source.Data.Length);
					return;
				} else {
					Data = new byte[GetStructLength(parent.Bsp.version, parent.LumpInfo.version)];
				}
			} else {
				if (source.Parent != null && source.Parent.Bsp != null) {
					Data = new byte[GetStructLength(source.Parent.Bsp.version, source.Parent.LumpInfo.version)];
				} else {
					Data = new byte[GetStructLength(MapType.Undefined, 0)];
				}
			}

			FirstSideIndex = source.FirstSideIndex;
			NumSides = source.NumSides;
			TextureIndex = source.TextureIndex;
			Contents = source.Contents;
		}

		/// <summary>
		/// Factory method to parse a <c>byte</c> array into a <see cref="Lump{Brush}"/>.
		/// </summary>
		/// <param name="data">The data to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <returns>A <see cref="Lump{Brush}"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
		public static Lump<Brush> LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			return new Lump<Brush>(data, GetStructLength(bsp.version, lumpInfo.version), bsp, lumpInfo);
		}

		/// <summary>
		/// Gets the length of this struct's data for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.
		/// </summary>
		/// <param name="mapType">The <see cref="LibBSP.MapType"/> of the BSP.</param>
		/// <param name="lumpVersion">The version number for the lump.</param>
		/// <returns>The length, in <c>byte</c>s, of this struct.</returns>
		/// <exception cref="ArgumentException">This struct is not valid or is not implemented for the given <paramref name="mapType"/> and <paramref name="lumpVersion"/>.</exception>
		public static int GetStructLength(MapType mapType, int lumpVersion = 0) {
			if (mapType.IsSubtypeOf(MapType.CoD)) {
				return 4;
			} else if (mapType.IsSubtypeOf(MapType.Quake2)
				|| mapType.IsSubtypeOf(MapType.Quake3)
				|| mapType == MapType.Nightfire
				|| mapType.IsSubtypeOf(MapType.Source)) {
				return 12;
			}

			throw new ArgumentException("Lump object " + MethodBase.GetCurrentMethod().DeclaringType.Name + " does not exist in map type " + mapType + " or has not been implemented.");
		}

		/// <summary>
		/// Gets the index for this lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump or it's not implemented.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)) {
				return 18;
			} else if (type.IsSubtypeOf(MapType.Quake2)) {
				return 14;
			} else if (type.IsSubtypeOf(MapType.MOHAA)) {
				return 12;
			} else if (type.IsSubtypeOf(MapType.STEF2)) {
				return 13;
			} else if (type.IsSubtypeOf(MapType.FAKK2)) {
				return 11;
			} else if (type == MapType.Nightfire) {
				return 15;
			} else if (type == MapType.CoD || type == MapType.CoDDemo) {
				return 4;
			} else if (type == MapType.CoD2) {
				return 6;
			} else if (type.IsSubtypeOf(MapType.Quake3)) {
				return 8;
			}

			return -1;
		}

	}
}
