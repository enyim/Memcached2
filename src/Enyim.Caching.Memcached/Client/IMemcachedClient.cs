using System;
using System.Threading.Tasks;

namespace Enyim.Caching.Memcached
{
	public interface IMemcachedClient
	{
		Task<T> GetAsync<T>(string key);
		Task<T> GetAndTouchAsync<T>(string key, in Expiration expiration);
		Task<bool> StoreAsync(StoreMode mode, string key, object value, Expiration expiration = default);
		Task<bool> DeleteAsync(string key);
		Task<ulong?> MutateAsync(MutationMode mode, string key, ulong delta, ulong defaultValue, Expiration expiration = default);
		Task<bool> ConcatAsync(ConcatenationMode mode, string key, ReadOnlyMemory<byte> data);
		Task<bool> TouchAsync(string key, Expiration expiration = default);

		Task<OperationResult<T>> GetWithResultAsync<T>(string key, ulong cas = Protocol.NO_CAS);
		Task<OperationResult<T>> GetAndTouchWithResultAsync<T>(string key, in Expiration expiration, ulong cas = Protocol.NO_CAS);
		Task<OperationResult> StoreWithResultAsync(StoreMode mode, string key, object value, ulong cas = Protocol.NO_CAS, Expiration expiration = default);
		Task<OperationResult> DeleteWithResultAsync(string key, ulong cas = Protocol.NO_CAS);
		Task<OperationResult<ulong>> MutateWithResultAsync(MutationMode mode, string key, ulong delta, ulong defaultValue, ulong cas = Protocol.NO_CAS, Expiration expiration = default);
		Task<OperationResult> ConcatWithResultAsync(ConcatenationMode mode, string key, ReadOnlyMemory<byte> data, ulong cas = Protocol.NO_CAS);
		Task<OperationResult> TouchWithResultAsync(string key, Expiration expiration = default);

		Task<MemcachedStats> StatsAsync(string type);
		Task<bool> FlushAll(Expiration when = default);
	}
}
