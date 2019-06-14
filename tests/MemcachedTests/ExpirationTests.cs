using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Enyim.Caching.Memcached
{
	public class ExpirationTests
	{
		[Fact]
		public void Empty_Expiration_Should_Never_Expire()
		{
			Assert.True(new Expiration().IsNever);
		}

		[Fact]
		public void TimeSpan_Zero_Should_Never_Expire()
		{
			var e = Expiration.From(TimeSpan.Zero);

			Assert.Equal(0u, e.Value);
			Assert.True(e.IsAbsolute);
			Assert.True(e.IsNever);
		}

		[Fact]
		public void TimeSpan_MaxValue_Should_Never_Expire()
		{
			var e = Expiration.From(TimeSpan.MaxValue);

			Assert.Equal(0u, e.Value);
			Assert.True(e.IsAbsolute);
			Assert.True(e.IsNever);
		}

		[Fact]
		public void TimeSpan_Less_Than_One_Month_Should_Become_Valid_Relative_Expiration()
		{
			var e = Expiration.From(TimeSpan.FromSeconds(100));

			Assert.Equal(100u, e.Value);
			Assert.False(e.IsAbsolute);
			Assert.False(e.IsNever);
		}

		[Fact]
		public void Negative_TimeSpan_Should_Become_Valid_Absolute_Expiration()
		{
			var old = SystemTime.Now;
			SystemTime.Set(() => new DateTime(2011, 12, 31, 23, 0, 0, DateTimeKind.Utc));

			try
			{
				var e = Expiration.From(TimeSpan.FromSeconds(-10));

				Assert.Equal(1325372390u, e.Value);
				Assert.True(e.IsAbsolute);
				Assert.False(e.IsNever);
			}
			finally
			{
				SystemTime.Set(old);
			}
		}


		[Fact]
		public void TimeSpan_Greater_Than_One_Month_Should_Become_Valid_Absolute_Expiration()
		{
			var old = SystemTime.Now;
			SystemTime.Set(() => new DateTime(2011, 12, 31, 23, 0, 0, DateTimeKind.Utc));

			try
			{
				var e = Expiration.From(TimeSpan.FromDays(31));

				Assert.Equal(1328050800u, e.Value);
				Assert.True(e.IsAbsolute);
				Assert.False(e.IsNever);
			}
			finally
			{
				SystemTime.Set(old);
			}
		}

		[Fact]
		public void Unix_Epoch_Should_Fail()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => { var a = (Expiration)new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); });
		}

		[Fact]
		public void DateTime_MinValue_Should_Never_Expire()
		{
			var e = Expiration.From(DateTime.MaxValue);

			Assert.Equal(0u, e.Value);
			Assert.True(e.IsAbsolute);
			Assert.True(e.IsNever);
		}

		[Fact]
		public void DateTime_MaxValue_Should_Never_Expire()
		{
			var e = Expiration.From(DateTime.MaxValue);

			Assert.Equal(0u, e.Value);
			Assert.True(e.IsAbsolute);
			Assert.True(e.IsNever);
		}

		[Fact]
		public void DateTime_Value_Should_Become_Valid_Absolute_Expiration()
		{
			var e = Expiration.From(new DateTime(2012, 01, 31, 23, 0, 0, DateTimeKind.Utc));

			Assert.Equal(1328050800u, e.Value);
			Assert.True(e.IsAbsolute);
			Assert.False(e.IsNever);
		}

		[Fact]
		public void Different_Instances_From_Same_DateTime_Must_Be_Equal()
		{
			var a = Expiration.From(new DateTime(2012, 01, 31, 23, 0, 0, DateTimeKind.Utc));
			var b = (Expiration)(new DateTime(2012, 01, 31, 23, 0, 0, DateTimeKind.Utc));

			Assert.True(a.Equals(a), "a = a");
			Assert.True(b.Equals(b), "b = b");

			Assert.True(a.Equals(b), "a = b");
			Assert.True(b.Equals(a), "b = a");
		}

		[Fact]
		public void Different_Relative_Instances_From_Same_TimeSpan_Must_Be_Equal()
		{
			var a = Expiration.From(TimeSpan.FromSeconds(100));
			var b = (Expiration)(TimeSpan.FromSeconds(100));

			Assert.True(a.Equals(a), "a = a");
			Assert.True(b.Equals(b), "b = b");

			Assert.True(a.Equals(b), "a = b");
			Assert.True(b.Equals(a), "b = a");
		}

		[Fact]
		public void Different_Absolute_Instances_From_Same_TimeSpan_Must_Be_Equal()
		{
			var old = SystemTime.Now;
			SystemTime.Set(() => new DateTime(2011, 12, 31, 23, 0, 0, DateTimeKind.Utc));

			try
			{
				var a = Expiration.From(TimeSpan.FromDays(60));
				var b = (Expiration)(TimeSpan.FromDays(60));

				Assert.True(a.Equals(a), "a = a");
				Assert.True(b.Equals(b), "b = b");

				Assert.True(a.Equals(b), "a = b");
				Assert.True(b.Equals(a), "b = a");
			}
			finally
			{
				SystemTime.Set(old);
			}
		}
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
