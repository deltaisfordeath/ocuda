﻿using System;
using System.Threading.Tasks;
using ImageOptimApi;

namespace Ocuda.Ops.Service.Interfaces.Promenade.Services
{
    public interface IImageOptimizerService
    {
        public string BgColor { get; set; }
        public bool Crop { get; set; }
        public CropType CropType { get; set; }
        public int? CropXFocalPoint { get; set; }
        public int? CropYFocalPoint { get; set; }
        public bool DisableWebCall { get; set; }
        public bool Fit { get; set; }
        public Format Format { get; set; }
        public int? Height { get; set; }
        public HighDpi HighDpi { get; set; }
        public Quality Quality { get; set; }
        public bool TrimBorder { get; set; }
        public string Username { get; set; }
        public int? Width { get; set; }

        public Task<OptimizedImageResult> OptimizeAsync(Uri imageUri);

        public Task<OptimizedImageResult> OptimizeAsync(string imagePath);

    }
}
