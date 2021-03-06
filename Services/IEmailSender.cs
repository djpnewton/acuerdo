﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace viafront3.Services
{
    public interface IEmailSender
    {
        String SiteName { get; }
        Task SendEmailAsync(string email, string subject, string message);
    }
}
