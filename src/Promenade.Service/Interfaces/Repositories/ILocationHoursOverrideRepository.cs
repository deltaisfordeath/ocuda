﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocuda.Ops.Service.Interfaces.Ops.Repositories;
using Ocuda.Promenade.Models.Entities;

namespace Ocuda.Promenade.Service.Interfaces.Repositories
{
    public interface ILocationHoursOverrideRepository : IRepository<LocationHoursOverride, int>
    {
        Task<LocationHoursOverride> GetByDateAsync(int locationId, DateTime date);

        Task<ICollection<LocationHoursOverride>> GetBetweenDatesAsync(int locationId,
            DateTime startDate,
            DateTime endDate);
    }
}
