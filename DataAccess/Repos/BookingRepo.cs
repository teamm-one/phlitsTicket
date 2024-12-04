﻿using DataAccess.Data;
using DataAccess.IRepos;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repos
{
    internal class BookingRepo:BaseRepo<Booking>,BookingIRepo
    {
        ApplicationDbContext _context;

        public BookingRepo(ApplicationDbContext context):base(context)
        {
            _context = context;
        }
    }
}
