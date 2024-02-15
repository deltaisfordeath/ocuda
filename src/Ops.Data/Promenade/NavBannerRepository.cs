﻿using Microsoft.Extensions.Logging;
using Ocuda.Promenade.Models.Entities;
using Ocuda.Ops.Service.Interfaces.Promenade.Repositories;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Ocuda.Ops.Data.Promenade
{
    public class NavBannerRepository : GenericRepository<PromenadeContext, NavBanner>,
        INavBannerRepository
    {
        public NavBannerRepository(ServiceFacade.Repository<PromenadeContext> repositoryFacade,
            ILogger<NavBannerRepository> logger) : base(repositoryFacade, logger)
        {
        }
        public async Task<NavBanner> GetByIdAsync(int navBannerId)
        {
            return await DbSet
                .AsNoTracking()
                .Where(_ => _.Id == navBannerId)
                .SingleOrDefaultAsync();
        }

    }
}
