﻿using HouseRentalManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Data.Interface
{
    public interface IAccessCodeRepository
    {
        Task<AccessCode> GetAccessCodeAsync();
        Task<bool> SaveAccessCodeAsync(AccessCode code);
    }
}
