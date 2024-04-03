using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ocuda.Promenade.Data.ServiceFacade;
using Ocuda.Promenade.Models.Entities;
using Ocuda.Promenade.Service.Interfaces.Repositories;
using System.Linq;

namespace Ocuda.Promenade.Data.Promenade
{
    public class ImageAltTextRepository : GenericRepository<PromenadeContext, LocationInteriorImageAltText>,
        IImageAltTextRepository
    {
        public ImageAltTextRepository(Repository<PromenadeContext> repositoryFacade,
            ILogger<ImageAltTextRepository> logger) : base(repositoryFacade, logger)
        {
        }

        public async Task<LocationInteriorImageAltText> GetByImageIdAsync(int imageId, int languageId)
        {
            return await DbSet
                .AsNoTracking()
                .Where(_ => _.LocationInteriorImageId == imageId && _.LanguageId == languageId)
                .FirstOrDefaultAsync();
        }
    }
}
