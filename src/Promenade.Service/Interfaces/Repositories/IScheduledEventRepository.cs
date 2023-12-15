﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocuda.Promenade.Models.Entities;

namespace Ocuda.Promenade.Service.Interfaces.Repositories
{
    public interface IScheduledEventRepository : IGenericRepository<ScheduledEvent>
    {
        public Task<ScheduledEvent> GetAsync(string slug);

        public Task<IEnumerable<ScheduledEvent>> GetUpcomingSummaryAsync(int locationId,
            DateTime asOf,
            int take);
    }
}