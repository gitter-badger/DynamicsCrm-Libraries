﻿#region Imports

using System;
using System.Linq;
using System.Runtime.Caching;
using System.ServiceModel;
using System.Text.RegularExpressions;
using LinkDev.Libraries.Common;
using LinkDev.Libraries.EnhancedOrgService.Cache;
using LinkDev.Libraries.EnhancedOrgService.Exceptions;
using LinkDev.Libraries.EnhancedOrgService.Helpers;
using LinkDev.Libraries.EnhancedOrgService.Params;
using LinkDev.Libraries.EnhancedOrgService.Services;
using LinkDev.Libraries.EnhancedOrgService.Transactions;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Client.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

#endregion

namespace LinkDev.Libraries.EnhancedOrgService.Factories
{
	/// <inheritdoc cref="IEnhancedServiceFactory{TEnhancedOrgService}" />
	public class EnhancedServiceFactory<TEnhancedOrgService>
		: IEnhancedServiceFactory<TEnhancedOrgService>
		where TEnhancedOrgService : EnhancedOrgServiceBase
	{
		private readonly EnhancedServiceParams parameters;
		private readonly ObjectCache factoryCache;

		public EnhancedServiceFactory(EnhancedServiceParams parameters)
		{
			this.parameters = parameters;

			if (parameters.IsCachingEnabled)
			{
				switch (parameters.CachingParams.CacheMode)
				{
					case CacheMode.Global:
						factoryCache = MemoryCache.Default;
						break;

					case CacheMode.Private:
						factoryCache = parameters.CachingParams.ObjectCache ?? new MemoryCache(parameters.ConnectionString);
						break;

					case CacheMode.PrivatePerInstance:
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(parameters.CachingParams.CacheMode));
				}
			}

			var isAsyncService = typeof(IAsyncOrgService).IsAssignableFrom(typeof(TEnhancedOrgService));

			if (parameters.IsConcurrencyEnabled && !isAsyncService)
			{
				throw new UnsupportedException("Cannot create an async service factory unless the given service is async.");
			}

			if (!parameters.IsConcurrencyEnabled && isAsyncService)
			{
				throw new UnsupportedException("Cannot create an async service factory unless concurrency is enabled.");
			}
		}

		public virtual TEnhancedOrgService CreateEnhancedService(int threads = 1)
		{
			return CreateEnhancedServiceInternal(true, threads);
		}

		internal virtual TEnhancedOrgService CreateEnhancedServiceInternal(bool isInitialiseCrmServices = true,
			int threads = 1)
		{
			var enhancedService = (TEnhancedOrgService)Activator.CreateInstance(typeof(TEnhancedOrgService), parameters);

			if (parameters.IsTransactionsEnabled)
			{
				enhancedService.TransactionManager = new TransactionManager();
			}

			if (parameters.IsCachingEnabled)
			{
				ObjectCache cache;

				switch (parameters.CachingParams.CacheMode)
				{
					case CacheMode.Global:
					case CacheMode.Private:
						cache = factoryCache;
						break;

					case CacheMode.PrivatePerInstance:
						cache = new MemoryCache(parameters.ConnectionString);
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(parameters.CachingParams.CacheMode));
				}

				var cacheSettings =
					new OrganizationServiceCacheSettings
					{
						PolicyFactory = new CacheItemPolicyFactory(parameters.CachingParams.Offset,
							parameters.CachingParams.SlidingExpiration)
					};

				enhancedService.Cache = new OrganizationServiceCache(cache, cacheSettings);
				enhancedService.ObjectCache = cache;
			}

			if (isInitialiseCrmServices)
			{
				enhancedService.FillServicesQueue(
					Enumerable.Range(0, threads).Select(e => CreateCrmService()));
			}

			return enhancedService;
		}

		public IOrganizationService CreateCrmService()
		{
			return ConnectionHelpers.CreateCrmService(parameters.ConnectionString);
		}

		/// <summary>
		///     Clears the memory cache on the level of the factory and any services created.
		/// </summary>
		public void ClearCache()
		{
			if (!parameters.IsCachingEnabled)
			{
				throw new UnsupportedException("Cannot clear the cache because caching is not enabled.");
			}

			if (factoryCache == null)
			{
				throw new UnsupportedException("Cache is scoped to service instances."
					+ " Use each instance's method to clear the cache instead");
			}

			factoryCache.Clear();
		}
	}
}
