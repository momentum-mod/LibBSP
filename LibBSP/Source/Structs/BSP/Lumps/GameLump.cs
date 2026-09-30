using System;
using System.Collections.Generic;

namespace LibBSP {

	/// <summary>
	/// Enum containing known game lumps.
	/// </summary>
	public enum GameLumpType : int {
		/// <summary> LDR detail prop lighting. </summary>
		hlpd = 1685089384,
		/// <summary> HDR detail prop lighting. </summary>
		tlpd = 1685089396,
		/// <summary> Detail props. </summary>
		prpd = 1685090928,
		/// <summary> <see cref="StaticProps"/>. </summary>
		prps = 1936749168,
		/// <summary> Primitive vertex texture info. </summary>
		pmti = 1886221417,
	}

	/// <summary>
	/// Class containing the identification and information for the various Game Lumps in Source
	/// engine BSPs.
	/// </summary>
	public class GameLump : Dictionary<GameLumpType, LumpInfo>, ILump {

		/// <summary>
		/// Flag set on LZMA compressed game lumps.
		/// </summary>
		public const int CompressedFlag = 1;

		private Dictionary<GameLumpType, ILump> _lumps;

		/// <summary>
		/// The length in the file of each compressed game lump. Their <see cref="LumpInfo.length"/> is the uncompressed length.
		/// </summary>
		private Dictionary<GameLumpType, int> _compressedLengths = new Dictionary<GameLumpType, int>();

		/// <summary>
		/// The <see cref="BSP"/> this <see cref="ILump"/> came from.
		/// </summary>
		public BSP Bsp { get; set; }

		/// <summary>
		/// The <see cref="LibBSP.LumpInfo"/> associated with this <see cref="ILump"/>.
		/// </summary>
		public LumpInfo LumpInfo { get; set; }

		/// <summary>
		/// Gets the length of this lump in bytes.
		/// </summary>
		public int Length {
			get {
				if (Count == 0) {
					return 4;
				}

				int lumpInfoLength = (Bsp.MapType == MapType.DMoMaM || Bsp.MapType == MapType.Vindictus) ? 20 : 16;
				int lumpDictionaryOffset = (Bsp.MapType == MapType.DMoMaM) ? 8 : 4;
				int length = lumpDictionaryOffset + (lumpInfoLength * Count);

				foreach (GameLumpType type in Keys) {
					if (_lumps.ContainsKey(type)) {
						length += _lumps[type].Length;
					} else {
						length += this[type].length;
					}
				}

				return length;
			}
		}

		/// <summary>
		/// Parses the passed <c>byte</c> array into a <see cref="GameLump"/> object.
		/// </summary>
		/// <param name="data">Array of <c>byte</c>s to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> was <c>null</c>.</exception>
		/// <exception cref="ArgumentException">This structure is not implemented for the given maptype.</exception>
		public GameLump(byte[] data, BSP bsp, LumpInfo lumpInfo = default(LumpInfo)) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			Bsp = bsp;
			LumpInfo = lumpInfo;

			int structLength = 0;
			if (bsp.MapType == MapType.DMoMaM
				|| bsp.MapType == MapType.Vindictus) {
				structLength = 20;
			} else if (bsp.MapType.IsSubtypeOf(MapType.Source)
				|| bsp.MapType == MapType.Titanfall) {
				structLength = 16;
			} else {
				throw new ArgumentException("Game lump does not exist in map type " + bsp.MapType + " or has not been implemented.");
			}

			if (data.Length < 4) {
				data = new byte[4];
			}

			int numGameLumps = BitConverter.ToInt32(data, 0);
			_lumps = new Dictionary<GameLumpType, ILump>(numGameLumps);

			if (numGameLumps > 0) {
				int lumpDictionaryOffset = (bsp.MapType == MapType.DMoMaM) ? 8 : 4;
				List<LumpInfo> infos = new List<LumpInfo>(numGameLumps);

				for (int i = 0; i < numGameLumps; ++i) {
					int lumpIdent = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset);
					int lumpFlags;
					int lumpVersion;
					int lumpOffset;
					int lumpLength;

					if (bsp.MapType == MapType.Vindictus) {
						lumpFlags = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset + 4);
						lumpVersion = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset + 8);
						lumpOffset = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset + 12);
						lumpLength = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset + 16);
					} else {
						lumpFlags = BitConverter.ToUInt16(data, (i * structLength) + lumpDictionaryOffset + 4);
						lumpVersion = BitConverter.ToUInt16(data, (i * structLength) + lumpDictionaryOffset + 6);
						lumpOffset = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset + 8);
						lumpLength = BitConverter.ToInt32(data, (i * structLength) + lumpDictionaryOffset + 12);
					}

					infos.Add(new LumpInfo {
						ident = lumpIdent,
						flags = lumpFlags,
						version = lumpVersion,
						offset = lumpOffset,
						length = lumpLength,
						lumpFile = lumpInfo.lumpFile,
					});
				}

				for (int i = 0; i < infos.Count; ++i) {
					LumpInfo info = infos[i];
					// Compressed game lumps are followed by an empty entry, which only marks where the last one ends
					if (info.ident == 0) {
						continue;
					}

					this[(GameLumpType)info.ident] = info;
				}

				// The length of a compressed game lump isn't stored, it ends where the next one starts
				int end = GetLowestLumpOffset() < lumpInfo.offset ? data.Length : lumpInfo.offset + data.Length;
				for (int i = 0; i < infos.Count; ++i) {
					LumpInfo info = infos[i];
					if (info.ident == 0 || (info.flags & CompressedFlag) == 0) {
						continue;
					}

					int next = i + 1 < infos.Count && infos[i + 1].offset > info.offset ? infos[i + 1].offset : end;
					_compressedLengths[(GameLumpType)info.ident] = next - info.offset;
				}
			}
		}

		/// <summary>
		/// Factory method to parse a <c>byte</c> array into a <see cref="GameLump"/> object.
		/// </summary>
		/// <param name="data">The data to parse.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <returns>A <see cref="GameLump"/> object.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> parameter was <c>null</c>.</exception>
		public static GameLump LumpFactory(byte[] data, BSP bsp, LumpInfo lumpInfo) {
			if (data == null) {
				throw new ArgumentNullException();
			}

			return new GameLump(data, bsp, lumpInfo);
		}

		/// <summary>
		/// Gets the index for this lump in the BSP file for a specific map format.
		/// </summary>
		/// <param name="type">The map type.</param>
		/// <returns>Index for this lump, or -1 if the format doesn't have this lump.</returns>
		public static int GetIndexForLump(MapType type) {
			if (type.IsSubtypeOf(MapType.Source)
				|| type == MapType.Titanfall) {
				return 35;
			}

			return -1;
		}

		/// <summary>
		/// Gets the lowest offset used for a Game Lump.
		/// </summary>
		/// <returns>The lowest offset used for a Game Lump, or <c>int.MaxValue</c> if no Game Lumps exist.</returns>
		public int GetLowestLumpOffset() {
			int lowest = int.MaxValue;
			foreach (LumpInfo info in Values) {
				if (info.offset < lowest) {
					lowest = info.offset;
				}
			}

			return lowest;
		}

		/// <summary>
		/// The <see cref="LibBSP.StaticProps"/> object in the BSP file extracted from this lump, if available.
		/// </summary>
		public StaticProps StaticProps {
			get {
				GameLumpType type = GameLumpType.prps;

				if (ContainsKey(type)) {
					if (!_lumps.ContainsKey(type)) {
						_lumps.Add(type, StaticProp.LumpFactory(ReadLump(this[type]), Bsp, this[type]));
					}

					return (StaticProps)_lumps[type];
				}

				return null;
			}
			set {
				_lumps[GameLumpType.prps] = value;
				value.Bsp = Bsp;
			}
		}

		/// <summary>
		/// Has the Static Props lump been loaded yet?
		/// </summary>
		public bool StaticPropsLoaded {
			get {
				return LumpLoaded(GameLumpType.prps);
			}
		}
		
		/// <summary>
		/// The <see cref="LibBSP.PrimitiveTextureInfo"/> objects in the BSP file extracted from this lump, if available.
		/// </summary>
		public Lump<PrimitiveTextureInfo> PrimitiveTextureInfo {
			get {
				GameLumpType type = GameLumpType.pmti;

				if (ContainsKey(type)) {
					if (!_lumps.ContainsKey(type)) {
                        _lumps.Add(type, LibBSP.PrimitiveTextureInfo.LumpFactory(ReadLump(this[type]), Bsp, this[type]));
					}

					return (Lump<PrimitiveTextureInfo>)_lumps[type];
				}

				return null;
			}
			set {
				_lumps[GameLumpType.pmti] = value;
				value.Bsp = Bsp;
			}
		}

		/// <summary>
		/// Has the Primitive Texture Info lump been loaded yet?
		/// </summary>
		public bool PrimitiveTextureInfoLoaded {
			get {
				return LumpLoaded(GameLumpType.pmti);
			}
		}

		/// <summary>
		/// Gets all loaded lumps.
		/// </summary>
		public Dictionary<GameLumpType, ILump> Lumps {
			get { return _lumps; }
		}

		/// <summary>
		/// Gets the bytes for a <see cref="LibBSP.LumpInfo"/>, if it exists.
		/// </summary>
		/// <param name="info">The <see cref="LibBSP.LumpInfo"/> to get data for.</param>
		/// <returns>The data for <paramref name="info"/>, decompressed if necessary, or <c>null</c> if it does not exist.</returns>
		public byte[] ReadLump(LumpInfo info) {
			GameLumpType gameLumpType = (GameLumpType)info.ident;
			if (ContainsKey(gameLumpType)) {
				int compressedLength = 0;
				bool compressed = (info.flags & CompressedFlag) != 0 && _compressedLengths.TryGetValue(gameLumpType, out compressedLength);
				if (compressed) {
					info.length = compressedLength;
				}

				// GameLump lumps may have their offset specified from either the beginning of the GameLump, or the beginning of the file.
				if (GetLowestLumpOffset() < LumpInfo.offset) {
					info.offset += LumpInfo.offset;
				}

				byte[] thisLump = Bsp.Reader.ReadLump(info);
				if (compressed) {
					thisLump = Lzma.Decompress(thisLump);
				}

				return thisLump;
			}

			return null;
		}

		/// <summary>
		/// Has lump <paramref name="type"/> been loaded yet?
		/// </summary>
		/// <param name="type">The <see cref="GameLumpType"/> of the lump.</param>
		/// <returns>Whether lump <paramref name="type"/> has been loaded.</returns>
		public bool LumpLoaded(GameLumpType type) {
			return _lumps != null && _lumps.ContainsKey(type);
		}

		/// <summary>
		/// Gets <see cref="ILump"/> <paramref name="type"/> if it is loaded.
		/// </summary>
		/// <param name="type">The <see cref="GameLumpType"/> of the lump.</param>
		/// <returns><see cref="ILump"/> <paramref name="type"/> if it is loaded.</returns>
		public ILump GetLoadedLump(GameLumpType type) {
			if (!LumpLoaded(type)) {
				return null;
			}

			return _lumps[type];
		}

		/// <summary>
		/// Gets all the data in this lump as a byte array, with every game lump uncompressed.
		/// </summary>
		/// <remarks>
		/// Game lump offsets are from the start of the file, so <see cref="LumpInfo"/> has to hold this lump's final offset.
		/// </remarks>
		/// <returns>The data.</returns>
		public byte[] GetBytes() {
			return GetBytes(false, LumpInfo.offset);
		}

		/// <summary>
		/// Gets all the data in this lump as a byte array, for writing it at <paramref name="lumpOffset"/>.
		/// </summary>
		/// <remarks>
		/// Game lump offsets are from the start of the file. Afterwards, they point to where they'll be once this lump is
		/// written at <paramref name="lumpOffset"/>.
		/// </remarks>
		/// <param name="compress">Whether to LZMA compress each game lump.</param>
		/// <param name="lumpOffset">The offset in the file this lump will be written at.</param>
		/// <returns>The data.</returns>
		public byte[] GetBytes(bool compress, int lumpOffset) {
			if (Count == 0) {
				return new byte[] { 0, 0, 0, 0 };
			}

			int lumpInfoLength = (Bsp.MapType == MapType.DMoMaM || Bsp.MapType == MapType.Vindictus) ? 20 : 16;
			int lumpDictionaryOffset = (Bsp.MapType == MapType.DMoMaM) ? 8 : 4;

			// Read everything before updating the offsets, which point into the file being read
			List<GameLumpType> types = new List<GameLumpType>(Keys);
			List<byte[]> lumpBytes = new List<byte[]>(Count);
			List<LumpInfo> infos = new List<LumpInfo>(Count);
			bool anyCompressed = false;
			foreach (GameLumpType type in types) {
				byte[] data = _lumps.ContainsKey(type) ? _lumps[type].GetBytes() : ReadLump(this[type]);

				LumpInfo info = this[type];
				info.length = data.Length;
				info.flags &= ~CompressedFlag;

				if (compress && data.Length > 0) {
					byte[] compressed = Lzma.Compress(data);
					if (compressed.Length < data.Length) {
						data = compressed;
						info.flags |= CompressedFlag;
						anyCompressed = true;
					}
				}

				lumpBytes.Add(data);
				infos.Add(info);
			}

			// The empty entry marks where the last compressed game lump ends
			int numEntries = Count + (anyCompressed ? 1 : 0);
			int length = lumpDictionaryOffset + (lumpInfoLength * numEntries);
			foreach (byte[] data in lumpBytes) {
				length += data.Length;
			}

			byte[] bytes = new byte[length];
			BitConverter.GetBytes(numEntries).CopyTo(bytes, 0);
			int offset = lumpDictionaryOffset + (numEntries * lumpInfoLength);
			_compressedLengths.Clear();

			for (int i = 0; i < types.Count; ++i) {
				LumpInfo info = infos[i];
				info.offset = lumpOffset + offset;
				this[types[i]] = info;
				if ((info.flags & CompressedFlag) != 0) {
					_compressedLengths[types[i]] = lumpBytes[i].Length;
				}

				WriteLumpInfo(bytes, lumpDictionaryOffset + (i * lumpInfoLength), info);
				lumpBytes[i].CopyTo(bytes, offset);
				offset += lumpBytes[i].Length;
			}

			if (anyCompressed) {
				WriteLumpInfo(bytes, lumpDictionaryOffset + (types.Count * lumpInfoLength), new LumpInfo() {
					offset = lumpOffset + offset
				});
			}

			return bytes;
		}

		/// <summary>
		/// Writes <paramref name="info"/> into this lump's directory.
		/// </summary>
		/// <param name="bytes">The lump data.</param>
		/// <param name="offset">The offset of the game lump's entry in the directory.</param>
		/// <param name="info">The game lump's information.</param>
		private void WriteLumpInfo(byte[] bytes, int offset, LumpInfo info) {
			BitConverter.GetBytes(info.ident).CopyTo(bytes, offset);

			if (Bsp.MapType == MapType.Vindictus) {
				BitConverter.GetBytes(info.flags).CopyTo(bytes, offset + 4);
				BitConverter.GetBytes(info.version).CopyTo(bytes, offset + 8);
				BitConverter.GetBytes(info.offset).CopyTo(bytes, offset + 12);
				BitConverter.GetBytes(info.length).CopyTo(bytes, offset + 16);
			} else {
				BitConverter.GetBytes((short)info.flags).CopyTo(bytes, offset + 4);
				BitConverter.GetBytes((short)info.version).CopyTo(bytes, offset + 6);
				BitConverter.GetBytes(info.offset).CopyTo(bytes, offset + 8);
				BitConverter.GetBytes(info.length).CopyTo(bytes, offset + 12);
			}
		}
	}
}
