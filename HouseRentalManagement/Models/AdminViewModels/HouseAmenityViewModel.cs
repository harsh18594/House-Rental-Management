﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Models.AdminViewModels
{
    public class HouseAmenityViewModel
    {
        public ICollection<AmenitiesListViewModel> Amenities { get; set; }
    }
}
