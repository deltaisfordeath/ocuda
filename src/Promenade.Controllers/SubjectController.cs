﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ocuda.Promenade.Controllers.Abstract;
using Ocuda.Promenade.Models.Entities;
using Ocuda.Promenade.Service;

namespace Ocuda.Promenade.Controllers
{
    [Route("[Controller]")]
    [Route("{culture:cultureConstraint}/[Controller]")]
    public class SubjectController : BasePageController<SubjectController>
    {
        protected override PageType PageType
        { get { return PageType.Subject; } }

        public SubjectController(ServiceFacades.Controller<SubjectController> context,
            PageService pageService,
            RedirectService redirectService,
            SocialCardService socialCardService)
            : base(context, pageService, redirectService, socialCardService)
        {
        }

        [Route("{stub?}")]
        public async Task<IActionResult> Page(string stub)
        {
            return await ReturnPageAsync(stub);
        }
    }
}
