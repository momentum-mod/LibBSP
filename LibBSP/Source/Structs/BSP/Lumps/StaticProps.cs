using System;
using System.Linq;
using System.Collections.Generic;

namespace LibBSP {

	/// <summary>
	/// List of <see cref="StaticProp"/> objects containing data relevant to Static Props, like the dictionary of actual model paths.
	/// </summary>
	public class StaticProps : Lump<StaticProp> {

		public const int ModelNameLength = 128;

		/// <summary>
		/// Gets or sets the dictionary of prop model names.
		/// </summary>
		public string[] ModelDictionary { get; set; }

		/// <summary>
		/// Gets or sets the lists of leaves each <see cref="StaticProp"/> occupies.
		/// </summary>
		public int[] LeafIndices { get; set; }

		/// <summary>
		/// Gets the length of this lump in bytes.
		/// </summary>
		public override int Length {
			get {
				return 12
				+ (ModelDictionary.Length * ModelNameLength)
				+ (LeafIndices.Length * LeafIndexLength)
				+ (Count > 0 ? Count * this[0].Data.Length : 0);
			}
		}

		/// <summary>
		/// Creates an empty <see cref="StaticProps"/> object.
		/// </summary>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		public StaticProps(BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(bsp, lumpInfo) {
			ModelDictionary = new string[] { };
			LeafIndices = new int[] { };
		}

		/// <summary>
		/// Creates a new <see cref="StaticProps"/> that contains elements copied from the passed <see cref="IEnumerable{StaticProp}"/> and the passed <paramref name="dictionary"/>.
		/// </summary>
		/// <param name="items">The elements to copy into this <c>Lump</c>.</param>
		/// <param name="dictionary">A dictionary of static prop models. This is referenced from <see cref="StaticProp"/> objects.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		public StaticProps(IEnumerable<StaticProp> items, IList<string> dictionary, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(items, bsp, lumpInfo) {
			this.ModelDictionary = dictionary.ToArray();
			LeafIndices = new int[] { };
		}

		/// <summary>
		/// Creates an empty <see cref="StaticProps"/> object with the specified initial capactiy.
		/// </summary>
		/// <param name="capacity">The number of elements that can initially be stored.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		public StaticProps(int capacity, BSP bsp = null, LumpInfo lumpInfo = default(LumpInfo)) : base(capacity, bsp, lumpInfo) {
			ModelDictionary = new string[] { };
			LeafIndices = new int[] { };
		}

		/// <summary>
		/// Parses the passed <c>byte</c> array into a <see cref="StaticProps"/> object.
		/// </summary>
		/// <param name="data">Array of <c>byte</c>s to parse.</param>
		/// <param name="structLength">Number of <c>byte</c>s to copy into the children. Will be recalculated based on BSP format.</param>
		/// <param name="bsp">The <see cref="BSP"/> this lump came from.</param>
		/// <param name="lumpInfo">The <see cref="LumpInfo"/> associated with this lump.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> or <paramref name="bsp"/> was <c>null</c>.</exception>
		public StaticProps(byte[] data, int structLength, BSP bsp, LumpInfo lumpInfo = default(LumpInfo)) : base(bsp, lumpInfo) {
			if (data == null || bsp == null) {
				throw new ArgumentNullException();
			}
			
			if (data.Length > 0) {
				int offset = 0;
				ModelDictionary = new string[BitConverter.ToInt32(data, 0)];
				offset += 4;
				for (int i = 0; i < ModelDictionary.Length; ++i) {
					ModelDictionary[i] = data.ToNullTerminatedString(offset, ModelNameLength);
					offset += ModelNameLength;
				}
				LeafIndices = new int[BitConverter.ToInt32(data, offset)];
				offset += 4;
				int leafIndexLength = GetLeafIndexLength(bsp.MapType, lumpInfo.version);
				for (int i = 0; i < LeafIndices.Length; ++i) {
					LeafIndices[i] = leafIndexLength == 4 ? BitConverter.ToInt32(data, offset) : BitConverter.ToUInt16(data, offset);
					offset += leafIndexLength;
				}
				if (Bsp.MapType == MapType.Vindictus && lumpInfo.version == 6) {
					int numPropScales = BitConverter.ToInt32(data, offset);
					offset += 4 + (numPropScales * 16);
				}
				int numProps = BitConverter.ToInt32(data, offset);
				if (Bsp.MapType == MapType.Titanfall && lumpInfo.version == 12) {
					offset += 12;
				} else {
					offset += 4;
				}
				if (numProps > 0) {
					structLength = (data.Length - offset) / numProps;
					for (int i = 0; i < numProps; ++i) {
						byte[] bytes = new byte[structLength];
						Array.Copy(data, offset, bytes, 0, structLength);
						Add(new StaticProp(bytes, this));
						offset += structLength;
					}
				}
			} else {
				ModelDictionary = new string[0];
				LeafIndices = new int[0];
			}
		}

		/// <summary>
		/// Gets the length of each of the <see cref="LeafIndices"/> in the lump data.
		/// </summary>
		/// <param name="mapType">The map type.</param>
		/// <param name="lumpVersion">The version of the static props lump.</param>
		/// <returns>The length of a leaf index in <c>byte</c>s.</returns>
		public static int GetLeafIndexLength(MapType mapType, int lumpVersion) {
			// Strata Source's version 12 and later
			if (mapType.IsSubtypeOf(MapType.Source) && lumpVersion >= 12) {
				return 4;
			}

			return 2;
		}

		/// <summary>
		/// Gets the length of each of the <see cref="LeafIndices"/> in this lump's data.
		/// </summary>
		private int LeafIndexLength {
			get {
				return Bsp == null ? 2 : GetLeafIndexLength(Bsp.MapType, LumpInfo.version);
			}
		}

		/// <summary>
		/// Gets all the data in this lump as a byte array.
		/// </summary>
		/// <returns>The data.</returns>
		public override byte[] GetBytes() {
			int length = Length;
			int leafIndexLength = LeafIndexLength;

			byte[] bytes = new byte[length];
			int offset = 0;
			BitConverter.GetBytes(ModelDictionary.Length).CopyTo(bytes, offset);
			offset += 4;

			for (int i = 0; i < ModelDictionary.Length; ++i) {
				string name = ModelDictionary[i];
				if (name.Length > ModelNameLength) {
					name = name.Substring(0, ModelNameLength);
				}
				System.Text.Encoding.ASCII.GetBytes(name).CopyTo(bytes, offset);
				offset += ModelNameLength;
			}

			BitConverter.GetBytes(LeafIndices.Length).CopyTo(bytes, offset);
			offset += 4;

			for (int i = 0; i < LeafIndices.Length; ++i) {
				if (leafIndexLength == 4) {
					BitConverter.GetBytes(LeafIndices[i]).CopyTo(bytes, offset);
				} else {
					BitConverter.GetBytes((ushort)LeafIndices[i]).CopyTo(bytes, offset);
				}
				offset += leafIndexLength;
			}

			BitConverter.GetBytes(Count).CopyTo(bytes, offset);
			offset += 4;

			base.GetBytes().CopyTo(bytes, offset);

			return bytes;
		}
	}
}
