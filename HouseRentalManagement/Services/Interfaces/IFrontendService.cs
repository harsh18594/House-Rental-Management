﻿using HouseRentalManagement.Models.HouseViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Services.Interfaces
{
    public interface IFrontendService
    {
        Task<IndexViewModel> GetIndexViewModelAsync();
        Task<(bool Success, string Error, HouseInfoViewModel Model)> GetHouseInfoViewModelAsync(string slug, Guid houseId, bool preview = false);
    }
}
