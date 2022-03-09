using System;
using System.Collections.Generic;
using System.Text;

namespace LibBSP {

	/// <summary>
	/// <c>List</c>&lt;<see cref="Texture"/>&gt; with some useful methods for manipulating <see cref="Texture"/> objects,
	/// especially when handling them as a group.
	/// </summary>
	public class Textures : Lump<Texture> {

		/// <summary>
		/// Creates an empty <see cref="Textures"/> object.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		public Textures(BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(bsp, lumpInfo) { }

		/// <summary>
		/// Creates a new <see cref="Textures"/> that contains elements copied from the passed <see cref="IEnumerable{Texture}"/>.
		/// </summary>
		/// <param name="items">The elements to copy into this <c>Lump</c>.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		public Textures(IEnumerable<Texture> items, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(items, bsp, lumpInfo) { }

		/// <summary>
		/// Creates an empty <see cref="Textures"/> object with the specified initial capactiy.
		/// </summary>
		/// <param name="capacity">The number of elements that can initially be stored.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		public Textures(int capacity, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(capacity, bsp, lumpInfo) { }

		/// <summary>
		/// Parses the passed <c>byte</c> array into a <see cref="Textures"/> object.
		/// </summary>
		/// <param name="data">Array of <c>byte</c>s to parse.</param>
		/// <param name="structLength">Number of <c>byte</c>s to copy into the children. Will be recalculated based on BSP format.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="bsp"/> was <c>null</c>.</exception>
		public Textures(byte[] data, int structLength, BSP bsp, LumpInfo lumpInfo = default(LumpInfo)) : base(bsp, lumpInfo) {
			if (data == null || bsp == null) {
				throw new ArgumentNullException();
			}

			if (bsp.version.IsSubtypeOf(MapType.Quake)) {
				int numElements = BitConverter.ToInt32(data, 0);
				structLength = 40;
				int currentOffset;
				int width;
				int height;
				int power;
				int mipmapOffset;
				for (int i = 0; i < numElements; ++i) {
					byte[] myBytes = new byte[structLength];
					byte[][] mipmaps = new byte[Texture.NumMipmaps][];
					currentOffset = BitConverter.ToInt32(data, (i + 1) * 4);
					if (currentOffset >= 0) {
						Array.Copy(data, currentOffset, myBytes, 0, structLength);
						width = BitConverter.ToInt32(myBytes, 16);
						height = BitConverter.ToInt32(myBytes, 20);
						power = 1;
						for (int j = 0; j < mipmaps.Length; ++j) {
							mipmapOffset = BitConverter.ToInt32(myBytes, 24 + (4 * j));
							if (mipmapOffset > 0) {
								mipmaps[j] = new byte[(width / power) * (height / power)];
								Array.Copy(data, currentOffset + mipmapOffset, mipmaps[j], 0, mipmaps[j].Length);
							}
							power *= 2;
						}
					}
					Add(new Texture(myBytes, this, mipmaps));
				}
				return;
			} else if (bsp.version.IsSubtypeOf(MapType.Source)) {
				int offset = 0;
				for (int i = 0; i < data.Length; ++i) {
					if (data[i] == (byte)0x00) {
						// They are null-terminated strings, of non-constant length (not padded)
						byte[] myBytes = new byte[i - offset];
						Array.Copy(data, offset, myBytes, 0, i - offset);
						Add(new Texture(myBytes, this));
						offset = i + 1;
					}
				}
				return;
			} else if (bsp.version == MapType.Nightfire) {
				structLength = 64;
			} else if (bsp.version == MapType.SiN) {
				structLength = 180;
			} else if (bsp.version.IsSubtypeOf(MapType.Quake2)
				|| bsp.version.IsSubtypeOf(MapType.STEF2)
				|| bsp.version.IsSubtypeOf(MapType.FAKK2)) {
				structLength = 76;
			} else if (bsp.version.IsSubtypeOf(MapType.MOHAA)) {
				structLength = 140;
			} else if (bsp.version.IsSubtypeOf(MapType.Quake3)) {
				structLength = 72;
			}

			int numObjects = data.Length / structLength;
			for (int i = 0; i < numObjects; ++i) {
				byte[] bytes = new byte[structLength];
				Array.Copy(data, (i * structLength), bytes, 0, structLength);
				Add(new Texture(bytes, this));
			}
		}

		/// <summary>
		/// Gets the name of the texture at the specified offset.
		/// </summary>
		/// <param name="offset">Lump offset of the texture name to find.</param>
		/// <returns>The name of the texture at offset <paramref name="offset" />, or null if it doesn't exist.</returns>
		public string GetTextureAtOffset(uint offset) {
			int current = 0;
			for (int i = 0; i < Count; ++i) {
				if (current < offset) {
					// Add 1 for the missing null byte.
					current += this[i].Name.Length + 1;
				} else {
					return this[i].Name;
				}
			}
			// If we get to this point, the strings ended before target offset was reached
			return null;
		}

		/// <summary>
		/// Finds the offset of the specified texture name.
		/// </summary>
		/// <param name="name">The texture name to find in the lump.</param>
		/// <returns>The offset of the specified texture, or -1 if it wasn't found.</returns>
		public int GetOffsetOf(string name) {
			int offset = 0;
			for (int i = 0; i < Count; ++i) {
				if (this[i].Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)) {
					return offset;
				} else {
					offset += this[i].Name.Length + 1;
				}
			}
			// If we get here, the requested texture didn't exist.
			return -1;
		}

		/// <summary>
		/// Gets all the data in this lump as a byte array.
		/// </summary>
		/// <returns>The data.</returns>
		public override byte[] GetBytes() {
			if (Count == 0) {
				return new byte[0];
			}

			if (Bsp.version.IsSubtypeOf(MapType.Quake2)
				|| Bsp.version.IsSubtypeOf(MapType.Quake3)
				|| Bsp.version == MapType.Nightfire) {
				return base.GetBytes();
			} else if (Bsp.version.IsSubtypeOf(MapType.Source)) {
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < Count; ++i) {
					sb.Append(this[i].Name).Append((char)0x00);
				}
				return Encoding.ASCII.GetBytes(sb.ToString());
			} else if (Bsp.version.IsSubtypeOf(MapType.Quake)) {
				byte[][] textureBytes = new byte[Count][];

				int offset = 0;
				for (int i = 0; i < Count; ++i) {
					Texture texture = this[i];
					if (texture.MipmapFullOffset > 0) {
						offset = 40;
						texture.MipmapFullOffset = offset;
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y());
						texture.MipmapHalfOffset = offset;
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y() / 4);
						texture.MipmapQuarterOffset = offset;
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y() / 16);
						texture.MipmapEighthOffset = offset;
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y() / 64);

						byte[] bytes = new byte[offset];
						offset = 0;
						texture.Data.CopyTo(bytes, 0);
						offset += 40;
						texture.Mipmaps[Texture.FullMipmap].CopyTo(bytes, offset);
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y());
						texture.Mipmaps[Texture.HalfMipmap].CopyTo(bytes, offset);
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y() / 4);
						texture.Mipmaps[Texture.QuarterMipmap].CopyTo(bytes, offset);
						offset += (int)(texture.Dimensions.X() * texture.Dimensions.Y() / 16);
						texture.Mipmaps[Texture.EighthMipmap].CopyTo(bytes, offset);

						textureBytes[i] = bytes;
					} else {
						textureBytes[i] = texture.Data;
					}
				}

				byte[] lumpBytes = new byte[(Count + 1) * 4];
				BitConverter.GetBytes(Count).CopyTo(lumpBytes, 0);
				offset = lumpBytes.Length;
				for (int i = 0; i < Count; ++i) {
					if (this[i].Name.Length == 0 && this[i].MipmapFullOffset == 0) {
						BitConverter.GetBytes(-1).CopyTo(lumpBytes, (i + 1) * 4);
					} else {
						BitConverter.GetBytes(offset).CopyTo(lumpBytes, (i + 1) * 4);
						byte[] newLumpBytes = new byte[offset + textureBytes[i].Length];
						lumpBytes.CopyTo(newLumpBytes, 0);
						textureBytes[i].CopyTo(newLumpBytes, offset);
						offset = newLumpBytes.Length;
						lumpBytes = newLumpBytes;
					}
				}

				return lumpBytes;
			}

			return new byte[0];
		}

	}
}
