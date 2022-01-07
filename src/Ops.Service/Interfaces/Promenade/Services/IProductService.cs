﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Ocuda.Ops.Service.Filters;
using Ocuda.Promenade.Models.Entities;
using Ocuda.Utility.Models;

namespace Ocuda.Ops.Service.Interfaces.Promenade.Services
{
    public interface IProductService
    {
        Task<ICollection<string>> BulkInventoryStatusUpdateAsync(int productId,
            bool addValues,
            IDictionary<int, int> adjustments);

        Task<ICollection<Product>> GetBySegmentIdAsync(int segmentId);

        Task<Product> GetBySlugAsync(string slug);

        Task<ProductLocationInventory> GetInventoryByProductAndLocationAsync(int productId, int locationId);

        Task<ICollection<ProductLocationInventory>> GetLocationInventoriesForProductAsync(int productId);

        Task<CollectionWithCount<Product>> GetPaginatedListAsync(BaseFilter filter);

        Task LinkSegment(int productId, int segmentId);

        Task<IDictionary<int, int>> ParseInventoryAsync(int productId, string filename);

        Task UnlinkSegment(int productId);

        Task UpdateInventoryStatusAsync(int productId, int locationId, int itemCount);

        Task UpdateThreshholdAsync(int productId, int locationId, int threshholdValue);
    }
}
