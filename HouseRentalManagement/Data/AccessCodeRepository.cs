﻿using HouseRentalManagement.Data.Interface;
using HouseRentalManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HouseRentalManagement.Data
{
    public class AccessCodeRepository : IAccessCodeRepository
    {
        private readonly ApplicationDbContext _context;
        public AccessCodeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccessCode> GetAccessCodeAsync()
        {
            return await _context.AccessCode.FirstOrDefaultAsync();
        }

        public async Task<bool> SaveAccessCodeAsync(AccessCode code)
        {
            if (_context.Entry(code).State == EntityState.Detached)
            {
                _context.Add(code);                
            }

            // if access code is not modified, consider it as true
            if (_context.Entry(code).State == EntityState.Unchanged)
            {
                return true;
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
