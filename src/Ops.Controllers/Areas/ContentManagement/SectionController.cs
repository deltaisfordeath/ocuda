﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Ocuda.Ops.Controllers.Abstract;
using Ocuda.Ops.Controllers.Areas.ContentManagement.ViewModels.Section;
using Ocuda.Ops.Controllers.Filters;
using Ocuda.Ops.Models.Entities;
using Ocuda.Ops.Service.Filters;
using Ocuda.Ops.Service.Interfaces.Ops.Services;
using Ocuda.Utility.Exceptions;
using Ocuda.Utility.Keys;
using Ocuda.Utility.Models;
using Ocuda.Utility.Services.Interfaces;

namespace Ocuda.Ops.Controllers.Areas.ContentManagement
{
    [Area("ContentManagement")]
    [Route("[area]/[controller]")]
    public class SectionController : BaseController<SectionController>
    {
        private readonly IOcudaCache _cache;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILinkService _linkService;
        private readonly IPermissionGroupService _permissionGroupService;
        private readonly IPostService _postService;
        private readonly ISectionService _sectionService;

        public SectionController(ServiceFacades.Controller<SectionController> context,
            IFileService fileService,
            ILinkService linkService,
            IOcudaCache cache,
            IPostService postService,
            IPermissionGroupService permissionGroupService,
            ISectionService sectionService,
            IWebHostEnvironment hostingEnvironment) : base(context)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _hostingEnvironment = hostingEnvironment
                ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _linkService = linkService ?? throw new ArgumentNullException(nameof(linkService));
            _postService = postService ?? throw new ArgumentNullException(nameof(postService));
            _permissionGroupService = permissionGroupService
                ?? throw new ArgumentNullException(nameof(permissionGroupService));
            _sectionService = sectionService
                ?? throw new ArgumentNullException(nameof(sectionService));
        }

        public static string Area { get { return "ContentManagement"; } }
        public static string Name { get { return "Section"; } }

        [HttpPost]
        [Route("[action]")]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> AddFileLibrary(SectionViewModel viewModel)
        {
            var section = await GetSectionAsManagerAsync(viewModel?.Section?.Stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (string.IsNullOrEmpty(viewModel.FileLibrary.Name))
            {
                ModelState.AddModelError("FileLibrary.Name", "A 'File Library Name' is required.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            if (string.IsNullOrEmpty(viewModel.FileLibrary.Stub))
            {
                ShowAlertDanger("Invalid name for File Library.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            viewModel.FileLibrary.SectionId = section.Id;
            await _fileService.CreateLibraryAsync(viewModel.FileLibrary, section.Id);
            var fileLibs = await _fileService.GetBySectionIdAsync(section.Id);
            var fileLib = fileLibs.SingleOrDefault(_ => _.Stub == viewModel.FileLibrary.Stub.Trim());
            var fileTypes = await _fileService.GetAllFileTypeIdsAsync();
            await _fileService.EditLibraryTypesAsync(fileLib, fileTypes);
            var filePath = Path.Combine(
                Directory.GetParent(_hostingEnvironment.WebRootPath).FullName, "shared");
            filePath = Path.Combine(filePath, "sections");
            filePath = Path.Combine(filePath, section.Stub);
            filePath = Path.Combine(filePath, fileLib.Stub);
            if (Directory.Exists(filePath))
            {
                ShowAlertDanger($"The File Library '{fileLib.Stub}' already exists for this section.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            else
            {
                Directory.CreateDirectory(filePath);
            }
            ShowAlertSuccess($"Added '{fileLib.Name}' to '{section.Name}'s File Library'");
            return RedirectToAction(nameof(SectionController.Section),
                new { sectionStub = section.Stub });
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddFileToLibrary(FileLibraryViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.SectionStub);
            if (model == null || section == null)
            {
                return RedirectToUnauthorized();
            }

            var fileLibrary = await _fileService.GetLibraryByIdAsync(model.FileLibraryId);

            var extension = Path.GetExtension(model.UploadFile.FileName);

            var path = GetFullPath(section.Stub, fileLibrary.Stub, model.File.Name + extension);

            try
            {
                _fileService.VerifyAddFileAsync(fileLibrary.Id, extension, path);
                model.File.FileLibraryId = fileLibrary.Id;
            }
            catch (OcudaException oex)
            {
                ShowAlertDanger(oex.Message);

                return RedirectToAction(nameof(FileLibrary), new
                {
                    sectionStub = section.Stub,
                    fileLibStub = fileLibrary.Stub,
                    page = model.CurrentPage
                });
            }

            if (model.UploadFile.Length > 0)
            {
                using var fileStream = new FileStream(path, FileMode.Create);
                await model.UploadFile.CopyToAsync(fileStream);
                await _fileService.AddFileLibraryFileAsync(model.File, model.UploadFile);
                ShowAlertSuccess($"Added to {fileLibrary.Name}: {model.File.Name}");
            }
            else
            {
                ShowAlertDanger($"Empty file {model.File.Name} not uploaded successfully.");
            }
            return RedirectToAction(nameof(FileLibrary), new
            {
                sectionStub = section.Stub,
                fileLibStub = fileLibrary.Stub,
                page = model.CurrentPage
            });
        }

        [HttpPost]
        [Route("[action]")]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> AddLinkLibrary(SectionViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.Section?.Stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (string.IsNullOrEmpty(model.LinkLibrary.Name))
            {
                ModelState.AddModelError("LinkLibrary.Name", "A 'Link Library Name' is required.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = model.Section.Stub });
            }
            if (string.IsNullOrEmpty(model.LinkLibrary.Stub))
            {
                ShowAlertDanger("Invalid name for File Library.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = model.Section.Stub });
            }
            model.LinkLibrary.SectionId = section.Id;
            var linkLib = await _linkService.CreateLibraryAsync(model.LinkLibrary, section.Id);
            ShowAlertSuccess($"Added Link Library '{linkLib.Name}' to '{section.Name}'");
            return RedirectToAction(nameof(SectionController.Section),
                new { sectionStub = model.Section.Stub });
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddLinkToLibrary(LinkLibraryViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.SectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (ModelState.IsValid)
            {
                model.Link.LinkLibraryId = model.LinkLibraryId;
                var link = await _linkService.CreateAsync(model.Link);
                var linkLib = await _linkService.GetLibraryByIdAsync(model.LinkLibraryId);
                model.LinkLibrary = linkLib;

                ShowAlertSuccess($"Added '{link.Name}' to '{linkLib.Name}'");
                return RedirectToAction(nameof(SectionController.LinkLibrary),
                    new { sectionStub = model.SectionStub, linkLibStub = linkLib.Stub });
            }
            else
            {
                ShowAlertDanger($"Could not add Link to Library");
                return RedirectToAction(nameof(SectionController.LinkLibrary),
                    new { sectionStub = model.SectionStub, linkLibStub = model.LinkLibraryStub });
            }
        }

        [HttpPost("[action]/{stub}/{permissionGroupId}")]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> AddPermissionGroup(string stub, int permissionGroupId)
        {
            var section = await GetSectionAsManagerAsync(stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            try
            {
                await _permissionGroupService
                    .AddToPermissionGroupAsync<PermissionGroupSectionManager>(section.Id,
                permissionGroupId);
                AlertInfo = "Group added for section management.";
            }
            catch (Exception ex)
            {
                AlertDanger = $"Problem adding permission: {ex.Message}";
            }

            return RedirectToAction(nameof(Permissions), new { stub });
        }

        [HttpGet("{sectionStub}/[action]")]
        [RestoreModelState]
        public async Task<IActionResult> AddPost(string sectionStub)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var viewModel = new AddPostViewModel
            {
                SectionStub = section.Stub,
                SectionId = section.Id,
                SectionName = section.Name,
                SectionCategories = await _postService.GetCategoriesBySectionIdAsync(section.Id)
            };
            viewModel.SelectionPostCategories = new SelectList(viewModel.SectionCategories, "Id", "Name");
            return View(viewModel);
        }

        [HttpPost]
        [Route("{sectionStub}/[action]")]
        [SaveModelState]
        public async Task<IActionResult> AddPost(AddPostViewModel viewModel)
        {
            Section section = viewModel?.Post?.SectionId != null
                ? await GetSectionAsManagerAsync(viewModel.Post.SectionId)
                : null;

            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (await _postService.GetSectionPostByStubAsync(viewModel.Post.Stub, section.Id) != null)
            {
                ModelState.AddModelError("Post.Stub", "This 'Stub' already exists.");
                ShowAlertDanger($"Could not create Post.");
                return RedirectToAction(nameof(AddPost), viewModel);
            }
            if (ModelState.IsValid)
            {
                viewModel.SectionCategories = await _postService.GetCategoriesBySectionIdAsync(section.Id);
                viewModel.SelectionPostCategories = new SelectList(viewModel.SectionCategories, "Id", "Name");
                try
                {
                    await _postService.CreatePostAsync(viewModel.Post);

                    var post = await _postService.GetSectionPostByStubAsync(
                        viewModel.Post.Stub.Trim(), section.Id);
                    await _postService.UpdatePostCategoriesAsync(viewModel.CategoryIds, post.Id);

                    ShowAlertSuccess($"Added post '{viewModel.Post.Title}'");
                    return RedirectToAction(nameof(SectionController.PostDetails),
                        new { sectionStub = section.Stub, postStub = post.Stub.Trim() });
                }
                catch
                {
                    ShowAlertDanger("Could not create post.");
                    return RedirectToAction(nameof(SectionController.PostDetails),
                        new { sectionStub = section.Stub, postStub = viewModel.Post.Stub.Trim() });
                }
            }
            else
            {
                ShowAlertDanger("Could not create post.");
                return RedirectToAction(nameof(SectionController.PostDetails),
                    new { sectionStub = section.Stub, postStub = viewModel.Post.Stub.Trim() });
            }
        }

        [Route("[action]")]
        [HttpPost]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> ClearSectionCache()
        {
            if (string.IsNullOrEmpty(UserClaim(ClaimType.SiteManager)))
            {
                return RedirectToUnauthorized();
            }

            await _cache.RemoveAsync(Cache.OpsSections);
            ShowAlertInfo("Section cache cleared.");
            return RedirectToAction(nameof(Index));
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> DeleteFileFromLibrary(FileLibraryViewModel viewModel)
        {
            var section = await GetSectionAsManagerAsync(viewModel.SectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (viewModel.File.Id == default
                || string.IsNullOrEmpty(viewModel.FileLibraryStub)
                || string.IsNullOrEmpty(viewModel.SectionStub))
            {
                ShowAlertWarning("You must supply a valid file to delete.");

                return RedirectToAction(nameof(Section), new { sectionStub = section.Stub });
            }
            else
            {
                var file = await _fileService.GetByIdAsync(viewModel.File.Id);
                var fileType = await _fileService.GetFileTypeByIdAsync(file.FileTypeId);

                var filePath = GetFullPath(section.Stub,
                    viewModel.FileLibraryStub,
                    file.Name + fileType.Extension);

                if (!System.IO.File.Exists(filePath))
                {
                    ShowAlertWarning($"File does not exist: {file.Name}");
                }
                else
                {
                    System.IO.File.Delete(filePath);
                    await _fileService.DeletePrivateFileAsync(file.Id);
                    ShowAlertSuccess($"Deleted file: {file.Name}");
                }

                return RedirectToAction(nameof(FileLibrary), new
                {
                    sectionStub = section.Stub,
                    fileLibStub = viewModel.FileLibraryStub,
                    page = viewModel.CurrentPage
                });
            }
        }

        [Route("[action]")]
        [HttpPost]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> DeleteFileLibrary(SectionViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.Section?.Stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            try
            {
                var fileLib = await _fileService.GetLibraryByIdAsync(model.FileLibrary.Id);
                var libFiles = await _fileService.GetFileLibraryFilesAsync(fileLib.Id);
                if (libFiles.Count > 0)
                {
                    ShowAlertDanger($"Please delete all files before deleting {fileLib.Name}.");
                    return RedirectToAction(nameof(SectionController.Section),
                        new { sectionStub = section.Stub });
                }

                var filePath = Path.Combine(
                    Directory.GetParent(_hostingEnvironment.WebRootPath).FullName, "shared");
                filePath = Path.Combine(filePath, "sections");
                filePath = Path.Combine(filePath, section.Stub);
                filePath = Path.Combine(filePath, fileLib.Stub);
                if (!Directory.Exists(filePath))
                {
                    _logger.LogError("The File Library {FileLibraryName} does not exist for this section.",
                        fileLib.Stub);
                    ShowAlertDanger($"Failed to delete File Library '{fileLib.Name}'");
                    return RedirectToAction(nameof(SectionController.Section),
                        new { sectionStub = section.Stub });
                }
                else
                {
                    Directory.Delete(filePath);
                    await _fileService.DeleteFileTypesByLibrary(fileLib.Id);
                    await _fileService.DeleteLibraryAsync(fileLib.Id);
                    ShowAlertSuccess($"Deleted '{fileLib.Name}' from '{section.Name}'s");
                }
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            catch
            {
                ShowAlertDanger($"Failed to delete File Library.");
                return RedirectToAction(nameof(FileLibrary),
                    new { sectionStub = model.Section.Stub });
            }
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> DeleteLinkFromLibrary(LinkLibraryViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.SectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (model.Link.Id != 0)
            {
                var link = await _linkService.GetByIdAsync(model.Link.Id);
                try
                {
                    await _linkService.DeleteAsync(model.Link.Id);
                    ShowAlertSuccess($"Deleted link '{link.Name}'");
                }
                catch
                {
                    ShowAlertDanger($"Failed to Delete '{link.Name}'");
                }
                return RedirectToAction(nameof(SectionController.LinkLibrary),
                    new { sectionStub = model.SectionStub, linkLibStub = model.LinkLibraryStub });
            }
            else
            {
                ShowAlertDanger($"Failed to Delete Link");
                return RedirectToAction(nameof(SectionController.LinkLibrary),
                    new { sectionStub = model.SectionStub, linkLibStub = model.LinkLibraryStub });
            }
        }

        [Route("[action]")]
        [HttpPost]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> DeleteLinkLibrary(SectionViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.Section?.Stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (model.LinkLibrary.Id == 0)
            {
                ShowAlertDanger("A 'Link Library Id' is required.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }

            var linkLib = await _linkService.GetLibraryByIdAsync(model.LinkLibrary.Id);
            if (linkLib == null)
            {
                ShowAlertDanger("Could not find the link library");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }

            var libLinks = await _linkService.GetLinkLibraryLinksAsync(linkLib.Id);
            if (libLinks.Count > 0)
            {
                ShowAlertDanger($"Please delete all links before deleting {linkLib.Name}.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            await _linkService.DeleteLibraryAsync(linkLib.Id);
            ShowAlertSuccess($"Deleted '{linkLib.Name}' from '{section.Name}'s");
            return RedirectToAction(nameof(SectionController.Section),
                new { sectionStub = section.Stub });
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> DeletePost(int postId, string sectionStub)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var post = await _postService.GetPostByIdAsync(postId);
            try
            {
                await _postService.RemovePostAsync(post);
                ShowAlertSuccess($"Deleted post '{post.Title}'");
            }
            catch
            {
                ShowAlertDanger($"Failed to Delete '{post.Title}'");
            }
            return RedirectToAction(nameof(SectionController.Posts),
                new { sectionStub });
        }

        [HttpGet("{sectionStub}/[action]/{postStub}")]
        [RestoreModelState]
        public async Task<IActionResult> EditPost(string sectionStub, string postStub)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            try
            {
                var post = await _postService.GetSectionPostByStubAsync(postStub, section.Id);
                var sectionCategories = await _postService.GetCategoriesBySectionIdAsync(section.Id);
                var viewModel = new EditPostViewModel
                {
                    SectionStub = section.Stub,
                    SectionId = section.Id,
                    Post = post,
                    SelectionPostCategories = new SelectList(sectionCategories, "Id", "Name"),
                    PostCategories = await _postService.GetPostCategoriesByIdAsync(post.Id)
                };
                viewModel.CategoryIds = viewModel.PostCategories.Select(_ => _.CategoryId).ToList();
                return View(viewModel);
            }
            catch
            {
                ShowAlertDanger($"The PostDetails Id {postStub} does not exist.");
                return RedirectToAction(nameof(SectionController.Section), new { sectionStub });
            }
        }

        [Route("{sectionStub}/[action]/{postStub}")]
        [HttpPost]
        [SaveModelState]
        public async Task<IActionResult> EditPost(EditPostViewModel viewModel)
        {
            var section = await GetSectionAsManagerAsync(viewModel.Post.SectionId);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _postService.UpdatePostAsync(viewModel.Post);
                    await _postService.UpdatePostCategoriesAsync(viewModel.CategoryIds, viewModel.Post.Id);
                    ShowAlertSuccess($"Updated post '{viewModel.Post.Title}'");
                    return RedirectToAction(nameof(SectionController.PostDetails),
                        new { sectionStub = section.Stub, postStub = viewModel.Post.Stub });
                }
                catch
                {
                    ShowAlertDanger($"Could not edit Post.");
                    return RedirectToAction(nameof(SectionController.EditPost),
                        new { sectionStub = viewModel.SectionStub, postStub = viewModel.Post.Stub });
                }
            }
            else
            {
                ShowAlertDanger($"Could not edit Post.");
                return RedirectToAction(nameof(SectionController.EditPost),
                    new { sectionStub = viewModel.SectionStub, postStub = viewModel.Post.Stub });
            }
        }

        [Route("{sectionStub}/[action]/{fileLibStub}")]
        [Route("{sectionStub}/[action]/{fileLibStub}/{page}")]
        public async Task<IActionResult> FileLibrary(string sectionStub,
            string fileLibStub,
            int page)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (page == default)
            {
                page = 1;
            }

            var fileLibrary = await _fileService.GetBySectionIdStubAsync(section.Id, fileLibStub);

            var itemsPerPage = await _siteSettingService
                .GetSettingIntAsync(Models.Keys.SiteSetting.UserInterface.ItemsPerPage);

            var filter = new BlogFilter(page, itemsPerPage)
            {
                FileLibraryId = fileLibrary.Id
            };

            var filesAndCount = await _fileService.GetPaginatedListAsync(filter);

            return View(new FileLibraryViewModel
            {
                HasAdminRights = IsSiteManager() || await _sectionService.IsManagerAsync(section.Id),
                CurrentPage = page,
                FileLibraryId = fileLibrary.Id,
                FileLibraryName = fileLibrary.Name,
                FileLibraryStub = fileLibrary.Stub,
                Files = filesAndCount.Data,
                FileTypes = await _fileService.GetFileLibrariesFileTypesAsync(fileLibrary.Id),
                ItemCount = filesAndCount.Count,
                ItemsPerPage = filter.Take.Value,
                HasReplaceRights = await _fileService.HasReplaceRightsAsync(fileLibrary.Id),
                SectionName = section.Name,
                SectionStub = section.Stub
            });
        }

        [HttpGet]
        [Route("[action]/{libraryId:int}/{fileId:int}")]
        [ResponseCache(NoStore = true)]
        public async Task<IActionResult> GetFile(int libraryId, int fileId)
        {
            var library = await _fileService.GetLibraryByIdAsync(libraryId);

            var section = await GetSectionAsManagerAsync(library.SectionId);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var file = await _fileService.GetByIdAsync(fileId);
            var type = await _fileService.GetFileTypeByIdAsync(file.FileTypeId);
            var rootPath = Directory.GetParent(_hostingEnvironment.WebRootPath).FullName;
            var filePath = Path.Combine(rootPath,
                "shared",
                "sections",
                section.Stub,
                library.Stub,
                file.Name + type.Extension);

            if (!System.IO.File.Exists(filePath))
            {
                ShowAlertDanger($"File not found in file library {library.Name}: {file.Name}");
                _logger.LogError("File {FileName} not found at path {FilePath} for library {LibraryName} (id {LibraryId})",
                    file.Name,
                    filePath,
                    library.Name,
                    library.Id);
                return RedirectToAction(nameof(SectionController.FileLibrary),
                    new { sectionStub = section.Stub, fileLibStub = library.Stub });
            }

            return File(new FileStream(filePath, FileMode.Open, FileAccess.Read),
                System.Net.Mime.MediaTypeNames.Application.Octet,
                file.Name + type.Extension);
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var permissionGroupIds = UserClaims(ClaimType.PermissionId)
                .Select(_ => int.Parse(_, CultureInfo.InvariantCulture));

            return View(new SectionIndexViewModel
            {
                IsSiteManager = IsSiteManager(),
                Sections = await _sectionService.GetManagedByCurrentUserAsync()
            });
        }

        [Route("{sectionStub}/[action]/{linkLibStub}")]
        [Route("{sectionStub}/[action]/{linkLibStub}/{page}")]
        public async Task<IActionResult> LinkLibrary(string sectionStub,
            string linkLibStub, int page = 1)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var linkLibs = await _linkService.GetBySectionIdAsync(section.Id);
            var linkLib = linkLibs.Find(_ => _.Stub == linkLibStub);
            var itemsPerPage = await _siteSettingService
                .GetSettingIntAsync(Models.Keys.SiteSetting.UserInterface.ItemsPerPage);
            var filter = new BlogFilter(page, itemsPerPage)
            {
                LinkLibraryId = linkLib.Id
            };
            var links = await _linkService.GetPaginatedListAsync(filter);
            var paginateModel = new PaginateModel
            {
                CurrentPage = page,
                ItemsPerPage = filter.Take.Value,
                ItemCount = links.Count
            };
            var viewModel = new LinkLibraryViewModel
            {
                SectionStub = section.Stub,
                SectionName = section.Name,
                LinkLibrary = linkLib,
                Links = links.Data,
                FileTypes = await _fileService.GetAllFileTypesAsync(),
                PaginateModel = paginateModel
            };
            return View(viewModel);
        }

        [HttpGet("[action]/{stub}")]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> Permissions(string stub)
        {
            var section = await GetSectionAsManagerAsync(stub);

            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var permissionGroups = await _permissionGroupService.GetAllAsync();
            var sectionPermissions = await _permissionGroupService
                .GetPermissionsAsync<PermissionGroupSectionManager>(section.Id);

            var viewModel = new PermissionsViewModel
            {
                Name = section.Name,
                Stub = section.Stub,
            };

            foreach (var permissionGroup in permissionGroups)
            {
                if (sectionPermissions.Any(_ => _.PermissionGroupId == permissionGroup.Id))
                {
                    viewModel.AssignedGroups.Add(permissionGroup.Id,
                        permissionGroup.PermissionGroupName);
                }
                else
                {
                    viewModel.AvailableGroups.Add(permissionGroup.Id,
                        permissionGroup.PermissionGroupName);
                }
            }

            return View(viewModel);
        }

        [Route("{sectionStub}/[action]/{postStub}")]
        public async Task<IActionResult> PostDetails(string sectionStub, string postStub)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            try
            {
                var post = await _postService.GetSectionPostByStubAsync(postStub, section.Id);
                post.Content = CommonMark.CommonMarkConverter.Convert(post.Content);
                var viewModel = new PostDetailsViewModel
                {
                    SectionStub = section.Stub,
                    SectionName = section.Name,
                    SectionCategories = await _postService.GetCategoriesBySectionIdAsync(section.Id),
                    SectionsPosts = await _postService.GetTopSectionPostsAsync(5, section.Id),
                    Post = post
                };
                viewModel.PostCategories = await _postService.GetPostCategoriesByIdAsync(viewModel.Post.Id);
                return View(viewModel);
            }
            catch
            {
                ShowAlertDanger($"The Post {postStub} does not exist.");
                return RedirectToAction(nameof(SectionController.Section), new { sectionStub });
            }
        }

        [Route("{sectionStub}/[action]")]
        [Route("{sectionStub}/[action]/{page}")]
        [Route("{sectionStub}/[action]/{categoryStub}/{page}")]
        public async Task<IActionResult> Posts(string sectionStub, string categoryStub, int page = 1)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var filter = new BlogFilter(page, 5)
            {
                SectionId = section.Id
            };

            var paginateModel = new PaginateModel
            {
                CurrentPage = page,
                ItemsPerPage = filter.Take.Value,
            };

            if (paginateModel.PastMaxPage)
            {
                return RedirectToRoute(
                    new
                    {
                        page = paginateModel.LastPage ?? 1
                    });
            }
            var topPosts = await _postService.GetTopSectionPostsAsync(5, section.Id);
            foreach (var topPost in topPosts.ToList())
            {
                topPost.Content = CommonMark.CommonMarkConverter.Convert(topPost.Content);
            }
            var viewModel = new PostsViewModel
            {
                SectionStub = section.Stub,
                SectionName = section.Name,
                SectionCategories = await _postService.GetCategoriesBySectionIdAsync(section.Id),
                SectionsPosts = topPosts
            };
            if (string.IsNullOrEmpty(categoryStub))
            {
                var posts = await _postService.GetPaginatedPostsAsync(filter);
                foreach (var post in posts.Data.ToList())
                {
                    post.Content = CommonMark.CommonMarkConverter.Convert(post.Content);
                }
                viewModel.PostCategories = await _postService.GetPostCategoriesByIdsAsync(
                    posts.Data.Select(_ => _.Id).ToList());
                paginateModel.ItemCount = posts.Count;
                viewModel.PaginateModel = paginateModel;
                viewModel.AllCategoryPosts = posts.Data;
                return View(viewModel);
            }
            else
            {
                try
                {
                    var category = await _postService.GetSectionCategoryByStubAsync(categoryStub, section.Id);
                    filter.CategoryId = category.Id;
                    var posts = await _postService.GetPaginatedPostsAsync(filter);
                    foreach (var post in posts.Data.ToList())
                    {
                        post.Content = CommonMark.CommonMarkConverter.Convert(post.Content);
                    }
                    paginateModel.ItemCount = posts.Count;
                    viewModel.PostCategories = await _postService.GetPostCategoriesByIdsAsync(
                        posts.Data.Select(_ => _.Id).ToList());
                    viewModel.PaginateModel = paginateModel;
                    viewModel.AllCategoryPosts = posts.Data;
                    return View(viewModel);
                }
                catch
                {
                    ShowAlertDanger($"The PostDetails Category {categoryStub} does not exist.");
                    return RedirectToAction(nameof(SectionController.Section), new { sectionStub });
                }
            }
        }

        [HttpPost("[action]/{stub}/{permissionGroupId:int}")]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> RemovePermissionGroup(string stub, int permissionGroupId)
        {
            var section = await GetSectionAsManagerAsync(stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            try
            {
                await _permissionGroupService
                    .RemoveFromPermissionGroupAsync<PermissionGroupSectionManager>(section.Id,
                    permissionGroupId);
                AlertInfo = "Group removed from section management.";
            }
            catch (Exception ex)
            {
                AlertDanger = $"Problem removing permission: {ex.Message}";
            }

            return RedirectToAction(nameof(Permissions), new { stub });
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ReplaceFile(FileLibraryViewModel viewModel)
        {
            var section = await GetSectionAsManagerAsync(viewModel.SectionStub);
            if (section == null)
            {
                var hasReplaceRights = await _fileService
                    .HasReplaceRightsAsync(viewModel.FileLibraryId);
                if (!hasReplaceRights)
                {
                    return RedirectToUnauthorized();
                }
            }

            var file = await _fileService.GetByIdAsync(viewModel.ReplaceFileId);
            var fileType = await _fileService.GetFileTypeByIdAsync(file.FileTypeId);
            var extension = Path.GetExtension(viewModel.UploadFile.FileName);

            if (!fileType.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase))
            {
                ShowAlertWarning($"Could not replace file: uploaded file type ({extension}) did not match existing file type ({fileType.Extension}).");
            }
            else
            {
                var path = GetFullPath(section.Stub, viewModel.FileLibraryStub, file.Name + extension);

                if (viewModel.UploadFile.Length > 0)
                {
                    using var fileStream = new FileStream(path, FileMode.Truncate);
                    await viewModel.UploadFile.CopyToAsync(fileStream);
                    await _fileService.ReplaceFileLibraryFileAsync(file.Id);
                    ShowAlertSuccess($"Replaced {file.Name}");
                }
                else
                {
                    ShowAlertDanger($"Empty file {viewModel.File.Name} not uploaded successfully.");
                }
            }

            return RedirectToAction(nameof(FileLibrary), new
            {
                sectionStub = viewModel.SectionStub,
                fileLibStub = viewModel.FileLibraryStub,
                page = viewModel.CurrentPage
            });
        }

        [Route("{sectionStub}")]
        [Route("{sectionStub}/{page}")]
        public async Task<IActionResult> Section(string sectionStub, int page = 1)
        {
            var section = await GetSectionAsManagerAsync(sectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            var filter = new BlogFilter(page, 5)
            {
                SectionId = section.Id
            };

            var posts = await _postService.GetPaginatedPostsAsync(filter);

            var paginateModel = new PaginateModel
            {
                ItemCount = posts.Count,
                CurrentPage = page,
                ItemsPerPage = filter.Take.Value
            };
            if (paginateModel.PastMaxPage)
            {
                return RedirectToRoute(
                    new
                    {
                        page = paginateModel.LastPage ?? 1
                    });
            }

            var viewModel = new SectionViewModel
            {
                Section = section,
                SectionCategories = await _postService.GetCategoriesBySectionIdAsync(section.Id),
                FileLibraries = await _fileService.GetBySectionIdAsync(section.Id),
                LinkLibraries = await _linkService.GetBySectionIdAsync(section.Id),
                PaginateModel = paginateModel,
                AllPosts = posts.Data.Select(_ =>
                {
                    _.Content = CommonMark.CommonMarkConverter.Convert(_.Content);
                    return _;
                }).ToList()
            };

            var postIds = viewModel.AllPosts
                .Select(_ => _.Id)
                .Skip(page - 1)
                .Take(filter.Take.Value)
                .ToList();
            viewModel.PostCategories = await _postService.GetPostCategoriesByIdsAsync(postIds);

            return View(viewModel);
        }

        [Route("[action]")]
        [HttpPost]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> UpdateFileLibrary(FileLibraryViewModel viewModel)
        {
            var section = await GetSectionAsManagerAsync(viewModel?.SectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (string.IsNullOrEmpty(viewModel.FileLibraryName))
            {
                ModelState.AddModelError("FileLibrary.Name", "A 'File Library Name' is required.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            if (string.IsNullOrEmpty(viewModel.FileLibraryStub))
            {
                ShowAlertDanger("Invalid name for File Library.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            var oldLib = await _fileService.GetLibraryByIdAsync(viewModel.FileLibraryId);
            var filePath = Path.Combine(
                Directory.GetParent(_hostingEnvironment.WebRootPath).FullName, "shared");
            filePath = Path.Combine(filePath, "sections");
            filePath = Path.Combine(filePath, section.Stub);
            var newPath = Path.Combine(filePath, viewModel.FileLibraryStub);
            var oldPath = Path.Combine(filePath, oldLib.Stub);
            if (Directory.Exists(newPath))
            {
                ShowAlertDanger($"The File Library '{viewModel.FileLibraryName}' already exists for this section.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            else
            {
                Directory.CreateDirectory(newPath);

                var oldDir = new DirectoryInfo(oldPath);
                var files = oldDir.GetFiles();

                foreach (FileInfo file in files)
                {
                    string temppath = Path.Combine(newPath, file.Name);
                    file.CopyTo(temppath, false);
                }

                Directory.Delete(oldPath, true);
                oldLib.Name = viewModel.FileLibraryName;
                oldLib.Stub = viewModel.FileLibraryStub;
                var types = new List<int>();
                await _fileService.UpdateLibrary(oldLib);
                await _fileService.EditLibraryTypesAsync(oldLib, types);
            }
            ShowAlertSuccess($"Added '{viewModel.FileLibraryName}' to '{section.Name}'s File Library'");
            return RedirectToAction(nameof(SectionController.Section),
                new { sectionStub = section.Stub });
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UpdateLinkFromLibrary(LinkLibraryViewModel model)
        {
            var section = await GetSectionAsManagerAsync(model?.SectionStub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (ModelState.IsValid)
            {
                var link = await _linkService.GetByIdAsync(model.Link.Id);
                try
                {
                    var updateLink = await _linkService.EditAsync(model.Link);
                    ShowAlertSuccess($"Updated link '{updateLink.Name}'");
                }
                catch
                {
                    ShowAlertDanger($"Failed to Update '{link.Name}'");
                }
                return RedirectToAction(nameof(SectionController.LinkLibrary),
                    new { sectionStub = model.SectionStub, linkLibStub = model.LinkLibraryStub });
            }
            else
            {
                ShowAlertDanger($"Could not Update Link");
                return RedirectToAction(nameof(SectionController.LinkLibrary),
                    new { sectionStub = model.SectionStub, linkLibStub = model.LinkLibraryStub });
            }
        }

        [Route("[action]")]
        [HttpPost]
        [Authorize(Policy = nameof(ClaimType.SiteManager))]
        public async Task<IActionResult> UpdateLinkLibrary(SectionViewModel viewModel)
        {
            var section = await GetSectionAsManagerAsync(viewModel?.Section?.Stub);
            if (section == null)
            {
                return RedirectToUnauthorized();
            }

            if (string.IsNullOrEmpty(viewModel.LinkLibrary.Name))
            {
                ModelState.AddModelError("LinkLibrary.Name", "A 'Link Library Name' is required.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = viewModel.Section.Stub });
            }
            if (string.IsNullOrEmpty(viewModel.LinkLibrary.Stub))
            {
                ShowAlertDanger("Invalid name for File Library.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = viewModel.Section.Stub });
            }
            if (viewModel.LinkLibrary.Id == 0)
            {
                ShowAlertDanger("A Link Library Id is required.");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = viewModel.Section.Stub });
            }
            if (ModelState.IsValid)
            {
                var oldLib = await _linkService.GetLibraryByIdAsync(viewModel.LinkLibrary.Id);
                oldLib.Name = viewModel.LinkLibrary.Name;
                oldLib.Stub = viewModel.LinkLibrary.Stub;
                await _linkService.UpdateLibraryAsync(oldLib);
                ShowAlertSuccess($"Updated link library '{oldLib.Name}'");
                return RedirectToAction(nameof(SectionController.Section),
                    new { sectionStub = section.Stub });
            }
            else
            {
                ShowAlertDanger($"Could Not Update {viewModel.LinkLibrary.Name}");
                return RedirectToAction(nameof(LinkLibrary),
                    new { sectionStub = viewModel.Section.Stub, linkLibStub = viewModel.LinkLibrary.Stub });
            }
        }

        private string GetFullPath(string sectionStub, string fileLibraryStub, string fileName)
        {
            return Path.Combine(
                Directory.GetParent(_hostingEnvironment.WebRootPath).FullName,
                "shared",
                "sections",
                sectionStub,
                fileLibraryStub,
                fileName);
        }

        private async Task<Section> GetSectionAsManagerAsync(int sectionId)
        {
            var section = await _sectionService.GetByIdAsync(sectionId);
            return await _sectionService.IsManagerAsync(section.Id)
                ? section
                : null;
        }

        private async Task<Section> GetSectionAsManagerAsync(string sectionStub)
        {
            if (string.IsNullOrEmpty(sectionStub))
            {
                return null;
            }

            var section = await _sectionService.GetByStubAsync(sectionStub);
            return await _sectionService.IsManagerAsync(section.Id)
                ? section
                : null;
        }
    }
}