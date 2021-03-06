﻿using HouseRentalManagement.Config;
using HouseRentalManagement.Data.Interface;
using HouseRentalManagement.Models;
using HouseRentalManagement.Models.AdminViewModels;
using HouseRentalManagement.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HouseRentalManagement.Services
{
    public class HouseService : IHouseService
    {
        private readonly IHouseRepository _houseRepository;
        private readonly IFacilityRepository _facilityRepository;
        private readonly IAmenityRepository _amenityRepository;
        private readonly ImageOptions _imageOptions;
        private readonly IHostingEnvironment _env;
        private readonly IHouseImageRepository _houseImageRepository;
        private readonly IGettingAroundRepository _gettingAroundRepository;
        private readonly ILogger _logger;
        private readonly IRestrictionRepository _restrictionRepository;

        public HouseService(IHouseRepository houseRepository,
            IFacilityRepository facilityRepository,
            IAmenityRepository amenityRepository,
            IOptions<ImageOptions> imageOptions,
            IHostingEnvironment env,
            IHouseImageRepository houseImageRepository,
            ILogger<HouseService> logger,
            IGettingAroundRepository gettingAroundRepository,
            IRestrictionRepository restrictionRepository)
        {
            _houseRepository = houseRepository;
            _facilityRepository = facilityRepository;
            _amenityRepository = amenityRepository;
            _imageOptions = imageOptions.Value;
            _env = env;
            _houseImageRepository = houseImageRepository;
            _logger = logger;
            _gettingAroundRepository = gettingAroundRepository;
            _restrictionRepository = restrictionRepository;
        }

        public async Task<(bool Success, ListHouseViewModel Model)> ListHousesAsync()
        {
            bool success = false;
            var model = new ListHouseViewModel();

            try
            {
                // get all the house
                var houses = await _houseRepository.ListHousesAsync();
                if (houses != null)
                {
                    foreach (var house in houses)
                    {
                        var parts = new string[] { house.AddressLine1, house.AddressLine2 };
                        var address = string.Join(",", parts.Where(a => !string.IsNullOrEmpty(a)));
                        model.Houses.Add(new HouseViewModel()
                        {
                            HouseId = house.HouseId,
                            Address = address,
                            Rent = house.Rent,
                            IsDisplaying = house.IsDisplaying
                        });
                    }
                }

                success = true;
            }
            catch (Exception)
            {
            }

            return (Success: success, Model: model);
        }

        public async Task<(bool Success, IErrorDictionary Errors, Guid Id)> AddHouseAsync(AddHouseViewModel model)
        {
            bool success = false;
            var errors = new ErrorDictionary();
            var id = Guid.NewGuid();

            try
            {
                if (model != null)
                {
                    // prepare house
                    House house = new House()
                    {
                        AddressLine1 = model.AddressLine1,
                        AddressLine2 = model.AddressLine2,
                        City = model.City,
                        PostalCode = model.PostalCode,
                        Province = model.Province,
                        Country = model.Country,
                        CreateUtc = DateTime.UtcNow,
                        AuditUtc = DateTime.UtcNow,
                        UrlSlug = ConvertHouseAddressToUrlSlug(model.AddressLine1 + "-" + model.City + "-" + model.PostalCode)
                    };

                    // save record
                    var result = await _houseRepository.AddHouseAsync(house);
                    if (result.Success)
                    {
                        id = result.id;
                        success = true;
                    }
                    else
                    {
                        errors.AddError("", "Unexpected error occured while saving data");
                    }
                }
                else
                {
                    errors.AddError("", "Invalid data submitted");
                }
            }
            catch (Exception e)
            {

            }

            return (Success: success, Errors: errors, Id: id);
        }

        public async Task<(bool Success, IErrorDictionary Errors, EditHouseViewModel Model)> GetEditHouseViewModelAsync(Guid id)
        {
            var success = false;
            var errors = new ErrorDictionary();
            var model = new EditHouseViewModel();

            try
            {
                // fetch house by id
                if (id != Guid.Empty)
                {
                    House house = await _houseRepository.FetchHouseByIdAsync(id);
                    if (house != null)
                    {
                        // prepare viewemodel
                        model.AddressLine1 = house.AddressLine1;
                        model.AddressLine2 = house.AddressLine2;
                        model.City = house.City;
                        model.PostalCode = house.PostalCode;
                        model.Province = house.Province;
                        model.Country = house.Country;
                        model.DateAvailableFrom = house.AvailableFrom == DateTime.MinValue ? DateTime.Now : house.AvailableFrom;
                        model.ContactForAvailableFrom = house.ContactForAvailableFrom;
                        //model.DateAvailableTo = house.AvailableTo == DateTime.MinValue ? DateTime.Now.AddMonths(4) : house.AvailableTo;
                        model.Rent = house.Rent;
                        model.Description = house.Description;
                        model.HouseId = house.HouseId;
                        model.ParkingSpace = house.ParakingSpace;
                        model.Washrooms = house.Washrooms;
                        model.Occupancy = house.Occupancy;
                        model.IsDisplaying = house.IsDisplaying;
                        model.UrlSlug = house.UrlSlug;
                    }
                    else
                    {
                        errors.AddError("", "Unable to locate the house details");
                    }

                    // add image viewmodel
                    model.AddHouseImageViewModel = new AddHouseImageViewModel()
                    {
                        HouseId = id
                    };

                    // add house getting around viewmodel
                    model.AddHouseGettingAroundViewModel = new AddHouseGettingAroundViewModel() { HouseId = id };

                    // set success
                    success = true;
                }
            }
            catch (Exception)
            {
                errors.AddError("", "Unexpected error occurred while processing your request");
            }

            return (Success: success, Errors: errors, Model: model);
        }

        public async Task<(bool Success, IErrorDictionary Errors)> DeleteHouseAsync(Guid id)
        {
            bool success = false;
            var errors = new ErrorDictionary();

            try
            {
                // fetch house by id
                House house = await _houseRepository.FetchHouseByIdAsync(id);

                // remove it
                if (house != null)
                {
                    success = await _houseRepository.DeleteHouseAsync(house);

                    // delete house directory and content
                    var baseDirectory = _env.WebRootPath;
                    var imageDirectory = string.Format(_imageOptions.HouseImagePath, house.HouseId);
                    var destinationPath = Path.Combine(baseDirectory, imageDirectory);
                    if (Directory.Exists(destinationPath))
                    {
                        Directory.Delete(destinationPath, recursive: true);
                    }
                }
                else
                {
                    errors.AddError("", "Unable to locate house");
                }
            }
            catch (Exception e)
            {
                errors.AddError("", "Unexpected error occured while deleting house");
            }

            return (Success: success, Errors: errors);
        }

        public async Task<(bool Success, IErrorDictionary Errors)> EditHouseAsync(EditHouseViewModel model)
        {
            bool success = false;
            var errors = new ErrorDictionary();

            try
            {
                if (model != null)
                {
                    House house = await _houseRepository.FetchHouseByIdAsync(model.HouseId);
                    if (house != null)
                    {
                        house.AddressLine1 = model.AddressLine1;
                        house.AddressLine2 = model.AddressLine2;
                        house.City = model.City;
                        house.PostalCode = model.PostalCode;
                        house.Province = model.Province;
                        house.Country = model.Country;
                        house.AvailableFrom = model.DateAvailableFrom ?? DateTime.Now;
                        house.ContactForAvailableFrom = model.ContactForAvailableFrom;
                        //house.AvailableTo = model.DateAvailableTo ?? DateTime.Now.AddMonths(4);
                        house.Rent = model.Rent ?? 0;
                        house.Description = model.Description;
                        house.ParakingSpace = model.ParkingSpace;
                        house.AuditUtc = DateTime.UtcNow;
                        house.Washrooms = model.Washrooms;
                        house.Occupancy = model.Occupancy;
                        house.UrlSlug = ConvertHouseAddressToUrlSlug(model.AddressLine1 + "-" + model.City + "-" + model.PostalCode);
                        house.IsDisplaying = model.IsDisplaying;
                    }

                    // save record

                    if (await _houseRepository.UpdateHouseAsync(house))
                    {
                        success = true;
                    }
                    else
                    {
                        errors.AddError("", "Unexpected error occured while saving data");
                    }
                }
                else
                {
                    errors.AddError("", "Invalid data submitted");
                }
            }
            catch (Exception e)
            {
                errors.AddError("", "Unexpected error occured while saving data");
            }

            return (Success: success, Errors: errors);
        }

        public async Task<HouseAmenityViewModel> GetHouseAmenityViewModelAsync(Guid houseId)
        {
            var model = new HouseAmenityViewModel();
            try
            {
                model.HouseId = houseId;
                var amenities = await _amenityRepository.ListAmenitiesAsync();
                var houseAmenities = await _amenityRepository.ListHouseAmenitiesByHouseIdAsync(id: houseId);
                if (amenities != null)
                {
                    model.Amenities = new List<AmenitiesListViewModel>();
                    foreach (var amenity in amenities)
                    {
                        model.Amenities.Add(new AmenitiesListViewModel
                        {
                            AmenityId = amenity.AmenityId,
                            Title = amenity.Description,
                            Checked = houseAmenities != null ? houseAmenities.Where(ha => ha.AmenityId == amenity.AmenityId).Any() : false,
                            ImageSrc = string.Format(_imageOptions.AmenityImagePath, amenity.ImageFileName),
                            IncludedInUtility = houseAmenities != null
                                                ? amenity.HouseAmenities.Where(a => a.AmenityId == amenity.AmenityId).Select(a => a.IncludedInUtility).FirstOrDefault()
                                                : false
                        });
                    }
                }
            }
            catch (Exception)
            {

            }
            return model;
        }

        public async Task<(bool Success, IErrorDictionary Errors)> UpdateHouseAmenitiesAsync(HouseAmenityViewModel model)
        {
            bool success = false;
            var errors = new ErrorDictionary();

            try
            {
                if (model.Amenities != null)
                {
                    // clear exisiting amenities
                    await _amenityRepository.ClearHouseAmenitiesByHouseIdAsync(model.HouseId);

                    foreach (var item in model.Amenities)
                    {
                        if (item.Checked)
                        {
                            // prepare record
                            HouseAmenity hm = new HouseAmenity()
                            {
                                AmenityId = item.AmenityId,
                                HouseId = model.HouseId,
                                IncludedInUtility = item.IncludedInUtility
                            };

                            // save record
                            if (!await _amenityRepository.SaveHouseAmenityAsync(hm))
                            {
                                errors.AddError("", "Unable to update amenity");
                            }
                        }
                    }
                }
                else
                {
                    errors.AddError("", "Unable to process your request");
                }
                success = true;
            }
            catch (Exception e)
            {
                errors.AddError("", "Unexpected error occurred while updating amenities");
            }

            return (success, errors);
        }

        public async Task<(bool Success, string Error, bool FileExist)> UploadHouseImageAsync(AddHouseImageViewModel model)
        {
            bool success = false;
            var error = string.Empty;
            bool fileExist = false;

            try
            {
                if (model.Image != null && model.Image.Length > 0)
                {
                    // save image to directory
                    var baseDirectory = _env.WebRootPath;
                    var imageDirectory = string.Format(_imageOptions.HouseImagePath, model.HouseId);
                    var destinationPath = Path.Combine(baseDirectory, imageDirectory);
                    // make sure the folder exists
                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }

                    var fullPath = string.Format("{0}{1}", destinationPath, model.Image.FileName);

                    if (!File.Exists(fullPath) || model.Override)
                    {
                        using (FileStream fs = new FileStream(fullPath, FileMode.OpenOrCreate))
                        {
                            await model.Image.CopyToAsync(fs);
                        }

                        if (!model.Override)
                        {
                            // determine whether it should be main image or not
                            if (model.IsHomePageImage)
                            {
                                // reset house image
                                await ResetMainImageByHouseIdAsync(model.HouseId);
                            }
                            else
                            {
                                // check if house has any main image
                                var mainImage = await _houseImageRepository.GetMainImageByHouseIdAsync(model.HouseId);
                                model.IsHomePageImage = mainImage == null;
                            }

                            // save image filename to database
                            HouseImage hi = new HouseImage()
                            {
                                HouseId = model.HouseId,
                                FileName = model.Image.FileName,
                                CreateUtc = DateTime.Now,
                                IsHomePageImage = model.IsHomePageImage
                            };

                            await _houseImageRepository.SaveHouseImageAsync(hi);
                        }
                        success = true;
                    }
                    else
                    {
                        fileExist = true;
                    }
                }
            }
            catch (Exception e)
            {
                error = "Unexpected error occurred while uploading image";
            }

            return (success, error, fileExist);
        }

        public async Task<(bool Success, String Error, bool NoImage, ListHouseImageViewModel Model)> FetchHouseImageListAsync(Guid houseId)
        {
            bool success = false;
            var error = string.Empty;
            var model = new ListHouseImageViewModel()
            {
                HouseImages = new List<HouseImageViewModel>()
            };
            bool noImage = false;

            try
            {
                if (houseId != Guid.NewGuid())
                {
                    var houseImages = await _houseImageRepository.ListHouseImagesAsync(houseId);
                    if (houseImages != null && houseImages.Any())
                    {
                        foreach (var image in houseImages)
                        {
                            // prepare path
                            var imageDirectory = string.Format(_imageOptions.HouseImagePath, image.HouseId);
                            var fullPath = string.Format("{0}{1}{2}", "/", imageDirectory, image.FileName);

                            model.HouseImages.Add(new HouseImageViewModel()
                            {
                                ImageId = image.HouseImageId,
                                HouseId = image.HouseId,
                                ImageSrc = fullPath,
                                fileName = image.FileName,
                                isHomePageImage = image.IsHomePageImage ?? false
                            });
                        }

                        success = true;
                    }
                    else
                    {
                        noImage = true;
                        error = "This house doesn't have any photos, please consider uploading some photos for this house under Photos tab.";
                    }
                }
                else
                {
                    error = "Invalid house Id";
                }
            }
            catch (Exception)
            {
                error = "Unexpected error occurred while processing your request";
            }

            return (Success: success, Error: error, NoImage: noImage, Model: model);
        }

        public async Task<(bool Success, string Error)> DeleteHouseImageAsync(Guid imageId)
        {
            bool success = false;
            string error = string.Empty;

            try
            {
                // update record
                var houseImage = await _houseImageRepository.FetchHouseImageByHouseImageId(imageId);
                if (houseImage != null)
                {
                    success = await _houseImageRepository.DeleteHouseImageAsync(houseImage);

                    // if deleted image is main image, set another image as main image
                    if (success && houseImage.IsHomePageImage.HasValue && houseImage.IsHomePageImage.Value)
                    {
                        // hd.20180715 fetch first image by house id
                        var houseImages = await _houseImageRepository.ListHouseImagesAsync(houseImage.HouseId);
                        if (houseImages != null)
                        {
                            await SetHomePageImageAsync(houseId: houseImage.HouseId, imageId: houseImages.Select(i=>i.HouseImageId).FirstOrDefault());
                        }
                    }                    
                }
                else
                {
                    error = "Image not found";
                }

                // delete actual file
                // save image to directory
                var baseDirectory = _env.WebRootPath;
                var imageDirectory = string.Format(_imageOptions.HouseImagePath, houseImage.HouseId);
                var destinationPath = Path.Combine(baseDirectory, imageDirectory);
                var fullPath = string.Format("{0}{1}", destinationPath, houseImage.FileName);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                error = "Unexpected error occurred while processing your request";
                _logger.LogError("HouseService/DeleteHouseImageAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }
            return (success, error);
        }

        public async Task<(bool Success, string Error)> SetHomePageImageAsync(Guid houseId, Guid imageId)
        {
            bool success = false;
            string error = string.Empty;

            try
            {
                // reset house image
                await ResetMainImageByHouseIdAsync(houseId);

                // update record
                var houseImage = await _houseImageRepository.FetchHouseImageByHouseImageId(imageId);
                if (houseImage != null)
                {
                    houseImage.IsHomePageImage = true;
                    await _houseImageRepository.SaveHouseImageAsync(houseImage);
                    success = true;
                }
                else
                {
                    error = "Image not found";
                }
            }
            catch (Exception ex)
            {
                error = "Unexpected error occurred while processing your request";
                _logger.LogError("HouseService/SetHomePageImageAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }
            return (success, error);
        }

        private async Task ResetMainImageByHouseIdAsync(Guid houseId)
        {
            // reset homepage image for house
            var houseImages = await _houseImageRepository.ListHouseImagesAsync(houseId);
            if (houseImages != null)
            {
                foreach (var image in houseImages)
                {
                    image.IsHomePageImage = false;

                    await _houseImageRepository.SaveHouseImageAsync(image);
                }
            }
        }

        public async Task<(bool Success, String Error, ListHouseGettingAroundViewModel Model)> FetchHouseGettingAroundByHouseId(Guid houseId)
        {
            bool success = false;
            var error = string.Empty;
            var model = new ListHouseGettingAroundViewModel()
            {
                GettingAroundCollection = new List<AddHouseGettingAroundViewModel>()
            };

            try
            {
                if (houseId != Guid.NewGuid())
                {
                    var gettingArounds = await _gettingAroundRepository.ListGettingAroundByHouseIdAsync(houseId);
                    if (gettingArounds != null && gettingArounds.Any())
                    {
                        foreach (var item in gettingArounds)
                        {
                            model.GettingAroundCollection.Add(new AddHouseGettingAroundViewModel()
                            {
                                HouseId = item.HouseId,
                                GetingAroundId = item.HouseGettingAroundId,
                                WalkingTime = item.WalkingTime,
                                BikeTime = item.BikeTime,
                                CarTime = item.CarTime,
                                Distance = item.Distance,
                                Location = item.LocationName
                            });
                        }

                        success = true;
                    }
                    if (gettingArounds != null && gettingArounds.Count() == 0)
                    {
                        // set success true if no record is uploaded
                        success = true;
                    }
                }
                else
                {
                    error = "Invalid house Id";
                }
            }
            catch (Exception)
            {
                error = "Unexpected error occurred while processing your request";
            }

            return (Success: success, Error: error, Model: model);
        }

        public async Task<(bool Success, string Error)> AddHouseGettingAroundAsync(AddHouseGettingAroundViewModel model)
        {
            bool success = false;
            string error = string.Empty;

            try
            {
                HouseGettingAround hga = new HouseGettingAround()
                {
                    HouseId = model.HouseId,
                    Distance = model.Distance ?? 1,
                    BikeTime = model.BikeTime,
                    CarTime = model.CarTime,
                    WalkingTime = model.WalkingTime,
                    LocationName = model.Location,
                    Create = DateTime.UtcNow
                };

                success = await _gettingAroundRepository.SaveGettingAroundAsync(hga);
            }
            catch (Exception)
            {
                error = "Unexpected error occured while processing your request";
            }

            return (Success: success, Error: error);
        }

        public async Task<(bool Success, string Error)> DeleteHouseGettingAroundAsync(int houseGettingAroundId)
        {
            bool success = false;
            string error = string.Empty;

            try
            {
                // update record
                var houseGettingAround = await _gettingAroundRepository.FetchHouseGettingAroundByIdAsync(houseGettingAroundId);
                if (houseGettingAround != null)
                {
                    success = await _gettingAroundRepository.DeleteGettingAroundByIdAsync(houseGettingAround);
                }
                else
                {
                    error = "Info not found";
                }
            }
            catch (Exception ex)
            {
                error = "Unexpected error occurred while processing your request";
                _logger.LogError("HouseService/DeleteHouseGettingAroundAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }
            return (success, error);
        }

        private string ConvertHouseAddressToUrlSlug(string name)
        {
            var returnStr = string.Empty;

            // Ensure a value was passed in
            if (!string.IsNullOrEmpty(name))
            {
                // Convert to lowercase 
                returnStr = name.ToLower();

                // Replace any spaces with hyphens
                returnStr = returnStr.Replace(" ", "-");

                // Replace any character that is not a hyphen/letter/number with nothing
                returnStr = Regex.Replace(returnStr, "[^a-z0-9\\-]+", "");
            }

            return returnStr;
        }

        public async Task<HouseFacilityViewModel> GetHouseFacilityViewModelAsync(Guid houseId)
        {
            var model = new HouseFacilityViewModel();
            try
            {
                model.HouseId = houseId;
                var facilities = await _facilityRepository.ListFacilitiesAsync();
                var houseFacilities = await _facilityRepository.ListHouseFacilitiesByHouseIdAsync(id: houseId);
                if (facilities != null)
                {
                    model.Facilities = new List<FacilitiesListViewModel>();
                    foreach (var facility in facilities)
                    {
                        model.Facilities.Add(new FacilitiesListViewModel
                        {
                            FacilityId = facility.FacilityId,
                            Title = facility.Name,
                            Checked = houseFacilities != null ? houseFacilities.Where(ha => ha.FacilityId == facility.FacilityId).Any() : false,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("HouseService/GetHouseFacilityViewModelAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }
            return model;
        }

        public async Task<(bool Success, IErrorDictionary Errors)> UpdateHouseFacilitesAsync(HouseFacilityViewModel model)
        {
            bool success = false;
            var errors = new ErrorDictionary();

            try
            {
                if (model.Facilities != null)
                {
                    // clear exisiting facilites
                    await _facilityRepository.ClearHouseFacilitiesByHouseIdAsync(model.HouseId);

                    foreach (var item in model.Facilities)
                    {
                        if (item.Checked)
                        {
                            // prepare record
                            HouseFacility hf = new HouseFacility()
                            {
                                FacilityId = item.FacilityId,
                                HouseId = model.HouseId
                            };

                            // save record
                            if (!await _facilityRepository.SaveHouseFacilityAsync(hf))
                            {
                                errors.AddError("", "Unable to update facility");
                            }
                        }
                    }
                }
                else
                {
                    errors.AddError("", "Unable to process your request");
                }
                success = true;
            }
            catch (Exception ex)
            {
                errors.AddError("", "Unexpected error occurred while updating facilites");
                _logger.LogError("HouseService/GetHouseFacilityViewModelAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return (success, errors);
        }

        public async Task<HouseRestrictionViewModel> GetHouseRestrictionViewModelAsync(Guid houseId)
        {
            var model = new HouseRestrictionViewModel();
            try
            {
                model.HouseId = houseId;
                var restrictions = await _restrictionRepository.ListRestrictionsAsync();
                var houseRestrictions = await _restrictionRepository.ListHouseRestrictionsByHouseIdAsync(id: houseId);
                if (restrictions != null)
                {
                    model.Restrictions = new List<RestrictionsListViewModel>();
                    foreach (var facility in restrictions)
                    {
                        model.Restrictions.Add(new RestrictionsListViewModel
                        {
                            RestrictionId = facility.RestrictionId,
                            Title = facility.Title,
                            Checked = houseRestrictions != null ? houseRestrictions.Where(ha => ha.RestrictionId == facility.RestrictionId).Any() : false,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("HouseService/GetHouseRestrictionViewModelAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }
            return model;
        }

        public async Task<(bool Success, IErrorDictionary Errors)> UpdateHouseRestrictionsAsync(HouseRestrictionViewModel model)
        {
            bool success = false;
            var errors = new ErrorDictionary();

            try
            {
                if (model.Restrictions != null)
                {
                    // clear exisiting facilites
                    await _restrictionRepository.ClearHouseRestrictionsByHouseIdAsync(model.HouseId);

                    foreach (var item in model.Restrictions)
                    {
                        if (item.Checked)
                        {
                            // prepare record
                            HouseRestriction hr = new HouseRestriction()
                            {
                                RestrictionId = item.RestrictionId,
                                HouseId = model.HouseId
                            };

                            // save record
                            if (!await _restrictionRepository.SaveHouseRestrictionAsync(hr))
                            {
                                errors.AddError("", "Unable to update restrictions");
                            }
                        }
                    }
                }
                else
                {
                    errors.AddError("", "Unable to process your request");
                }
                success = true;
            }
            catch (Exception ex)
            {
                errors.AddError("", "Unexpected error occurred while updating restrictions.");
                _logger.LogError("HouseService/UpdateHouseRestrictionsAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return (success, errors);
        }

        public async Task<(bool Success, string Error)> UploadHouseMapImageAsync(AddHouseMapImageViewModel model)
        {
            bool success = false;
            var error = string.Empty;

            try
            {
                if (model.Image != null && model.Image.Length > 0 && model.HouseId != Guid.Empty)
                {
                    // save image filename to database
                    HouseMapImage image = await _houseImageRepository.FetchMapImageByHouseIdAsync(model.HouseId);

                    if (image == null)
                    {
                        image = new HouseMapImage()
                        {
                            CreateUtc = DateTime.Now
                        };
                    }

                    // delete exisiting file
                    var baseDirectory = _env.WebRootPath;
                    var imageDirectory = string.Format(_imageOptions.HouseMapImagePath, model.HouseId);
                    var destinationPath = Path.Combine(baseDirectory, imageDirectory);
                    var fullPath = string.Format("{0}{1}", destinationPath, image.FileName);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }

                    image.HouseId = model.HouseId;
                    image.FileName = model.Image.FileName;

                    // save image to directory
                    baseDirectory = _env.WebRootPath;
                    imageDirectory = string.Format(_imageOptions.HouseMapImagePath, model.HouseId);
                    destinationPath = Path.Combine(baseDirectory, imageDirectory);
                    // make sure the folder exists
                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }

                    fullPath = string.Format("{0}{1}", destinationPath, model.Image.FileName);

                    using (FileStream fs = new FileStream(fullPath, FileMode.OpenOrCreate))
                    {
                        await model.Image.CopyToAsync(fs);
                    }

                    await _houseImageRepository.SaveHouseMapImageAsync(image);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                error = "Unexpected error occurred while uploading image";
                _logger.LogError("HouseService/UploadHouseMapImageAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return (success, error);
        }

        public async Task<(bool Success, string Error, string ImageUrl, bool NoImage, Guid ImageId)> FetchHouseMapImageAsync(Guid houseId)
        {
            bool success = false;
            string error = string.Empty;
            string imageUrl = string.Empty;
            bool noImage = false;
            Guid imageId = Guid.Empty;

            try
            {
                if (houseId != Guid.Empty)
                {
                    HouseMapImage image = await _houseImageRepository.FetchMapImageByHouseIdAsync(houseId);
                    if (image != null)
                    {
                        // prepare path
                        var imageDirectory = string.Format(_imageOptions.HouseMapImagePath, image.HouseId);
                        var fullPath = string.Format("{0}{1}{2}", "/", imageDirectory, image.FileName);

                        imageUrl = fullPath;
                        imageId = image.HouseMapImageId;
                        success = true;
                    }
                    else
                    {
                        success = false;
                        noImage = true;
                    }
                }
                else
                {
                    error = "Invalid house id";
                }
            }
            catch (Exception ex)
            {
                error = "Unexpected error occurred while fetching image";

                _logger.LogError("HouseService/FetchHouseMapImageAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return (success, error, imageUrl, noImage, imageId);
        }

        public async Task<(bool Success, string Error)> DeleteHouseMapImageAsync(Guid imageId)
        {
            bool success = false;
            string error = string.Empty;

            try
            {
                if (imageId != Guid.Empty)
                {
                    HouseMapImage image = await _houseImageRepository.FetchMapImageByImageIdAsync(imageId);
                    if (image != null)
                    {
                        // delete exisiting file
                        var baseDirectory = _env.WebRootPath;
                        var imageDirectory = string.Format(_imageOptions.HouseMapImagePath, image.HouseId);
                        var destinationPath = Path.Combine(baseDirectory, imageDirectory);
                        var fullPath = string.Format("{0}{1}", destinationPath, image.FileName);
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                        }

                        success = await _houseImageRepository.DeleteHouseMapImageAsync(image);
                    }
                    else
                    {
                        error = "Image not found";
                    }

                }
                else
                {
                    error = "Invalid image id";
                }
            }
            catch (Exception ex)
            {
                error = "Unexpected error occurred while deleting image";

                _logger.LogError("HouseService/DeleteHouseMapImageAsync - exception:{@Ex}", new object[]{
                    ex
                });
            }

            return (success, error);
        }
    }
}
