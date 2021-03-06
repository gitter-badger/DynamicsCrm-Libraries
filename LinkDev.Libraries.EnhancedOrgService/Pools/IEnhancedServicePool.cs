﻿#region Imports

using LinkDev.Libraries.EnhancedOrgService.Services;

#endregion

namespace LinkDev.Libraries.EnhancedOrgService.Pools
{
	/// <summary>
	///     Manages a pool of cached Enhanced Org Services.
	///     Releasing the services to the pool is done manually, or through the 'using' keyword.<br />
	///     Author: Ahmed el-Sawalhy
	/// </summary>
	public interface IEnhancedServicePool<TService>
		where TService : IEnhancedOrgService
	{
		/// <summary>
		///     If the pool is empty, creates a new Enhanced Service.
		///     Blocks if the pool exceeds capacity until a service is released.<br />
		///     The threads are the actual CRM services used to communicate with CRM internally.
		///     This is useful for parallel requests when threading is used.
		///     Blocks if the threads are busy until a request has finished.
		/// </summary>
		/// <param name="threads">Number of internal CRM services to create.</param>
		TService GetService(int threads = 1);

		/// <summary>
		///     Puts the service back into the pool for re-use.
		///     The state of the service becomes invalid; so it should not be used after calling this method.
		/// </summary>
		/// <param name="service">Service to release.</param>
		void ReleaseService(TService service);
	}
}
