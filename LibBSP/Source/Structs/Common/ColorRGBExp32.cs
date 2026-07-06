namespace LibBSP {

	/// <summary>
	/// Holds an RGB color with an exponent, as used by the Source engine.
	/// The linear value of each channel is (channel * 2^exponent).
	/// </summary>
	public struct ColorRGBExp32 {

		public byte r;
		public byte g;
		public byte b;
		public sbyte exponent;

		/// <summary>
		/// Creates a new <see cref="ColorRGBExp32"/> using the passed data.
		/// </summary>
		/// <param name="r">The red channel.</param>
		/// <param name="g">The green channel.</param>
		/// <param name="b">The blue channel.</param>
		/// <param name="exponent">The exponent.</param>
		public ColorRGBExp32(byte r, byte g, byte b, sbyte exponent) {
			this.r = r;
			this.g = g;
			this.b = b;
			this.exponent = exponent;
		}

	}
}
