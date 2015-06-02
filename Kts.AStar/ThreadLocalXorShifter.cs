using System;
using System.Threading;

namespace Kts.AStar
{
	public static class ThreadLocalXorShifter
	{
		// not sure that I really want to use thread-local storage
		// the only other option, though, is a lock and that would degrade
		// the m ore searches were running in parallel
		// as it stands it adds an extra 2kb to every thread that calls into here (forever)

		private static readonly ThreadLocal<byte[]> _randoms = new ThreadLocal<byte[]>(() => new byte[2048]);
		private static readonly ThreadLocal<int> _spot = new ThreadLocal<int>(() => 2047);
		public static byte NextRandom(int cap)
		{
			var ret = _randoms.Value[_spot.Value++];
			if (_spot.Value >= _randoms.Value.Length)
			{
				_spot.Value = 0;
				FillBufferWithXorShift(_randoms.Value);
			}
			return (byte)(ret % cap);
		}

		private static uint _x = 123456789;
		private static uint _y = 362436069;
		private static uint _z = 521288629;
		private static uint _w = 88675123;
		internal static void FillBufferWithXorShift(byte[] buf)
		{
			// buf should be multiple of 16
			uint x = _x, y = _y, z = _z, w = _w; // copy the state into locals temporarily
			for (int i = 0; i < buf.Length; i+=16)
			{
				uint tx = x ^ (x << 11);
				uint ty = y ^ (y << 11);
				uint tz = z ^ (z << 11);
				uint tw = w ^ (w << 11);
				x = w ^ (w >> 19) ^ (tx ^ (tx >> 8));
				y = x ^ (x >> 19) ^ (ty ^ (ty >> 8));
				z = y ^ (y >> 19) ^ (tz ^ (tz >> 8));
				w = z ^ (z >> 19) ^ (tw ^ (tw >> 8));
				buf[i + 00] = (byte)(x >> 0);
				buf[i + 01] = (byte)(x >> 8);
				buf[i + 02] = (byte)(x >> 16);
				buf[i + 03] = (byte)(x >> 24);
				buf[i + 04] = (byte)(y >> 0);
				buf[i + 05] = (byte)(y >> 8);
				buf[i + 06] = (byte)(y >> 16);
				buf[i + 07] = (byte)(y >> 24);
				buf[i + 08] = (byte)(z >> 0);
				buf[i + 09] = (byte)(z >> 8);
				buf[i + 10] = (byte)(z >> 16);
				buf[i + 11] = (byte)(z >> 24);
				buf[i + 12] = (byte)(w >> 0);
				buf[i + 13] = (byte)(w >> 8);
				buf[i + 14] = (byte)(w >> 16);
				buf[i + 15] = (byte)(w >> 24);
			}
			_x = x; _y = y; _z = z; _w = w;
		}
	}
}