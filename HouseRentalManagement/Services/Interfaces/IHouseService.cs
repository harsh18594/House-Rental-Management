﻿using HouseRentalManagement.Models.AdminViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Services.Interfaces
{
    public interface IHouseService
    {
        Task<(bool Success, ListHouseViewModel Model)> ListHousesAsync();
        Task<(bool Success, IErrorDictionary Errors, Guid Id)> AddHouseAsync(AddHouseViewModel model);
        Task<(bool Success, IErrorDictionary Errors, EditHouseViewModel Model)> GetEditHouseViewModelAsync(Guid id);
        Task<(bool Success, IErrorDictionary Errors)> DeleteHouseAsync(Guid id);
        Task<(bool Success, IErrorDictionary Errors)> EditHouseAsync(EditHouseViewModel model);
        Task<HouseAmenityViewModel> GetHouseAmenityViewModelAsync(Guid houseId);
        Task<(bool Success, IErrorDictionary Errors)> UpdateHouseAmenitiesAsync(HouseAmenityViewModel model);
        Task<(bool Success, string Error, bool FileExist)> UploadHouseImageAsync(AddHouseImageViewModel model);
        Task<(bool Success, string Error, bool NoImage, ListHouseImageViewModel Model)> FetchHouseImageListAsync(Guid houseId);
        Task<(bool Success, string Error)> DeleteHouseImageAsync(Guid imageId);
        Task<(bool Success, string Error)> SetHomePageImageAsync(Guid houseId, Guid imageId);
        Task<(bool Success, String Error, ListHouseGettingAroundViewModel Model)> FetchHouseGettingAroundByHouseId(Guid houseId);
        Task<(bool Success, string Error)> AddHouseGettingAroundAsync(AddHouseGettingAroundViewModel model);
        Task<(bool Success, string Error)> DeleteHouseGettingAroundAsync(int houseGettingAroundId);
        Task<HouseFacilityViewModel> GetHouseFacilityViewModelAsync(Guid houseId);
        Task<(bool Success, IErrorDictionary Errors)> UpdateHouseFacilitesAsync(HouseFacilityViewModel model);
        Task<HouseRestrictionViewModel> GetHouseRestrictionViewModelAsync(Guid houseId);
        Task<(bool Success, IErrorDictionary Errors)> UpdateHouseRestrictionsAsync(HouseRestrictionViewModel model);
        Task<(bool Success, string Error)> UploadHouseMapImageAsync(AddHouseMapImageViewModel model);
        Task<(bool Success, string Error, string ImageUrl, bool NoImage, Guid ImageId)> FetchHouseMapImageAsync(Guid houseId);
        Task<(bool Success, string Error)> DeleteHouseMapImageAsync(Guid imageId);
    }
}
