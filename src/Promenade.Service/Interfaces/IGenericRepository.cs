﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ocuda.Promenade.Service.Interfaces
{
    public interface IGenericRepository<TEntity, TKeyType>
        where TEntity : class
        where TKeyType : struct
    {
        Task<TEntity> FindAsync(TKeyType id);
    }
}
