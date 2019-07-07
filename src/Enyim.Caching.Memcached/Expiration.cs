using System;

namespace Enyim.Caching.Memcached
{
	public readonly struct Expiration : IEquatable<Expiration>
	{
		private const int MaxSeconds = 60 * 60 * 24 * 30;

		private static readonly DateTime UnixEpochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly DateTime MaxUnixDate = UnixEpochUtc.AddSeconds(UInt32.MaxValue);

		public static readonly Expiration Never = new Expiration(0);

		private readonly bool isAbsolute;

		/// <summary>
		/// Creates a new Expiration instance. See https://github.com/memcached/memcached/wiki/Programming#expiration
		/// </summary>
		/// <param name="value">Expiration expressed in number of seconds. If larger than 30 days (60*60*24*30) it will be interpreted as a UNIX timestamo. 0 value means no expration. It's recommended to use the From static factory methods.</param>
		public Expiration(uint value)
		{
			Value = value;
			isAbsolute = value == 0 || value >= MaxSeconds;
		}

		private Expiration(uint value, bool isAbsolute)
		{
			Value = value;
			this.isAbsolute = isAbsolute;
		}

		public uint Value { get; }
		public bool IsNever => Value == 0 || Value == UInt32.MaxValue;
		public bool IsAbsolute => isAbsolute || IsNever;

		public static implicit operator Expiration(TimeSpan validFor) => From(validFor);
		public static implicit operator Expiration(in DateTime expiresAt) => From(expiresAt);

		public static explicit operator Expiration(uint value) => new Expiration(value);
		public static explicit operator uint(in Expiration value) => value.Value;

		public static bool operator ==(in Expiration a, in Expiration b) => a.Equals(b);
		public static bool operator !=(in Expiration a, in Expiration b) => !a.Equals(b);

		public static Expiration operator +(in Expiration a, in Expiration b) => new Expiration(a.Value + b.Value);
		public static Expiration operator -(in Expiration a, in Expiration b) => new Expiration(Math.Max(0, a.Value - b.Value));

		public static Expiration From(TimeSpan validFor)
		{
			// infinity
			if (validFor == TimeSpan.Zero || validFor == TimeSpan.MaxValue)
				return Never;

			var seconds = validFor.Ticks * 1E-07;

			return seconds < 0 || seconds > MaxSeconds
					? From(SystemTime.Now() + validFor)
					: new Expiration((uint)seconds);
		}

		public static Expiration From(in DateTime expiresAt)
		{
			if (expiresAt == DateTime.MinValue || expiresAt > MaxUnixDate)
				return Never;

			if (expiresAt <= UnixEpochUtc)
				throw new ArgumentOutOfRangeException("expiresAt must be > " + UnixEpochUtc);

			// just in case someboody provides a DateTime less than 30 days away from the Epoch
			return new Expiration((uint)(expiresAt.ToUniversalTime() - UnixEpochUtc).TotalSeconds, true);
		}

		public override bool Equals(object obj) => Equals((Expiration)obj);
		public bool Equals(Expiration other) => Value == other.Value && IsAbsolute == other.IsAbsolute;
		public override int GetHashCode() => HashCode.Combine(Value, IsAbsolute);
	}
}

#region [ License information          ]

/*

Copyright (c) Attila Kiskó, enyim.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*/

#endregion
