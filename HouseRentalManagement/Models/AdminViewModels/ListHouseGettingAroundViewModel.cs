﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Models.AdminViewModels
{
    public class ListHouseGettingAroundViewModel
    {
        public ICollection<AddHouseGettingAroundViewModel> GettingAroundCollection { get; set; } = new HashSet<AddHouseGettingAroundViewModel>();
    }
}
