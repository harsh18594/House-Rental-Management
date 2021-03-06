﻿using HouseRentalManagement.Config;
using HouseRentalManagement.Data;
using HouseRentalManagement.Data.Interface;
using HouseRentalManagement.Models;
using HouseRentalManagement.Models.HouseViewModels;
using HouseRentalManagement.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HouseRentalManagement.Services
{
    public class FrontendService : IFrontendService
    {
        private readonly IHouseRepository _houseRepository;
        private ImageOptions _imageOptions;
        private readonly IFeaturedPhotoRepository _featuredPhotoRepository;
        private readonly IAmenityRepository _amenityRepository;
        private readonly IRestrictionRepository _restrictionRepository;
        private readonly IHouseImageRepository _houseImageRepository;
        private readonly ISiteConfigRepository _siteConfigRepository;
        private ILogger _logger;

        public FrontendService(IHouseRepository houseRepository,
            IOptions<ImageOptions> imageOptions,
            IFeaturedPhotoRepository featuredPhotoRepository,
            ILogger<FrontendService> logger,
            IAmenityRepository amenityRepository,
            IRestrictionRepository restrictionRepository,
            IHouseImageRepository houseImageRepository,
            ISiteConfigRepository siteConfigRepository)
        {
            _houseRepository = houseRepository;
            _imageOptions = imageOptions.Value;
            _featuredPhotoRepository = featuredPhotoRepository;
            _logger = logger;
            _amenityRepository = amenityRepository;
            _restrictionRepository = restrictionRepository;
            _houseImageRepository = houseImageRepository;
            _siteConfigRepository = siteConfigRepository;
        }

        public async Task<IndexViewModel> GetIndexViewModelAsync()
        {
            var model = new IndexViewModel()
            {
                Houses = new List<HouseViewModel>(),
                FeaturedImages = new List<FeaturedPhotosViewModel>()
            };

            try
            {
                // add houses
                var houses = await _houseRepository.GetHouseListForIndexPageAsync();
                if (houses != null)
                {
                    foreach (var house in houses)
                    {
                        model.Houses.Add(GetHouseViewModelFromHouse(house));
                    }
                }

                // add feature photos
                var featuredPhotos = await _featuredPhotoRepository.ListToBeDisplayedFeaturedImagesAsync();
                if (featuredPhotos != null)
                {
                    foreach (var photo in featuredPhotos)
                    {
                        model.FeaturedImages.Add(GetFeaturedPhotosViewModelFromFeatureImage(photo));
                    }
                }

                // add site config
                var siteConfig = await _siteConfigRepository.GetSiteConfigAsync();
                if (siteConfig != null)
                {
                    if (!string.IsNullOrEmpty(siteConfig.WhatsappNumber))
                    {
                        var whatsAppNumber = siteConfig.WhatsappNumber.Replace(".", "");
                        whatsAppNumber = whatsAppNumber.Replace("-", "");
                        whatsAppNumber = whatsAppNumber.Replace(" ", "");
                        model.ContactWhatsappNumber = "1" + whatsAppNumber;
                    }

                    model.ContactEmail = siteConfig.PrimaryEmail;
                    model.ContactPhoneNumber = siteConfig.PhoneNumber;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("FrontendService/GetIndexViewModelAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return model;
        }

        private HouseViewModel GetHouseViewModelFromHouse(House house)
        {
            var model = new HouseViewModel();

            // set image src
            var imageSrc = string.Empty;
            if (house.HouseImages != null)
            {
                var fileName = house.HouseImages?.Where(a => a.IsHomePageImage.Value).Select(a => a.FileName).FirstOrDefault();
                if (!string.IsNullOrEmpty(fileName))
                {
                    var imageDirectory = string.Format(_imageOptions.HouseImagePath, house.HouseId);
                    imageSrc = string.Format("{0}{1}{2}", "/", imageDirectory, fileName);
                }
                else
                {
                    imageSrc = "icons/brand-logo.png";
                }

            }

            model.DateAvailable = house.ContactForAvailableFrom ? "Please contact" : house.AvailableFrom.ToString("MMM-dd-yyyy");
            model.Description = house.Description;
            var fullAddressParts = new string[] { house.AddressLine1, house.AddressLine2, house.City };
            var fullAddress = string.Join(", ", fullAddressParts.Where(a => !string.IsNullOrEmpty(a)));
            model.FullAddress = fullAddress;
            model.Rent = house.Rent > 0 ? house.Rent.ToString("C0") + "/month" : string.Empty;
            model.MainImageSrc = imageSrc;
            model.UrlSlug = house.UrlSlug;

            return model;
        }

        private FeaturedPhotosViewModel GetFeaturedPhotosViewModelFromFeatureImage(FeaturedImage image)
        {
            var model = new FeaturedPhotosViewModel();

            // prepare path
            var destinationPath = Path.Combine(_imageOptions.FeaturedPhotoPath);
            var fullPath = string.Format("{0}{1}{2}", "/", destinationPath, image.FileName);
            model.ImageSrc = fullPath;

            return model;
        }

        public async Task<(bool Success, string Error, HouseInfoViewModel Model)> GetHouseInfoViewModelAsync(string slug, Guid houseId, bool preview = false)
        {
            bool success = false;
            string error = string.Empty;
            var model = new HouseInfoViewModel();

            try
            {
                if (!string.IsNullOrEmpty(slug) || houseId != Guid.Empty)
                {
                    // get house by id or slug
                    House house = null;
                    if (preview)
                    {
                        house = await _houseRepository.GetHouseByIdForPreviewAsync(houseId);
                    }
                    else
                    {
                        house = await _houseRepository.GetHouseByIdOrSlugAsync(slug: slug, id: houseId);
                    }

                    if (house != null)
                    {
                        // basic info
                        string[] parts = new string[] { house.AddressLine1, house.AddressLine2, house.City, house.PostalCode };
                        model.FullAddress = string.Join(", ", parts.Where(a => !string.IsNullOrEmpty(a)));
                        model.DateAvailable = house.ContactForAvailableFrom ? "Please contact" : house.AvailableFrom.ToString("MMM-dd-yyyy");
                        model.Description = house.Description;
                        model.Rent = house.Rent > 0 ? house.Rent.ToString("C0") + " (CDN$) per month" : "Please Contact";
                        model.ParkingSpace = house.ParakingSpace > 0 ? house.ParakingSpace.ToString() : "Please Contact";
                        model.Occupancy = house.Occupancy > 0 ? house.Occupancy.ToString() : "Please Contact";
                        model.Washrooms = house.Washrooms > 0 ? string.Format("{0:0.0}", house.Washrooms) : "Please Contact";

                        // amenities
                        if (house.HouseAmenities != null)
                        {
                            foreach (var amenity in house.HouseAmenities)
                            {
                                // all amenities
                                model.AllAmenities.Add(new AmenityViewModel()
                                {
                                    Title = amenity.Amenity?.Description,
                                    ImageSrc = string.Format(_imageOptions.AmenityImagePath, amenity.Amenity?.ImageFileName)
                                });

                                // included amenities
                                if (amenity.IncludedInUtility)
                                {
                                    model.IncludedAmenities.Add(new AmenityViewModel()
                                    {
                                        Title = amenity.Amenity?.Description,
                                        ImageSrc = string.Format(_imageOptions.AmenityImagePath, amenity.Amenity?.ImageFileName)
                                    });
                                }
                            }
                        }

                        // images
                        if (house.HouseImages != null)
                        {
                            foreach (var image in house.HouseImages.OrderByDescending(a => a.IsHomePageImage))
                            {
                                // prepare path
                                var imageDirectory = string.Format(_imageOptions.HouseImagePath, image.HouseId);
                                var fullPath = string.Format("{0}{1}{2}", "/", imageDirectory, image.FileName);

                                model.Images.Add(new ImagesViewModel()
                                {
                                    ImageSrc = fullPath
                                });

                                // set main image src
                                if (image.IsHomePageImage.HasValue && image.IsHomePageImage.Value)
                                {
                                    model.MainImageSrc = fullPath;
                                }
                            }
                        }

                        // restrictions
                        if (house.HouseRestrictions != null)
                        {
                            foreach (var restriction in house.HouseRestrictions)
                            {
                                model.Restrictions.Add(restriction.Restriction?.Title);
                            }
                        }

                        // getting around
                        if (house.HouseGettingArounds != null)
                        {
                            foreach (var item in house.HouseGettingArounds)
                            {
                                model.GettingArounds.Add(new GettingAroundViewModel()
                                {
                                    Location = item.LocationName,
                                    BikeTime = item.BikeTime,
                                    Distance = item.Distance,
                                    WalkingTime = item.WalkingTime,
                                    CarTime = item.CarTime
                                });
                            }
                        }

                        // facilities
                        if (house.Facilities != null)
                        {
                            foreach (var facility in house.Facilities)
                            {
                                model.Facilities.Add(facility.Facility?.Name);
                            }
                        }

                        // map image
                        var mapImage = await _houseImageRepository.FetchMapImageByHouseIdAsync(house.HouseId);
                        if (mapImage != null)
                        {
                            // prepare path
                            var imageDirectory = string.Format(_imageOptions.HouseMapImagePath, mapImage.HouseId);
                            var fullPath = string.Format("{0}{1}{2}", "/", imageDirectory, mapImage.FileName);

                            model.MapImageSrc = fullPath;
                        }

                        string[] queryParts = new string[] { house.AddressLine1, house.AddressLine2, house.City, house.PostalCode };
                        var queryString = string.Join(", ", queryParts.Where(a => !string.IsNullOrEmpty(a)));
                        var query = WebUtility.UrlEncode(queryString);
                        var url = $"https://www.google.com/maps/search/?api=1&query={query}";

                        model.GoogleMapUrl = url;

                        // add site config
                        var siteConfig = await _siteConfigRepository.GetSiteConfigAsync();
                        if (siteConfig != null)
                        {
                            if (!string.IsNullOrEmpty(siteConfig.WhatsappNumber))
                            {
                                var whatsAppNumber = siteConfig.WhatsappNumber.Replace(".", "");
                                whatsAppNumber = whatsAppNumber.Replace("-", "");
                                whatsAppNumber = whatsAppNumber.Replace(" ", "");
                                model.ContactWhatsappNumber = "1" + whatsAppNumber;
                            }

                            model.ContactEmail = siteConfig.PrimaryEmail;
                            model.ContactPhoneNumber = siteConfig.PhoneNumber;
                        }

                        success = true;
                    }
                    else
                    {
                        error = "Unable to locate house details";
                    }
                }
                else
                {
                    error = "Invalid request";
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("FrontendService/GetIndexViewModelAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return (Success: success, Error: error, Model: model);
        }
    }
}
