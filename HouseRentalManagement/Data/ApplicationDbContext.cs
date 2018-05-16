﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HouseRentalManagement.Models;

namespace HouseRentalManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<House> House { get; set; }
        public DbSet<Tenant> Tenant { get; set; }
        public DbSet<HouseImage> HouseImage { get; set; }
        public DbSet<HouseFacility> HouseFacility { get; set; }
        public DbSet<Facility> Facility { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<House>().HasKey(e => e.HouseId);
            builder.Entity<House>().HasMany<Tenant>(e=>e.Tenants);
            builder.Entity<House>().HasMany<HouseImage>(e=>e.HouseImages);
            
            builder.Entity<Tenant>().HasKey(e => e.TenantId);
            builder.Entity<Tenant>().HasOne<House>(e=>e.House);

            builder.Entity<HouseImage>().HasKey(e => e.HouseImageId);
            builder.Entity<HouseImage>().HasOne<House>(e => e.House);

            builder.Entity<HouseFacility>().HasKey(e => e.HouseFacilityId);
            builder.Entity<HouseFacility>().HasMany<Facility>();
            builder.Entity<HouseFacility>().HasMany<House>();

            builder.Entity<Facility>().HasKey(e => e.FacilityId);
        }
    }
}