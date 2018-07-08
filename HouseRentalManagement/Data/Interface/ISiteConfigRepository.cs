﻿using HouseRentalManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Data.Interface
{
    public interface ISiteConfigRepository
    {
        Task<SiteConfig> GetSiteConfigAsync();
        Task<bool> SaveSiteConfigAsync(SiteConfig record);
    }
}
