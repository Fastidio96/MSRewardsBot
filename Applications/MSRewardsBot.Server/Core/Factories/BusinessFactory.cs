using System;
using Microsoft.Extensions.DependencyInjection;

namespace MSRewardsBot.Server.Core.Factories
{
    public sealed class BusinessFactory
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BusinessFactory(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public ScopedBusiness Create()
        {
            return new ScopedBusiness(_scopeFactory);
        }
    }

    public sealed class ScopedBusiness : IDisposable
    {
        public BusinessLayer Business { get; private set; }

        private readonly IServiceScope _scope;

        public ScopedBusiness(IServiceScopeFactory scopeFactory)
        {
            _scope = scopeFactory.CreateScope();
            Business = _scope.ServiceProvider.GetRequiredService<BusinessLayer>();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
