﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Ocuda.Ops.Service.Abstract;
using Ocuda.Ops.Service.Filters;
using Ocuda.Ops.Service.Interfaces.Ops.Services;
using Ocuda.Ops.Service.Interfaces.Promenade.Repositories;
using Ocuda.Promenade.Models.Entities;
using Ocuda.Utility.Exceptions;
using Ocuda.Utility.Models;

namespace Ocuda.Ops.Service
{
    public class FeatureService : BaseService<FeatureService>, IFeatureService
    {
        private readonly IFeatureRepository _featureRepository;

        public FeatureService(ILogger<FeatureService> logger,
            IHttpContextAccessor httpContextAccessor,
            IFeatureRepository featureRepository)
            : base(logger, httpContextAccessor)
        {
            _featureRepository = featureRepository
                ?? throw new ArgumentNullException(nameof(featureRepository));
        }

        public async Task<DataWithCount<ICollection<Feature>>> GetPaginatedListAsync(
            BaseFilter filter)
        {
            return await _featureRepository.GetPaginatedListAsync(filter);
        }

        public async Task<List<Feature>> GetAllFeaturesAsync()
        {
            return await _featureRepository.GetAllFeaturesAsync();
        }

        public async Task<Feature> GetFeatureByNameAsync(string featureName)
        {
            try
            {
                return await _featureRepository.GetFeatureByName(featureName);
            }
            catch (OcudaException ex)
            {
                _logger.LogError(ex,
                    "Problem finding feature {FeatureName}: {Message}",
                    featureName,
                    ex.Message);
                throw new OcudaException($"Could not find feature: {featureName}");
            }
        }

        public async Task<Feature> GetFeatureByIdAsync(int featureId)
        {
            return await _featureRepository.FindAsync(featureId);
        }

        public async Task<Feature> AddFeatureAsync(Feature feature)
        {
            feature.Icon = "fa-inverse " + feature.Icon + " fa-stack-1x";
            feature.Name = feature.Name?.Trim();
            feature.BodyText = feature.BodyText?.Trim();
            feature.Stub = feature.Stub?.Trim();

            await ValidateAsync(feature);
            await _featureRepository.AddAsync(feature);
            await _featureRepository.SaveAsync();

            return feature;
        }

        public async Task<Feature> EditAsync(Feature feature)
        {
            var currentFeature = await _featureRepository.FindAsync(feature.Id);
            await ValidateAsync(feature);
            if (currentFeature != null)
            {
                if (!feature.Icon.Contains("fa-inverse"))
                {
                    feature.Icon = "fa-inverse " + feature.Icon + " fa-stack-1x";
                }
                currentFeature.BodyText = feature.BodyText?.Trim();
                currentFeature.Icon = feature.Icon;
                currentFeature.Name = feature.Name?.Trim();
                currentFeature.Stub = feature.Stub?.Trim();
                currentFeature.IsAtThisLocation = feature.IsAtThisLocation;

                _featureRepository.Update(feature);
                await _featureRepository.SaveAsync();
                return await _featureRepository.FindAsync(currentFeature.Id);
            }
            else
            {
                throw new OcudaException($"Could not find feature id {feature.Id} to edit.");
            }
        }

        public async Task DeleteAsync(int id)
        {
            var feature = await _featureRepository.FindAsync(id);
            _featureRepository.Remove(feature);
            await _featureRepository.SaveAsync();
        }

        public async Task<DataWithCount<ICollection<Feature>>> PageItemsAsync(
            FeatureFilter filter)
        {
            return new DataWithCount<ICollection<Feature>>
            {
                Data = await _featureRepository.PageAsync(filter),
                Count = await _featureRepository.CountAsync(filter)
            };
        }

        private async Task ValidateAsync(Feature feature)
        {
            if (await _featureRepository.IsDuplicateNameAsync(feature))
            {
                throw new OcudaException($"A feature named '{feature.Name}' already exists.");
            }
            if (!string.IsNullOrEmpty(feature.Stub)
                && await _featureRepository.IsDuplicateStubAsync(feature))
            {
                throw new OcudaException($"A feature with stub '{feature.Stub}' already exists.");
            }
        }

        public async Task<ICollection<Feature>> GetFeaturesByIdsAsync(IEnumerable<int> featureIds)
        {
            return await _featureRepository.GetByIdsAsync(featureIds);
        }
    }
}