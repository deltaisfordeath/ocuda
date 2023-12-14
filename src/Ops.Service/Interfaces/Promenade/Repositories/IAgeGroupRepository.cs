﻿using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ocuda.Promenade.Models.Entities;

namespace Ocuda.Ops.Service.Interfaces.Promenade.Repositories
{
    public interface IAgeGroupRepository : IGenericRepository<AgeGroup>
    {
        public Task<ICollection<AgeGroup>> GetAllAsync();
    }
}